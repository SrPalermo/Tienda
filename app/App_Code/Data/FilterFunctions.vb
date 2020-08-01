Imports System
Imports System.Collections.Generic
Imports System.Data.Common
Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Caching
Imports System.Web.Security

Namespace Norwindt.Data
    
    Public Class FilterFunctions
        
        Public Shared All As SortedDictionary(Of String, FilterFunctionBase) = New SortedDictionary(Of String, FilterFunctionBase)()
        
        Private m_Command As DbCommand
        
        Private m_Filter As String
        
        Private m_Expressions As SelectClauseDictionary
        
        Shared Sub New()
            All.Add("username", New UserNameFilterFunction())
            All.Add("userid", New UserIdFilterFunction())
            All.Add("external", New ExternalFilterFunction())
            All.Add("beginswith", New TextMatchingFilterFunction("{0}%"))
            All.Add("doesnotbeginwith", New NegativeTextMatchingFilterFunction("{0}%"))
            All.Add("contains", New TextMatchingFilterFunction("%{0}%"))
            All.Add("doesnotcontain", New NegativeTextMatchingFilterFunction("%{0}%"))
            All.Add("endswith", New TextMatchingFilterFunction("%{0}"))
            All.Add("doesnotendwith", New NegativeTextMatchingFilterFunction("%{0}"))
            All.Add("between", New BetweenFilterFunction())
            All.Add("in", New InFilterFunction())
            All.Add("notin", New NotInFilterFunction())
            All.Add("month1", New DateRangeFilterFunction(1))
            All.Add("month2", New DateRangeFilterFunction(2))
            All.Add("month3", New DateRangeFilterFunction(3))
            All.Add("month4", New DateRangeFilterFunction(4))
            All.Add("month5", New DateRangeFilterFunction(5))
            All.Add("month6", New DateRangeFilterFunction(6))
            All.Add("month7", New DateRangeFilterFunction(7))
            All.Add("month8", New DateRangeFilterFunction(8))
            All.Add("month9", New DateRangeFilterFunction(9))
            All.Add("month10", New DateRangeFilterFunction(10))
            All.Add("month11", New DateRangeFilterFunction(11))
            All.Add("month12", New DateRangeFilterFunction(12))
            All.Add("thismonth", New ThisMonthFilterFunction(0))
            All.Add("nextmonth", New ThisMonthFilterFunction(1))
            All.Add("lastmonth", New ThisMonthFilterFunction(-1))
            All.Add("quarter1", New QuarterFilterFunction(1))
            All.Add("quarter2", New QuarterFilterFunction(2))
            All.Add("quarter3", New QuarterFilterFunction(3))
            All.Add("quarter4", New QuarterFilterFunction(4))
            All.Add("thisquarter", New ThisQuarterFilterFunction(0))
            All.Add("lastquarter", New ThisQuarterFilterFunction(-1))
            All.Add("nextquarter", New ThisQuarterFilterFunction(1))
            All.Add("thisyear", New ThisYearFilterFunction(0))
            All.Add("lastyear", New ThisYearFilterFunction(-1))
            All.Add("nextyear", New ThisYearFilterFunction(1))
            All.Add("yeartodate", New YearToDateFilterFunction())
            All.Add("thisweek", New ThisWeekFilterFunction(0))
            All.Add("lastweek", New ThisWeekFilterFunction(-1))
            All.Add("nextweek", New ThisWeekFilterFunction(1))
            All.Add("today", New TodayFilterFunction(0))
            All.Add("yesterday", New TodayFilterFunction(-1))
            All.Add("tomorrow", New TodayFilterFunction(1))
            All.Add("past", New PastFilterFunction())
            All.Add("future", New FutureFilterFunction())
            All.Add("true", New TrueFilterFunction())
            All.Add("false", New FalseFilterFunction())
            All.Add("isempty", New IsEmptyFilterFunction())
            All.Add("isnotempty", New IsNotEmptyFilterFunction())
        End Sub
        
        Public Sub New(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal filter As String)
            MyBase.New
            Me.m_Command = command
            Me.m_Filter = filter
            Me.m_Expressions = expressions
        End Sub
        
        Public Shared Function Replace(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal filter As String) As String
            Dim functions = New FilterFunctions(command, expressions, filter)
            Return functions.ToString()
        End Function
        
        Public Overrides Function ToString() As String
            Dim filter = Regex.Replace(Me.m_Filter, "\$((?'Name'\w+)\s*\((?'Arguments'[\s\S]*?)\)\s*)", AddressOf DoReplaceFunctions)
            Return filter
        End Function
        
        Private Function DoReplaceFunctions(ByVal m As Match) As String
            Return String.Format("({0})", All(m.Groups("Name").Value.ToLower()).ExpandWith(m_Command, m_Expressions, m.Groups("Arguments").Value))
        End Function
    End Class
    
    Public Class FilterFunctionBase
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Overridable ReadOnly Property YearsBack() As Integer
            Get
                Return 16
            End Get
        End Property
        
        Public Overridable ReadOnly Property YearsForward() As Integer
            Get
                Return 1
            End Get
        End Property
        
        Public Overridable Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Return String.Empty
        End Function
        
        Protected Function CreateParameter(ByVal command As DbCommand) As DbParameter
            Dim p = command.CreateParameter()
            Dim marker = SqlStatement.ConvertTypeToParameterMarker(p.GetType())
            p.ParameterName = ((marker + "p")  _
                        + command.Parameters.Count.ToString())
            command.Parameters.Add(p)
            Return p
        End Function
        
        Public Function FirstArgument(ByVal arguments As String) As String
            Dim m = Regex.Match(arguments, "^\s*(\w+)\s*(,|\$comma\$)?")
            Return m.Groups(1).Value
        End Function
        
        Protected Function ExtractArgument(ByVal arguments As String, ByVal index As Integer) As String
            Dim m = Regex.Match(arguments, "^\s*(\w+)\s*(,|\$comma\$)\s*([\s\S]*?)\s*$")
            Dim s = m.Groups(3).Value
            If (m.Groups(2).Value = "$comma$") Then
                s = Encoding.UTF8.GetString(Convert.FromBase64String(s))
            End If
            m = Regex.Match(s, "^([\s\S]*?)\$and\$([\s\S]*?)$")
            If m.Success Then
                Return m.Groups(index).Value
            End If
            Return s
        End Function
        
        Public Function SecondArgument(ByVal arguments As String) As String
            Return ExtractArgument(arguments, 1)
        End Function
        
        Public Function ThirdArgument(ByVal arguments As String) As String
            Return ExtractArgument(arguments, 2)
        End Function
    End Class
    
    Public Class UserNameFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = HttpContext.Current.User.Identity.Name
            If String.IsNullOrEmpty(arguments) Then
                Return p.ParameterName
            End If
            Return String.Format("{0}={1}", arguments, p.ParameterName)
        End Function
    End Class
    
    Public Class UserIdFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = Membership.GetUser().ProviderUserKey
            If String.IsNullOrEmpty(arguments) Then
                Return p.ParameterName
            End If
            Return String.Format("{0}={1}", arguments, p.ParameterName)
        End Function
    End Class
    
    Public Class TextMatchingFilterFunction
        Inherits FilterFunctionBase
        
        Private m_Pattern As String
        
        Public Sub New(ByVal pattern As String)
            MyBase.New
            Me.m_Pattern = pattern
        End Sub
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = String.Format(m_Pattern, SqlStatement.EscapePattern(command, Convert.ToString(Controller.StringToValue(SecondArgument(arguments)))))
            Return String.Format("{0} like {1}", expressions(FirstArgument(arguments)), p.ParameterName)
        End Function
    End Class
    
    Public Class NegativeTextMatchingFilterFunction
        Inherits TextMatchingFilterFunction
        
        Public Sub New(ByVal pattern As String)
            MyBase.New(pattern)
        End Sub
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Return String.Format("not({0})", MyBase.ExpandWith(command, expressions, arguments))
        End Function
    End Class
    
    Public Class BetweenFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = Controller.StringToValue(SecondArgument(arguments))
            Dim p2 = CreateParameter(command)
            p2.Value = Controller.StringToValue(ThirdArgument(arguments))
            If expressions.ContainsKey("_DataView_RowFilter_") Then
                Return String.Format("{0} >= {1} and {0} <= {2}", expressions(FirstArgument(arguments)), p.ParameterName, p2.ParameterName)
            Else
                Return String.Format("{0} between {1} and {2}", expressions(FirstArgument(arguments)), p.ParameterName, p2.ParameterName)
            End If
        End Function
    End Class
    
    Public Class InFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim fieldExpression = expressions(FirstArgument(arguments))
            Dim sb = New StringBuilder(fieldExpression)
            sb.Append(" in (")
            Dim list = SecondArgument(arguments).Split(New String() {"$or$"}, StringSplitOptions.RemoveEmptyEntries)
            Dim hasNull = false
            Dim hasValues = false
            For Each v in list
                If Controller.StringIsNull(v) Then
                    hasNull = true
                Else
                    If hasValues Then
                        sb.Append(",")
                    Else
                        hasValues = true
                    End If
                    Dim p = CreateParameter(command)
                    p.Value = Controller.StringToValue(v)
                    sb.Append(p.ParameterName)
                End If
            Next
            sb.Append(")")
            If hasNull Then
                If hasValues Then
                    Return String.Format("({0} is null) or ({1})", fieldExpression, sb.ToString())
                Else
                    Return String.Format("{0} is null", fieldExpression)
                End If
            Else
                Return sb.ToString()
            End If
        End Function
    End Class
    
    Public Class NotInFilterFunction
        Inherits InFilterFunction
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim list = SecondArgument(arguments).Split(New String() {"$or$"}, StringSplitOptions.RemoveEmptyEntries)
            Dim filter = String.Format("not({0})", MyBase.ExpandWith(command, expressions, arguments))
            If (Array.IndexOf(list, "null") = -1) Then
                filter = String.Format("({0}) or {1} is null", filter, expressions(FirstArgument(arguments)))
            End If
            Return filter
        End Function
    End Class
    
    Partial Public Class DateRangeFilterFunction
        Inherits FilterFunctionBase
        
        Private m_Month As Integer
        
        Private m_StartYear As Integer
        
        Private m_EndYear As Integer
        
        Public Sub New(ByVal month As Integer)
            Me.New(month, -1, -1)
        End Sub
        
        Public Sub New()
            Me.New(0, 0, 0)
        End Sub
        
        Public Sub New(ByVal month As Integer, ByVal startYear As Integer, ByVal endYear As Integer)
            MyBase.New
            Me.m_Month = month
            If (startYear = -1) Then
                startYear = YearsBack
            End If
            Me.m_StartYear = startYear
            If (endYear = -1) Then
                endYear = YearsForward
            End If
            Me.m_EndYear = endYear
        End Sub
        
        Public ReadOnly Property Month() As Integer
            Get
                Return m_Month
            End Get
        End Property
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim sb = New StringBuilder()
            Dim currentYear = DateTime.Today.Year
            Dim i = (currentYear - m_StartYear)
            Do While (i <= (currentYear + m_EndYear))
                Dim p = CreateParameter(command)
                Dim p2 = CreateParameter(command)
                Dim startDate As DateTime
                Dim endDate As DateTime
                AssignRange(i, startDate, endDate)
                p.Value = startDate
                p2.Value = endDate
                If (sb.Length > 0) Then
                    sb.Append("or")
                End If
                If expressions.ContainsKey("_DataView_RowFilter_") Then
                    sb.AppendFormat("({0} >= {1} and {0} <= {2})", expressions(FirstArgument(arguments)), p.ParameterName, p2.ParameterName)
                Else
                    sb.AppendFormat("({0} between {1} and {2})", expressions(FirstArgument(arguments)), p.ParameterName, p2.ParameterName)
                End If
                i = (i + 1)
            Loop
            Return sb.ToString()
        End Function
        
        Protected Overridable Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            startDate = New DateTime(year, Month, 1)
            endDate = startDate.AddMonths(1).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class ThisMonthFilterFunction
        Inherits DateRangeFilterFunction
        
        Private m_DeltaMonth As Integer
        
        Public Sub New(ByVal deltaMonth As Integer)
            MyBase.New
            Me.m_DeltaMonth = deltaMonth
        End Sub
        
        Protected Overrides Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            startDate = New DateTime(year, DateTime.Today.Month, 1).AddMonths(m_DeltaMonth)
            endDate = startDate.AddMonths(1).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class QuarterFilterFunction
        Inherits DateRangeFilterFunction
        
        Public Sub New(ByVal quarter As Integer)
            MyBase.New((((quarter - 1)  _
                            * 3)  _
                            + 1))
        End Sub
        
        Protected Overrides Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            startDate = New DateTime(year, Month, 1)
            endDate = startDate.AddMonths(3).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class ThisQuarterFilterFunction
        Inherits DateRangeFilterFunction
        
        Private m_DeltaQuarter As Integer
        
        Public Sub New(ByVal deltaQuarter As Integer)
            MyBase.New
            Me.m_DeltaQuarter = deltaQuarter
        End Sub
        
        Protected Overrides Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            Dim month = DateTime.Today.Month
            Do While Not (((month Mod 3) = 1))
                month = (month - 1)
            Loop
            startDate = New DateTime(year, month, 1).AddMonths((m_DeltaQuarter * 3))
            endDate = startDate.AddMonths(3).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class ThisYearFilterFunction
        Inherits DateRangeFilterFunction
        
        Private m_DeltaYear As Integer
        
        Public Sub New(ByVal deltaYear As Integer)
            MyBase.New
            Me.m_DeltaYear = deltaYear
        End Sub
        
        Protected Overrides Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            startDate = New DateTime(DateTime.Today.Year, 1, 1).AddYears(m_DeltaYear)
            endDate = startDate.AddMonths(12).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class YearToDateFilterFunction
        Inherits DateRangeFilterFunction
        
        Protected Overrides Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            startDate = New DateTime(DateTime.Today.Year, 1, 1)
            endDate = DateTime.Today.AddDays(1).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class ThisWeekFilterFunction
        Inherits DateRangeFilterFunction
        
        Private m_DeltaWeek As Integer
        
        Public Sub New(ByVal deltaWeek As Integer)
            MyBase.New
            Me.m_DeltaWeek = deltaWeek
        End Sub
        
        Protected Overrides Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            startDate = DateTime.Today
            Do While Not ((startDate.DayOfWeek = CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek))
                startDate = startDate.AddDays(-1)
            Loop
            startDate = startDate.AddDays((7 * m_DeltaWeek))
            endDate = startDate.AddDays(7).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class TodayFilterFunction
        Inherits DateRangeFilterFunction
        
        Private m_DeltaDays As Integer
        
        Public Sub New(ByVal deltaDays As Integer)
            MyBase.New
            Me.m_DeltaDays = deltaDays
        End Sub
        
        Protected Overrides Sub AssignRange(ByVal year As Integer, ByRef startDate As DateTime, ByRef endDate As DateTime)
            startDate = DateTime.Today.AddDays(m_DeltaDays)
            endDate = startDate.AddDays(1).AddSeconds(-1)
        End Sub
    End Class
    
    Public Class PastFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = DateTime.Now
            Return String.Format("{0}<{1}", expressions(FirstArgument(arguments)), p.ParameterName)
        End Function
    End Class
    
    Public Class FutureFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = DateTime.Now
            Return String.Format("{0}<{1}", p.ParameterName, expressions(FirstArgument(arguments)))
        End Function
    End Class
    
    Public Class TrueFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = true
            Return String.Format("{0}={1}", expressions(FirstArgument(arguments)), p.ParameterName)
        End Function
    End Class
    
    Public Class FalseFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = false
            Return String.Format("{0}={1}", expressions(FirstArgument(arguments)), p.ParameterName)
        End Function
    End Class
    
    Public Class IsEmptyFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Return String.Format("{0} is null", expressions(FirstArgument(arguments)))
        End Function
    End Class
    
    Public Class IsNotEmptyFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Return String.Format("{0} is not null", expressions(FirstArgument(arguments)))
        End Function
    End Class
    
    Public Class ExternalFilterFunction
        Inherits FilterFunctionBase
        
        Public Overrides Function ExpandWith(ByVal command As DbCommand, ByVal expressions As SelectClauseDictionary, ByVal arguments As String) As String
            Dim p = CreateParameter(command)
            p.Value = DBNull.Value
            If (Not (PageRequest.Current) Is Nothing) Then
                Dim parameterName = Regex.Match(arguments, "\w+").Value
                For Each v in PageRequest.Current.ExternalFilter
                    If v.Name.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase) Then
                        p.Value = v.Value
                        Exit For
                    End If
                Next
            End If
            Return p.ParameterName
        End Function
    End Class
End Namespace
