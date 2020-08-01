Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.Common
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Data
    
    Public Class ControllerNodeSet
        
        Private m_Navigator As XPathNavigator
        
        Private m_Resolver As XmlNamespaceManager
        
        Private m_Nodes As List(Of XPathNavigator)
        
        Private m_Args() As Object
        
        Private m_ArgIndex As Integer
        
        Private Shared m_VariableRegex As Regex = New Regex("\$\w+")
        
        Private Shared m_ElementNameRegex As Regex = New Regex("(?'String'""([\s\S]+?)"")|(?'String'\'([\s\S]+?)\')|(?'AttrOrVar'(@|\$)\w+)|(?'Func"& _ 
                "tion'\w+\s*\()|(?'Element'(?'Axis'[\w-]+::)?(?'Namespace'\w+\:)?(([\w-]+::\*)|(?"& _ 
                "'Name'[a-z]\w*)))", RegexOptions.IgnoreCase)
        
        Private Shared m_KeywordRegex As Regex = New Regex("^(or|and|mod|div|[\w-]+::\*)$", RegexOptions.IgnoreCase)
        
        Private Shared m_CreateElementRegex As Regex = New Regex("\<(\w+)/?\>$")
        
        Private Shared m_NamespaceRegex As Regex = New Regex("^\w+::")
        
        Private m_Current As Nullable(Of Integer)
        
        Public Sub New(ByVal nodeSet As ControllerNodeSet)
            Me.New(nodeSet.m_Navigator, nodeSet.m_Resolver)
        End Sub
        
        Public Sub New(ByVal navigator As XPathNavigator, ByVal resolver As XmlNamespaceManager)
            MyBase.New
            Me.m_Navigator = navigator
            Me.m_Resolver = resolver
            m_Nodes = New List(Of XPathNavigator)()
        End Sub
        
        Public Sub New(ByVal nodeSet As ControllerNodeSet, ByVal node As XPathNavigator)
            Me.New(nodeSet, New List(Of XPathNavigator)())
            m_Nodes.Add(node)
        End Sub
        
        Public Sub New(ByVal nodeSet As ControllerNodeSet, ByVal nodes As List(Of XPathNavigator))
            MyBase.New
            Me.m_Navigator = nodeSet.m_Navigator
            Me.m_Resolver = nodeSet.m_Resolver
            Me.m_Nodes = nodes
        End Sub
        
        Public ReadOnly Property Nodes() As List(Of XPathNavigator)
            Get
                Return m_Nodes
            End Get
        End Property
        
        ''' <summary>
        ''' Returns true if the node set is empty.
        ''' </summary>
        Public ReadOnly Property IsEmpty() As Boolean
            Get
                Return (m_Nodes.Count = 0)
            End Get
        End Property
        
        Public ReadOnly Property Current() As ControllerNodeSet
            Get
                If (Not (m_Current.HasValue) OrElse (m_Nodes.Count <= m_Current)) Then
                    Return Nothing
                End If
                Return New ControllerNodeSet(Me, m_Nodes(m_Current.Value))
            End Get
        End Property
        
        Public Overrides Function ToString() As String
            If ((Not (m_Nodes) Is Nothing) AndAlso (m_Nodes.Count = 1)) Then
                Return m_Nodes(0).Value
            End If
            Return MyBase.ToString()
        End Function
        
        Private Function DoReplaceVariable(ByVal m As Match) As String
            Dim o = m_Args(m_ArgIndex)
            m_ArgIndex = (m_ArgIndex + 1)
            Return String.Format("'{0}'", o)
        End Function
        
        Private Function DoReplaceElementName(ByVal m As Match) As String
            Dim name = m.Groups("Name").Value
            Dim axis = m.Groups("Axis").Value
            Dim ns = m.Groups("Namespace").Value
            If ((Not (String.IsNullOrEmpty(name)) AndAlso String.IsNullOrEmpty(ns)) AndAlso Not (m_KeywordRegex.IsMatch(m.Value))) Then
                Return String.Format("{0}c:{1}", axis, name)
            End If
            Return m.Value
        End Function
        
        ''' <summary>
        ''' Adds selected nodes to the current node set.
        ''' </summary>
        ''' <param name="selector">XPath expression evaluated against the definition of the data controller. May contain variables.</param>
        ''' <param name="args">Optional values of variables. If variables are specified then the expression is evaluated for each variable or group of variables specified in the selector.</param>
        ''' <example>field[@name=$name]</example>
        ''' <returns>Returns a combined nodeset.</returns>
        Public Function Add(ByVal selector As String, ByVal ParamArray args() as System.[Object]) As ControllerNodeSet
            Return InternalSelect(true, selector, args)
        End Function
        
        ''' <summary>
        ''' Selects a node set containing zero or more XML nodes from the data controller definition.
        ''' </summary>
        ''' <param name="selector">XPath expression evaluated against the definition of the data controller. May contain variables.</param>
        ''' <param name="args">Optional values of variables. If variables are specified then the expression is evaluated for each variable or group of variables specified in the selector.</param>
        ''' <example>field[@name=$name]</example>
        ''' <returns>A node set containing selected data controller nodes.</returns>
        Public Function [Select](ByVal selector As String, ByVal ParamArray args() as System.[Object]) As ControllerNodeSet
            Dim m = m_CreateElementRegex.Match(selector)
            If m.Success Then
                Dim document = New XmlDocument()
                document.LoadXml(String.Format("<{0}/>", m.Groups(1).Value))
                Return New ControllerNodeSet(Me, document.FirstChild.CreateNavigator())
            Else
                Return InternalSelect(false, selector, args)
            End If
        End Function
        
        Private Function InternalSelect(ByVal add As Boolean, ByVal selector As String, ByVal ParamArray args() as System.[Object]) As ControllerNodeSet
            m_ArgIndex = 0
            selector = m_ElementNameRegex.Replace(selector, AddressOf DoReplaceElementName)
            Dim list = New List(Of XPathNavigator)()
            If add Then
                list.AddRange(m_Nodes)
            End If
            Dim rootNodes = m_Nodes
            If ((rootNodes.Count = 0) OrElse add) Then
                rootNodes = New List(Of XPathNavigator)()
                rootNodes.Add(m_Navigator)
                If ([Char].IsLetter(selector, 0) AndAlso Not (m_NamespaceRegex.IsMatch(selector))) Then
                    selector = ("//" + selector)
                End If
            Else
                If ([Char].IsLetter(selector, 0) AndAlso Not (m_NamespaceRegex.IsMatch(selector))) Then
                    selector = (".//" + selector)
                End If
            End If
            For Each root in rootNodes
                If (args.Length > 0) Then
                    m_Args = args
                    Do While (m_ArgIndex < args.Length)
                        Dim xpath = m_VariableRegex.Replace(selector, AddressOf DoReplaceVariable)
                        Dim iterator = root.Select(xpath, m_Resolver)
                        Do While iterator.MoveNext()
                            list.Add(iterator.Current.Clone())
                        Loop
                    Loop
                Else
                    Dim iterator = root.Select(selector, m_Resolver)
                    Do While iterator.MoveNext()
                        list.Add(iterator.Current.Clone())
                    Loop
                End If
            Next
            Return New ControllerNodeSet(Me, list)
        End Function
        
        ''' <summary>
        ''' Deletes all nodes in the node set from the data controller definition.
        ''' </summary>
        ''' <returns>An empty node set.</returns>
        Public Function Delete() As ControllerNodeSet
            For Each node in m_Nodes
                node.DeleteSelf()
            Next
            Return New ControllerNodeSet(m_Navigator, m_Resolver)
        End Function
        
        ''' <summary>
        ''' Selects the value of the attribute with the specified name from all nodes in the node set.
        ''' </summary>
        ''' <param name="name">The name of the XML attribute.</param>
        ''' <returns>The collection of the XML nodes representing values of the specified attribute.</returns>
        Public Overloads Function Attr(ByVal name As String) As ControllerNodeSet
            Return InternalSelect(false, ("@" + name))
        End Function
        
        ''' <summary>
        ''' Assigns the value to the attribute with the specified name for all nodes in the node set.
        ''' </summary>
        ''' <param name="name">The name of the XML attribute.</param>
        ''' <param name="value">The value of the XML attribute.</param>
        ''' <returns></returns>
        Public Overloads Function Attr(ByVal name As String, ByVal value As Object) As ControllerNodeSet
            Dim s = Convert.ToString(value)
            If TypeOf value Is Boolean Then
                s = s.ToLower()
            End If
            For Each nav in m_Nodes
                Dim attrNav = nav.SelectSingleNode(("@" + name))
                If (Not (attrNav) Is Nothing) Then
                    attrNav.SetValue(s)
                Else
                    nav.CreateAttribute(String.Empty, name, String.Empty, s)
                End If
            Next
            Return Me
        End Function
        
        ''' <summary>
        ''' Appends a collection specified by the argument to each node in the node sets.
        ''' </summary>
        ''' <param name="nodeSet">The collection of child nodes.</param>
        ''' <returns>The collection of child nodes after they were appended to the nodes in the original node set.</returns>
        Public Overloads Function AppendTo(ByVal nodeSet As ControllerNodeSet) As ControllerNodeSet
            For Each node in Me.m_Nodes
                For Each parentNode in nodeSet.m_Nodes
                    parentNode.AppendChild(node.OuterXml)
                Next
            Next
            Return nodeSet
        End Function
        
        ''' <summary>
        ''' Appends a collection specified by the argument to each node in the node sets.
        ''' </summary>
        ''' <param name="selector">XPath expression evaluated against the definition of the data controller. May contain variables.</param>
        ''' <param name="args">Optional values of variables. If variables are specified then the expression is evaluated for each variable or group of variables specified in the selector.</param>
        ''' <example>field[@name=$name]</example>
        ''' <returns>The collection of child nodes after they were appended to the nodes in the original node set.</returns>
        Public Overloads Function AppendTo(ByVal selector As String, ByVal ParamArray args() as System.[Object]) As ControllerNodeSet
            Return AppendTo(New ControllerNodeSet(m_Navigator, m_Resolver).Select(selector, args))
        End Function
        
        Public Function Arrange(ByVal selector As String, ByVal ParamArray sequence() as System.[String]) As ControllerNodeSet
            Dim i = (sequence.Length - 1)
            Do While (i >= 0)
                For Each node in m_Nodes
                    Dim seqNav = node.SelectSingleNode(selector, m_Resolver)
                    If ((Not (seqNav) Is Nothing) AndAlso (seqNav.Value = sequence(i))) Then
                        Dim sibling = node.Clone()
                        sibling.MoveToParent()
                        sibling.MoveToFirstChild()
                        If Not (sibling.IsSamePosition(node)) Then
                            sibling.InsertBefore(node)
                            node.DeleteSelf()
                        End If
                        Exit For
                    End If
                Next
                'continue to the next value in sequence
                i = (i - 1)
            Loop
            Return Me
        End Function
        
        Public Overloads Function Elem(ByVal name As String) As ControllerNodeSet
            Return InternalSelect(false, name)
        End Function
        
        Public Overloads Function Elem(ByVal name As String, ByVal value As Object) As ControllerNodeSet
            Dim s = Convert.ToString(value)
            Dim selector = ("c:" + name)
            For Each node in m_Nodes
                Dim elemNav = node.SelectSingleNode(selector, m_Resolver)
                If (elemNav Is Nothing) Then
                    node.AppendChild(String.Format("<{0}/>", name))
                    elemNav = node.SelectSingleNode(selector, m_Resolver)
                End If
                elemNav.SetValue(s)
            Next
            Return Me
        End Function
        
        Private Function SelectInContext(ByVal contextNode As String, ByVal selector As String, ByVal ParamArray args() as System.[Object]) As ControllerNodeSet
            For Each node in m_Nodes
                If (node.Name = contextNode) Then
                    node.MoveToParent()
                    Return InternalSelect(false, selector, args)
                End If
            Next
            Return InternalSelect(false, selector, args)
        End Function
        
        ''' <summary>
        ''' Select the data controller field node.
        ''' </summary>
        ''' <param name="name">The name of the field.</param>
        ''' <returns></returns>
        Public Function SelectField(ByVal name As String) As ControllerNodeSet
            Return NodeSet().InternalSelect(false, String.Format("/dataController/fields/field[@name='{0}']", name))
        End Function
        
        Public Function SelectCommand(ByVal id As String) As ControllerNodeSet
            Return NodeSet().InternalSelect(false, String.Format("/dataController/commands/command[@id='{0}']", id))
        End Function
        
        Public Function SelectViews(ByVal ParamArray identifiers() as System.[String]) As ControllerNodeSet
            If (identifiers.Length = 0) Then
                Return NodeSet().Select("/dataController/views/view")
            End If
            Dim searchByType = false
            For Each s in New String() {"Grid", "Form", "DataSheet", "Chart", "Tree"}
                If Not ((Array.IndexOf(identifiers, s) = -1)) Then
                    searchByType = true
                    Exit For
                End If
            Next
            If searchByType Then
                Return NodeSet().Select("/dataController/views/view[@type=$type]", identifiers)
            End If
            Return NodeSet().Select("/dataController/views/view[@id=$id]", identifiers)
        End Function
        
        ''' <summary>
        ''' Creates an empty data controller node set.
        ''' </summary>
        ''' <returns>Returns an empty data controller node set.</returns>
        Public Function NodeSet() As ControllerNodeSet
            Return New ControllerNodeSet(Me)
        End Function
        
        Public Function SelectView(ByVal id As String) As ControllerNodeSet
            Return NodeSet().Select(String.Format("/dataController/views/view[@id='{0}']", id))
        End Function
        
        Public Function SelectDataFields(ByVal ParamArray fieldNames() as System.[String]) As ControllerNodeSet
            Dim list = New List(Of XPathNavigator)()
            For Each node in m_Nodes
                Dim nodeSet = New ControllerNodeSet(Me, node)
                If (fieldNames.Length = 0) Then
                    list.AddRange(nodeSet.SelectInContext("dataField", "dataField").Nodes)
                Else
                    list.AddRange(nodeSet.SelectInContext("dataField", "dataField[@fieldName=$fieldName]", fieldNames).Nodes)
                End If
            Next
            Return New ControllerNodeSet(Me, list)
        End Function
        
        Public Function SelectDataField(ByVal fieldName As String) As ControllerNodeSet
            Return SelectDataFields(fieldName)
        End Function
        
        Public Function SelectCategory(ByVal id As String) As ControllerNodeSet
            Return SelectInContext("category", String.Format("category[@id='{0}']", id))
        End Function
        
        Public Function SelectAction(ByVal id As String) As ControllerNodeSet
            Return SelectInContext("action", String.Format("action[@id='{0}']", id))
        End Function
        
        Public Function SelectActions(ByVal ParamArray commandNames() as System.[String]) As ControllerNodeSet
            If (commandNames.Length = 0) Then
                Return SelectInContext("action", "action")
            End If
            Dim commandNameList = New List(Of String)(commandNames)
            If commandNameList.Contains("CHANGE") Then
                commandNameList.Remove("CHANGE")
                commandNameList.Add("Edit")
                commandNameList.Add("BatchEdit")
                commandNameList.Add("New")
                commandNameList.Add("Delete")
                commandNameList.Add("Update")
                commandNameList.Add("Insert")
                commandNameList.Add("Import")
                commandNameList.Add("Duplicate")
            End If
            If commandNameList.Contains("NEW") Then
                commandNameList.Remove("NEW")
                commandNameList.Add("New")
                commandNameList.Add("Insert")
                commandNameList.Add("Import")
                commandNameList.Add("Duplicate")
            End If
            If commandNameList.Contains("EDIT") Then
                commandNameList.Remove("EDIT")
                commandNameList.Add("Edit")
                commandNameList.Add("BatchEdit")
                commandNameList.Add("Update")
            End If
            If commandNameList.Contains("EXPORT") Then
                commandNameList.Remove("EXPORT")
                commandNameList.Add("ExportCsv")
                commandNameList.Add("ExportRss")
                commandNameList.Add("ExportRowset")
            End If
            If commandNameList.Contains("REPORT") Then
                commandNameList.Remove("REPORT")
                commandNameList.Add("Report")
                commandNameList.Add("ReportAsPdf")
                commandNameList.Add("ReportAsImage")
                commandNameList.Add("ReportAsExcel")
                commandNameList.Add("ReportAsWord")
            End If
            Return SelectInContext("action", "action[@commandName=$commandName]", commandNameList.ToArray())
        End Function
        
        Public Function SelectCustomAction(ByVal commandArgument As String) As ControllerNodeSet
            Return SelectCustomActions(commandArgument)
        End Function
        
        Public Function SelectCustomActions(ByVal ParamArray commandArguments() as System.[String]) As ControllerNodeSet
            If (commandArguments.Length = 0) Then
                Return SelectInContext("action", "action[@commandName='Custom']")
            End If
            Return SelectInContext("action", "action[@commandName='Custom' and @commandArgument=$commandArgument]", commandArguments)
        End Function
        
        Public Function SelectActionGroups(ByVal ParamArray scopes() as System.[String]) As ControllerNodeSet
            If (scopes.Length = 0) Then
                Return SelectInContext("actionGroup", "actionGroup")
            Else
                Return SelectInContext("actionGroup", "actionGroup[@scope=$scope]", scopes)
            End If
        End Function
        
        Public Function SelectActionGroup(ByVal id As String) As ControllerNodeSet
            Return SelectInContext("actionGroup", String.Format("actionGroup[@id='{0}']", id))
        End Function
        
        Private Function SetProperty(ByVal name As String, ByVal value As Object, ByVal ParamArray requiresElement() as System.[String]) As ControllerNodeSet
            For Each node in m_Nodes
                Dim nodeSet = New ControllerNodeSet(Me, node)
                If (Array.IndexOf(requiresElement, node.Name) >= 0) Then
                    nodeSet.Elem(name, value)
                Else
                    nodeSet.Attr(name, value)
                End If
            Next
            Return Me
        End Function
        
        ''' <summary>
        ''' Restricts access to the field or action to a list of comma-separated roles.
        ''' </summary>
        ''' <param name="roles">The list of comma-separated roles.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetRoles(ByVal roles As String) As ControllerNodeSet
            Return SetProperty("roles", roles)
        End Function
        
        ''' <summary>
        ''' Restricts 'write' access to the field to the list of comma-separated roles.
        ''' </summary>
        ''' <param name="writeRoles">The list of comma-separated roles.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetWriteRoles(ByVal writeRoles As String) As ControllerNodeSet
            Return SetProperty("writeRoles", writeRoles)
        End Function
        
        Public Overridable Function SetTag(ByVal value As String) As ControllerNodeSet
            Dim attributeName = "tag"
            If ((m_Nodes.Count > 0) AndAlso (m_Nodes(0).Name = "view")) Then
                attributeName = "tags"
            End If
            Dim tagList = Attr(attributeName).Value()
            If (Not (String.IsNullOrEmpty(tagList)) AndAlso Not (String.IsNullOrEmpty(value))) Then
                tagList = (tagList + " ")
                value = (tagList + value)
            End If
            Return Attr(attributeName, value)
        End Function
        
        Public Function SetHeaderText(ByVal headerText As String) As ControllerNodeSet
            Return SetProperty("headerText", headerText, "dataField", "view")
        End Function
        
        Public Function SetFooterText(ByVal footerText As String) As ControllerNodeSet
            Return SetProperty("footerText", footerText, "dataField", "view")
        End Function
        
        Public Function SetLabel(ByVal label As String) As ControllerNodeSet
            Return SetProperty("label", label)
        End Function
        
        Public Function SetSortExpression(ByVal sortExpression As String) As ControllerNodeSet
            Return SetProperty("sortExpression", sortExpression)
        End Function
        
        Public Function SetGroupExpression(ByVal groupExpression As String) As ControllerNodeSet
            Return SetProperty("groupExpression", groupExpression)
        End Function
        
        Public Function SetFilter(ByVal filter As String) As ControllerNodeSet
            Return SetProperty("filter", filter)
        End Function
        
        Public Function SetGroup(ByVal group As String) As ControllerNodeSet
            Return SetProperty("group", group)
        End Function
        
        Public Overloads Function SetShowInSelector(ByVal showInSelector As Boolean) As ControllerNodeSet
            Return SetProperty("showInSelector", showInSelector.ToString().ToLower())
        End Function
        
        Public Overloads Function SetShowInSelector(ByVal showInSelector As String) As ControllerNodeSet
            Return SetShowInSelector(Convert.ToBoolean(showInSelector))
        End Function
        
        Public Function SetReportFont(ByVal reportFont As String) As ControllerNodeSet
            Return SetProperty("reportFont", reportFont)
        End Function
        
        Public Function SetReportLabel(ByVal reportLabel As String) As ControllerNodeSet
            Return SetProperty("reportLabel", reportLabel)
        End Function
        
        Public Function SetReportOrientation(ByVal reportOrientation As String) As ControllerNodeSet
            Return SetProperty("reportOrientation", reportOrientation)
        End Function
        
        Public Function SetReportTemplate(ByVal reportTemplate As String) As ControllerNodeSet
            Return SetProperty("reportTemplate", reportTemplate)
        End Function
        
        Public Overloads Function SetHidden(ByVal hidden As Boolean) As ControllerNodeSet
            Return SetProperty("hidden", hidden.ToString().ToLower())
        End Function
        
        Public Overloads Function SetHidden(ByVal hidden As String) As ControllerNodeSet
            Return SetHidden(Convert.ToBoolean(hidden))
        End Function
        
        Public Overloads Function SetReadOnly(ByVal [readOnly] As Boolean) As ControllerNodeSet
            Return SetProperty("readOnly", [readOnly].ToString().ToLower())
        End Function
        
        Public Overloads Function SetReadOnly(ByVal [readOnly] As String) As ControllerNodeSet
            Return SetReadOnly(Convert.ToBoolean([readOnly]))
        End Function
        
        Public Overloads Function SetFormatOnClient(ByVal formatOnClient As Boolean) As ControllerNodeSet
            Return SetProperty("formatOnClient", formatOnClient.ToString().ToLower())
        End Function
        
        Public Overloads Function SetFormatOnClient(ByVal formatOnClient As String) As ControllerNodeSet
            Return SetFormatOnClient(Convert.ToBoolean(formatOnClient))
        End Function
        
        Public Function SetCommandName(ByVal commandName As String) As ControllerNodeSet
            Return SetProperty("commandName", commandName)
        End Function
        
        Public Function SetCommandArgument(ByVal commandArgument As String) As ControllerNodeSet
            Return SetProperty("commandArgument", commandArgument)
        End Function
        
        Public Function SetConfirmation(ByVal confirmation As String) As ControllerNodeSet
            Return SetProperty("confirmation", confirmation)
        End Function
        
        Public Function SetType(ByVal type As String) As ControllerNodeSet
            Return SetProperty("type", type)
        End Function
        
        Public Function SetScope(ByVal scope As String) As ControllerNodeSet
            Return SetProperty("scope", scope)
        End Function
        
        Public Overloads Function SetFlat(ByVal flat As Boolean) As ControllerNodeSet
            Return SetProperty("flat", flat.ToString().ToLower())
        End Function
        
        Public Overloads Function SetFlat(ByVal flat As String) As ControllerNodeSet
            Return SetFlat(Convert.ToBoolean(flat))
        End Function
        
        Public Overloads Function SetNewColumn(ByVal newColumn As Boolean) As ControllerNodeSet
            Return SetProperty("newColumn", newColumn.ToString().ToLower())
        End Function
        
        Public Overloads Function SetNewColumn(ByVal newColumn As String) As ControllerNodeSet
            Return SetNewColumn(Convert.ToBoolean(newColumn))
        End Function
        
        Public Overloads Function SetFloating(ByVal floating As Boolean) As ControllerNodeSet
            Return SetProperty("floating", floating.ToString().ToLower())
        End Function
        
        Public Overloads Function SetFloating(ByVal floating As String) As ControllerNodeSet
            Return SetFloating(Convert.ToBoolean(floating))
        End Function
        
        Public Function SetTab(ByVal tab As String) As ControllerNodeSet
            Return SetProperty("tab", tab)
        End Function
        
        Public Function SetDescription(ByVal description As String) As ControllerNodeSet
            Return SetProperty("description", description, "category")
        End Function
        
        Public Overloads Function SetColumns(ByVal columns As Integer) As ControllerNodeSet
            Return SetProperty("columns", columns)
        End Function
        
        Public Overloads Function SetColumns(ByVal columns As String) As ControllerNodeSet
            Return SetColumns(Convert.ToInt32(columns))
        End Function
        
        Public Overloads Function SetLength(ByVal length As Integer) As ControllerNodeSet
            Return SetProperty("length", length)
        End Function
        
        Public Overloads Function SetLength(ByVal length As String) As ControllerNodeSet
            Return SetLength(Convert.ToInt32(length))
        End Function
        
        Public Overloads Function SetRows(ByVal rows As Integer) As ControllerNodeSet
            Return SetProperty("rows", rows)
        End Function
        
        Public Overloads Function SetRows(ByVal rows As String) As ControllerNodeSet
            Return SetRows(Convert.ToInt32(rows))
        End Function
        
        Public Function SetDataFormatString(ByVal dataFormatString As String) As ControllerNodeSet
            Return SetProperty("dataFormatString", dataFormatString)
        End Function
        
        Public Function SetTextMode(ByVal textMode As String) As ControllerNodeSet
            Return SetProperty("textMode", textMode)
        End Function
        
        Public Function SetSearch(ByVal search As String) As ControllerNodeSet
            Return SetProperty("search", search)
        End Function
        
        Public Function SetSearchOptions(ByVal searchOptions As String) As ControllerNodeSet
            Return SetProperty("searchOptions", searchOptions)
        End Function
        
        Public Function SetAccess(ByVal access As String) As ControllerNodeSet
            Return SetProperty("access", access)
        End Function
        
        Public Function SetAggregate(ByVal aggregate As String) As ControllerNodeSet
            Return SetProperty("aggregate", aggregate)
        End Function
        
        Public Function SetAutoCompletePrefixLength(ByVal autoCompletePrefixLength As String) As ControllerNodeSet
            Return SetProperty("autoCompletePrefixLength", autoCompletePrefixLength)
        End Function
        
        Public Function SetHyperlinkFormatString(ByVal hyperlinkFormatString As String) As ControllerNodeSet
            Return SetProperty("hyperlinkFormatString", hyperlinkFormatString)
        End Function
        
        Public Function SetName(ByVal name As String) As ControllerNodeSet
            Return SetProperty("name", name)
        End Function
        
        Public Function SetFieldName(ByVal fieldName As String) As ControllerNodeSet
            Return SetProperty("fieldName", fieldName)
        End Function
        
        ''' <summary>
        ''' Allows action if the last command name executed in the data view matches the argument.
        ''' </summary>
        ''' <param name="lastCommandName">The name of the last command.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Function WhenLastCommandName(ByVal lastCommandName As String) As ControllerNodeSet
            Return SetProperty("whenLastCommandName", lastCommandName)
        End Function
        
        ''' <summary>
        ''' Allows action if the last command argument executed in the data view matches the argument.
        ''' </summary>
        ''' <param name="lastCommandArgument">The name of the last argument.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Function WhenLastCommandArgument(ByVal lastCommandArgument As String) As ControllerNodeSet
            Return SetProperty("whenLastCommandArgument", lastCommandArgument)
        End Function
        
        ''' <summary>
        ''' Allows action if the JavaScript expression specified in the argument evalues as true. The field values can be referenced in square brackets by name. For example, [Status] == 'Open'
        ''' </summary>
        ''' <param name="clientScript">The JavaScript expression.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Function WhenClientScript(ByVal clientScript As String) As ControllerNodeSet
            Return SetProperty("whenClientScript", clientScript)
        End Function
        
        ''' <summary>
        ''' Allows action if the regular expression specified in the argument evalutes as a match against the URL in the address bar of the web browser.
        ''' </summary>
        ''' <param name="href">The regular expression.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Function WhenHRef(ByVal href As String) As ControllerNodeSet
            Return SetProperty("whenHRef", href)
        End Function
        
        ''' <summary>
        ''' Allows action if the regular expression specified in the argument evalutes as a match against the data view 'Tag' property.
        ''' </summary>
        ''' <param name="tag">The regular expression.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Function WhenTag(ByVal tag As String) As ControllerNodeSet
            Return SetProperty("whenTag", tag)
        End Function
        
        ''' <summary>
        ''' Allows action if the regular expression specified in the argument evalutes as a match against the ID of the view controller. For example, (grid1|grid2).
        ''' </summary>
        ''' <param name="viewId">The regular expression.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Function WhenView(ByVal viewId As String) As ControllerNodeSet
            Return SetProperty("whenView", viewId)
        End Function
        
        ''' <summary>
        ''' Allows action if a data row is selected in the data view.
        ''' </summary>
        ''' <param name="keySelected">The boolean value indicating if a data row is selected.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Overloads Function WhenKeySelected(ByVal keySelected As Boolean) As ControllerNodeSet
            Return SetProperty("whenKeySelected", keySelected.ToString().ToLower())
        End Function
        
        ''' <summary>
        ''' Allows action if a data row is selected in the data view.
        ''' </summary>
        ''' <param name="keySelected">The boolean value indicating if a data row is selected.</param>
        ''' <returns>The node set containing the action.</returns>
        Public Overloads Function WhenKeySelected(ByVal keySelected As String) As ControllerNodeSet
            Return WhenKeySelected(Convert.ToBoolean(keySelected))
        End Function
        
        Public Overloads Function CreateActionGroup() As ControllerNodeSet
            Return CreateActionGroup(Nothing)
        End Function
        
        Public Overloads Function CreateActionGroup(ByVal id As String) As ControllerNodeSet
            Dim actionGroupNode = New ControllerNodeSet(m_Navigator, m_Resolver).Select("<actionGroup/>").AppendTo("/dataController/actions").Select("/dataController/actions/actionGroup[last()]")
            If Not (String.IsNullOrEmpty(id)) Then
                actionGroupNode.Attr("id", id)
            End If
            Return actionGroupNode
        End Function
        
        Public Overloads Function CreateAction() As ControllerNodeSet
            Return CreateAction(Nothing, Nothing, Nothing)
        End Function
        
        Public Overloads Function CreateAction(ByVal id As String) As ControllerNodeSet
            Return CreateAction(Nothing, Nothing, id)
        End Function
        
        Public Overloads Function CreateAction(ByVal commandName As String, ByVal commandArgument As String) As ControllerNodeSet
            Return CreateAction(commandName, commandArgument, Nothing)
        End Function
        
        Public Overloads Function CreateAction(ByVal commandName As String, ByVal commandArgument As String, ByVal id As String) As ControllerNodeSet
            Dim actionNode = [Select]("<action/>").AppendTo(Me.Select("ancestor-or-self::actionGroup")).Select("action[last()]")
            If Not (String.IsNullOrEmpty(id)) Then
                actionNode.Attr("id", id)
            End If
            If Not (String.IsNullOrEmpty(commandName)) Then
                actionNode.Attr("commandName", commandName)
            End If
            If Not (String.IsNullOrEmpty(commandArgument)) Then
                actionNode.Attr("commandArgument", commandArgument)
            End If
            Return actionNode
        End Function
        
        Public Overloads Function CreateView(ByVal id As String) As ControllerNodeSet
            Return CreateView(id, "Grid", Nothing)
        End Function
        
        Public Overloads Function CreateView(ByVal id As String, ByVal type As String) As ControllerNodeSet
            Return CreateView(id, type, Nothing)
        End Function
        
        Public Overloads Function CreateView(ByVal id As String, ByVal type As String, ByVal commandId As String) As ControllerNodeSet
            If String.IsNullOrEmpty(commandId) Then
                Dim commandIdNav = m_Navigator.SelectSingleNode("/c:dataController/c:commands/c:command/@id", m_Resolver)
                If (Not (commandIdNav) Is Nothing) Then
                    commandId = commandIdNav.Value
                End If
            End If
            Return New ControllerNodeSet(m_Navigator, m_Resolver).Select("<view/>").AppendTo("/dataController/views").Select("/dataController/views/view[last()]").Attr("type", type).Attr("commandId", commandId).Attr("id", id)
        End Function
        
        Public Overloads Function CreateCategory(ByVal id As String) As ControllerNodeSet
            Return CreateCategory(id, Nothing)
        End Function
        
        Public Overloads Function CreateCategory(ByVal id As String, ByVal headerText As String) As ControllerNodeSet
            For Each node in m_Nodes
                Dim parentNode = New ControllerNodeSet(Me, node)
                Dim categoriesNode = parentNode
                If Not ((node.Name = "categories")) Then
                    categoriesNode = parentNode.Select("categories|ancestor::categories[1]")
                    If (categoriesNode.Nodes.Count = 0) Then
                        [Select]("<categories/>").AppendTo(parentNode)
                        categoriesNode = parentNode.Select("categories")
                    End If
                End If
                Return [Select]("<category/>").AppendTo(categoriesNode).Select("category[last()]").Attr("id", id).Attr("headerText", headerText).Elem("dataFields", Nothing)
            Next
            Return Me
        End Function
        
        Public Overloads Function CreateDataField(ByVal fieldName As String) As ControllerNodeSet
            Return CreateDataField(fieldName, Nothing)
        End Function
        
        Public Overloads Function CreateDataField(ByVal fieldName As String, ByVal aliasFieldName As String) As ControllerNodeSet
            Dim existingFieldNode = SelectDataField(fieldName)
            If (existingFieldNode.Nodes.Count > 0) Then
                Return existingFieldNode
            End If
            For Each node in m_Nodes
                Dim parentNode = New ControllerNodeSet(Me, node)
                Dim dataFieldsNode = parentNode
                If Not ((node.Name = "dataFields")) Then
                    dataFieldsNode = parentNode.Select("dataFields|ancestor::dataFields[1]")
                    If (dataFieldsNode.Nodes.Count = 0) Then
                        [Select]("<dataFields/>").AppendTo(parentNode)
                        dataFieldsNode = parentNode.Select("dataFields")
                    End If
                End If
                Dim dataFieldNode = [Select]("<dataField/>").AppendTo(dataFieldsNode).Select("dataField[last()]").Attr("fieldName", fieldName)
                If Not (String.IsNullOrEmpty(aliasFieldName)) Then
                    dataFieldNode.Attr("aliasFieldName", aliasFieldName)
                End If
                Return dataFieldNode
            Next
            Return Me
        End Function
        
        Public Function Hide() As ControllerNodeSet
            Return SetHidden(true)
        End Function
        
        Public Function Show() As ControllerNodeSet
            Return SetHidden(false)
        End Function
        
        Public Function ArrangeViews(ByVal ParamArray sequence() as System.[String]) As ControllerNodeSet
            [Select]("/dataController/views/view").Arrange("@id", sequence)
            Return Me
        End Function
        
        Public Function ArrangeDataFields(ByVal ParamArray sequence() as System.[String]) As ControllerNodeSet
            [Select]("dataField").Arrange("@fieldName", sequence)
            Return Me
        End Function
        
        Public Function ArrangeCategories(ByVal ParamArray sequence() as System.[String]) As ControllerNodeSet
            [Select]("category").Arrange("@id", sequence)
            Return Me
        End Function
        
        Public Function ArrangeActionGroups(ByVal ParamArray sequence() as System.[String]) As ControllerNodeSet
            [Select]("/dataController/actions/actionGroup").Arrange("@id", sequence)
            Return Me
        End Function
        
        Public Function ArrangeActions(ByVal ParamArray sequence() as System.[String]) As ControllerNodeSet
            [Select]("action").Arrange("@id", sequence)
            Return Me
        End Function
        
        Public Function Move(ByVal target As ControllerNodeSet) As ControllerNodeSet
            If Not ((target.Nodes.Count = 1)) Then
                Return Me
            End If
            Dim targetNode = target.Nodes(0)
            For Each node in m_Nodes
                Dim skip = true
                If (((targetNode.Name = "category") OrElse (targetNode.Name = "view")) AndAlso (node.Name = "dataField")) Then
                    skip = false
                End If
                If ((targetNode.Name = "actionGroup") AndAlso (node.Name = "action")) Then
                    skip = false
                End If
                If Not (skip) Then
                    Dim newParent = targetNode
                    If (targetNode.Name = "category") Then
                        newParent = targetNode.SelectSingleNode("c:dataFields", m_Resolver)
                    End If
                    If (Not (newParent) Is Nothing) Then
                        newParent.AppendChild(node)
                        node.DeleteSelf()
                    End If
                End If
            Next
            Return New ControllerNodeSet(Me, targetNode)
        End Function
        
        Public Function Parent() As ControllerNodeSet
            For Each node in m_Nodes
                node.MoveToParent()
                Return New ControllerNodeSet(Me, node)
            Next
            Return Me
        End Function
        
        Public Function SelectFields(ByVal ParamArray names() as System.[String]) As ControllerNodeSet
            If (names.Length = 0) Then
                Return NodeSet().Select("/dataController/fields/field")
            End If
            Return NodeSet().Select("/dataController/fields/field[@name=$name]", names)
        End Function
        
        Public Function Use() As ControllerNodeSet
            If (m_Nodes.Count > 0) Then
                Dim sb = New StringBuilder()
                For Each node in m_Nodes
                    sb.Append(node.OuterXml)
                Next
                Dim nodeName = m_Nodes(0).Name
                Dim parentNode = m_Nodes(0).SelectSingleNode("parent::*")
                parentNode.InnerXml = sb.ToString()
                Dim nodeSet = New ControllerNodeSet(Me, parentNode)
                Dim list = New List(Of XPathNavigator)()
                list.AddRange(nodeSet.SelectInContext(nodeName, nodeName).Nodes)
                Return New ControllerNodeSet(Me, list)
            End If
            Return Me
        End Function
        
        Protected Function SelectFieldItemsNode() As ControllerNodeSet
            Dim list = New List(Of XPathNavigator)()
            For Each node in m_Nodes
                Dim parentNode = New ControllerNodeSet(Me, node)
                Dim itemsNode = parentNode.Select("items")
                If (itemsNode.Nodes.Count = 0) Then
                    parentNode.Select("<items/>").AppendTo(parentNode)
                    itemsNode = parentNode.Select("items")
                End If
                list.AddRange(itemsNode.Nodes)
            Next
            Return New ControllerNodeSet(Me, list)
        End Function
        
        ''' <summary>
        ''' Sets the style of lookup presentation for the field.
        ''' </summary>
        ''' <param name="style">The style of the lookup presentation. Supported values are AutoComplete, CheckBox, CheckBoxList, DropDownList, ListBox, Lookup, RadioButtonList, UserIdLookup, and UserNameLookup.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsStyle(ByVal style As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("style", style)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the new data view that will allow creating lookup items in-place.
        ''' </summary>
        ''' <param name="viewId">The id of a form view.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsNewView(ByVal viewId As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("newDataView", viewId)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the data controller providing dynamic items for the lookup field.
        ''' </summary>
        ''' <param name="controller">The name of a the lookup data controller.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsController(ByVal controller As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("dataController", controller)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the target data controller of a many-to-many lookup field.
        ''' </summary>
        ''' <param name="controller">The name of a the data controller that serves as a target of many-to-many lookup field.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsTargetController(ByVal controller As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("targetController", controller)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the view of a data controller providing dynamic items for the lookup field.
        ''' </summary>
        ''' <param name="viewId">The id of the view in the lookup data controller.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsView(ByVal viewId As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("dataView", viewId)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the name of the field in the lookup data controller that will provide the lookup value.
        ''' </summary>
        ''' <param name="fieldName">The name of the field in the lookup data controller.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsDataValueField(ByVal fieldName As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("dataValueField", fieldName)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the name of the field in the lookup data controller that will provide the user-friendly text displayed when a lookup value is selected.
        ''' </summary>
        ''' <param name="fieldName">The name of the field in the lookup data controller.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsDataTextField(ByVal fieldName As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("dataTextField", fieldName)
            Return Me
        End Function
        
        ''' <summary>
        ''' Assigns a 'copy' map to the lookup field. The map will control, which fields from the lookup data controller will be copied when a lookup value is selected.
        ''' </summary>
        ''' <param name="map">The 'copy' map of the lookup field. Example: ShipName=ContactName,ShipAddress=Address,ShipRegion=Region</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsCopyMap(ByVal map As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("copy", map)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the text displayed in the header area of lookup window.
        ''' </summary>
        ''' <param name="description">The description of the lookup window.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsDescription(ByVal description As String) As ControllerNodeSet
            SelectFieldItemsNode().Attr("description", description)
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the flag that will cause the automatic display of a lookup window in 'edit' and 'new' modes when the lookup field is blank.
        ''' </summary>
        ''' <param name="enable">The value indicating if lookup window is activated in 'edit' and 'new' modes.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsAutoSelect(ByVal enable As Boolean) As ControllerNodeSet
            SelectFieldItemsNode().Attr("autoSelect", enable.ToString().ToLower())
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the flag that will allow searching by first letter in the lookup window.
        ''' </summary>
        ''' <param name="enable">The value indicating if search by first letter is enabled in the lookup window.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsSearchByFirstLetter(ByVal enable As Boolean) As ControllerNodeSet
            SelectFieldItemsNode().Attr("letters", enable.ToString().ToLower())
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the flag that will force the lookup window to display in 'search' mode instead of rendering the first page of lookup data rows.
        ''' </summary>
        ''' <param name="enable">The value indicating if the 'search' mode is enabled in the lookup window.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsSearchOnStart(ByVal enable As Boolean) As ControllerNodeSet
            SelectFieldItemsNode().Attr("searchOnStart", enable.ToString().ToLower())
            Return Me
        End Function
        
        ''' <summary>
        ''' Sets the initial page size of the lookup window.
        ''' </summary>
        ''' <param name="size">The initial page size of the lookup window.</param>
        ''' <returns>Returns the current node set.</returns>
        Public Function SetItemsPageSize(ByVal size As Integer) As ControllerNodeSet
            SelectFieldItemsNode().Attr("pageSize", size)
            Return Me
        End Function
        
        ''' <summary>
        ''' Selects the items with the specified values.
        ''' </summary>
        ''' <param name="values">List of item values.</param>
        ''' <returns>Returns a node set with items that were matched to the list of values.</returns>
        Public Function SelectItems(ByVal ParamArray values() as System.[Object]) As ControllerNodeSet
            If (values.Length = 0) Then
                Return [Select]("item")
            End If
            Return [Select]("item[@value=$value]", values)
        End Function
        
        ''' <summary>
        ''' Create a new static item for this field.
        ''' </summary>
        ''' <param name="value">Value of the item stored in the database.</param>
        ''' <param name="text">Text of the item presented to the user.</param>
        ''' <returns>The node set containing the field.</returns>
        Public Function CreateItem(ByVal value As Object, ByVal text As String) As ControllerNodeSet
            Dim itemsNode = [Select]("items")
            If (itemsNode.Nodes.Count = 0) Then
                [Select]("<items/>").AppendTo(Me)
                itemsNode = [Select]("items").Attr("style", "DropDownList")
            End If
            [Select]("<item/>").AppendTo(itemsNode).Select("item[last()]").Attr("value", value).Attr("text", text)
            Return Me
        End Function
        
        ''' <summary>
        ''' Defines a JavaScript expression to evaluate visibility of a data field or category at runtime.
        ''' </summary>
        ''' <param name="clientScript">The JavaScript expression evaluating the data field or category visibility.</param>
        ''' <param name="args">The list of arguments referenced in the JavaScript expression.</param>
        ''' <returns>The node set containing the data field or category.</returns>
        Public Function VisibleWhen(ByVal clientScript As String, ByVal ParamArray args() as System.[Object]) As ControllerNodeSet
            Return CreateExpression("visibility", "test", String.Format(clientScript, args))
        End Function
        
        ''' <summary>
        ''' Defines a JavaScript expression to evaluate if the data field is read-only. If that is the case, then the client libraray will set the 'Mode' property of the data field to 'Static'.
        ''' </summary>
        ''' <param name="clientScript">The JavaScript expression evaluating if the data field is read-only.</param>
        ''' <param name="args">The list of arguments referenced in the JavaScript expression.</param>
        ''' <returns>The node set containing the data field.</returns>
        Public Function ReadOnlyWhen(ByVal clientScript As String, ByVal ParamArray args() as System.[Object]) As ControllerNodeSet
            Return CreateExpression("readOnly", "test", String.Format(clientScript, args))
        End Function
        
        Protected Function CreateExpression(ByVal rootElement As String, ByVal ParamArray attributes() as System.[String]) As ControllerNodeSet
            For Each node in m_Nodes
                Dim nodeSet = New ControllerNodeSet(Me, node)
                Dim rootNode = nodeSet.Select(rootElement)
                If (rootNode.Nodes.Count = 0) Then
                    [Select](String.Format("<{0}/>", rootElement)).AppendTo(nodeSet)
                    rootNode = nodeSet.Select(rootElement)
                End If
                Dim expressionNode = nodeSet.Select("expression[1]")
                If (expressionNode.Nodes.Count = 0) Then
                    [Select]("<expression/>").AppendTo(rootNode)
                    expressionNode = rootNode.Select("expression")
                End If
                Dim i = 0
                Do While (i < attributes.Length)
                    expressionNode.Attr(attributes(i), attributes((i + 1)))
                    i = (i + 2)
                Loop
            Next
            Return Me
        End Function
        
        Function SelectBusinessRules(ByVal filter As String) As ControllerNodeSet
            Dim selector = "/dataController/businessRules/rule"
            If Not (String.IsNullOrEmpty(selector)) Then
                selector = String.Format("{0}[{1}]", selector, filter)
            End If
            Return NodeSet().InternalSelect(false, selector)
        End Function
        
        Private Function CreateBusinessRuleFilter(ByVal type As String, ByVal phase As String, ByVal commandName As String, ByVal commandArgument As String, ByVal view As String) As String
            Dim sb = New StringBuilder()
            Dim first = true
            If Not (String.IsNullOrEmpty(type)) Then
                sb.AppendFormat(" @type='{0}'", type)
                first = false
            End If
            If Not (String.IsNullOrEmpty(phase)) Then
                If Not (first) Then
                    sb.Append(" and ")
                End If
                sb.AppendFormat(" @phase='{0}'", phase)
                first = false
            End If
            If Not (String.IsNullOrEmpty(commandName)) Then
                If Not (first) Then
                    sb.Append(" and ")
                End If
                sb.AppendFormat(" @commandName='{0}'", commandName)
                first = false
            End If
            If Not (String.IsNullOrEmpty(commandArgument)) Then
                If Not (first) Then
                    sb.Append(" and ")
                End If
                sb.AppendFormat(" @commandArgument='{0}'", commandArgument)
            End If
            If Not (String.IsNullOrEmpty(view)) Then
                If Not (first) Then
                    sb.Append(" and ")
                End If
                sb.AppendFormat(" @view='{0}'", view)
                first = false
            End If
            Return sb.ToString()
        End Function
        
        Public Function SelectSqlBusinessRules(ByVal phase As String, ByVal commandName As String, ByVal commandArgument As String, ByVal view As String) As ControllerNodeSet
            Return SelectBusinessRules(CreateBusinessRuleFilter("Sql", phase, commandName, commandArgument, view))
        End Function
        
        Public Function SelectEmailBusinessRules(ByVal phase As String, ByVal commandName As String, ByVal commandArgument As String, ByVal view As String) As ControllerNodeSet
            Return SelectBusinessRules(CreateBusinessRuleFilter("Email", phase, commandName, commandArgument, view))
        End Function
        
        Public Function SelectJavaScriptBusinessRules(ByVal phase As String, ByVal commandName As String, ByVal commandArgument As String, ByVal view As String) As ControllerNodeSet
            Return SelectBusinessRules(CreateBusinessRuleFilter("JavaScript", phase, commandName, commandArgument, view))
        End Function
        
        Public Overloads Function Value(ByVal v As String) As ControllerNodeSet
            For Each node in m_Nodes
                node.SetValue(Convert.ToString(v))
            Next
            Return Me
        End Function
        
        Public Overloads Function CreateBusinessRule(ByVal type As String, ByVal phase As String, ByVal commandName As String, ByVal commandArgument As String, ByVal view As String, ByVal script As String) As ControllerNodeSet
            Return CreateBusinessRule(type, phase, commandName, commandArgument, view, script, Nothing)
        End Function
        
        Public Overloads Function CreateBusinessRule(ByVal type As String, ByVal phase As String, ByVal commandName As String, ByVal commandArgument As String, ByVal view As String, ByVal script As String, ByVal name As String) As ControllerNodeSet
            Dim businessRulesNode = [Select]("/dataController/businessRules")
            If (businessRulesNode.Nodes.Count = 0) Then
                businessRulesNode = [Select]("<businessRules/>").AppendTo("/dataController").Select("businessRules")
            End If
            Dim ruleNode = [Select]("<rule/>").AppendTo(businessRulesNode).Select("rule[last()]")
            ruleNode.Attr("id", String.Format("crule{0}", businessRulesNode.Nodes(0).Evaluate("count(child::*)+1")))
            ruleNode.Attr("type", type)
            ruleNode.Attr("phase", phase)
            ruleNode.Attr("commandName", commandName)
            If Not (String.IsNullOrEmpty(commandArgument)) Then
                ruleNode.Attr("commandArgument", commandArgument)
            End If
            If Not (String.IsNullOrEmpty(view)) Then
                ruleNode.Attr("view", view)
            End If
            If Not (String.IsNullOrEmpty(name)) Then
                ruleNode.Attr("name", name)
            End If
            ruleNode.Value(script)
            Return ruleNode
        End Function
        
        Public Overloads Function CreateField(ByVal name As String, ByVal type As String) As ControllerNodeSet
            Return CreateField(name, type, Nothing)
        End Function
        
        Public Overloads Function CreateField(ByVal name As String, ByVal type As String, ByVal formula As String) As ControllerNodeSet
            Dim fieldsNode = [Select]("/dataController/fields")
            If fieldsNode.IsEmpty Then
                [Select]("<fields/>").AppendTo([Select]("/dataController"))
                fieldsNode = [Select]("/dataController/fields")
            End If
            If String.IsNullOrEmpty(type) Then
                type = "String"
            End If
            Dim fieldNode = [Select]("<field/>").AppendTo(fieldsNode).Select("field[last()]").Attr("name", name).Attr("type", type)
            If (type = "String") Then
                fieldNode.Attr("length", 250)
            End If
            If Not (String.IsNullOrEmpty(formula)) Then
                fieldNode.Attr("computed", true).Elem("formula", formula)
            End If
            Return fieldNode
        End Function
        
        Public Overloads Function StatusBar(ByVal statusMap As String) As ControllerNodeSet
            Return StatusBar(Nothing, Nothing, statusMap)
        End Function
        
        Public Overloads Function StatusBar(ByVal formula As String, ByVal statusMap As String) As ControllerNodeSet
            Return StatusBar(formula, Nothing, statusMap)
        End Function
        
        Public Overloads Function StatusBar(ByVal formula As String, ByVal type As String, ByVal statusMap As String) As ControllerNodeSet
            If (Not (String.IsNullOrEmpty(formula)) AndAlso SelectField("Status").IsEmpty) Then
                CreateField("Status", type, formula).Attr("readOnly", true)
            End If
            Dim statusBarNode = [Select]("/dataController/statusBar")
            If statusBarNode.IsEmpty Then
                statusBarNode = [Select]("<statusBar/>").AppendTo([Select]("/dataController")).Select("/dataController/statusBar")
            End If
            statusBarNode.Value(statusMap)
            Return statusBarNode
        End Function
        
        Public Function CreateStatusDataField() As ControllerNodeSet
            Return CreateDataField("Status")
        End Function
        
        Public Overloads Function Value() As String
            For Each node in m_Nodes
                Return node.Value
            Next
            Return String.Empty
        End Function
        
        Public Sub Reset()
            m_Current = Nothing
        End Sub
        
        Public Function MoveNext() As Boolean
            If (m_Nodes.Count = 0) Then
                Return false
            End If
            If (m_Current.HasValue AndAlso (m_Current.Value >= (m_Nodes.Count - 1))) Then
                Return false
            End If
            If Not (m_Current.HasValue) Then
                m_Current = 0
            Else
                m_Current = (m_Current + 1)
            End If
            Return true
        End Function
        
        Public Function GetName() As String
            Return Attr("name").Value()
        End Function
        
        Public Function GetFieldName() As String
            Return Attr("fieldName").Value()
        End Function
        
        Public Function GetLabel() As String
            Return Attr("label").Value()
        End Function
    End Class
End Namespace
