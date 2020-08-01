Imports Newtonsoft.Json.Linq
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Configuration
Imports System.Data
Imports System.Data.Common
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Transactions
Imports System.Web
Imports System.Web.Caching
Imports System.Web.Configuration
Imports System.Web.Security
Imports System.Xml
Imports System.Xml.XPath
Imports System.Xml.Xsl

Namespace Norwindt.Data
    
    Partial Public Class DataControllerBase
        
        Private m_ViewFilter As String
        
        Private m_Parameters As BusinessObjectParameters
        
        Private m_HasWhere As Boolean
        
        Private m_CurrentCommand As DbCommand
        
        Private m_CurrentExpressions As SelectClauseDictionary
        
        Public Shared FilterExpressionRegex As Regex = New Regex("(?'Alias'[\w\,\.]+):(?'Values'[\s\S]*)")
        
        Public Shared MatchingModeRegex As Regex = New Regex("^(?'Match'_match_|_donotmatch_)\:(?'Scope'\$all\$|\$any\$)$")
        
        Public Shared FilterValueRegex As Regex = New Regex("(?'Operation'\*|\$\w+\$|=|~|<(=|>){0,1}|>={0,1})(?'Value'[\s\S]*?)(\0|$)")
        
        Public Shared NegativeFilterOperations() As String = New String() {"$notin$", "$doesnotequal$", "<>", "$doesnotbegingwith$>", "$doesnotcontain$>", "$doesnotendwith$>"}
        
        Public Shared ReverseNegativeFilterOperations() As String = New String() {"$in$", "$equals$", "=", "$beginswith$>", "$contains$>", "$endswith$>"}
        
        Private Sub AppendWhereExpressions(ByVal sb As StringBuilder, ByVal command As DbCommand, ByVal page As ViewPage, ByVal expressions As SelectClauseDictionary, ByVal values() As FieldValue)
            Dim surrogatePK() As String = Nothing
            For Each fvo in values
                If (fvo.Name = "_SurrogatePK") Then
                    If TypeOf fvo.Value Is JArray Then
                        surrogatePK = CType(fvo.Value,JArray).ToObject(Of String())()
                    Else
                        surrogatePK = CType(fvo.Value,String())
                    End If
                    Exit For
                End If
            Next
            sb.AppendLine()
            sb.Append("where")
            Dim firstField = true
            For Each v in values
                Dim field = page.FindField(v.Name)
                If ((Not (field) Is Nothing) AndAlso ((field.IsPrimaryKey AndAlso (surrogatePK Is Nothing)) OrElse ((Not (surrogatePK) Is Nothing) AndAlso surrogatePK.Contains(v.Name)))) Then
                    sb.AppendLine()
                    If firstField Then
                        firstField = false
                    Else
                        sb.Append("and ")
                    End If
                    sb.AppendFormat(RemoveTableAliasFromExpression(expressions(v.Name)))
                    sb.AppendFormat("={0}p{1}", m_ParameterMarker, command.Parameters.Count)
                    Dim parameter = command.CreateParameter()
                    parameter.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                    AssignParameterValue(parameter, field.Type, v.OldValue)
                    command.Parameters.Add(parameter)
                End If
            Next
            If (m_Config.ConflictDetectionEnabled AndAlso (surrogatePK Is Nothing)) Then
                For Each v in values
                    Dim field = page.FindField(v.Name)
                    If ((Not (field) Is Nothing) AndAlso (Not ((field.IsPrimaryKey OrElse field.OnDemand)) AndAlso Not (v.ReadOnly))) Then
                        sb.AppendLine()
                        If firstField Then
                            firstField = false
                        Else
                            sb.Append("and ")
                        End If
                        sb.Append(RemoveTableAliasFromExpression(expressions(v.Name)))
                        If (v.OldValue Is Nothing) Then
                            sb.Append(" is null")
                        Else
                            sb.AppendFormat("={0}p{1}", m_ParameterMarker, command.Parameters.Count)
                            Dim parameter = command.CreateParameter()
                            parameter.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                            AssignParameterValue(parameter, field.Type, v.OldValue)
                            command.Parameters.Add(parameter)
                        End If
                    End If
                Next
            End If
            sb.AppendLine()
        End Sub
        
        Private Sub EnsureWhereKeyword(ByVal sb As StringBuilder)
            If Not (m_HasWhere) Then
                m_HasWhere = true
                sb.AppendLine("where")
            End If
        End Sub
        
        Private Function ProcessViewFilter(ByVal page As ViewPage, ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary) As String
            m_CurrentCommand = command
            m_CurrentExpressions = expressions
            Dim filter = Regex.Replace(m_ViewFilter, "/\*Sql\*/(?'Sql'[\s\S]+)/\*Sql\*/|(?'Parameter'(@|:)\w+)|(?'Other'(""|'|\[|`)\s*\w"& _ 
                    "+)|(?'Function'\$\w+\s*\((?'Arguments'[\S\s]*?)\))|(?'Name'\w+)", AddressOf DoReplaceKnownNames)
            Return filter
        End Function
        
        Private Function DoReplaceKnownNames(ByVal m As Match) As String
            Dim sql = m.Groups("Sql").Value
            If Not (String.IsNullOrEmpty(sql)) Then
                Return sql
            End If
            If Not (String.IsNullOrEmpty(m.Groups("Other").Value)) Then
                Return m.Value
            End If
            If Not (String.IsNullOrEmpty(m.Groups("Parameter").Value)) Then
                Return AssignFilterParameterValue(m.Groups("Parameter").Value)
            Else
                If Not (String.IsNullOrEmpty(m.Groups("Function").Value)) Then
                    Return FilterFunctions.Replace(m_CurrentCommand, m_CurrentExpressions, m.Groups("Function").Value)
                Else
                    Dim s As String = Nothing
                    If m_CurrentExpressions.TryGetValue(m.Groups("Name").Value, s) Then
                        Return s
                    End If
                End If
            End If
            Return m.Value
        End Function
        
        Private Function AssignFilterParameterValue(ByVal qualifiedName As String) As String
            Dim prefix = qualifiedName(0)
            Dim name = qualifiedName.Substring(1)
            If ((prefix.Equals(Global.Microsoft.VisualBasic.ChrW(64)) OrElse prefix.Equals(Global.Microsoft.VisualBasic.ChrW(58))) AndAlso Not (m_CurrentCommand.Parameters.Contains(qualifiedName))) Then
                Dim result As Object = Nothing
                If ((Not (m_Parameters) Is Nothing) AndAlso m_Parameters.ContainsKey(qualifiedName)) Then
                    result = m_Parameters(qualifiedName)
                Else
                    Dim rules = m_ServerRules
                    If (rules Is Nothing) Then
                        rules = CreateBusinessRules()
                    End If
                    result = rules.GetProperty(name)
                End If
                Dim enumerable As IEnumerable(Of Object) = Nothing
                If GetType(IEnumerable(Of Object)).IsInstanceOfType(result) Then
                    enumerable = CType(result,IEnumerable(Of Object))
                End If
                If (Not (enumerable) Is Nothing) Then
                    Dim sb = New StringBuilder()
                    sb.Append("(")
                    Dim parameterIndex = 0
                    For Each o in enumerable
                        Dim p = m_CurrentCommand.CreateParameter()
                        m_CurrentCommand.Parameters.Add(p)
                        p.ParameterName = (qualifiedName + parameterIndex.ToString())
                        p.Value = o
                        If (parameterIndex > 0) Then
                            sb.Append(",")
                        End If
                        sb.Append(p.ParameterName)
                        parameterIndex = (parameterIndex + 1)
                    Next
                    sb.Append(")")
                    Return sb.ToString()
                Else
                    Dim p = m_CurrentCommand.CreateParameter()
                    m_CurrentCommand.Parameters.Add(p)
                    p.ParameterName = qualifiedName
                    If (result Is Nothing) Then
                        result = DBNull.Value
                    End If
                    p.Value = result
                End If
            End If
            Return qualifiedName
        End Function
        
        Protected Overridable Sub AppendFilterExpressionsToWhere(ByVal sb As StringBuilder, ByVal page As ViewPage, ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal whereClause As String)
            expressions.TrackAliases = true
            Dim quickFindHint = page.QuickFindHint
            Dim firstCriteria = String.IsNullOrEmpty(m_ViewFilter)
            If Not (firstCriteria) Then
                EnsureWhereKeyword(sb)
                sb.AppendLine("(")
                sb.Append(ProcessViewFilter(page, command, expressions))
            End If
            Dim matchListCount = 0
            Dim firstDoNotMatch = true
            Dim logicalConcat = "and "
            Dim useExclusiveQuickFind = false
            For Each f in page.Fields
                If (Not (String.IsNullOrEmpty(f.SearchOptions)) AndAlso Regex.IsMatch(f.SearchOptions, "\$quickfind(?!disabled)")) Then
                    useExclusiveQuickFind = true
                    Exit For
                End If
            Next
            If (Not (page.Filter) Is Nothing) Then
                For Each filterExpression in page.Filter
                    Dim matchingMode = MatchingModeRegex.Match(filterExpression)
                    If matchingMode.Success Then
                        Dim doNotMatch = (matchingMode.Groups("Match").Value = "_donotmatch_")
                        If doNotMatch Then
                            If firstDoNotMatch Then
                                firstDoNotMatch = false
                                EnsureWhereKeyword(sb)
                                If Not (firstCriteria) Then
                                    sb.AppendLine(")")
                                End If
                                If (matchListCount > 0) Then
                                    sb.AppendLine(")")
                                End If
                                If (Not (firstCriteria) OrElse (matchListCount > 0)) Then
                                    sb.AppendLine("and")
                                End If
                                matchListCount = 0
                                sb.AppendLine(" not")
                                firstCriteria = true
                            End If
                        End If
                        If (matchListCount = 0) Then
                            EnsureWhereKeyword(sb)
                            If Not (firstCriteria) Then
                                sb.Append(") and")
                            End If
                            'the list of matches begins
                            sb.AppendLine("(")
                        Else
                            sb.AppendLine(")")
                            sb.AppendLine("or")
                        End If
                        'begin a list of conditions for the next match
                        If (matchingMode.Groups("Scope").Value = "$all$") Then
                            logicalConcat = " and "
                        Else
                            logicalConcat = " or "
                        End If
                        matchListCount = (matchListCount + 1)
                        firstCriteria = true
                    End If
                    Dim filterMatch = FilterExpressionRegex.Match(filterExpression)
                    If filterMatch.Success Then
                        '"ProductName:?g", "CategoryCategoryName:=Condiments\x00=Seafood"
                        Dim firstValue = true
                        Dim fieldOperator = " or "
                        If Regex.IsMatch(filterMatch.Groups("Values").Value, ">|<") Then
                            fieldOperator = " and "
                        End If
                        Dim valueMatch = FilterValueRegex.Match(filterMatch.Groups("Values").Value)
                        Do While valueMatch.Success
                            Dim fieldAlias = filterMatch.Groups("Alias").Value
                            Dim operation = valueMatch.Groups("Operation").Value
                            Dim paramValue = valueMatch.Groups("Value").Value
                            If ((operation = "~") AndAlso (fieldAlias = "_quickfind_")) Then
                                fieldAlias = page.Fields(0).Name
                            End If
                            Dim deepSearching = fieldAlias.Contains(",")
                            Dim field = page.FindField(fieldAlias)
                            If (((((Not (field) Is Nothing) AndAlso field.AllowQBE) OrElse (operation = "~")) AndAlso (((Not ((page.DistinctValueFieldName = field.Name)) OrElse (matchListCount > 0)) OrElse (operation = "~")) OrElse (page.AllowDistinctFieldInFilter OrElse page.CustomFilteredBy(field.Name)))) OrElse deepSearching) Then
                                If firstValue Then
                                    If firstCriteria Then
                                        EnsureWhereKeyword(sb)
                                        sb.AppendLine("(")
                                        firstCriteria = false
                                    Else
                                        sb.Append(logicalConcat)
                                    End If
                                    sb.Append("(")
                                    firstValue = false
                                Else
                                    sb.Append(fieldOperator)
                                End If
                                If deepSearching Then
                                    Dim deepSearchFieldName = fieldAlias.Substring(0, fieldAlias.IndexOf(Global.Microsoft.VisualBasic.ChrW(44)))
                                    Dim hint = fieldAlias.Substring((deepSearchFieldName.Length + 1))
                                    Dim deepFilterExpression = (deepSearchFieldName + filterExpression.Substring(filterExpression.IndexOf(Global.Microsoft.VisualBasic.ChrW(58))))
                                    AppendDeepFilter(hint, page, command, sb, deepFilterExpression)
                                Else
                                    If (operation = "~") Then
                                        paramValue = Convert.ToString(StringToValue(paramValue))
                                        Dim words = New List(Of String)()
                                        Dim phrases = New List(Of List(Of String))()
                                        phrases.Add(words)
                                        Dim currentCulture = CultureInfo.CurrentCulture
                                        Dim textDateNumber = ("\p{L}\d" + Regex.Escape((currentCulture.DateTimeFormat.DateSeparator  _
                                                        + (currentCulture.DateTimeFormat.TimeSeparator + currentCulture.NumberFormat.NumberDecimalSeparator))))
                                        Dim removableNumericCharacters = New String() {currentCulture.NumberFormat.NumberGroupSeparator, currentCulture.NumberFormat.CurrencyGroupSeparator, currentCulture.NumberFormat.CurrencySymbol}
                                        Dim m = Regex.Match(paramValue, String.Format("\s*(?'Token'((?'Quote'"")(?'Value'.+?)"")|((?'Quote'\')(?'Value'.+?)\')|(,|;|(^|\s+"& _ 
                                                    ")-)|(?'Value'[{0}]+))", textDateNumber))
                                        Dim negativeSample = false
                                        Do While m.Success
                                            Dim token = m.Groups("Token").Value.Trim()
                                            If ((token = ",") OrElse (token = ";")) Then
                                                words = New List(Of String)()
                                                phrases.Add(words)
                                                negativeSample = false
                                            Else
                                                If (token = "-") Then
                                                    negativeSample = true
                                                Else
                                                    Dim exactFlag = "="
                                                    If String.IsNullOrEmpty(m.Groups("Quote").Value) Then
                                                        exactFlag = " "
                                                    End If
                                                    Dim negativeFlag = " "
                                                    If negativeSample Then
                                                        negativeFlag = "-"
                                                        negativeSample = false
                                                    End If
                                                    words.Add(String.Format("{0}{1}{2}", negativeFlag, exactFlag, m.Groups("Value").Value))
                                                End If
                                            End If
                                            m = m.NextMatch()
                                        Loop
                                        Dim firstPhrase = true
                                        For Each phrase in phrases
                                            If (phrase.Count > 0) Then
                                                If firstPhrase Then
                                                    firstPhrase = false
                                                Else
                                                    sb.AppendLine("or")
                                                End If
                                                sb.AppendLine("(")
                                                Dim firstWord = true
                                                Dim paramValueAsDate As Date
                                                For Each paramValueWord in phrase
                                                    Dim negativeFlag = (paramValueWord(0) = Global.Microsoft.VisualBasic.ChrW(45))
                                                    Dim exactFlag = (paramValueWord(1) = Global.Microsoft.VisualBasic.ChrW(61))
                                                    Dim comparisonOperator = "like"
                                                    If exactFlag Then
                                                        comparisonOperator = "="
                                                    End If
                                                    Dim pv = paramValueWord.Substring(2)
                                                    Dim fieldNameFilter As String = Nothing
                                                    Dim complexParam = Regex.Match(pv, "^(.+)\:(.+)$")
                                                    If complexParam.Success Then
                                                        fieldNameFilter = complexParam.Groups(1).Value
                                                        Dim fieldIsMatched = false
                                                        For Each tf in page.Fields
                                                            If ((tf.AllowQBE AndAlso String.IsNullOrEmpty(tf.AliasName)) AndAlso (Not ((tf.IsPrimaryKey AndAlso tf.Hidden)) AndAlso tf.IsMatchedByName(fieldNameFilter))) Then
                                                                fieldIsMatched = true
                                                                Exit For
                                                            End If
                                                        Next
                                                        If fieldIsMatched Then
                                                            pv = complexParam.Groups(2).Value
                                                        Else
                                                            fieldNameFilter = Nothing
                                                        End If
                                                    End If
                                                    Dim paramValueIsDate = SqlStatement.TryParseDate(command.GetType(), pv, paramValueAsDate)
                                                    Dim firstTry = true
                                                    Dim parameter As DbParameter = Nothing
                                                    If Not (paramValueIsDate) Then
                                                        pv = SqlStatement.EscapePattern(command, pv)
                                                    End If
                                                    Dim paramValueAsNumber As Double
                                                    Dim testNumber = pv
                                                    For Each s in removableNumericCharacters
                                                        testNumber = testNumber.Replace(s, String.Empty)
                                                    Next
                                                    Dim paramValueIsNumber = Double.TryParse(testNumber, paramValueAsNumber)
                                                    If (Not (exactFlag) AndAlso Not (pv.Contains("%"))) Then
                                                        pv = String.Format("%{0}%", pv)
                                                    End If
                                                    If firstWord Then
                                                        firstWord = false
                                                    Else
                                                        sb.Append("and")
                                                    End If
                                                    If negativeFlag Then
                                                        sb.Append(" not")
                                                    End If
                                                    sb.Append("(")
                                                    Dim hasTests = false
                                                    Dim originalParameter As DbParameter = Nothing
                                                    If (String.IsNullOrEmpty(quickFindHint) OrElse Not (quickFindHint.StartsWith(";"))) Then
                                                        For Each tf in page.Fields
                                                            If ((tf.AllowQBE AndAlso String.IsNullOrEmpty(tf.AliasName)) AndAlso (Not ((tf.IsPrimaryKey AndAlso tf.Hidden)) AndAlso (Not (tf.Type.StartsWith("Date")) OrElse paramValueIsDate))) Then
                                                                If (String.IsNullOrEmpty(fieldNameFilter) OrElse tf.IsMatchedByName(fieldNameFilter)) Then
                                                                    If ((Not (useExclusiveQuickFind) AndAlso (String.IsNullOrEmpty(tf.SearchOptions) OrElse Not (tf.SearchOptions.Contains("$quickfinddisabled")))) OrElse (useExclusiveQuickFind AndAlso (Not (String.IsNullOrEmpty(tf.SearchOptions)) AndAlso tf.SearchOptions.Contains("$quickfind")))) Then
                                                                        hasTests = true
                                                                        If ((parameter Is Nothing) OrElse command.GetType().FullName.Contains("ManagedDataAccess")) Then
                                                                            parameter = command.CreateParameter()
                                                                            parameter.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                                                                            parameter.DbType = DbType.String
                                                                            command.Parameters.Add(parameter)
                                                                            parameter.Value = pv
                                                                            If (exactFlag AndAlso paramValueIsNumber) Then
                                                                                parameter.DbType = DbType.Double
                                                                                parameter.Value = paramValueAsNumber
                                                                            End If
                                                                        End If
                                                                        If Not ((exactFlag AndAlso ((Not (tf.Type.Contains("String")) AndAlso Not (paramValueIsNumber)) OrElse (tf.Type.Contains("String") AndAlso paramValueIsNumber)))) Then
                                                                            If firstTry Then
                                                                                firstTry = false
                                                                            Else
                                                                                sb.Append(" or ")
                                                                            End If
                                                                            If tf.Type.StartsWith("Date") Then
                                                                                Dim dateParameter = command.CreateParameter()
                                                                                dateParameter.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                                                                                dateParameter.DbType = DbType.DateTime
                                                                                command.Parameters.Add(dateParameter)
                                                                                dateParameter.Value = paramValueAsDate
                                                                                If negativeFlag Then
                                                                                    sb.AppendFormat("({0} is not null)and", expressions(tf.ExpressionName()))
                                                                                End If
                                                                                sb.AppendFormat("({0} = {1})", expressions(tf.ExpressionName()), dateParameter.ParameterName)
                                                                            Else
                                                                                Dim skipLike = false
                                                                                If (Not ((comparisonOperator = "=")) AndAlso ((tf.Type = "String") AndAlso ((tf.Len > 0) AndAlso (tf.Len < pv.Length)))) Then
                                                                                    Dim pv2 = pv
                                                                                    pv2 = pv2.Substring(1)
                                                                                    If (tf.Len < pv2.Length) Then
                                                                                        pv2 = pv2.Substring(0, (pv2.Length - 1))
                                                                                    End If
                                                                                    If (pv2.Length > tf.Len) Then
                                                                                        skipLike = true
                                                                                    Else
                                                                                        originalParameter = parameter
                                                                                        parameter = command.CreateParameter()
                                                                                        parameter.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                                                                                        parameter.DbType = DbType.String
                                                                                        command.Parameters.Add(parameter)
                                                                                        parameter.Value = pv2
                                                                                    End If
                                                                                End If
                                                                                If m_ServerRules.EnableResultSet Then
                                                                                    Dim fieldNameExpression = expressions(tf.ExpressionName())
                                                                                    If ((Not ((tf.Type = "String")) AndAlso Not (exactFlag)) OrElse (tf.Type = "Boolean")) Then
                                                                                        fieldNameExpression = String.Format("convert({0}, 'System.String')", fieldNameExpression)
                                                                                    End If
                                                                                    If negativeFlag Then
                                                                                        sb.AppendFormat("({0} is not null)and", fieldNameExpression)
                                                                                    End If
                                                                                    sb.AppendFormat("({0} {2} {1})", fieldNameExpression, parameter.ParameterName, comparisonOperator)
                                                                                Else
                                                                                    If skipLike Then
                                                                                        sb.Append("1=0")
                                                                                    Else
                                                                                        If negativeFlag Then
                                                                                            sb.AppendFormat("({0} is not null)and", expressions(tf.ExpressionName()))
                                                                                        End If
                                                                                        If DatabaseEngineIs(command, "Oracle", "DB2", "Firebird") Then
                                                                                            sb.AppendFormat("(upper({0}) {2} {1})", expressions(tf.ExpressionName()), parameter.ParameterName, comparisonOperator)
                                                                                            parameter.Value = Convert.ToString(parameter.Value).ToUpper()
                                                                                        Else
                                                                                            sb.AppendFormat("({0} {2} {1})", expressions(tf.ExpressionName()), parameter.ParameterName, comparisonOperator)
                                                                                        End If
                                                                                    End If
                                                                                End If
                                                                            End If
                                                                        End If
                                                                        If (Not (originalParameter) Is Nothing) Then
                                                                            parameter = originalParameter
                                                                            originalParameter = Nothing
                                                                        End If
                                                                    End If
                                                                End If
                                                            End If
                                                        Next
                                                    End If
                                                    If (Not (String.IsNullOrEmpty(quickFindHint)) AndAlso quickFindHint.Contains(";")) Then
                                                        sb.AppendLine()
                                                        If hasTests Then
                                                            sb.AppendLine("or")
                                                        Else
                                                            hasTests = true
                                                        End If
                                                        sb.AppendLine("(")
                                                        Dim firstHint = true
                                                        For Each hint in quickFindHint.Substring((quickFindHint.IndexOf(Global.Microsoft.VisualBasic.ChrW(59)) + 1)).Split(New Char() {Global.Microsoft.VisualBasic.ChrW(59)})
                                                            If firstHint Then
                                                                firstHint = false
                                                            Else
                                                                sb.AppendLine()
                                                                sb.AppendLine("or")
                                                            End If
                                                            sb.AppendLine("(")
                                                            Dim newFilterExpression = filterExpression
                                                            Dim reversedFilterExpression = New StringBuilder()
                                                            If negativeFlag Then
                                                                Dim firstExpressionPhrase = true
                                                                For Each ph in phrases
                                                                    If firstExpressionPhrase Then
                                                                        firstExpressionPhrase = false
                                                                    Else
                                                                        reversedFilterExpression.Append(",")
                                                                    End If
                                                                    Dim firstExpressionWord = true
                                                                    For Each w in ph
                                                                        If firstExpressionWord Then
                                                                            firstExpressionWord = false
                                                                        Else
                                                                            reversedFilterExpression.Append(" ")
                                                                        End If
                                                                        If Not ((w(0) = Global.Microsoft.VisualBasic.ChrW(45))) Then
                                                                            reversedFilterExpression.Append("-")
                                                                        End If
                                                                        If (w(1) = Global.Microsoft.VisualBasic.ChrW(61)) Then
                                                                            reversedFilterExpression.Append("""")
                                                                        End If
                                                                        reversedFilterExpression.Append(w.Substring(2))
                                                                        If (w(1) = Global.Microsoft.VisualBasic.ChrW(61)) Then
                                                                            reversedFilterExpression.Append("""")
                                                                        End If
                                                                    Next
                                                                Next
                                                                newFilterExpression = ("_quickfind_:~" + ValueToString(reversedFilterExpression.ToString()))
                                                            End If
                                                            AppendDeepFilter(hint, page, command, sb, newFilterExpression)
                                                            sb.AppendLine(")")
                                                        Next
                                                        sb.AppendLine(")")
                                                    End If
                                                    If Not (hasTests) Then
                                                        If (negativeFlag AndAlso quickFindHint.StartsWith(";")) Then
                                                            sb.Append("1=1")
                                                        Else
                                                            sb.Append("1=0")
                                                        End If
                                                    End If
                                                    sb.Append(")")
                                                Next
                                                sb.AppendLine(")")
                                            End If
                                        Next
                                        If firstPhrase Then
                                            sb.Append("1=1")
                                        End If
                                    Else
                                        If operation.StartsWith("$") Then
                                            sb.Append(FilterFunctions.Replace(command, expressions, String.Format("{0}({1}$comma${2})", operation.TrimEnd(Global.Microsoft.VisualBasic.ChrW(36)), fieldAlias, Convert.ToBase64String(Encoding.UTF8.GetBytes(paramValue)))))
                                        Else
                                            Dim parameter = command.CreateParameter()
                                            parameter.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                                            AssignParameterDbType(parameter, field.Type)
                                            sb.Append(expressions(field.ExpressionName()))
                                            Dim requiresRangeAdjustment = ((operation = "=") AndAlso (field.Type.StartsWith("DateTime") AndAlso Not (StringIsNull(paramValue))))
                                            If ((operation = "<>") AndAlso StringIsNull(paramValue)) Then
                                                sb.Append(" is not null ")
                                            Else
                                                If ((operation = "=") AndAlso StringIsNull(paramValue)) Then
                                                    sb.Append(" is null ")
                                                Else
                                                    If (operation = "*") Then
                                                        sb.Append(" like ")
                                                        parameter.DbType = DbType.String
                                                        If Not (paramValue.Contains("%")) Then
                                                            paramValue = (SqlStatement.EscapePattern(command, paramValue) + "%")
                                                        End If
                                                    Else
                                                        If requiresRangeAdjustment Then
                                                            sb.Append(">=")
                                                        Else
                                                            sb.Append(operation)
                                                        End If
                                                    End If
                                                    Try 
                                                        parameter.Value = StringToValue(field, paramValue)
                                                        If ((parameter.DbType = DbType.Binary) AndAlso TypeOf parameter.Value Is Guid) Then
                                                            parameter.Value = CType(parameter.Value,Guid).ToByteArray()
                                                        End If
                                                    Catch __exception As Exception
                                                        parameter.Value = DBNull.Value
                                                    End Try
                                                    sb.Append(parameter.ParameterName)
                                                    command.Parameters.Add(parameter)
                                                    If requiresRangeAdjustment Then
                                                        Dim rangeParameter = command.CreateParameter()
                                                        AssignParameterDbType(rangeParameter, field.Type)
                                                        rangeParameter.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                                                        sb.Append(String.Format(" and {0} < {1}", expressions(field.ExpressionName()), rangeParameter.ParameterName))
                                                        If (field.Type = "DateTimeOffset") Then
                                                            Dim dt = Convert.ToDateTime(parameter.Value)
                                                            parameter.Value = New DateTimeOffset(dt).AddHours(-14)
                                                            rangeParameter.Value = New DateTimeOffset(dt).AddDays(1).AddHours(14)
                                                        Else
                                                            rangeParameter.Value = Convert.ToDateTime(parameter.Value).AddDays(1)
                                                        End If
                                                        command.Parameters.Add(rangeParameter)
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                            valueMatch = valueMatch.NextMatch()
                        Loop
                        If Not (firstValue) Then
                            sb.AppendLine(")")
                        End If
                    End If
                Next
            End If
            If (matchListCount > 0) Then
                sb.AppendLine(")")
                'the end of the "match" list
                sb.AppendLine(")")
                firstCriteria = true
            End If
            If Not (firstCriteria) Then
                sb.AppendLine(")")
                If Not (String.IsNullOrEmpty(whereClause)) Then
                    sb.Append("and ")
                End If
            End If
            If Not (String.IsNullOrEmpty(whereClause)) Then
                If (matchListCount > 0) Then
                    sb.Append("and")
                End If
                sb.AppendLine("(")
                sb.AppendLine(whereClause)
                sb.AppendLine(")")
            End If
            expressions.TrackAliases = false
        End Sub
        
        Protected Overridable Sub AppendDeepFilter(ByVal hint As String, ByVal page As ViewPage, ByVal command As DbCommand, ByVal sb As StringBuilder, ByVal filter As String)
            Dim hintInfo = hint.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(46)})
            Dim index = filter.IndexOf(":")
            Dim fieldData = filter.Substring((index + 1))
            Dim i = 0
            Do While (i < NegativeFilterOperations.Length)
                Dim negativeOperation = NegativeFilterOperations(i)
                If fieldData.StartsWith(negativeOperation) Then
                    sb.Append("not ")
                    filter = (filter.Substring(0, (index + 1))  _
                                + (ReverseNegativeFilterOperations(i) + filter.Substring((index  _
                                    + (1 + negativeOperation.Length)))))
                    Exit Do
                End If
                i = (i + 1)
            Loop
            sb.Append("exists(")
            Dim r = New PageRequest()
            r.Controller = hintInfo(0)
            r.View = hintInfo(1)
            r.Filter = New String() {filter}
            Dim controller = CType(ControllerFactory.CreateDataController(),DataControllerBase)
            For Each field in page.Fields
                If field.IsPrimaryKey Then
                    r.InnerJoinPrimaryKey = ("resultset__." + field.Name)
                    r.InnerJoinForeignKey = hintInfo(2)
                    Exit For
                End If
            Next
            controller.ConfigureSelectExistingCommand(r, command, sb)
            sb.Append(")")
        End Sub
        
        Protected Overridable Sub ConfigureSelectExistingCommand(ByVal request As PageRequest, ByVal command As DbCommand, ByVal sb As StringBuilder)
            Dim controller = request.Controller
            Dim view = request.View
            SelectView(controller, view)
            request.AssignContext(controller, Me.m_ViewId, m_Config)
            Dim page = New ViewPage(request)
            If (Not (m_Config.PlugIn) Is Nothing) Then
                m_Config.PlugIn.PreProcessPageRequest(request, page)
            End If
            m_Config.AssignDynamicExpressions(page)
            InitBusinessRules(request, page)
            Using connection = CreateConnection(Me)
                Dim selectCommand = CreateCommand(connection)
                If ((selectCommand Is Nothing) AndAlso m_ServerRules.EnableResultSet) Then
                    'it is not possible to "deep" search in this controller
                    sb.AppendLine("select 1")
                    Return
                End If
                ConfigureCommand(selectCommand, page, CommandConfigurationType.SelectExisting, Nothing)
            End Using
            Dim commandText = m_CurrentCommand.CommandText
            Dim parameterIndex = (m_CurrentCommand.Parameters.Count - 1)
            Do While (parameterIndex >= 0)
                Dim p = m_CurrentCommand.Parameters(parameterIndex)
                Dim newParameterName = (m_ParameterMarker  _
                            + ("cp" + command.Parameters.Count.ToString()))
                commandText = commandText.Replace(p.ParameterName, newParameterName)
                p.ParameterName = newParameterName
                m_CurrentCommand.Parameters.RemoveAt(parameterIndex)
                command.Parameters.Add(p)
                parameterIndex = (parameterIndex - 1)
            Loop
            Dim resultSetIndex = commandText.IndexOf("resultset__")
            Dim resultSetLastIndex = commandText.LastIndexOf("resultset__")
            If (resultSetIndex < resultSetLastIndex) Then
                commandText = (commandText.Substring(0, (resultSetIndex + 9))  _
                            + ("2" + commandText.Substring((resultSetIndex + 9))))
            End If
            sb.AppendLine(commandText)
        End Sub
        
        Protected Overridable Sub AppendSystemFilter(ByVal command As DbCommand, ByVal page As ViewPage, ByVal expressions As SelectClauseDictionary)
            Dim systemFilter = page.SystemFilter
            If (Not (RequiresHierarchy(page)) OrElse ((systemFilter Is Nothing) OrElse (systemFilter.Length < 2))) Then
                Return
            End If
            If Not (String.IsNullOrEmpty(m_ViewFilter)) Then
                m_ViewFilter = String.Format("({0})and", m_ViewFilter)
            End If
            Dim sb = New StringBuilder(m_ViewFilter)
            sb.Append("(")
            Dim collapse = (systemFilter(0) = "collapse-nodes")
            Dim parentField As DataField = Nothing
            For Each field in page.Fields
                If field.IsTagged("hierarchy-parent") Then
                    parentField = field
                    Exit For
                End If
            Next
            Dim parentFieldExpression = expressions(parentField.Name)
            sb.AppendFormat("{0} is null or ", parentFieldExpression)
            If collapse Then
                sb.Append("not(")
            End If
            sb.AppendFormat("{0} in (", parentFieldExpression)
            Dim first = true
            Dim i = 1
            Do While (i < systemFilter.Length)
                Dim v = StringToValue(systemFilter(i))
                Dim p = command.CreateParameter()
                p.ParameterName = String.Format("{0}p{1}", m_ParameterMarker, command.Parameters.Count)
                p.Value = v
                command.Parameters.Add(p)
                If first Then
                    first = false
                Else
                    sb.Append(",")
                End If
                sb.Append(p.ParameterName)
                i = (i + 1)
            Loop
            If collapse Then
                sb.Append(")")
            End If
            sb.Append("))")
            m_ViewFilter = sb.ToString()
        End Sub
        
        Private Sub AppendAccessControlRules(ByVal command As DbCommand, ByVal page As ViewPage, ByVal expressions As SelectClauseDictionary)
            Dim handler = m_Config.CreateActionHandler()
            If Not (TypeOf handler Is BusinessRules) Then
                Return
            End If
            Dim rules = m_ServerRules
            If ((rules Is Nothing) AndAlso (Not (handler) Is Nothing)) Then
                rules = CType(handler,BusinessRules)
            End If
            If (rules Is Nothing) Then
                rules = CreateBusinessRules()
            End If
            expressions.TrackAliases = true
            Dim accessControlFilter = rules.EnumerateAccessControlRules(command, m_Config.ControllerName, m_ParameterMarker, page, expressions)
            expressions.TrackAliases = false
            If String.IsNullOrEmpty(accessControlFilter) Then
                Return
            End If
            If Not (String.IsNullOrEmpty(m_ViewFilter)) Then
                m_ViewFilter = (m_ViewFilter + " and ")
            End If
            m_ViewFilter = String.Format("{0}/*Sql*/{1}/*Sql*/", m_ViewFilter, accessControlFilter)
        End Sub
    End Class
End Namespace
