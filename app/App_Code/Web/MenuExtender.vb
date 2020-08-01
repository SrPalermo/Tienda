Imports Norwindt.Data
Imports Norwindt.Services
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls

Namespace Norwindt.Web
    
    Public Enum MenuHoverStyle
        
        [Auto] = 1
        
        Click = 1
        
        ClickAndStay = 1
    End Enum
    
    Public Enum MenuPresentationStyle
        
        MultiLevel
        
        TwoLevel
        
        NavigationButton
    End Enum
    
    Public Enum MenuOrientation
        
        Horizontal
    End Enum
    
    Public Enum MenuPopupPosition
        
        Left
        
        Right
    End Enum
    
    Public Enum MenuItemDescriptionStyle
        
        None
        
        Inline
        
        ToolTip
    End Enum
    
    <TargetControlType(GetType(Panel)),  _
     TargetControlType(GetType(HtmlContainerControl)),  _
     DefaultProperty("TargetControlID")>  _
    Public Class MenuExtender
        Inherits System.Web.UI.WebControls.HierarchicalDataBoundControl
        Implements IExtenderControl
        
        Private m_Items As String
        
        Private m_Sm As ScriptManager
        
        Private m_TargetControlID As String
        
        Private m_Visible As Boolean
        
        Private m_HoverStyle As MenuHoverStyle
        
        Private m_PopupPosition As MenuPopupPosition
        
        Private m_ItemDescriptionStyle As MenuItemDescriptionStyle
        
        Private m_ShowSiteActions As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_PresentationStyle As MenuPresentationStyle
        
        Public MenuItemPropRegex As Regex = New Regex("\s*(?'Name'\w+)\s*(=|:)\s*(?'Value'.+?)\s*(\r?\n|$)")
        
        Public Shared MenuItemRegex As Regex = New Regex("(?'Text'(?'Depth'(#|\+|\^)+)\s*(?'Title'.+?)\r?\n(?'Url'.*?)(\r?\n|$)(?'PropList'"& _ 
                "(\s*\w+\s*(:|=)\s*.+?(\r?\n|$))*))")
        
        Public Sub New()
            MyBase.New()
            Me.Visible = true
            ItemDescriptionStyle = MenuItemDescriptionStyle.ToolTip
            HoverStyle = MenuHoverStyle.Auto
        End Sub
        
        <IDReferenceProperty(),  _
         Category("Behavior"),  _
         DefaultValue("")>  _
        Public Property TargetControlID() As String
            Get
                Return m_TargetControlID
            End Get
            Set
                m_TargetControlID = value
            End Set
        End Property
        
        <EditorBrowsable(EditorBrowsableState.Never),  _
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),  _
         Browsable(false)>  _
        Public Overrides Property Visible() As Boolean
            Get
                Return m_Visible
            End Get
            Set
                m_Visible = value
            End Set
        End Property
        
        Public Property HoverStyle() As MenuHoverStyle
            Get
                Return m_HoverStyle
            End Get
            Set
                m_HoverStyle = value
            End Set
        End Property
        
        Public Property PopupPosition() As MenuPopupPosition
            Get
                Return m_PopupPosition
            End Get
            Set
                m_PopupPosition = value
            End Set
        End Property
        
        Public Property ItemDescriptionStyle() As MenuItemDescriptionStyle
            Get
                Return m_ItemDescriptionStyle
            End Get
            Set
                m_ItemDescriptionStyle = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The ""Site Actions"" menu is automatically displayed."),  _
         System.ComponentModel.DefaultValue(false)>  _
        Public Property ShowSiteActions() As Boolean
            Get
                Return m_ShowSiteActions
            End Get
            Set
                m_ShowSiteActions = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specifies the menu presentation style."),  _
         System.ComponentModel.DefaultValue(MenuPresentationStyle.MultiLevel)>  _
        Public Property PresentationStyle() As MenuPresentationStyle
            Get
                Return m_PresentationStyle
            End Get
            Set
                m_PresentationStyle = value
            End Set
        End Property
        
        Protected Overrides Sub PerformDataBinding()
            MyBase.PerformDataBinding()
            If (Not (IsBoundUsingDataSourceID) AndAlso (Not (DataSource) Is Nothing)) Then
                Return
            End If
            Dim view = GetData(String.Empty)
            Dim enumerable = view.Select()
            If (ApplicationServices.IsSiteContentEnabled AndAlso Not (ApplicationServices.IsSafeMode)) Then
                Dim sitemaps = ApplicationServices.Current.ReadSiteContent("sys/sitemaps%", "%")
                If (sitemaps.Count > 0) Then
                    Dim hasMain = false
                    For Each f in sitemaps
                        If (f.PhysicalName = "main") Then
                            hasMain = true
                            sitemaps.Remove(f)
                            sitemaps.Insert(0, f)
                            Exit For
                        End If
                    Next
                    If (Not (hasMain) AndAlso (Not (enumerable) Is Nothing)) Then
                        Dim msb = New StringBuilder()
                        BuildMainMenu(enumerable, msb, 1)
                        Dim main = New SiteContentFile()
                        main.Text = Localizer.Replace("Pages", Path.GetFileName(Page.Request.PhysicalPath), msb.ToString())
                        sitemaps.Insert(0, main)
                    End If
                    Dim text As String = Nothing
                    If (sitemaps.Count > 1) Then
                        Dim sm = New SiteMapBuilder()
                        For Each cf in sitemaps
                            Dim sitemapText = cf.Text
                            If Not (String.IsNullOrEmpty(sitemapText)) Then
                                Dim coll = MenuItemRegex.Matches(sitemapText)
                                For Each m As Match in coll
                                    sm.Insert(m.Groups("Title").Value, m.Groups("Depth").Value.Length, m.Groups("Text").Value)
                                Next
                            End If
                        Next
                        text = sm.ToString()
                    Else
                        text = sitemaps(0).Text
                    End If
                    Dim sb = New StringBuilder()
                    If Not (String.IsNullOrEmpty(text)) Then
                        Dim first = true
                        Dim m = MenuItemRegex.Match(text)
                        Do While m.Success
                            BuildNode(m, sb, first)
                            If first Then
                                first = false
                            End If
                        Loop
                        m_Items = Regex.Replace(sb.ToString(), "(\{\}\,?)+", String.Empty).Replace("},]", "}]")
                        Return
                    End If
                End If
            End If
            If (Not (enumerable) Is Nothing) Then
                Dim sb = New StringBuilder()
                RecursiveDataBindInternal(enumerable, sb)
                m_Items = sb.ToString()
            End If
        End Sub
        
        Private Sub BuildMainMenu(ByVal enumerable As IHierarchicalEnumerable, ByVal sb As StringBuilder, ByVal depth As Integer)
            For Each item in enumerable
                Dim data = enumerable.GetHierarchyData(item)
                If (Not (data) Is Nothing) Then
                    Dim props = TypeDescriptor.GetProperties(data)
                    If (props.Count > 0) Then
                        Dim title = CType(props("Title").GetValue(data),String)
                        Dim description = CType(props("Description").GetValue(data),String)
                        Dim url = CType(props("Url").GetValue(data),String)
                        Dim cssClass As String = Nothing
                        Dim roles As String = Nothing
                        Dim roleList = CType(props("Roles").GetValue(data),ArrayList)
                        If (roleList.Count > 0) Then
                            roles = String.Join(",", CType(roleList.ToArray(GetType(String)),String()))
                        End If
                        If TypeOf item Is SiteMapNode Then
                            cssClass = CType(item,SiteMapNode)("cssClass")
                            If ("true" = CType(item,SiteMapNode)("public")) Then
                                roles = "?"
                            End If
                        End If
                        sb.AppendFormat("{0} {1}", New [String](Global.Microsoft.VisualBasic.ChrW(43), depth), title)
                        sb.AppendLine()
                        If Not (String.IsNullOrEmpty(url)) Then
                            sb.AppendLine(url)
                        Else
                            sb.AppendLine("about:blank")
                        End If
                        If Not (String.IsNullOrEmpty(description)) Then
                            sb.AppendFormat("description: {0}", description)
                            sb.AppendLine()
                        End If
                        If Not (String.IsNullOrEmpty(roles)) Then
                            sb.AppendFormat("roles: {0}", roles)
                            sb.AppendLine()
                        End If
                        If Not (String.IsNullOrEmpty(cssClass)) Then
                            sb.AppendFormat("cssclass: {0}", cssClass)
                            sb.AppendLine()
                        End If
                        sb.AppendLine()
                        If data.HasChildren Then
                            Dim childrenEnumerable = data.GetChildren()
                            If (Not (childrenEnumerable) Is Nothing) Then
                                BuildMainMenu(childrenEnumerable, sb, (depth + 1))
                            End If
                        End If
                    End If
                End If
            Next
        End Sub
        
        Private Sub BuildNode(ByRef node As Match, ByVal sb As StringBuilder, ByVal first As Boolean)
            If Not (first) Then
                sb.Append(",")
            End If
            Dim propList = New SortedDictionary(Of String, String)()
            Dim prop = MenuItemPropRegex.Match(node.Groups("PropList").Value)
            Do While prop.Success
                propList(prop.Groups("Name").Value.ToLower().Replace("-", String.Empty)) = prop.Groups("Value").Value
                prop = prop.NextMatch()
            Loop
            Dim roles As String = Nothing
            propList.TryGetValue("roles", roles)
            Dim users As String = Nothing
            propList.TryGetValue("users", users)
            Dim roleExceptions As String = Nothing
            propList.TryGetValue("roleexceptions", roleExceptions)
            Dim userExceptions As String = Nothing
            propList.TryGetValue("userexceptions", userExceptions)
            Dim cssClass As String = Nothing
            propList.TryGetValue("cssclass", cssClass)
            Dim url = node.Groups("Url").Value.Trim()
            Dim target As String = Nothing
            If url.StartsWith("_blank:") Then
                target = "_blank:"
                url = url.Substring(7)
            End If
            url = ResolveUrl(url)
            If Not (String.IsNullOrEmpty(target)) Then
                url = (target + url)
            End If
            Dim resourceAuthorized = true
            If Not (String.IsNullOrEmpty(roles)) Then
                If Not (ApplicationServices.UserIsAuthorizedToAccessResource(url, roles)) Then
                    resourceAuthorized = false
                End If
            End If
            If (resourceAuthorized AndAlso Not (String.IsNullOrEmpty(users))) Then
                If (Not ((users = "?")) AndAlso (Array.IndexOf(users.ToLower().Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries), Page.User.Identity.Name.ToLower()) = -1)) Then
                    resourceAuthorized = false
                End If
            End If
            If (Not (resourceAuthorized) AndAlso Not (String.IsNullOrEmpty(roleExceptions))) Then
                If DataControllerBase.UserIsInRole(roleExceptions) Then
                    resourceAuthorized = true
                End If
            End If
            If (Not (resourceAuthorized) AndAlso Not (String.IsNullOrEmpty(userExceptions))) Then
                If Not ((Array.IndexOf(userExceptions.ToLower().Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries), Page.User.Identity.Name.ToLower()) = -1)) Then
                    resourceAuthorized = true
                End If
            End If
            sb.Append("{")
            If resourceAuthorized Then
                Dim title = node.Groups("Title").Value.Trim()
                Dim depth = node.Groups("Depth").Value
                sb.AppendFormat("title:""{0}""", BusinessRules.JavaScriptString(title))
                If Not ((url = "about:blank")) Then
                    sb.AppendFormat(",url:""{0}""", BusinessRules.JavaScriptString(url))
                End If
                If (Page.Request.RawUrl = url) Then
                    sb.Append(",selected:true")
                End If
                Dim description As String = Nothing
                propList.TryGetValue("description", description)
                If Not (String.IsNullOrEmpty(description)) Then
                    sb.AppendFormat(",description:""{0}""", BusinessRules.JavaScriptString(description))
                End If
                If Not (String.IsNullOrEmpty(cssClass)) Then
                    sb.AppendFormat(",cssClass:""{0}""", BusinessRules.JavaScriptString(cssClass))
                End If
                node = node.NextMatch()
                If node.Success Then
                    Dim firstChildDepth = node.Groups("Depth").Value
                    If (firstChildDepth.Length > depth.Length) Then
                        sb.Append(",children:[")
                        first = true
                        Do While node.Success
                            BuildNode(node, sb, first)
                            If first Then
                                first = false
                            End If
                            If node.Success Then
                                Dim nextDepth = node.Groups("Depth").Value
                                If (firstChildDepth.Length > nextDepth.Length) Then
                                    Exit Do
                                End If
                            End If
                        Loop
                        sb.Append("]")
                    End If
                End If
            Else
                node = node.NextMatch()
            End If
            sb.Append("}")
        End Sub
        
        Private Sub RecursiveDataBindInternal(ByVal enumerable As IHierarchicalEnumerable, ByVal sb As StringBuilder)
            Dim first = true
            If (Not (Me.Site) Is Nothing) Then
                Return
            End If
            For Each item in enumerable
                Dim data = enumerable.GetHierarchyData(item)
                If (Not (data) Is Nothing) Then
                    Dim props = TypeDescriptor.GetProperties(data)
                    If (props.Count > 0) Then
                        Dim title = CType(props("Title").GetValue(data),String)
                        Dim description = CType(props("Description").GetValue(data),String)
                        Dim url = CType(props("Url").GetValue(data),String)
                        Dim cssClass As String = Nothing
                        Dim isPublic = false
                        If TypeOf item Is SiteMapNode Then
                            cssClass = CType(item,SiteMapNode)("cssClass")
                            isPublic = ("true" = CType(CType(item,SiteMapNode)("public"),String))
                        End If
                        Dim roles = String.Empty
                        Dim roleList = CType(props("Roles").GetValue(data),ArrayList)
                        If (roleList.Count > 0) Then
                            roles = String.Join(",", CType(roleList.ToArray(GetType(String)),String()))
                        End If
                        Dim resourceAuthorized = true
                        If resourceAuthorized Then
                            If first Then
                                first = false
                            Else
                                sb.Append(",")
                            End If
                            sb.AppendFormat("{{title:""{0}"",url:""{1}""", BusinessRules.JavaScriptString(title), BusinessRules.JavaScriptString(url))
                            If Not (String.IsNullOrEmpty(description)) Then
                                sb.AppendFormat(",description:""{0}""", BusinessRules.JavaScriptString(description))
                            End If
                            If (url = Page.Request.RawUrl) Then
                                sb.Append(",selected:true")
                            End If
                            If Not (String.IsNullOrEmpty(cssClass)) Then
                                sb.AppendFormat(",cssClass:""{0}""", cssClass)
                            End If
                            If data.HasChildren Then
                                Dim childrenEnumerable = data.GetChildren()
                                If (Not (childrenEnumerable) Is Nothing) Then
                                    sb.Append(",""children"":[")
                                    RecursiveDataBindInternal(childrenEnumerable, sb)
                                    sb.Append("]")
                                End If
                            End If
                            sb.Append("}")
                        End If
                    End If
                End If
            Next
        End Sub
        
        Protected Overrides Sub OnInit(ByVal e As EventArgs)
            MyBase.OnInit(e)
            m_Sm = ScriptManager.GetCurrent(Page)
        End Sub
        
        Protected Overrides Sub OnLoad(ByVal e As EventArgs)
            MyBase.OnLoad(e)
            AquariumExtenderBase.RegisterFrameworkSettings(Page)
        End Sub
        
        Protected Overrides Sub OnPreRender(ByVal e As EventArgs)
            MyBase.OnPreRender(e)
            If (Nothing Is m_Sm) Then
                Return
            End If
            Dim script = String.Format("Web.Menu.Nodes.{0}=[{1}];", Me.ClientID, m_Items)
            Dim target = Page.Form.FindControl(TargetControlID)
            If ((Not (target) Is Nothing) AndAlso target.Visible) Then
                ScriptManager.RegisterStartupScript(Me, GetType(MenuExtender), "Nodes", script, true)
            End If
            m_Sm.RegisterExtenderControl(Of MenuExtender)(Me, target)
        End Sub
        
        Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)
            Dim isTouchUI = ApplicationServices.IsTouchClient
            If ((Nothing Is m_Sm) OrElse (m_Sm.IsInAsyncPostBack OrElse isTouchUI)) Then
                Return
            End If
            m_Sm.RegisterScriptDescriptors(Me)
        End Sub
        
        Function IExtenderControl_GetScriptDescriptors(ByVal targetControl As Control) As IEnumerable(Of ScriptDescriptor) Implements IExtenderControl.GetScriptDescriptors
            Dim descriptor = New ScriptBehaviorDescriptor("Web.Menu", targetControl.ClientID)
            descriptor.AddProperty("id", Me.ClientID)
            If Not ((HoverStyle = MenuHoverStyle.Auto)) Then
                descriptor.AddProperty("hoverStyle", Convert.ToInt32(HoverStyle))
            End If
            If Not ((PopupPosition = MenuPopupPosition.Left)) Then
                descriptor.AddProperty("popupPosition", Convert.ToInt32(PopupPosition))
            End If
            If Not ((ItemDescriptionStyle = MenuItemDescriptionStyle.ToolTip)) Then
                descriptor.AddProperty("itemDescriptionStyle", Convert.ToInt32(ItemDescriptionStyle))
            End If
            If ShowSiteActions Then
                descriptor.AddProperty("showSiteActions", "true")
            End If
            If Not ((PresentationStyle = MenuPresentationStyle.MultiLevel)) Then
                descriptor.AddProperty("presentationStyle", Convert.ToInt32(PresentationStyle))
            End If
            Return New ScriptBehaviorDescriptor() {descriptor}
        End Function
        
        Function IExtenderControl_GetScriptReferences() As IEnumerable(Of ScriptReference) Implements IExtenderControl.GetScriptReferences
            Return AquariumExtenderBase.StandardScripts()
        End Function
    End Class
    
    Public Class SiteMapBuilder
        
        Private m_Root As SiteMapBuilderNode = New SiteMapBuilderNode(String.Empty, 0, String.Empty)
        
        Private m_Last As SiteMapBuilderNode
        
        Public Sub Insert(ByVal title As String, ByVal depth As Integer, ByVal text As String)
            If (m_Last Is Nothing) Then
                m_Last = m_Root
            End If
            Dim entry = New SiteMapBuilderNode(title, depth, text)
            m_Last = m_Last.AddNode(entry)
        End Sub
        
        Public Overrides Function ToString() As String
            Return m_Root.ToString()
        End Function
    End Class
    
    Public Class SiteMapBuilderNode
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Title As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Depth As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Text As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Parent As SiteMapBuilderNode
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Children As List(Of SiteMapBuilderNode)
        
        Public Sub New(ByVal title As String, ByVal depth As Integer, ByVal text As String)
            MyBase.New
            Me.Title = title
            Me.Depth = depth
            Me.Text = text
            Children = New List(Of SiteMapBuilderNode)()
        End Sub
        
        Public Property Title() As String
            Get
                Return m_Title
            End Get
            Set
                m_Title = value
            End Set
        End Property
        
        Public Property Depth() As Integer
            Get
                Return m_Depth
            End Get
            Set
                m_Depth = value
            End Set
        End Property
        
        Public Property Text() As String
            Get
                Return m_Text
            End Get
            Set
                m_Text = value
            End Set
        End Property
        
        Public Property Parent() As SiteMapBuilderNode
            Get
                Return m_Parent
            End Get
            Set
                m_Parent = value
            End Set
        End Property
        
        Public Property Children() As List(Of SiteMapBuilderNode)
            Get
                Return m_Children
            End Get
            Set
                m_Children = value
            End Set
        End Property
        
        Public Function AddNode(ByVal entry As SiteMapBuilderNode) As SiteMapBuilderNode
            'go up
            If (entry.Depth <= Depth) Then
                Return Parent.AddNode(entry)
            Else
                'current child
                For Each child in Children
                    If (child.Title = entry.Title) Then
                        If Not (String.IsNullOrWhiteSpace(entry.Text.Replace(entry.Title, String.Empty).Replace("+", String.Empty))) Then
                            child.Text = entry.Text
                        End If
                        Return child
                    End If
                Next
                Children.Add(entry)
                entry.Parent = Me
                Return entry
            End If
        End Function
        
        Public Overrides Function ToString() As String
            Dim sb = New StringBuilder()
            If Not (String.IsNullOrEmpty(Text)) Then
                sb.AppendLine(Text)
            End If
            For Each entry in Children
                sb.AppendLine(entry.ToString())
            Next
            Return sb.ToString()
        End Function
    End Class
End Namespace
