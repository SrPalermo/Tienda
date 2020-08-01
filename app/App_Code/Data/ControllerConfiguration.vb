Imports Newtonsoft.Json.Linq
Imports Norwindt.Services
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Caching
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Data
    
    Public Class ControllerConfiguration
        
        Private m_Navigator As XPathNavigator
        
        Private m_NamespaceManager As XmlNamespaceManager
        
        Private m_Resolver As IXmlNamespaceResolver
        
        Private m_ActionHandlerType As String
        
        Private m_DataFilterType As String
        
        Private m_HandlerType As String
        
        Public Shared VariableDetectionRegex As Regex = New Regex("\$\w+\$")
        
        Public Shared VariableReplacementRegex As Regex = New Regex("\$(\w+)\$([\s\S]*?)\$(\w+)\$")
        
        Public Shared LocalizationDetectionRegex As Regex = New Regex("\^\w+\^")
        
        Public Const [Namespace] As String = "urn:schemas-codeontime-com:data-aquarium"
        
        Private m_ConnectionStringName As String
        
        Private m_ControllerName As String
        
        Private m_ConflictDetectionEnabled As Boolean
        
        Private m_Expressions() As DynamicExpression
        
        Private m_PlugIn As IPlugIn
        
        Private m_RawConfiguration As String
        
        Private m_UsesVariables As Boolean
        
        Private m_RequiresLocalization As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_PendingAlterations As SiteContentFileList
        
        Public Sub New(ByVal path As String)
            Me.New(File.OpenRead(path))
        End Sub
        
        Public Sub New(ByVal stream As Stream)
            MyBase.New
            Dim sr = New StreamReader(stream)
            Me.m_RawConfiguration = sr.ReadToEnd()
            sr.Close()
            Me.m_UsesVariables = VariableDetectionRegex.IsMatch(Me.m_RawConfiguration)
            Me.m_RequiresLocalization = LocalizationDetectionRegex.IsMatch(Me.m_RawConfiguration)
            Initialize(New XPathDocument(New StringReader(Me.m_RawConfiguration)).CreateNavigator())
        End Sub
        
        Public Sub New(ByVal document As XPathDocument)
            Me.New(document.CreateNavigator())
        End Sub
        
        Public Sub New(ByVal navigator As XPathNavigator)
            MyBase.New
            Initialize(navigator)
        End Sub
        
        Public ReadOnly Property ConnectionStringName() As String
            Get
                Return m_ConnectionStringName
            End Get
        End Property
        
        Public ReadOnly Property ControllerName() As String
            Get
                Return m_ControllerName
            End Get
        End Property
        
        Public ReadOnly Property ConflictDetectionEnabled() As Boolean
            Get
                Return m_ConflictDetectionEnabled
            End Get
        End Property
        
        Public ReadOnly Property Resolver() As IXmlNamespaceResolver
            Get
                Return m_Resolver
            End Get
        End Property
        
        Public ReadOnly Property Navigator() As XPathNavigator
            Get
                Return m_Navigator
            End Get
        End Property
        
        Public Property Expressions() As DynamicExpression()
            Get
                Return m_Expressions
            End Get
            Set
                m_Expressions = value
            End Set
        End Property
        
        Public ReadOnly Property PlugIn() As IPlugIn
            Get
                Return m_PlugIn
            End Get
        End Property
        
        Public ReadOnly Property RawConfiguration() As String
            Get
                Return m_RawConfiguration
            End Get
        End Property
        
        Public ReadOnly Property UsesVariables() As Boolean
            Get
                Return m_UsesVariables
            End Get
        End Property
        
        Public ReadOnly Property RequiresLocalization() As Boolean
            Get
                Return m_RequiresLocalization
            End Get
        End Property
        
        Public Property PendingAlterations() As SiteContentFileList
            Get
                Return m_PendingAlterations
            End Get
            Set
                m_PendingAlterations = value
            End Set
        End Property
        
        Public ReadOnly Property TrimmedNavigator() As XPathNavigator
            Get
                Dim hiddenFields = New List(Of String)()
                Dim fieldIterator = [Select]("/c:dataController/c:fields/c:field[@roles!='']")
                Do While fieldIterator.MoveNext()
                    Dim roles = fieldIterator.Current.GetAttribute("roles", String.Empty)
                    If Not (DataControllerBase.UserIsInRole(roles)) Then
                        hiddenFields.Add(fieldIterator.Current.GetAttribute("name", String.Empty))
                    End If
                Loop
                If (hiddenFields.Count = 0) Then
                    Return Navigator
                End If
                Dim doc = New XmlDocument()
                doc.LoadXml(Navigator.OuterXml)
                Dim nav = doc.CreateNavigator()
                Dim dataFieldIterator = nav.Select("//c:dataField", Resolver)
                Do While dataFieldIterator.MoveNext()
                    If hiddenFields.Contains(dataFieldIterator.Current.GetAttribute("fieldName", String.Empty)) Then
                        Dim hiddenAttr = dataFieldIterator.Current.SelectSingleNode("@hidden")
                        If (hiddenAttr Is Nothing) Then
                            dataFieldIterator.Current.CreateAttribute(String.Empty, "hidden", String.Empty, "true")
                        Else
                            hiddenAttr.SetValue("true")
                        End If
                    End If
                Loop
                Return nav
            End Get
        End Property
        
        Public Function RequiresVirtualization(ByVal controllerName As String) As Boolean
            Dim rules = CreateBusinessRules()
            Return ((Not (rules) Is Nothing) AndAlso rules.SupportsVirtualization(controllerName))
        End Function
        
        Public Function Virtualize(ByVal controllerName As String) As ControllerConfiguration
            Dim config = Me
            If Not (m_Navigator.CanEdit) Then
                Dim doc = New XmlDocument()
                doc.LoadXml(m_Navigator.OuterXml)
                config = New ControllerConfiguration(doc.CreateNavigator())
            End If
            Dim rules = CreateBusinessRules()
            If (Not (rules) Is Nothing) Then
                rules.VirtualizeController(controllerName, config.m_Navigator, config.m_NamespaceManager)
                config.PendingAlterations = rules.PendingAlterations
            End If
            Return config
        End Function
        
        Protected Overridable Sub Initialize(ByVal navigator As XPathNavigator)
            m_Navigator = navigator
            m_NamespaceManager = New XmlNamespaceManager(m_Navigator.NameTable)
            m_NamespaceManager.AddNamespace("c", ControllerConfiguration.Namespace)
            m_Resolver = m_NamespaceManager
            ResolveBaseViews()
            m_ControllerName = CType(Evaluate("string(/c:dataController/@name)"),String)
            m_HandlerType = CType(Evaluate("string(/c:dataController/@handler)"),String)
            If String.IsNullOrEmpty(m_HandlerType) Then
                Dim t = ApplicationServices.StringToType("Norwindt.Rules.SharedBusinessRules")
                If (Not (t) Is Nothing) Then
                    m_HandlerType = t.FullName
                End If
            End If
            m_ActionHandlerType = m_HandlerType
            m_DataFilterType = m_HandlerType
            Dim s = CType(Evaluate("string(/c:dataController/@actionHandlerType)"),String)
            If Not (String.IsNullOrEmpty(s)) Then
                m_ActionHandlerType = s
            End If
            s = CType(Evaluate("string(/c:dataController/@dataFilterType)"),String)
            If Not (String.IsNullOrEmpty(s)) Then
                m_DataFilterType = s
            End If
            Dim plugInType = CType(Evaluate("string(/c:dataController/@plugIn)"),String)
            If (Not (String.IsNullOrEmpty(plugInType)) AndAlso ApplicationServices.IsTouchClient) Then
                plugInType = String.Empty
            End If
            If Not (String.IsNullOrEmpty(plugInType)) Then
                Dim t = Type.GetType(plugInType)
                m_PlugIn = CType(t.Assembly.CreateInstance(t.FullName),IPlugIn)
                m_PlugIn.Config = Me
            End If
        End Sub
        
        Public Overridable Sub Complete()
            m_ConnectionStringName = CType(Evaluate("string(/c:dataController/@connectionStringName)"),String)
            If String.IsNullOrEmpty(m_ConnectionStringName) Then
                m_ConnectionStringName = "Norwindt"
            End If
            m_ConflictDetectionEnabled = CType(Evaluate("/c:dataController/@conflictDetection='compareAllValues'"),Boolean)
            Dim expressions = New List(Of DynamicExpression)()
            Dim expressionIterator = [Select]("//c:expression[@test!='' or @result!='']")
            Do While expressionIterator.MoveNext()
                expressions.Add(New DynamicExpression(expressionIterator.Current, m_NamespaceManager))
            Loop
            Dim ruleIterator = [Select]("/c:dataController/c:businessRules/c:rule[@type='JavaScript']")
            Do While ruleIterator.MoveNext()
                Dim rule = New DynamicExpression()
                rule.Type = DynamicExpressionType.ClientScript
                rule.Scope = DynamicExpressionScope.Rule
                Dim ruleNav = ruleIterator.Current
                rule.Result = String.Format("<id>{0}</id><command>{1}</command><argument>{2}</argument><view>{3}</view><phase>"& _ 
                        "{4}</phase><js>{5}</js>", ruleNav.GetAttribute("id", String.Empty), ruleNav.GetAttribute("commandName", String.Empty), ruleNav.GetAttribute("commandArgument", String.Empty), ruleNav.GetAttribute("view", String.Empty), ruleNav.GetAttribute("phase", String.Empty), ruleNav.Value)
                expressions.Add(rule)
            Loop
            m_Expressions = expressions.ToArray()
        End Sub
        
        Private Sub EnsureChildNode(ByVal parent As XPathNavigator, ByVal nodeName As String)
            Dim child = parent.SelectSingleNode(String.Format("c:{0}", nodeName), m_Resolver)
            If (child Is Nothing) Then
                parent.AppendChild(String.Format("<{0}/>", nodeName))
            End If
        End Sub
        
        Public Overridable Function EnsureVitalElements() As ControllerConfiguration
            'verify that the data controller has views and actions
            Dim root = SelectSingleNode("/c:dataController[c:views/c:view and c:actions/c:actionGroup]")
            If (Not (root) Is Nothing) Then
                Return Me
            End If
            'add missing configuration elements
            Dim doc = New XmlDocument()
            doc.LoadXml(m_Navigator.OuterXml)
            Dim config = New ControllerConfiguration(doc.CreateNavigator())
            Dim fieldsNode = config.SelectSingleNode("/c:dataController/c:fields[not(c:field[@isPrimaryKey='true'])]")
            If (Not (fieldsNode) Is Nothing) Then
                fieldsNode.AppendChild("<field name=""PrimaryKey"" type=""Int32"" isPrimaryKey=""true"" readOnly=""true""/>")
            End If
            root = config.SelectSingleNode("/c:dataController")
            EnsureChildNode(root, "views")
            Dim viewsNode = config.SelectSingleNode("/c:dataController/c:views[not(c:view)]")
            If (Not (viewsNode) Is Nothing) Then
                Dim sb = New StringBuilder("<view id=""view1"" type=""Form"" label=""Form""><categories><category id=""c1"" flow=""New"& _ 
                        "Column""><dataFields>")
                Dim fieldIterator = config.Select("/c:dataController/c:fields/c:field")
                Do While fieldIterator.MoveNext()
                    Dim fieldName = fieldIterator.Current.GetAttribute("name", String.Empty)
                    Dim hidden = (fieldName = "PrimaryKey")
                    Dim length = fieldIterator.Current.GetAttribute("length", String.Empty)
                    If (String.IsNullOrEmpty(length) AndAlso (CType(fieldIterator.Current.Evaluate("not(c:items/@style!='')", m_Resolver),Boolean) = true)) Then
                        If (fieldIterator.Current.GetAttribute("type", String.Empty) = "String") Then
                            length = "50"
                        Else
                            length = "20"
                        End If
                    End If
                    sb.AppendFormat("<dataField fieldName=""{0}"" hidden=""{1}""", fieldName, hidden.ToString().ToLower())
                    If Not (String.IsNullOrEmpty(length)) Then
                        sb.AppendFormat(" columns=""{0}""", length)
                    End If
                    sb.Append(" />")
                Loop
                sb.Append("</dataFields></category></categories></view>")
                viewsNode.AppendChild(sb.ToString())
            End If
            EnsureChildNode(root, "actions")
            Dim actionsNode = config.SelectSingleNode("/c:dataController/c:actions[not(c:actionGroup)]")
            If (Not (actionsNode) Is Nothing) Then
                actionsNode.AppendChild("" & ControlChars.CrLf &"                          <actionGroup id=""ag1"" scope=""Form"">" & ControlChars.CrLf &"<action id=""a1"" "& _ 
                        "commandName=""Confirm"" causesValidation=""true"" whenLastCommandName=""New"" />" & ControlChars.CrLf &"<act"& _ 
                        "ion id=""a2"" commandName=""Cancel"" whenLastCommandName=""New"" />" & ControlChars.CrLf &"<action id=""a3"" c"& _ 
                        "ommandName=""Confirm"" causesValidation=""true"" whenLastCommandName=""Edit"" />" & ControlChars.CrLf &"<act"& _ 
                        "ion id=""a4"" commandName=""Cancel"" whenLastCommandName=""Edit"" />" & ControlChars.CrLf &"<action id=""a5"" "& _ 
                        "commandName=""Edit"" causesValidation=""true"" />" & ControlChars.CrLf &"</actionGroup>")
            End If
            Dim plugIn = config.SelectSingleNode("/c:dataController/@plugIn")
            If (Not (plugIn) Is Nothing) Then
                plugIn.DeleteSelf()
                config.m_PlugIn = Nothing
            End If
            Return config
        End Function
        
        Protected Overridable Sub ResolveBaseViews()
            Dim firstUnresolvedView = SelectSingleNode("/c:dataController/c:views/c:view[@baseViewId!='' and not (.//c:dataField)]")
            If (Not (firstUnresolvedView) Is Nothing) Then
                Dim document = New XmlDocument()
                document.LoadXml(m_Navigator.OuterXml)
                m_Navigator = document.CreateNavigator()
                Dim unresolvedViewIterator = [Select]("/c:dataController/c:views/c:view[@baseViewId!='']")
                Do While unresolvedViewIterator.MoveNext()
                    Dim baseViewId = unresolvedViewIterator.Current.GetAttribute("baseViewId", String.Empty)
                    unresolvedViewIterator.Current.SelectSingleNode("@baseViewId").DeleteSelf()
                    Dim baseView = SelectSingleNode(String.Format("/c:dataController/c:views/c:view[@id='{0}']", baseViewId))
                    If (Not (baseView) Is Nothing) Then
                        Dim nodesToDelete = New List(Of XPathNavigator)()
                        Dim emptyNodeIterator = unresolvedViewIterator.Current.Select("c:*[not(child::*) and .='']", m_Resolver)
                        Do While emptyNodeIterator.MoveNext()
                            nodesToDelete.Add(emptyNodeIterator.Current.Clone())
                        Loop
                        For Each n in nodesToDelete
                            n.DeleteSelf()
                        Next
                        Dim copyNodeIterator = baseView.Select("c:*", m_Resolver)
                        Do While copyNodeIterator.MoveNext()
                            If (unresolvedViewIterator.Current.SelectSingleNode(("c:" + copyNodeIterator.Current.LocalName), m_Resolver) Is Nothing) Then
                                unresolvedViewIterator.Current.AppendChild(copyNodeIterator.Current.OuterXml)
                            End If
                        Loop
                    End If
                Loop
                m_Navigator = New XPathDocument(New StringReader(m_Navigator.OuterXml)).CreateNavigator()
            End If
        End Sub
        
        Private Sub InitializeHandler(ByVal handler As Object)
            If ((Not (handler) Is Nothing) AndAlso TypeOf handler Is BusinessRules) Then
                CType(handler,BusinessRules).ControllerName = ControllerName
            End If
        End Sub
        
        Public Function CreateBusinessRules() As BusinessRules
            Dim handler = CreateActionHandler()
            If (handler Is Nothing) Then
                Return Nothing
            Else
                Dim rules = CType(handler,BusinessRules)
                rules.Config = Me
                Return rules
            End If
        End Function
        
        Public Function CreateActionHandler() As IActionHandler
            If String.IsNullOrEmpty(m_ActionHandlerType) Then
                Return Nothing
            Else
                Dim handler = ApplicationServices.CreateInstance(m_ActionHandlerType)
                InitializeHandler(handler)
                If TypeOf handler Is BusinessRules Then
                    CType(handler,BusinessRules).Config = Me
                End If
                Return CType(handler,IActionHandler)
            End If
        End Function
        
        Public Function CreateDataFilter() As IDataFilter
            If String.IsNullOrEmpty(m_DataFilterType) Then
                Return Nothing
            Else
                Dim dataFilter = ApplicationServices.CreateInstance(m_DataFilterType)
                InitializeHandler(dataFilter)
                If GetType(IDataFilter).IsInstanceOfType(dataFilter) Then
                    Return CType(dataFilter,IDataFilter)
                Else
                    Return Nothing
                End If
            End If
        End Function
        
        Public Function CreateRowHandler() As IRowHandler
            If String.IsNullOrEmpty(m_ActionHandlerType) Then
                Return Nothing
            Else
                Dim t = Type.GetType(m_ActionHandlerType)
                Dim handler = t.Assembly.CreateInstance(t.FullName)
                InitializeHandler(handler)
                If GetType(IRowHandler).IsInstanceOfType(handler) Then
                    Return CType(handler,IRowHandler)
                Else
                    Return Nothing
                End If
            End If
        End Function
        
        Public Sub AssignDynamicExpressions(ByVal page As ViewPage)
            Dim list = New List(Of DynamicExpression)()
            If page.IncludeMetadata("expressions") Then
                For Each de in m_Expressions
                    If de.AllowedInView(page.View) Then
                        list.Add(de)
                    End If
                Next
            End If
            page.Expressions = list.ToArray()
        End Sub
        
        Public Function Clone() As ControllerConfiguration
            Dim variablesPath = Path.Combine(HttpRuntime.AppDomainAppPath, "Controllers\_variables.xml")
            Dim variables = CType(HttpRuntime.Cache(variablesPath),SortedDictionary(Of String, String))
            If (variables Is Nothing) Then
                variables = New SortedDictionary(Of String, String)()
                If File.Exists(variablesPath) Then
                    Dim varDoc = New XPathDocument(variablesPath)
                    Dim varNav = varDoc.CreateNavigator()
                    Dim varIterator = varNav.Select("/variables/variable")
                    Do While varIterator.MoveNext()
                        Dim varName = varIterator.Current.GetAttribute("name", String.Empty)
                        Dim varValue = varIterator.Current.Value
                        If Not (variables.ContainsKey(varName)) Then
                            variables.Add(varName, varValue)
                        Else
                            variables(varName) = varValue
                        End If
                    Loop
                End If
                HttpRuntime.Cache.Insert(variablesPath, variables, New CacheDependency(variablesPath))
            End If
            Return New ControllerConfiguration(New XPathDocument(New StringReader(New ControllerConfigurationUtility(m_RawConfiguration, variables).ReplaceVariables())))
        End Function
        
        Public Function Localize(ByVal controller As String) As ControllerConfiguration
            Dim localizedContent = Localizer.Replace("Controllers", (controller + ".xml"), m_Navigator.OuterXml)
            If (Not (PlugIn) Is Nothing) Then
                Dim doc = New XmlDocument()
                doc.LoadXml(localizedContent)
                Return New ControllerConfiguration(doc.CreateNavigator())
            Else
                Return New ControllerConfiguration(New XPathDocument(New StringReader(localizedContent)))
            End If
        End Function
        
        Public Function SelectSingleNode(ByVal selector As String, ByVal ParamArray args() as System.[Object]) As XPathNavigator
            Return m_Navigator.SelectSingleNode(String.Format(selector, args), m_Resolver)
        End Function
        
        Public Function [Select](ByVal selector As String, ByVal ParamArray args() as System.[Object]) As XPathNodeIterator
            Return m_Navigator.Select(String.Format(selector, args), m_Resolver)
        End Function
        
        Public Function Evaluate(ByVal selector As String, ByVal ParamArray args() as System.[Object]) As Object
            Return m_Navigator.Evaluate(String.Format(selector, args), m_Resolver)
        End Function
        
        Public Function ReadActionData(ByVal path As String) As String
            If Not (String.IsNullOrEmpty(path)) Then
                Dim p = path.Split(Global.Microsoft.VisualBasic.ChrW(47))
                If (p.Length = 2) Then
                    Dim dataNav = SelectSingleNode("/c:dataController/c:actions/c:actionGroup[@id='{0}']/c:action[@id='{1}']/c:data", p(0), p(1))
                    If (Not (dataNav) Is Nothing) Then
                        Return dataNav.Value
                    End If
                End If
            End If
            Return Nothing
        End Function
        
        Public Sub ParseActionData(ByVal path As String, ByVal variables As SortedDictionary(Of String, String))
            Dim data = ReadActionData(path)
            If Not (String.IsNullOrEmpty(data)) Then
                Dim m = Regex.Match(data, "^\s*(\w+)\s*=\s*(.+?)\s*$", RegexOptions.Multiline)
                Do While m.Success
                    variables(m.Groups(1).Value) = m.Groups(2).Value
                    m = m.NextMatch()
                Loop
            End If
        End Sub
        
        Public Function LoadLayout(ByVal view As String) As String
            Dim viewLayout As String = Nothing
            'load the view layout
            Dim fileName = String.Format("{0}.{1}.html", Me.ControllerName, view)
            Dim tryLoad = true
            Do While tryLoad
                fileName = Path.Combine(Path.Combine(HttpRuntime.AppDomainAppPath, "Views"), fileName)
                If File.Exists(fileName) Then
                    viewLayout = File.ReadAllText(fileName)
                Else
                    Dim stream = [GetType]().Assembly.GetManifestResourceStream(String.Format("Norwindt.Views.{0}.{1}.html", Me.ControllerName, view))
                    If (Not (stream) Is Nothing) Then
                        Using sr = New StreamReader(stream)
                            viewLayout = sr.ReadToEnd()
                        End Using
                    End If
                End If
                If ((Not (viewLayout) Is Nothing) AndAlso Regex.IsMatch(viewLayout, "^\s*\w+\.\w+\.html\s*$", RegexOptions.IgnoreCase)) Then
                    fileName = viewLayout
                Else
                    tryLoad = false
                End If
            Loop
            Return viewLayout
        End Function
        
        Public Function ToJson() As String
            Dim config = Me.Virtualize(Me.ControllerName)
            Complete()
            Dim ruleIterator = config.Select("/c:dataController/c:businessRules/c:rule")
            Dim newOnServer = false
            Dim calculateOnServer = false
            Do While ruleIterator.MoveNext()
                Dim type = ruleIterator.Current.GetAttribute("type", String.Empty)
                Dim commandName = ruleIterator.Current.GetAttribute("commandName", String.Empty)
                If Not ((type = "JavaScript")) Then
                    If ((commandName = "New") AndAlso Not (newOnServer)) Then
                        newOnServer = true
                        config.SelectSingleNode("/c:dataController").CreateAttribute(String.Empty, "newOnServer", Nothing, "true")
                    Else
                        If ((commandName = "Calculate") AndAlso Not (calculateOnServer)) Then
                            calculateOnServer = true
                            config.SelectSingleNode("/c:dataController").CreateAttribute(String.Empty, "calculateOnServer", Nothing, "true")
                        End If
                    End If
                End If
            Loop
            Dim expressions = JArray.FromObject(Me.Expressions).ToString()
            Dim exceptions = New String() {"//comment()", "c:dataController/c:commands", "c:dataController/@handler", "//c:field/c:formula", "//c:businessRules/c:rule[@type=""Code"" or @type=""Sql"" or @type=""Email""]", "//c:businessRules/c:rule/text()", "//c:validate", "//c:styles", "//c:visibility", "//c:readOnly", "//c:expression", "//c:blobAdapterConfig"}
            For Each ex in exceptions
                Dim toDelete = New List(Of XPathNavigator)()
                Dim iterator = config.Select(ex)
                Do While iterator.MoveNext()
                    toDelete.Add(iterator.Current.Clone())
                Loop
                For Each node in toDelete
                    node.DeleteSelf()
                Next
            Next
            'special case of items/item serialization
            Dim itemsIterator = config.Select("//c:items[c:item]")
            Do While itemsIterator.MoveNext()
                Dim lovBuilder = New StringBuilder("<list>")
                Dim itemIterator = itemsIterator.Current.SelectChildren(XPathNodeType.Element)
                Do While itemIterator.MoveNext()
                    lovBuilder.Append(itemIterator.Current.OuterXml)
                Loop
                lovBuilder.Append("</list>")
                itemsIterator.Current.InnerXml = lovBuilder.ToString()
            Loop
            'load custom view layouts
            Dim viewIterator = config.Select("//c:views/c:view")
            Do While viewIterator.MoveNext()
                Dim layout = LoadLayout(viewIterator.Current.GetAttribute("id", String.Empty))
                If Not (String.IsNullOrEmpty(layout)) Then
                    viewIterator.Current.AppendChild(String.Format("<layout><![CDATA[{0}]]></layout>", layout))
                End If
            Loop
            'extend JSON with "expressions"
            Dim json = XmlConverter.ToJson(config.Navigator, "dataController", true, true, "commands", "output", "fields", "views", "categories", "dataFields", "actions", "actionGroup", "businessRules", "list")
            Dim eof = Regex.Match(json, "\}\s*\}\s*$")
            json = (json.Substring(0, eof.Index)  _
                        + (",""expressions"":"  _
                        + (expressions + eof.Value)))
            Return json
        End Function
    End Class
    
    Public Class ControllerConfigurationUtility
        
        Private Shared m_AssemblyResources As SortedDictionary(Of String, String)
        
        Private m_RawConfiguration As String
        
        Private m_Variables As SortedDictionary(Of String, String)
        
        Shared Sub New()
            m_AssemblyResources = New SortedDictionary(Of String, String)()
            Dim a = GetType(ControllerConfigurationUtility).Assembly
            For Each resource in a.GetManifestResourceNames()
                m_AssemblyResources(resource.ToLowerInvariant()) = resource
            Next
        End Sub
        
        Public Sub New(ByVal rawConfiguration As String, ByVal variables As SortedDictionary(Of String, String))
            MyBase.New
            m_RawConfiguration = rawConfiguration
            m_Variables = variables
        End Sub
        
        Public Function ReplaceVariables() As String
            Return ControllerConfiguration.VariableReplacementRegex.Replace(m_RawConfiguration, AddressOf DoReplace)
        End Function
        
        Private Function DoReplace(ByVal m As Match) As String
            If (m.Groups(1).Value = m.Groups(3).Value) Then
                Dim s As String = Nothing
                If m_Variables.TryGetValue(m.Groups(1).Value, s) Then
                    Return s
                Else
                    Return m.Groups(2).Value
                End If
            End If
            Return m.Value
        End Function
        
        Public Overloads Shared Function GetResourceStream(ByVal ParamArray resourceNames() as string) As Stream
            Dim name As String = Nothing
            Return GetResourceStream(name, resourceNames)
        End Function
        
        Public Overloads Shared Function GetResourceStream(ByRef resourceName As String, ByVal ParamArray resourceNames() as string) As Stream
            Dim a = GetType(ControllerConfigurationUtility).Assembly
            resourceName = Nothing
            For Each resource in resourceNames
                If m_AssemblyResources.TryGetValue(resource.ToLowerInvariant(), resourceName) Then
                    Return a.GetManifestResourceStream(resourceName)
                End If
            Next
            Return Nothing
        End Function
        
        Public Shared Function GetResourceText(ByVal ParamArray resourceNames() as string) As String
            Dim name = String.Empty
            Dim res = GetResourceStream(name, resourceNames)
            If (res Is Nothing) Then
                Return Nothing
            End If
            Using sr = New StreamReader(res)
                Return Localizer.Replace(String.Empty, name, sr.ReadToEnd())
            End Using
        End Function
        
        Public Shared Function GetFilePath(ByVal ParamArray paths() as string) As String
            For Each path in paths
                If File.Exists(path) Then
                    Return path
                End If
            Next
            Return Nothing
        End Function
        
        Public Shared Function GetFileText(ByVal ParamArray paths() as string) As String
            Dim p = GetFilePath(paths)
            If Not (String.IsNullOrEmpty(p)) Then
                Return Localizer.Replace(Path.GetDirectoryName(p), Path.GetFileName(p), File.ReadAllText(p))
            End If
            Return Nothing
        End Function
    End Class
    
    Public Class XmlConverter
        
        Private m_Navigator As XPathNavigator
        
        Private m_Arrays() As String = Nothing
        
        Private m_RenderMetadata As Boolean = false
        
        Private m_Root As String
        
        Private m_Sb As StringBuilder
        
        Private m_ExplicitElementValues As Boolean
        
        Public Sub New(ByVal navigator As XPathNavigator, ByVal root As String, ByVal metadata As Boolean, ByVal explicitElementValues As Boolean, ByVal arrays() As String)
            MyBase.New
            m_Navigator = navigator
            m_Root = root
            m_RenderMetadata = metadata
            m_Arrays = arrays
            m_ExplicitElementValues = explicitElementValues
            If String.IsNullOrEmpty(root) Then
                'cycle to the first element with a name
                Do While (String.IsNullOrEmpty(navigator.Name) AndAlso navigator.MoveToFirstChild())
                Loop
                m_Root = navigator.Name
            End If
        End Sub
        
        Public Overloads Shared Function ToJson(ByVal navigator As XPathNavigator, ByVal root As String, ByVal metadata As Boolean, ByVal explicitElementValues As Boolean, ByVal ParamArray arrays() as System.[String]) As String
            Dim xmlc = New XmlConverter(navigator, root, metadata, explicitElementValues, arrays)
            Return xmlc.ToJson()
        End Function
        
        Public Overloads Function ToJson() As String
            Dim nav = m_Navigator
            m_Sb = New StringBuilder("{"&Global.Microsoft.VisualBasic.ChrW(10))
            Do While (Not ((nav.Name = m_Root)) AndAlso nav.MoveToFirstChild())
            Loop
            XmlToJson(nav, false, 1)
            m_Sb.AppendLine(""&Global.Microsoft.VisualBasic.ChrW(10)&"}")
            Return m_Sb.ToString()
        End Function
        
        Private Sub WriteJsonValue(ByVal nav As XPathNavigator)
            Dim v = nav.ToString()
            Dim tempInt32 As Integer
            If Integer.TryParse(v, tempInt32) Then
                m_Sb.Append(tempInt32)
            Else
                Dim tempBool As Boolean
                If Boolean.TryParse(v, tempBool) Then
                    m_Sb.Append(tempBool.ToString().ToLower())
                Else
                    m_Sb.Append(HttpUtility.JavaScriptStringEncode(v, true))
                End If
            End If
        End Sub
        
        Private Sub WriteMultilineValue(ByVal nav As XPathNavigator)
            Dim type As String = Nothing
            Dim props = nav.CreateNavigator()
            Dim keepGoing = true
            Do While keepGoing
                props.MoveToParent()
                If props.MoveToFirstAttribute() Then
                    keepGoing = false
                End If
            Loop
            keepGoing = true
            Do While keepGoing
                If (props.Name = "type") Then
                    type = props.Value
                End If
                If Not (props.MoveToNextAttribute()) Then
                    keepGoing = false
                End If
            Loop
            If String.IsNullOrEmpty(type) Then
                WriteJsonValue(nav)
            Else
                props.MoveToRoot()
                props.MoveToFirstChild()
                WriteJsonValue(nav)
            End If
        End Sub
        
        Private Sub XmlToJson(ByVal nav As XPathNavigator, ByVal isArrayMember As Boolean, ByVal depth As Integer)
            Dim padding = New String(Global.Microsoft.VisualBasic.ChrW(32), (depth * 2))
            Dim isArray = m_Arrays.Contains(nav.Name)
            Dim isComplexArray = (isArray AndAlso nav.HasAttributes)
            Dim closingBracket = true
            Dim hasAttributes = nav.HasAttributes
            Dim isEmpty = ((Not (hasAttributes) AndAlso Not (nav.HasChildren)) AndAlso (nav.IsEmptyElement OrElse String.IsNullOrEmpty(nav.InnerXml.Trim())))
            If Not (isComplexArray) Then
                If Not (isArrayMember) Then
                    m_Sb.AppendFormat((padding + """{0}"": "), nav.Name)
                    If nav.MoveToFirstChild() Then
                        If ((nav.NodeType = XPathNodeType.Text) AndAlso Not (hasAttributes)) Then
                            closingBracket = false
                        End If
                        nav.MoveToParent()
                    End If
                End If
                If closingBracket Then
                    If isArray Then
                        m_Sb.AppendLine("[")
                    Else
                        If Not (isArrayMember) Then
                            If isEmpty Then
                                m_Sb.Append("null")
                            Else
                                m_Sb.AppendLine("{")
                            End If
                        Else
                            m_Sb.AppendLine((padding + "{"))
                        End If
                    End If
                End If
            End If
            Dim firstProp = true
            Dim childPadding = (padding + "  ")
            Dim keepGoing As Boolean
            If (isComplexArray AndAlso isArrayMember) Then
                m_Sb.AppendLine((padding + "{"))
            End If
            If nav.MoveToFirstAttribute() Then
                keepGoing = true
                Do While keepGoing
                    If firstProp Then
                        firstProp = false
                    Else
                        m_Sb.AppendLine(",")
                    End If
                    m_Sb.AppendFormat((childPadding + """{0}"": "), nav.Name)
                    WriteJsonValue(nav)
                    If Not (nav.MoveToNextAttribute()) Then
                        keepGoing = false
                    End If
                Loop
                nav.MoveToParent()
                If isComplexArray Then
                    m_Sb.AppendLine(",")
                    m_Sb.AppendFormat((childPadding + """{0}"": ["&Global.Microsoft.VisualBasic.ChrW(10)), nav.Name)
                    firstProp = true
                End If
            End If
            If nav.MoveToFirstChild() Then
                If (nav.NodeType = XPathNodeType.Text) Then
                    Dim hasParentWithoutAttributes = false
                    If isArrayMember Then
                        m_Sb.AppendLine(",")
                        m_Sb.Append((childPadding + """@text"": "))
                    Else
                        Dim parent = nav.Clone()
                        parent.MoveToParent()
                        hasParentWithoutAttributes = Not (parent).HasAttributes
                        If (Not (hasParentWithoutAttributes) OrElse m_ExplicitElementValues) Then
                            If hasAttributes Then
                                m_Sb.AppendLine(",")
                            Else
                                m_Sb.AppendLine(" {")
                            End If
                            m_Sb.Append((childPadding + """@value"": "))
                        End If
                    End If
                    If nav.Value.Contains(""&Global.Microsoft.VisualBasic.ChrW(10)) Then
                        WriteMultilineValue(nav)
                    Else
                        WriteJsonValue(nav)
                    End If
                    If (Not (isArrayMember) AndAlso (hasParentWithoutAttributes AndAlso m_ExplicitElementValues)) Then
                        m_Sb.Append((""&Global.Microsoft.VisualBasic.ChrW(10)  _
                                        + (padding + "}")))
                    End If
                Else
                    keepGoing = true
                    Do While keepGoing
                        If firstProp Then
                            firstProp = false
                        Else
                            m_Sb.AppendLine(",")
                        End If
                        XmlToJson(nav, isArray, (depth + 1))
                        If Not (nav.MoveToNext()) Then
                            keepGoing = false
                        End If
                    Loop
                End If
                nav.MoveToParent()
            End If
            If closingBracket Then
                If Not (isEmpty) Then
                    m_Sb.AppendLine()
                End If
                If isComplexArray Then
                    m_Sb.Append((padding + "  ]"))
                Else
                    If isArray Then
                        m_Sb.Append((padding + "]"))
                    Else
                        If Not (isEmpty) Then
                            m_Sb.Append((padding + "}"))
                        End If
                    End If
                End If
            End If
            If (isComplexArray AndAlso isArrayMember) Then
                m_Sb.Append((""&Global.Microsoft.VisualBasic.ChrW(10)  _
                                + (padding + "}")))
            End If
            If nav.MoveToNext() Then
                m_Sb.AppendLine(",")
                XmlToJson(nav, isArrayMember, depth)
            End If
        End Sub
    End Class
End Namespace
