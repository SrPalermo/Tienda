Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Norwindt.Data
Imports Norwindt.Services
Imports Norwindt.Web
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Configuration
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls
Imports System.Xml.XPath

Namespace Norwindt.Handlers
    
    Partial Public Class Site
        Inherits SiteBase
    End Class
    
    Public Class SiteBase
        Inherits Norwindt.Web.PageBase
        
        Private m_IsTouchUI As Boolean
        
        Private m_BodyAttributes As AttributeDictionary
        
        Private m_BodyTag As LiteralControl
        
        Private m_PageHeaderContent As LiteralContainer
        
        Private m_PageTitleContent As LiteralContainer
        
        Private m_HeadContent As LiteralContainer
        
        Private m_PageContent As LiteralContainer
        
        Private m_PageFooterContent As LiteralContainer
        
        Private m_PageSideBarContent As LiteralContainer
        
        Private m_SummaryDisabled As Boolean = false
        
        Public Shared UnsupportedDataViewProperties() As String = New String() {"data-start-command-name", "data-start-command-argument"}
        
        Public Shared ReadOnly Property Copyright() As String
            Get
                Return "&copy; 2020 Norwindt. ^Copyright^All rights reserved.^Copyright^"
            End Get
        End Property
        
        Public Overrides ReadOnly Property Device() As String
            Get
                Return m_BodyAttributes("data-device")
            End Get
        End Property
        
        Public Function ResolveAppUrl(ByVal html As String) As String
            Dim appPath = Request.ApplicationPath
            If Not (appPath.EndsWith("/")) Then
                appPath = (appPath + "/")
            End If
            Return html.Replace("=""~/", ("=""" + appPath))
        End Function
        
        Protected Overridable Function InjectPrefetch(ByVal s As String) As String
            If m_IsTouchUI Then
                Dim prefetch = PreparePrefetch(s)
                If Not (String.IsNullOrEmpty(prefetch)) Then
                    s = (prefetch + s)
                End If
            End If
            Return s
        End Function
        
        Protected Overrides Sub OnInit(ByVal e As EventArgs)
            If (Request.Path.StartsWith((ResolveUrl(AquariumExtenderBase.DefaultServicePath) + "/"), StringComparison.CurrentCultureIgnoreCase) OrElse Request.Path.StartsWith((ResolveUrl(AquariumExtenderBase.AppServicePath) + "/"), StringComparison.CurrentCultureIgnoreCase)) Then
                ApplicationServices.HandleServiceRequest(Context)
            End If
            If (Request.Params("_page") = "_blank") Then
                Return
            End If
            Dim link = Request.Params("_link")
            If Not (String.IsNullOrEmpty(link)) Then
                Dim permalink = StringEncryptor.FromString(link.Replace(" ", "+").Split(Global.Microsoft.VisualBasic.ChrW(44))(0)).Split(Global.Microsoft.VisualBasic.ChrW(63))
                If (permalink.Length = 2) Then
                    Page.ClientScript.RegisterStartupScript([GetType](), "Redirect", String.Format("window.location.replace('{0}?_link={1}');", permalink(0), HttpUtility.UrlEncode(link)), true)
                End If
            Else
                Dim requestUrl = Request.RawUrl
                If ((requestUrl.Length > 1) AndAlso requestUrl.EndsWith("/")) Then
                    requestUrl = requestUrl.Substring(0, (requestUrl.Length - 1))
                End If
                If Request.ApplicationPath.Equals(requestUrl, StringComparison.CurrentCultureIgnoreCase) Then
                    Dim homePageUrl = ApplicationServices.HomePageUrl
                    If Not (Request.ApplicationPath.Equals(homePageUrl)) Then
                        Response.Redirect(homePageUrl)
                    End If
                End If
            End If
            Dim contentInfo = ApplicationServices.LoadContent()
            InitializeSiteMaster()
            Dim s As String = Nothing
            If Not (contentInfo.TryGetValue("PageTitle", s)) Then
                s = ApplicationServicesBase.Current.DisplayName
            End If
            Me.Title = s
            If (Not (m_PageTitleContent) Is Nothing) Then
                If m_IsTouchUI Then
                    m_PageTitleContent.Text = String.Empty
                Else
                    m_PageTitleContent.Text = s
                End If
            End If
            Dim appName = New HtmlMeta()
            appName.Name = "application-name"
            appName.Content = ApplicationServicesBase.Current.DisplayName
            Header.Controls.Add(appName)
            If (contentInfo.TryGetValue("Head", s) AndAlso (Not (m_HeadContent) Is Nothing)) Then
                m_HeadContent.Text = s
            End If
            If (contentInfo.TryGetValue("PageContent", s) AndAlso (Not (m_PageContent) Is Nothing)) Then
                If m_IsTouchUI Then
                    s = String.Format("<div id=""PageContent"" style=""display:none"">{0}</div>", s)
                End If
                Dim userControl = Regex.Match(s, "<div\s+data-user-control\s*=s*""([\s\S]+?)"".*?>\s*</div>")
                If userControl.Success Then
                    Dim startPos = 0
                    Do While userControl.Success
                        m_PageContent.Controls.Add(New LiteralControl(s.Substring(startPos, (userControl.Index - startPos))))
                        startPos = (userControl.Index + userControl.Length)
                        Dim controlFileName = userControl.Groups(1).Value
                        Dim controlExtension = Path.GetExtension(controlFileName)
                        Dim siteControlText As String = Nothing
                        If Not (controlFileName.StartsWith("~")) Then
                            controlFileName = (controlFileName + "~")
                        End If
                        If String.IsNullOrEmpty(controlExtension) Then
                            Dim testFileName = (controlFileName + ".ascx")
                            If File.Exists(Server.MapPath(testFileName)) Then
                                controlFileName = testFileName
                                controlExtension = ".ascx"
                            Else
                                If ApplicationServices.IsSiteContentEnabled Then
                                    Dim relativeControlPath = controlFileName.Substring(1)
                                    If relativeControlPath.StartsWith("/") Then
                                        relativeControlPath = relativeControlPath.Substring(1)
                                    End If
                                    siteControlText = ApplicationServices.Current.ReadSiteContentString(("sys/" + relativeControlPath))
                                End If
                                If (siteControlText Is Nothing) Then
                                    testFileName = (controlFileName + ".html")
                                    If File.Exists(Server.MapPath(testFileName)) Then
                                        controlFileName = testFileName
                                        controlExtension = ".html"
                                    End If
                                End If
                            End If
                        End If
                        Dim userControlAuthorizeRoles = Regex.Match(userControl.Value, "data-authorize-roles\s*=\s*""(.+?)""")
                        Dim allowUserControl = Not (userControlAuthorizeRoles.Success)
                        If Not (allowUserControl) Then
                            Dim authorizeRoles = userControlAuthorizeRoles.Groups(1).Value
                            If (authorizeRoles = "?") Then
                                If Not (Context.User.Identity.IsAuthenticated) Then
                                    allowUserControl = true
                                End If
                            Else
                                allowUserControl = ApplicationServices.UserIsAuthorizedToAccessResource(controlFileName, authorizeRoles)
                            End If
                        End If
                        If allowUserControl Then
                            Try 
                                If (controlExtension = ".ascx") Then
                                    m_PageContent.Controls.Add(LoadControl(controlFileName))
                                Else
                                    Dim controlText = siteControlText
                                    If (controlText Is Nothing) Then
                                        controlText = File.ReadAllText(Server.MapPath(controlFileName))
                                    End If
                                    Dim bodyMatch = Regex.Match(controlText, "<body[\s\S]*?>([\s\S]+?)</body>")
                                    If bodyMatch.Success Then
                                        controlText = bodyMatch.Groups(1).Value
                                    End If
                                    controlText = ApplicationServices.EnrichData(Localizer.Replace("Controls", Path.GetFileName(Server.MapPath(controlFileName)), controlText))
                                    m_PageContent.Controls.Add(New LiteralControl(InjectPrefetch(controlText)))
                                End If
                            Catch ex As Exception
                                m_PageContent.Controls.Add(New LiteralControl(String.Format("Error loading '{0}': {1}", controlFileName, ex.Message)))
                            End Try
                        End If
                        userControl = userControl.NextMatch()
                    Loop
                    If (startPos < s.Length) Then
                        m_PageContent.Controls.Add(New LiteralControl(s.Substring(startPos)))
                    End If
                Else
                    m_PageContent.Text = InjectPrefetch(s)
                End If
            Else
                If m_IsTouchUI Then
                    m_PageContent.Text = "<div id=""PageContent"" style=""display:none""><div data-app-role=""page"">404 Not Foun"& _ 
                        "d</div></div>"
                    Me.Title = ApplicationServicesBase.Current.DisplayName
                Else
                    m_PageContent.Text = "404 Not Found"
                End If
            End If
            If m_IsTouchUI Then
                If (Not (m_PageFooterContent) Is Nothing) Then
                    m_PageFooterContent.Text = (("<footer style=""display:none""><small>" + Copyright)  _
                                + "</small></footer>")
                End If
            Else
                If contentInfo.TryGetValue("About", s) Then
                    If (Not (m_PageSideBarContent) Is Nothing) Then
                        m_PageSideBarContent.Text = String.Format("<div class=""TaskBox About""><div class=""Inner""><div class=""Header"">About</div><div"& _ 
                                " class=""Value"">{0}</div></div></div>", s)
                    End If
                End If
            End If
            Dim bodyAttributes As String = Nothing
            If contentInfo.TryGetValue("BodyAttributes", bodyAttributes) Then
                m_BodyAttributes.Parse(bodyAttributes)
            End If
            Dim classAttr = m_BodyAttributes("class")
            If String.IsNullOrEmpty(classAttr) Then
                classAttr = String.Empty
            End If
            If Not (m_IsTouchUI) Then
                If Not (classAttr.Contains("Wide")) Then
                    classAttr = (classAttr + " Standard")
                End If
                classAttr = ((classAttr + " ")  _
                            + (Regex.Replace(Request.Path.ToLower(), "\W", "_").Substring(1) + "_html"))
            Else
                If m_SummaryDisabled Then
                    classAttr = (classAttr + " see-all-always")
                End If
            End If
            If Not (String.IsNullOrEmpty(classAttr)) Then
                m_BodyAttributes("class") = classAttr.Trim()
            End If
            m_BodyTag.Text = String.Format("" & ControlChars.CrLf &"<body{0}>" & ControlChars.CrLf , m_BodyAttributes.ToString())
            MyBase.OnInit(e)
        End Sub
        
        Protected Function PreparePrefetch(ByVal content As String) As String
            Dim output As String = Nothing
            If (Not (String.IsNullOrEmpty(Request.Url.Query)) OrElse (Request.Headers("X-Cot-Manifest-Request") = "true")) Then
                Return output
            End If
            Dim token = ApplicationServices.TryGetJsonProperty(ApplicationServices.Current.DefaultSettings, "ui.history.dataView")
            Dim supportGridPrefetch = ((Not (token) Is Nothing) AndAlso Not (Regex.IsMatch(CType(token,String), "\b(search|sort|group|filter)\b")))
            Dim prefetches = New List(Of String)()
            Dim prefetch = false
            Dim dataViews = New List(Of Tuple(Of String, AttributeDictionary))()
            For Each m As Match in Regex.Matches(content, "<div\s+(id=""(?'Id'\w+)"")\s+(?'Props'data-controller.*?)>")
                dataViews.Add(New Tuple(Of String, AttributeDictionary)(m.Groups("Id").Value, New AttributeDictionary(m.Groups("Props").Value)))
            Next
            If (dataViews.Count = 1) Then
                prefetch = true
            Else
                'LEGACY MASTER DETAIL PAGE SUPPORT
                '
                '                      
                ' 1. convert text of containers into single container with single dataview referring to virtual dashboard controller
                '                      
                ' <div data-flow="row">
                '   <div id="view1" data-controller="Dashboards" data-view="form1" data-show-action-buttons="none"></div> 
                '
                ' </div>
                '
                ' 2. produce response for this controller.
                ' a. standalone data views become data view fields of the virtual controller
                ' b. the layout of the page is optionally converted into form1 layout of the virtual controller
                ' c. render json response of virtual controller with layout in it
                '                    
            End If
            If prefetch Then
                Dim i = 0
                Do While (i < dataViews.Count)
                    Dim dataView = dataViews(i)
                    Dim dataViewId = dataView.Item1
                    Dim attrs = dataView.Item2
                    For Each p in UnsupportedDataViewProperties
                        If attrs.ContainsKey(p) Then
                            Return output
                        End If
                    Next
                    Dim controllerName = attrs("data-controller")
                    Dim viewId As String = Nothing
                    Dim tags As String = Nothing
                    attrs.TryGetValue("data-tags", tags)
                    Dim c = Controller.CreateConfigurationInstance([GetType](), controllerName)
                    If Not (attrs.TryGetValue("data-view", viewId)) Then
                        viewId = CType(c.Evaluate("string(/c:dataController/c:views/c:view[1]/@id)"),String)
                    End If
                    Dim viewNav = c.SelectSingleNode("/c:dataController/c:views/c:view[@id='{0}']", viewId)
                    If (Not (Context.User.Identity.IsAuthenticated) AndAlso Not ((viewNav.GetAttribute("access", String.Empty) = "Public"))) Then
                        Return output
                    End If
                    Dim roles As String = Nothing
                    If (attrs.TryGetValue("data-roles", roles) AndAlso Not (New ControllerUtilities().UserIsInRole(roles.Split(Global.Microsoft.VisualBasic.ChrW(44))))) Then
                        Return output
                    End If
                    tags = (tags  _
                                + (" " + viewNav.GetAttribute("tags", String.Empty)))
                    Dim isForm = (viewNav.GetAttribute("type", String.Empty) = "Form")
                    If isForm Then
                        m_SummaryDisabled = true
                    End If
                    If (Not (Regex.IsMatch(tags, "\bprefetch-data-none\b")) AndAlso (supportGridPrefetch OrElse isForm)) Then
                        Dim request = New PageRequest(-1, 30, Nothing, Nothing)
                        request.Controller = controllerName
                        request.View = viewId
                        request.Tag = tags
                        request.ContextKey = dataViewId
                        request.SupportsCaching = true
                        If attrs.ContainsKey("data-search-on-start") Then
                            request.DoesNotRequireData = true
                        End If
                        Dim response = ControllerFactory.CreateDataController().GetPage(request.Controller, request.View, request)
                        Dim result = String.Format("{{ ""d"": {0} }}", ApplicationServices.CompressViewPageJsonOutput(JsonConvert.SerializeObject(response)))
                        prefetches.Add(String.Format("<script type=""application/json"" id=""_{0}_prefetch"">{1}</script>", dataViewId, Regex.Replace(result, "(<(/?\s*script)(\s|>))", "]_[$2$3]^[", RegexOptions.IgnoreCase)))
                        If isForm Then
                            For Each field in response.Fields
                                If (String.IsNullOrEmpty(field.DataViewFilterFields) AndAlso (field.Type = "DataView")) Then
                                    Dim fieldAttr = New AttributeDictionary(String.Empty)
                                    fieldAttr.Add("data-controller", field.DataViewController)
                                    fieldAttr.Add("data-view", field.DataViewId)
                                    fieldAttr.Add("data-tags", field.Tag)
                                    If field.DataViewSearchOnStart Then
                                        fieldAttr.Add("data-search-on-start", "true")
                                    End If
                                    dataViews.Add(New Tuple(Of String, AttributeDictionary)(String.Format("{0}_{1}", dataViewId, field.Name), fieldAttr))
                                End If
                            Next
                        End If
                    End If
                    i = (i + 1)
                Loop
            End If
            If (prefetches.Count > 0) Then
                output = String.Join(String.Empty, prefetches)
            End If
            Return output
        End Function
        
        Protected Overridable Sub InitializeSiteMaster()
            m_IsTouchUI = ApplicationServices.IsTouchClient
            Dim html = String.Empty
            Dim siteMasterPath = "~/site.desktop.html"
            If m_IsTouchUI Then
                siteMasterPath = "~/site.touch.html"
            End If
            siteMasterPath = Server.MapPath(siteMasterPath)
            If Not (File.Exists(siteMasterPath)) Then
                siteMasterPath = Server.MapPath("~/site.html")
            End If
            If File.Exists(siteMasterPath) Then
                html = File.ReadAllText(siteMasterPath)
            Else
                Throw New Exception("File site.html has not been found.")
            End If
            Dim htmlMatch = Regex.Match(html, "<html(?'HtmlAttr'[\S\s]*?)>\s*<head(?'HeadAttr'[\S\s]*?)>\s*(?'Head'[\S\s]*?)\s*<"& _ 
                    "/head>\s*<body(?'BodyAttr'[\S\s]*?)>\s*(?'Body'[\S\s]*?)\s*</body>\s*</html>\s*")
            If Not (htmlMatch.Success) Then
                Throw New Exception("File site.html must contain 'head' and 'body' elements.")
            End If
            'instructions
            Controls.Add(New LiteralControl(html.Substring(0, htmlMatch.Index)))
            'html
            Controls.Add(New LiteralControl(String.Format("<html{0} xml:lang={1} lang=""{1}"">" & ControlChars.CrLf , htmlMatch.Groups("HtmlAttr").Value, CultureInfo.CurrentUICulture.IetfLanguageTag)))
            'head
            Controls.Add(New HtmlHead())
            If m_IsTouchUI Then
                Header.Controls.Add(New LiteralControl("<meta charset=""utf-8"">" & ControlChars.CrLf ))
            Else
                Header.Controls.Add(New LiteralControl("<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">" & ControlChars.CrLf ))
            End If
            Dim headHtml = Regex.Replace(htmlMatch.Groups("Head").Value, "\s*<title([\s\S+]*?title>)\s*", String.Empty)
            Header.Controls.Add(New LiteralControl(headHtml))
            m_HeadContent = New LiteralContainer()
            Header.Controls.Add(m_HeadContent)
            'body
            m_BodyTag = New LiteralControl()
            m_BodyAttributes = New AttributeDictionary(htmlMatch.Groups("BodyAttr").Value)
            Controls.Add(m_BodyTag)
            Dim themePath = Server.MapPath("~/App_Themes/Norwindt")
            If Directory.Exists(themePath) Then
                For Each stylesheetFileName in Directory.GetFiles(themePath, "*.css")
                    Dim fileName = Path.GetFileName(stylesheetFileName)
                    If Not (fileName.Equals("_Theme_Aquarium.css")) Then
                        Dim link = New HtmlLink()
                        link.Href = ("~/App_Themes/Norwindt/" + fileName)
                        link.Attributes("type") = "text/css"
                        link.Attributes("rel") = "stylesheet"
                        Header.Controls.Add(link)
                    End If
                Next
            End If
            'form
            Controls.Add(New HtmlForm())
            Form.ID = "aspnetForm"
            'ScriptManager
            Dim sm = New ScriptManager()
            sm.ID = "sm"
            sm.AjaxFrameworkMode = AjaxFrameworkMode.Disabled
            If AquariumExtenderBase.EnableCombinedScript Then
                sm.EnableScriptLocalization = false
            End If
            sm.ScriptMode = ScriptMode.Release
            Form.Controls.Add(sm)
            'SiteMapDataSource
            Dim siteMapDataSource1 = New SiteMapDataSource()
            siteMapDataSource1.ID = "SiteMapDataSource1"
            siteMapDataSource1.ShowStartingNode = false
            Form.Controls.Add(siteMapDataSource1)
            'parse and initialize placeholders
            Dim body = htmlMatch.Groups("Body").Value
            Dim placeholderMatch = Regex.Match(body, "<div\s+data-role\s*=\s*""placeholder""(?'Attributes'[\s\S]+?)>\s*(?'DefaultContent'"& _ 
                    "[\s\S]*?)\s*</div>")
            Dim startPos = 0
            Do While placeholderMatch.Success
                Dim attributes = New AttributeDictionary(placeholderMatch.Groups("Attributes").Value)
                'create placeholder content
                Form.Controls.Add(New LiteralControl(body.Substring(startPos, (placeholderMatch.Index - startPos))))
                Dim placeholder = attributes("data-placeholder")
                Dim defaultContent = placeholderMatch.Groups("DefaultContent").Value
                If Not (CreatePlaceholder(Form.Controls, placeholder, defaultContent, attributes)) Then
                    Dim placeholderControl = New LiteralContainer()
                    placeholderControl.Text = defaultContent
                    Form.Controls.Add(placeholderControl)
                    If (placeholder = "page-header") Then
                        m_PageHeaderContent = placeholderControl
                    End If
                    If (placeholder = "page-title") Then
                        m_PageTitleContent = placeholderControl
                    End If
                    If (placeholder = "page-side-bar") Then
                        m_PageSideBarContent = placeholderControl
                    End If
                    If (placeholder = "page-content") Then
                        m_PageContent = placeholderControl
                    End If
                    If (placeholder = "page-footer") Then
                        m_PageFooterContent = placeholderControl
                    End If
                End If
                startPos = (placeholderMatch.Index + placeholderMatch.Length)
                placeholderMatch = placeholderMatch.NextMatch()
            Loop
            If (startPos < body.Length) Then
                Form.Controls.Add(New LiteralControl(body.Substring(startPos)))
            End If
            'end body
            Controls.Add(New LiteralControl("" & ControlChars.CrLf &"</body>" & ControlChars.CrLf ))
            'end html
            Controls.Add(New LiteralControl("" & ControlChars.CrLf &"</html>" & ControlChars.CrLf ))
        End Sub
        
        Protected Overridable Function CreatePlaceholder(ByVal container As ControlCollection, ByVal placeholder As String, ByVal defaultContent As String, ByVal attributes As AttributeDictionary) As Boolean
            If (placeholder = "membership-bar") Then
            End If
            If (placeholder = "menu-bar") Then
                Dim menuDiv = New HtmlGenericControl()
                menuDiv.TagName = "div"
                menuDiv.ID = "PageMenuBar"
                menuDiv.Attributes("class") = "PageMenuBar"
                container.Add(menuDiv)
                Dim menu = New MenuExtender()
                menu.ID = "Menu1"
                menu.DataSourceID = "SiteMapDataSource1"
                menu.TargetControlID = menuDiv.ID
                menu.HoverStyle = CType(TypeDescriptor.GetConverter(GetType(MenuHoverStyle)).ConvertFromString(attributes.ValueOf("data-hover-style", "Auto")),MenuHoverStyle)
                menu.PopupPosition = CType(TypeDescriptor.GetConverter(GetType(MenuPopupPosition)).ConvertFromString(attributes.ValueOf("data-popup-position", "Left")),MenuPopupPosition)
                menu.ShowSiteActions = (attributes("data-show-site-actions") = "true")
                menu.PresentationStyle = CType(TypeDescriptor.GetConverter(GetType(MenuPresentationStyle)).ConvertFromString(attributes.ValueOf("data-presentation-style", "MultiLevel")),MenuPresentationStyle)
                container.Add(menu)
                Return true
            End If
            If (placeholder = "site-map-path") Then
                Dim siteMapPath1 = New SiteMapPath()
                siteMapPath1.ID = "SiteMapPath1"
                siteMapPath1.CssClass = "SiteMapPath"
                siteMapPath1.PathSeparatorStyle.CssClass = "PathSeparator"
                siteMapPath1.CurrentNodeStyle.CssClass = "CurrentNode"
                siteMapPath1.NodeStyle.CssClass = "Node"
                siteMapPath1.RootNodeStyle.CssClass = "RootNode"
                container.Add(siteMapPath1)
                Return true
            End If
            Return false
        End Function
        
        Protected Overrides Sub OnPreRender(ByVal e As EventArgs)
            ApplicationServices.RegisterCssLinks(Me)
            If m_IsTouchUI Then
                'hide top-level literals
                For Each c As Control in Form.Controls
                    If TypeOf c Is LiteralControl Then
                        c.Visible = false
                    End If
                Next
                'look deep in children for ASP.NET controls
                HideAspNetControls(Form.Controls)
            End If
            MyBase.OnPreRender(e)
        End Sub
        
        Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)
            'create page content
            Dim sb = New StringBuilder()
            Dim w = New HtmlTextWriter(New StringWriter(sb))
            MyBase.Render(w)
            w.Flush()
            w.Close()
            Dim content = sb.ToString()
            If m_IsTouchUI Then
                'perform cleanup for super lightweight output
                content = Regex.Replace(content, "(<body([\s\S]*?)>\s*)<form\s+([\s\S]*?)</div>\s*", "$1")
                content = Regex.Replace(content, "\s*</form>\s*(</body>)", "" & ControlChars.CrLf &"$1")
                content = Regex.Replace(content, "<script(?'Attributes'[\s\S]*?)>(?'Script'[\s\S]*?)</script>\s*", AddressOf DoValidateScript)
                content = Regex.Replace(content, "<title>\s*([\s\S]+?)\s*</title>", "<title>$1</title>")
                content = Regex.Replace(content, "<div>\s*<input([\s\S]+?)VIEWSTATEGENERATOR([\s\S]+?)</div>", String.Empty)
                content = Regex.Replace(content, "<div.+?></div>.+?(<div.+?class=""PageMenuBar""></div>)\s*", String.Empty)
                content = Regex.Replace(content, "\$get\("".*?mb_d""\)", "null")
                content = Regex.Replace(content, "\s*(<footer[\s\S]+?</small></footer>)\s*", "$1")
                content = Regex.Replace(content, "\s*type=""text/javascript""\s*", " ")
            End If
            content = Regex.Replace(content, "(>\s+)//<\!\[CDATA\[\s*", "$1")
            content = Regex.Replace(content, "\s*//\]\]>\s*</script>", "" & ControlChars.CrLf &"</script>")
            content = Regex.Replace(content, "<div\s+data-role\s*=""placeholder""\s+(?'Attributes'[\s\S]+?)>(?'DefaultContent'[\s"& _ 
                    "\S]*?)</div>", AddressOf DoReplacePlaceholder)
            content = ResolveAppUrl(content)
            ApplicationServices.CompressOutput(Context, content)
        End Sub
        
        Private Function DoReplacePlaceholder(ByVal m As Match) As String
            Dim attributes = New AttributeDictionary(m.Groups("Attributes").Value)
            Dim defaultContent = m.Groups("DefaultContent").Value
            Dim replacement = ReplaceStaticPlaceholder(attributes("data-placeholder"), attributes, defaultContent)
            If (replacement Is Nothing) Then
                Return m.Value
            Else
                Return replacement
            End If
        End Function
        
        Public Overridable Function ReplaceStaticPlaceholder(ByVal name As String, ByVal attributes As AttributeDictionary, ByVal defaultContent As String) As String
            Return Nothing
        End Function
        
        Private Sub HideAspNetControls(ByVal controls As ControlCollection)
            Dim i = 0
            Do While (i < controls.Count)
                Dim c = controls(i)
                If (TypeOf c Is SiteMapPath OrElse (TypeOf c Is Image OrElse TypeOf c Is TreeView)) Then
                    controls.Remove(c)
                Else
                    HideAspNetControls(c.Controls)
                    i = (i + 1)
                End If
            Loop
        End Sub
        
        Private Function DoValidateScript(ByVal m As Match) As String
            Dim script = m.Groups("Script").Value
            If script.Contains("aspnetForm") Then
                Return String.Empty
            End If
            Dim srcMatch = Regex.Match(m.Groups("Attributes").Value, "src=""(.+?)""")
            If srcMatch.Success Then
                Dim src = srcMatch.Groups(1).Value
                If src.Contains(".axd?") Then
                    Try 
                        Dim client = New WebClient()
                        script = client.DownloadString(String.Format("http://{0}/{1}", Request.Url.Authority, src))
                    Catch __exception As Exception
                        Return script
                    End Try
                    If script.Contains("WebForm_PostBack") Then
                        Return String.Empty
                    End If
                End If
            End If
            script = m.Value.Replace("WebForm_InitCallback();", String.Empty)
            Return script
        End Function
    End Class
    
    Public Class LiteralContainer
        Inherits Panel
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Text As String
        
        Public Property Text() As String
            Get
                Return m_Text
            End Get
            Set
                m_Text = value
            End Set
        End Property
        
        Protected Overrides Sub Render(ByVal output As HtmlTextWriter)
            If (Controls.Count > 0) Then
                For Each c As Control in Controls
                    c.RenderControl(output)
                Next
            Else
                output.Write(Text)
            End If
        End Sub
    End Class
    
    Public Class AttributeDictionary
        Inherits SortedDictionary(Of String, String)
        
        Public Sub New(ByVal attributes As String)
            MyBase.New
            Parse(attributes)
        End Sub
        
        Public Shadows Default Property Item(ByVal name As String) As String
            Get
                Return Me.ValueOf(name, Nothing)
            End Get
            Set
                If (value Is Nothing) Then
                    Remove(name)
                Else
                    MyBase.Item(name) = value
                End If
            End Set
        End Property
        
        Public Function ValueOf(ByVal name As String, ByVal defaultValue As String) As String
            Dim v As String = Nothing
            If Not (TryGetValue(name, v)) Then
                v = defaultValue
            End If
            Return v
        End Function
        
        Public Sub Parse(ByVal attributes As String)
            Dim attributeMatch = Regex.Match(attributes, "\s*(?'Name'[\w\-]+?)\s*=\s*""(?'Value'.+?)""")
            Do While attributeMatch.Success
                Me(attributeMatch.Groups("Name").Value) = attributeMatch.Groups("Value").Value
                attributeMatch = attributeMatch.NextMatch()
            Loop
        End Sub
        
        Public Overrides Function ToString() As String
            Dim sb = New StringBuilder()
            For Each name in Keys
                sb.AppendFormat(" {0}=""{1}""", name, Me(name))
            Next
            Return sb.ToString()
        End Function
    End Class
End Namespace
