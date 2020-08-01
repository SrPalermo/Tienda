Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Norwindt.Data
Imports Norwindt.Handlers
Imports Norwindt.Web
Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Configuration
Imports System.Data
Imports System.Data.Common
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports System.Net
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Security.Principal
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Web
Imports System.Web.Caching
Imports System.Web.Configuration
Imports System.Web.Routing
Imports System.Web.Security
Imports System.Web.SessionState
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Xml.XPath

Namespace Norwindt.Services
    
    Public MustInherit Class ServiceRequestHandler
        
        Public Overridable ReadOnly Property AllowedMethods() As String()
            Get
                Return New String() {"POST"}
            End Get
        End Property
        
        Public Overridable ReadOnly Property RequiresAuthentication() As Boolean
            Get
                Return false
            End Get
        End Property
        
        Public MustOverride Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
        
        Public Overridable Function HandleException(ByVal args As JObject, ByVal ex As Exception) As Object
            Return ApplicationServices.Current.HandleException(args, ex)
        End Function
        
        Public Shared Sub Redirect(ByVal redirectUrl As String)
            Throw New ServiceRequestRedirectException(redirectUrl)
        End Sub
    End Class
    
    Public Class GetPageServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim r = args("request").ToObject(Of PageRequest)()
            Return service.GetPage(ControllerUtilities.ValidateName(CType(args("controller"),String)), ControllerUtilities.ValidateName(CType(args("view"),String)), r)
        End Function
    End Class
    
    Public Class GetControllerListServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim jsonArray = New StringBuilder("[")
            Dim list = args("controllers").ToObject(Of String())()
            Dim first = true
            For Each name in list
                If first Then
                    first = false
                Else
                    jsonArray.Append(",")
                End If
                Dim config = DataControllerBase.CreateConfigurationInstance([GetType](), name)
                Dim json = config.ToJson()
                jsonArray.Append(json)
            Next
            jsonArray.Append("]")
            Return jsonArray.ToString()
        End Function
    End Class
    
    Public Class CommitServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim tm = New TransactionManager()
            Return tm.Commit(CType(args("log"),JArray))
        End Function
    End Class
    
    Public Class GetPageListServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.GetPageList(args("requests").ToObject(Of PageRequest())())
        End Function
    End Class
    
    Public Class GetListOfValuesServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim r = args("request").ToObject(Of DistinctValueRequest)()
            Return service.GetListOfValues(ControllerUtilities.ValidateName(CType(args("controller"),String)), ControllerUtilities.ValidateName(CType(args("view"),String)), r)
        End Function
    End Class
    
    Public Class ExecuteServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim a = args("args").ToObject(Of ActionArgs)()
            Return service.Execute(ControllerUtilities.ValidateName(CType(args("controller"),String)), ControllerUtilities.ValidateName(CType(args("view"),String)), a)
        End Function
    End Class
    
    Public Class ExecuteAndGetPageServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim arg = args.ToObject(Of ExecuteViewPageArgs)()
            Dim a = arg.Args
            Dim result = service.Execute(a.Controller, a.View, a)
            If (result.Errors.Count > 0) Then
                Dim vp = New ViewPage()
                vp.Errors = result.Errors
                Return vp
            Else
                Dim request = New PageRequest(0, arg.PageSize, String.Empty, Nothing)
                request.Controller = a.Controller
                request.View = a.View
                request.LastCommandName = a.CommandName
                request.LastCommandArgument = a.CommandArgument
                request.RequiresMetaData = arg.Metadata
                request.DoesNotRequireAggregates = Not (arg.Aggregates)
                request.RequiresRowCount = arg.RowCount
                request.SyncKey = GetPrimaryKey(result, a)
                Return service.GetPage(a.Controller, a.View, request)
            End If
        End Function
        
        Private Function GetPrimaryKey(ByVal result As ActionResult, ByVal args As ActionArgs) As Object()
            Dim config = Controller.CreateConfigurationInstance([GetType](), args.Controller)
            Dim pKeys = New SortedDictionary(Of String, FieldValue)()
            For Each nav As XPathNavigator in config.Select("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
                For Each fvo in result.Values
                    If (fvo.Name = nav.GetAttribute("name", String.Empty)) Then
                        pKeys(fvo.Name) = fvo
                        Exit For
                    End If
                Next
                For Each fvo in args.Values
                    If (fvo.Name = nav.GetAttribute("name", String.Empty)) Then
                        pKeys(fvo.Name) = fvo
                        Exit For
                    End If
                Next
            Next
            Dim key = New List(Of Object)()
            For Each fvo in pKeys.Values
                key.Add(fvo.Value)
            Next
            Return key.ToArray()
        End Function
    End Class
    
    Public Class ExecuteListServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.ExecuteList(args("requests").ToObject(Of ActionArgs())())
        End Function
    End Class
    
    Public Class GetCompletionListServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.GetCompletionList(CType(args("prefixText"),String), CType(args("count"),Integer), CType(args("contextKey"),String))
        End Function
    End Class
    
    Public Class LoginServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.Login(CType(args("username"),String), CType(args("password"),String), CType(args("createPersistentCookie"),Boolean))
        End Function
    End Class
    
    Public Class LogoutServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            service.Logout()
            Return Nothing
        End Function
    End Class
    
    Public Class RolesServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.Roles()
        End Function
    End Class
    
    Public Class AddonServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim type = CType(args("type"),String)
            Dim method = CType(args("method"),String)
            Dim result As Object = Nothing
            For Each addon in ApplicationServices.Addons
                Dim t = addon.GetType()
                If ((t.Name = type) OrElse (type = "All")) Then
                    result = t.GetMethod("Invoke").Invoke(addon, New Object() {service, method, args("args")})
                    If Not ((type = "All")) Then
                        Exit For
                    End If
                End If
            Next
            Return result
        End Function
    End Class
    
    Public Class ThemesServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.Themes()
        End Function
    End Class
    
    Public Class SavePermalinkServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            service.SavePermalink(CType(args("link"),String), CType(args("html"),String))
            Return Nothing
        End Function
    End Class
    
    Public Class EncodePermalinkServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.EncodePermalink(CType(args("link"),String), CType(args("rooted"),Boolean))
        End Function
    End Class
    
    Public Class ListAllPermalinksServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.ListAllPermalinks()
        End Function
    End Class
    
    Public Class GetSurveyServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Return service.GetSurvey(CType(args("name"),String))
        End Function
    End Class
    
    Public Class GetIdentityServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides ReadOnly Property AllowedMethods() As String()
            Get
                Return New String() {"GET", "POST"}
            End Get
        End Property
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim user = Membership.GetUser()
            Dim res = HttpContext.Current.Response
            If Not (ApplicationServicesBase.AuthorizationIsSupported) Then
                res.Write("<h1>This app does not have a built-in security system and cannot run in native mo"& _ 
                        "de. Add membership support to the app.</h1>")
                res.End()
            End If
            If ((user Is Nothing) OrElse Not ((HttpContext.Current.Request.QueryString("force") = "false"))) Then
                FormsAuthentication.SignOut()
                Dim returnUrl = (HttpContext.Current.Request.ApplicationPath.TrimEnd(Global.Microsoft.VisualBasic.ChrW(47)) + "/_invoke/getidentity?force=false")
                res.Redirect(String.Format("{0}?ReturnUrl={1}&_accMan=login", FormsAuthentication.LoginUrl, HttpUtility.UrlEncode(returnUrl)), true)
            Else
                Dim ticket = ApplicationServices.Current.CreateTicket(user, Nothing)
                ticket.Claims.Add("deviceId", Guid.NewGuid().ToString().Replace("-", String.Empty))
                ticket.Claims.Add("culture", CultureInfo.CurrentUICulture.Name)
                res.Clear()
                res.Write("<html><body><script>")
                Dim token = JsonConvert.SerializeObject(ticket, Formatting.None)
                Dim userAgent = HttpContext.Current.Request.UserAgent
                If (userAgent.Contains("UMA-iOS") OrElse userAgent.Contains("UMA-OSX")) Then
                    res.Write("window.webkit.messageHandlers.invoke.postMessage({ method: 'addidentity', args: ")
                    res.Write(token)
                    res.Write("});")
                Else
                    If userAgent.Contains("UMA-W7") Then
                        res.Write("CefSharp.BindObjectAsync('CloudOnTime').then(function(){window.CloudOnTime.invoke"& _ 
                                "('addidentity', '")
                        res.Write(HttpUtility.UrlPathEncode(token))
                        res.Write("')});")
                    Else
                        res.Write("function add() {window.CloudOnTime.invoke('addidentity', '")
                        res.Write(HttpUtility.UrlPathEncode(token))
                        res.Write("');}if(typeof(CefSharp)!='undefined')CefSharp.BindObjectAsync('CloudOnTime').then"& _ 
                                "(add);else add();")
                    End If
                End If
                res.Write("</script></body></html>")
                res.End()
            End If
            Return Nothing
        End Function
        
        Public Overrides Function HandleException(ByVal args As JObject, ByVal ex As Exception) As Object
            If (ex.GetType() = GetType(ThreadAbortException)) Then
                Return MyBase.HandleException(args, ex)
            End If
            Throw ex
        End Function
    End Class
    
    Public Class GetManifestServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides ReadOnly Property AllowedMethods() As String()
            Get
                Return New String() {"GET", "POST"}
            End Get
        End Property
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim services = ApplicationServicesBase.Current
            Dim response = New JObject()
            Dim settings = services.DefaultSettings
            response("name") = ApplicationServicesBase.TryGetJsonProperty(settings, "appName")
            If String.IsNullOrEmpty(CType(response("name"),String)) Then
                response("name") = services.DisplayName
            End If
            response("appVersion") = ApplicationServices.Version
            response("hostVersion") = ApplicationServices.HostVersion
            response("copyright") = SiteBase.Copyright
            response("home") = services.UserHomePageUrl()
            response("icon") = ApplicationServicesBase.TryGetJsonProperty(settings, "host.icon")
            response("color") = ApplicationServicesBase.TryGetJsonProperty(settings, "host.color")
            response("image") = ApplicationServicesBase.TryGetJsonProperty(settings, "host.image")
            response("description") = ApplicationServicesBase.TryGetJsonProperty(settings, "host.description")
            response("defaultTheme") = ApplicationServicesBase.TryGetJsonProperty(settings, "ui.theme.name")
            response("defaultAccent") = ApplicationServicesBase.TryGetJsonProperty(settings, "ui.theme.accent")
            Dim themeInfo = services.UserThemes()
            response("themes") = themeInfo("themes")
            response("accents") = themeInfo("accents")
            If HttpContext.Current.Request.IsAuthenticated Then
                Dim files = New List(Of ManifestFile)()
                Dim reqFiles = New List(Of ManifestFile)()
                'enumerate files to download
                AddSiteFiles(files, "css")
                AddSiteFiles(files, "js")
                AddSiteFiles(files, "fonts")
                AddSiteFiles(files, "images")
                AddSiteFiles(files, "pages")
                files.Add(ManifestFile.FromPath("js/daf/add.min.js"))
                files.Add(ManifestFile.FromPath("css/daf/add.min.css"))
                files.Add(ManifestFile.FromPath("touch-settings.json"))
                'add config.js
                Dim pageContent = GetConfigJS()
                response("config") = JObject.FromObject(ManifestFile.GetConfig(pageContent))
                'build required js
                Dim combinedScript = AquariumExtenderBase.EnableCombinedScript
                AquariumExtenderBase.EnableCombinedScript = true
                For Each s in AquariumExtenderBase.StandardScripts(true)
                    If Not (String.IsNullOrEmpty(s.Path)) Then
                        reqFiles.Add(ManifestFile.FromPath(s.Path))
                    Else
                        Dim f = ManifestFile.FromResource(s.Name)
                        files.Add(f)
                        reqFiles.Add(f)
                    End If
                Next
                AquariumExtenderBase.EnableCombinedScript = combinedScript
                Dim require = New JObject()
                response("require") = require
                require("js") = JArray.FromObject(reqFiles)
                reqFiles.Clear()
                'build required css
                For Each css in services.EnumerateTouchUIStylesheets()
                    If Not (Path.GetFileName(css).StartsWith("touch-theme")) Then
                        reqFiles.Add(ManifestFile.FromPath(css))
                    End If
                Next
                require("css") = JArray.FromObject(reqFiles)
                'complete files
                response("files") = JArray.FromObject(files)
            End If
            Return response
        End Function
        
        Public Overrides Function HandleException(ByVal args As JObject, ByVal ex As Exception) As Object
            Throw ex
        End Function
        
        Sub AddSiteFiles(ByVal files As List(Of ManifestFile), ByVal siteFolder As String)
            Dim rootPath = HttpContext.Current.Server.MapPath("~\")
            For Each file in Directory.EnumerateFiles(Path.Combine(rootPath, siteFolder), "*.*", SearchOption.AllDirectories)
                Dim m = Regex.Match(file, "\.(?'Culture'\w\w(-\w\w)?)\.(\w+)$")
                If (Not (m.Success) OrElse (m.Groups("Culture").Value = Thread.CurrentThread.CurrentUICulture.Name)) Then
                    Dim relativePath = file.Substring(rootPath.Length)
                    If Path.GetExtension(file).ToLower().Equals(".html") Then
                        files.Add(ManifestFile.FromUrl(relativePath))
                    Else
                        files.Add(ManifestFile.FromPath(relativePath))
                    End If
                End If
            Next
        End Sub
        
        Public Shared Function GetConfigJS() As String
            Dim m = Regex.Match(ManifestFile.GetContent(ApplicationServices.HomePageUrl), "<\/footer><script >\s*(?'Config'[\S\s]+?)<\/script>")
            If Not (m.Success) Then
                Throw New Exception("Home Page failed to process.")
            End If
            Return m.Groups("Config").Value
        End Function
    End Class
    
    Public Class GetManifestFileServiceRequestHandler
        Inherits ServiceRequestHandler
        
        Public Overrides ReadOnly Property RequiresAuthentication() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides Function HandleRequest(ByVal service As DataControllerService, ByVal args As JObject) As Object
            Dim context = HttpContext.Current
            Dim requestedFile = CType(args("file"),String)
            If String.IsNullOrEmpty(requestedFile) Then
                Throw New HttpException(400, "File argument is required.")
            End If
            requestedFile = requestedFile.Replace(Global.Microsoft.VisualBasic.ChrW(47), Global.Microsoft.VisualBasic.ChrW(92))
            If (requestedFile = "js\host\config.js") Then
                context.Response.Write(GetManifestServiceRequestHandler.GetConfigJS())
            Else
                If requestedFile.StartsWith("_resources\") Then
                    Dim resourceName = requestedFile.Substring(11)
                    Using s = ControllerConfigurationUtility.GetResourceStream(resourceName)
                        If (Not (s) Is Nothing) Then
                            s.CopyTo(context.Response.OutputStream)
                        Else
                            Throw New HttpException(404, "File does not exist.")
                        End If
                    End Using
                Else
                    If (requestedFile = "js\daf\add.min.js") Then
                        context.Response.Write(ApplicationServices.Current.AddScripts())
                    Else
                        If (requestedFile = "css\daf\add.min.css") Then
                            context.Response.Write(ApplicationServices.Current.AddStyleSheets())
                        Else
                            If Path.GetExtension(requestedFile).StartsWith(".htm") Then
                                context.Response.Write(ManifestFile.GetContent(("~/" + requestedFile.Substring(0, requestedFile.IndexOf(Global.Microsoft.VisualBasic.ChrW(46))).Replace(Global.Microsoft.VisualBasic.ChrW(92), Global.Microsoft.VisualBasic.ChrW(47)))))
                            Else
                                Dim p = context.Server.MapPath(("~\" + requestedFile))
                                If File.Exists(p) Then
                                    Using f = File.OpenRead(p)
                                        f.CopyTo(context.Response.OutputStream)
                                    End Using
                                Else
                                    Throw New HttpException(404, String.Format("File {0} does not exist.", p))
                                End If
                            End If
                        End If
                    End If
                End If
            End If
            context.Response.End()
            Return Nothing
        End Function
        
        Public Overrides Function HandleException(ByVal args As JObject, ByVal ex As Exception) As Object
            If (ex.GetType() = GetType(ThreadAbortException)) Then
                Return Nothing
            End If
            Throw ex
        End Function
    End Class
    
    Public Class ServiceRequestError
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ExceptionType As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Message As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_StackTrace As String
        
        Public Property ExceptionType() As String
            Get
                Return m_ExceptionType
            End Get
            Set
                m_ExceptionType = value
            End Set
        End Property
        
        Public Property Message() As String
            Get
                Return m_Message
            End Get
            Set
                m_Message = value
            End Set
        End Property
        
        Public Property StackTrace() As String
            Get
                Return m_StackTrace
            End Get
            Set
                m_StackTrace = value
            End Set
        End Property
    End Class
    
    Public Class ServiceRequestRedirectException
        Inherits Exception
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RedirectUrl As String
        
        Public Sub New(ByVal redirectUrl As String)
            MyBase.New
            Me.RedirectUrl = redirectUrl
        End Sub
        
        Public Overridable Property RedirectUrl() As String
            Get
                Return m_RedirectUrl
            End Get
            Set
                m_RedirectUrl = value
            End Set
        End Property
    End Class
    
    Partial Public Class RequestValidationService
        Inherits RequestValidationServiceBase
    End Class
    
    Public Class RequestValidationServiceBase
        
        Public Shared ValidRequestRegex As Regex = New Regex("<[^\w<>]*(?:[^<>""'\s]*:)?[^\w<>]*(?:\W*s\W*c\W*r\W*i\W*p\W*t|\W*f\W*o\W*r\W*m|\W*"& _ 
                "s\W*t\W*y\W*l\W*e|\W*s\W*v\W*g|\W*m\W*a\W*r\W*q\W*u\W*e\W*e|(?:\W*l\W*i\W*n\W*k|"& _ 
                "\W*o\W*b\W*j\W*e\W*c\W*t|\W*e\W*m\W*b\W*e\W*d|\W*a\W*p\W*p\W*l\W*e\W*t|\W*p\W*a\"& _ 
                "W*r\W*a\W*m|\W*i?\W*f\W*r\W*a\W*m\W*e|\W*b\W*a\W*s\W*e|\W*b\W*o\W*d\W*y|\W*m\W*e"& _ 
                "\W*t\W*a|\W*i\W*m\W*a?\W*g\W*e?|\W*v\W*i\W*d\W*e\W*o|\W*a\W*u\W*d\W*i\W*o|\W*b\W"& _ 
                "*i\W*n\W*d\W*i\W*n\W*g\W*s|\W*s\W*e\W*t|\W*i\W*s\W*i\W*n\W*d\W*e\W*x|\W*a\W*n\W*"& _ 
                "i\W*m\W*a\W*t\W*e)[^>\w])|(?:<\w[\s\S]*[\s\0\/]|['""])(?:formaction|style|backgro"& _ 
                "und|src|lowsrc|ping|on(?:d(?:e(?:vice(?:(?:orienta|mo)tion|proximity|found|light"& _ 
                ")|livery(?:success|error)|activate)|r(?:ag(?:e(?:n(?:ter|d)|xit)|(?:gestur|leav)"& _ 
                "e|start|drop|over)?|op)|i(?:s(?:c(?:hargingtimechange|onnect(?:ing|ed))|abled)|a"& _ 
                "ling)|ata(?:setc(?:omplete|hanged)|(?:availabl|chang)e|error)|urationchange|ownl"& _ 
                "oading|blclick)|Moz(?:M(?:agnifyGesture(?:Update|Start)?|ouse(?:PixelScroll|Hitt"& _ 
                "est))|S(?:wipeGesture(?:Update|Start|End)?|crolledAreaChanged)|(?:(?:Press)?TapG"& _ 
                "estur|BeforeResiz)e|EdgeUI(?:C(?:omplet|ancel)|Start)ed|RotateGesture(?:Update|S"& _ 
                "tart)?|A(?:udioAvailable|fterPaint))|c(?:o(?:m(?:p(?:osition(?:update|start|end)"& _ 
                "|lete)|mand(?:update)?)|n(?:t(?:rolselect|extmenu)|nect(?:ing|ed))|py)|a(?:(?:ll"& _ 
                "schang|ch)ed|nplay(?:through)?|rdstatechange)|h(?:(?:arging(?:time)?ch)?ange|eck"& _ 
                "ing)|(?:fstate|ell)change|u(?:echange|t)|l(?:ick|ose))|m(?:o(?:z(?:pointerlock(?"& _ 
                ":change|error)|(?:orientation|time)change|fullscreen(?:change|error)|network(?:d"& _ 
                "own|up)load)|use(?:(?:lea|mo)ve|o(?:ver|ut)|enter|wheel|down|up)|ve(?:start|end)"& _ 
                "?)|essage|ark)|s(?:t(?:a(?:t(?:uschanged|echange)|lled|rt)|k(?:sessione|comma)nd"& _ 
                "|op)|e(?:ek(?:complete|ing|ed)|(?:lec(?:tstar)?)?t|n(?:ding|t))|u(?:ccess|spend|"& _ 
                "bmit)|peech(?:start|end)|ound(?:start|end)|croll|how)|b(?:e(?:for(?:e(?:(?:scrip"& _ 
                "texecu|activa)te|u(?:nload|pdate)|p(?:aste|rint)|c(?:opy|ut)|editfocus)|deactiva"& _ 
                "te)|gin(?:Event)?)|oun(?:dary|ce)|l(?:ocked|ur)|roadcast|usy)|a(?:n(?:imation(?:"& _ 
                "iteration|start|end)|tennastatechange)|fter(?:(?:scriptexecu|upda)te|print)|udio"& _ 
                "(?:process|start|end)|d(?:apteradded|dtrack)|ctivate|lerting|bort)|DOM(?:Node(?:"& _ 
                "Inserted(?:IntoDocument)?|Removed(?:FromDocument)?)|(?:CharacterData|Subtree)Mod"& _ 
                "ified|A(?:ttrModified|ctivate)|Focus(?:Out|In)|MouseScroll)|r(?:e(?:s(?:u(?:m(?:"& _ 
                "ing|e)|lt)|ize|et)|adystatechange|pea(?:tEven)?t|movetrack|trieving|ceived)|ow(?"& _ 
                ":s(?:inserted|delete)|e(?:nter|xit))|atechange)|p(?:op(?:up(?:hid(?:den|ing)|sho"& _ 
                "w(?:ing|n))|state)|a(?:ge(?:hide|show)|(?:st|us)e|int)|ro(?:pertychange|gress)|l"& _ 
                "ay(?:ing)?)|t(?:ouch(?:(?:lea|mo)ve|en(?:ter|d)|cancel|start)|ime(?:update|out)|"& _ 
                "ransitionend|ext)|u(?:s(?:erproximity|sdreceived)|p(?:gradeneeded|dateready)|n(?"& _ 
                ":derflow|load))|f(?:o(?:rm(?:change|input)|cus(?:out|in)?)|i(?:lterchange|nish)|"& _ 
                "ailed)|l(?:o(?:ad(?:e(?:d(?:meta)?data|nd)|start)?|secapture)|evelchange|y)|g(?:"& _ 
                "amepad(?:(?:dis)?connected|button(?:down|up)|axismove)|et)|e(?:n(?:d(?:Event|ed)"& _ 
                "?|abled|ter)|rror(?:update)?|mptied|xit)|i(?:cc(?:cardlockerror|infochange)|n(?:"& _ 
                "coming|valid|put))|o(?:(?:(?:ff|n)lin|bsolet)e|verflow(?:changed)?|pen)|SVG(?:(?"& _ 
                ":Unl|L)oad|Resize|Scroll|Abort|Error|Zoom)|h(?:e(?:adphoneschange|l[dp])|ashchan"& _ 
                "ge|olding)|v(?:o(?:lum|ic)e|ersion)change|w(?:a(?:it|rn)ing|heel)|key(?:press|do"& _ 
                "wn|up)|(?:AppComman|Loa)d|no(?:update|match)|Request|zoom))[\s\0]*=")
        
        Public Shared Function ToJson(ByVal context As HttpContext) As JObject
            Dim service = New RequestValidationService()
            Dim data((context.Request.InputStream.Length) - 1) As Byte
            context.Request.InputStream.Read(data, 0, data.Length)
            Dim args = service.ValidateJson(Encoding.UTF8.GetString(data), context)
            Dim json As JObject = Nothing
            If Not (String.IsNullOrEmpty(args)) Then
                json = service.ValidateJson(JObject.Parse(args), context)
            End If
            context.Items("ServiceRequestHandler_args") = json
            Return json
        End Function
        
        Public Overloads Overridable Function ValidateJson(ByVal json As String, ByVal context As HttpContext) As String
            If ValidRequestRegex.IsMatch(json) Then
                Throw New HttpException(400, "Bad Request")
            End If
            Return HttpUtility.HtmlDecode(json)
        End Function
        
        Public Overloads Overridable Function ValidateJson(ByVal json As JObject, ByVal context As HttpContext) As JObject
            Dim isBad = false
            If (Not (json("IgnoreBusinessRules")) Is Nothing) Then
                isBad = true
            End If
            If (Not (json("requests")) Is Nothing) Then
                Dim list = CType(json("requests"),JArray)
                For Each args in list.Values(Of JObject)()
                    If (Not (args("IgnoreBusinessRules")) Is Nothing) Then
                        isBad = true
                        Exit For
                    End If
                Next
            End If
            If isBad Then
                Throw New HttpException(400, "Bad Request")
            End If
            Return json
        End Function
    End Class
    
    Public Class WorkflowResources
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_StaticResources As SortedDictionary(Of String, String)
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DynamicResources As List(Of Regex)
        
        Public Sub New()
            MyBase.New
            m_StaticResources = New SortedDictionary(Of String, String)()
            m_DynamicResources = New List(Of Regex)()
        End Sub
        
        Public Property StaticResources() As SortedDictionary(Of String, String)
            Get
                Return m_StaticResources
            End Get
            Set
                m_StaticResources = value
            End Set
        End Property
        
        Public Property DynamicResources() As List(Of Regex)
            Get
                Return m_DynamicResources
            End Get
            Set
                m_DynamicResources = value
            End Set
        End Property
    End Class
    
    Partial Public Class WorkflowRegister
        Inherits WorkflowRegisterBase
    End Class
    
    Public Class WorkflowRegisterBase
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Resources As SortedDictionary(Of String, WorkflowResources)
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RoleRegister As SortedDictionary(Of String, List(Of String))
        
        Public Sub New()
            MyBase.New
            'initialize system workflows
            m_Resources = New SortedDictionary(Of String, WorkflowResources)()
            RegisterBuiltinWorkflowResources()
            For Each w in ApplicationServices.Current.ReadSiteContent("sys/workflows%", "%")
                Dim text = w.Text
                If Not (String.IsNullOrEmpty(text)) Then
                    Dim wr As WorkflowResources = Nothing
                    If Not (Resources.TryGetValue(w.PhysicalName, wr)) Then
                        wr = New WorkflowResources()
                        Resources(w.PhysicalName) = wr
                    End If
                    For Each s in text.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(10)}, StringSplitOptions.RemoveEmptyEntries)
                        Dim query = s.Trim()
                        If Not (String.IsNullOrEmpty(query)) Then
                            If s.StartsWith("regex ") Then
                                Dim regexQuery = s.Substring(6).Trim()
                                If Not (String.IsNullOrEmpty(regexQuery)) Then
                                    Try 
                                        wr.DynamicResources.Add(New Regex(regexQuery, RegexOptions.IgnoreCase))
                                    Catch __exception As Exception
                                    End Try
                                End If
                            Else
                                wr.StaticResources(query.ToLower()) = query
                            End If
                        End If
                    Next
                End If
            Next
            'read "role" workflows from the register
            m_RoleRegister = New SortedDictionary(Of String, List(Of String))()
            For Each rr in ApplicationServices.Current.ReadSiteContent("sys/register/roles%", "%")
                Dim text = rr.Text
                If Not (String.IsNullOrEmpty(text)) Then
                    Dim workflows As List(Of String) = Nothing
                    If Not (RoleRegister.TryGetValue(rr.PhysicalName, workflows)) Then
                        workflows = New List(Of String)()
                        RoleRegister(rr.PhysicalName) = workflows
                    End If
                    For Each s in text.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(10), Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries)
                        Dim name = s.Trim()
                        If Not (String.IsNullOrEmpty(name)) Then
                            workflows.Add(name)
                        End If
                    Next
                End If
            Next
        End Sub
        
        Public Property Resources() As SortedDictionary(Of String, WorkflowResources)
            Get
                Return m_Resources
            End Get
            Set
                m_Resources = value
            End Set
        End Property
        
        Public Property RoleRegister() As SortedDictionary(Of String, List(Of String))
            Get
                Return m_RoleRegister
            End Get
            Set
                m_RoleRegister = value
            End Set
        End Property
        
        Public ReadOnly Property UserWorkflows() As List(Of String)
            Get
                Dim workflows = CType(HttpContext.Current.Items("WorkflowRegister_UserWorkflows"),List(Of String))
                If (workflows Is Nothing) Then
                    workflows = New List(Of String)()
                    Dim identity = HttpContext.Current.User.Identity
                    If identity.IsAuthenticated Then
                        For Each urf in ApplicationServices.Current.ReadSiteContent("sys/register/users%", identity.Name)
                            Dim text = urf.Text
                            If Not (String.IsNullOrEmpty(text)) Then
                                For Each s in text.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(10), Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries)
                                    Dim name = s.Trim()
                                    If (Not (String.IsNullOrEmpty(name)) AndAlso Not (workflows.Contains(name))) Then
                                        workflows.Add(name)
                                    End If
                                Next
                            End If
                        Next
                    End If
                    'enumerate role workflows
                    Dim isAuthenticated = HttpContext.Current.User.Identity.IsAuthenticated
                    For Each role in RoleRegister.Keys
                        If ((((role = "?") AndAlso Not (isAuthenticated)) OrElse ((role = "*") AndAlso isAuthenticated)) OrElse DataControllerBase.UserIsInRole(role)) Then
                            For Each name in RoleRegister(role)
                                If Not (workflows.Contains(name)) Then
                                    workflows.Add(name)
                                End If
                            Next
                        End If
                    Next
                    HttpContext.Current.Items("WorkflowRegister_UserWorkflows") = workflows
                End If
                Return workflows
            End Get
        End Property
        
        Public ReadOnly Property Enabled() As Boolean
            Get
                Return (m_Resources.Count > 0)
            End Get
        End Property
        
        Public Shared ReadOnly Property IsEnabled() As Boolean
            Get
                If Not (ApplicationServices.IsSiteContentEnabled) Then
                    Return false
                End If
                Dim wr = WorkflowRegister.GetCurrent()
                Return ((Not (wr) Is Nothing) AndAlso wr.Enabled)
            End Get
        End Property
        
        Public Overridable ReadOnly Property CacheDuration() As Integer
            Get
                Return 30
            End Get
        End Property
        
        Protected Overridable Sub RegisterBuiltinWorkflowResources()
        End Sub
        
        Public Shared Function Allows(ByVal fileName As String) As Boolean
            If Not (ApplicationServices.IsSiteContentEnabled) Then
                Return false
            End If
            Dim wr = WorkflowRegister.GetCurrent(fileName)
            If ((wr Is Nothing) OrElse Not (wr.Enabled)) Then
                Return false
            End If
            Return wr.IsMatch(fileName)
        End Function
        
        Public Overloads Function IsMatch(ByVal physicalPath As String, ByVal physicalName As String) As Boolean
            Dim fileName = physicalPath
            If String.IsNullOrEmpty(fileName) Then
                fileName = physicalName
            Else
                fileName = ((fileName + "/")  _
                            + physicalName)
            End If
            Return IsMatch(fileName)
        End Function
        
        Public Overloads Function IsMatch(ByVal fileName As String) As Boolean
            fileName = fileName.ToLower()
            Dim activeWorkflows = UserWorkflows
            For Each wf in activeWorkflows
                Dim resourceList As WorkflowResources = Nothing
                If Resources.TryGetValue(wf, resourceList) Then
                    If resourceList.StaticResources.ContainsKey(fileName) Then
                        Return true
                    End If
                    For Each re in resourceList.DynamicResources
                        If re.IsMatch(fileName) Then
                            Return true
                        End If
                    Next
                End If
            Next
            Return false
        End Function
        
        Public Overloads Shared Function GetCurrent() As WorkflowRegister
            Return GetCurrent(Nothing)
        End Function
        
        Public Overloads Shared Function GetCurrent(ByVal relativePath As String) As WorkflowRegister
            If Not (ApplicationServicesBase.Create().Supports(ApplicationFeature.WorkflowRegister)) Then
                Return Nothing
            End If
            If ((Not (relativePath) Is Nothing) AndAlso (relativePath.StartsWith("sys/workflows") OrElse relativePath.StartsWith("sys/register"))) Then
                Return Nothing
            End If
            Dim key = "WorkflowRegister_Current"
            Dim context = HttpContext.Current
            Dim instance = CType(context.Items(key),WorkflowRegister)
            If (instance Is Nothing) Then
                instance = CType(context.Cache(key),WorkflowRegister)
                If (instance Is Nothing) Then
                    instance = New WorkflowRegister()
                    context.Cache.Add(key, instance, Nothing, DateTime.Now.AddSeconds(instance.CacheDuration), Cache.NoSlidingExpiration, CacheItemPriority.AboveNormal, Nothing)
                End If
                context.Items(key) = instance
            End If
            Return instance
        End Function
    End Class
    
    Public Enum SiteContentFields
        
        SiteContentId
        
        DataFileName
        
        DataContentType
        
        Length
        
        Path
        
        Data
        
        Roles
        
        Users
        
        Text
        
        CacheProfile
        
        RoleExceptions
        
        UserExceptions
        
        Schedule
        
        ScheduleExceptions
        
        CreatedDate
        
        ModifiedDate
    End Enum
    
    Public Class SiteContentFile
        
        Private m_Id As Object
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Name As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Path As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ContentType As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Length As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Data() As Byte
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_PhysicalName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Error As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Schedule As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ScheduleExceptions As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CreatedDate As DateTime
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ModifiedDate As DateTime
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CacheProfile As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CacheDuration As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CacheLocation As HttpCacheability
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CacheVaryByParams() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CacheVaryByHeaders() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CacheNoStore As Boolean
        
        Public Sub New()
            MyBase.New
            Me.CacheLocation = HttpCacheability.NoCache
        End Sub
        
        Public Property Id() As Object
            Get
                Return m_Id
            End Get
            Set
                If ((Not (value) Is Nothing) AndAlso TypeOf value Is Byte) Then
                    value = New Guid(CType(value,Byte()))
                End If
                m_Id = value
            End Set
        End Property
        
        Public Property Name() As String
            Get
                Return m_Name
            End Get
            Set
                m_Name = value
            End Set
        End Property
        
        Public Property Path() As String
            Get
                Return m_Path
            End Get
            Set
                m_Path = value
            End Set
        End Property
        
        Public Property ContentType() As String
            Get
                Return m_ContentType
            End Get
            Set
                m_ContentType = value
            End Set
        End Property
        
        Public Property Length() As Integer
            Get
                Return m_Length
            End Get
            Set
                m_Length = value
            End Set
        End Property
        
        Public Property Data() As Byte()
            Get
                Return m_Data
            End Get
            Set
                m_Data = value
            End Set
        End Property
        
        Public Property PhysicalName() As String
            Get
                Return m_PhysicalName
            End Get
            Set
                m_PhysicalName = value
            End Set
        End Property
        
        Public ReadOnly Property FullName() As String
            Get
                Return (Path  _
                            + ("/" + PhysicalName))
            End Get
        End Property
        
        Public Property [Error]() As String
            Get
                Return m_Error
            End Get
            Set
                m_Error = value
            End Set
        End Property
        
        Public Property Schedule() As String
            Get
                Return m_Schedule
            End Get
            Set
                m_Schedule = value
            End Set
        End Property
        
        Public Property ScheduleExceptions() As String
            Get
                Return m_ScheduleExceptions
            End Get
            Set
                m_ScheduleExceptions = value
            End Set
        End Property
        
        Public Property CreatedDate() As DateTime
            Get
                Return m_CreatedDate
            End Get
            Set
                m_CreatedDate = value
            End Set
        End Property
        
        Public Property ModifiedDate() As DateTime
            Get
                Return m_ModifiedDate
            End Get
            Set
                m_ModifiedDate = value
            End Set
        End Property
        
        Public Property CacheProfile() As String
            Get
                Return m_CacheProfile
            End Get
            Set
                m_CacheProfile = value
            End Set
        End Property
        
        Public Property CacheDuration() As Integer
            Get
                Return m_CacheDuration
            End Get
            Set
                m_CacheDuration = value
            End Set
        End Property
        
        Public Property CacheLocation() As HttpCacheability
            Get
                Return m_CacheLocation
            End Get
            Set
                m_CacheLocation = value
            End Set
        End Property
        
        Public Property CacheVaryByParams() As String()
            Get
                Return m_CacheVaryByParams
            End Get
            Set
                m_CacheVaryByParams = value
            End Set
        End Property
        
        Public Property CacheVaryByHeaders() As String()
            Get
                Return m_CacheVaryByHeaders
            End Get
            Set
                m_CacheVaryByHeaders = value
            End Set
        End Property
        
        Public Property CacheNoStore() As Boolean
            Get
                Return m_CacheNoStore
            End Get
            Set
                m_CacheNoStore = value
            End Set
        End Property
        
        Public Property Text() As String
            Get
                If ((Not (Me.Data) Is Nothing) AndAlso IsText) Then
                    Return Encoding.UTF8.GetString(Me.Data)
                End If
                Return Nothing
            End Get
            Set
                If (value Is Nothing) Then
                    m_Data = Nothing
                Else
                    m_Data = Encoding.UTF8.GetBytes(value)
                    m_ContentType = "text/plain"
                End If
            End Set
        End Property
        
        Public ReadOnly Property IsText() As Boolean
            Get
                Return ((Not (m_ContentType) Is Nothing) AndAlso Regex.IsMatch(m_ContentType, "^((text/\w+)|(application/(javascript|json)))$"))
            End Get
        End Property
        
        Public Shared Function ReadAllBytes(ByVal relativePath As String) As Byte()
            Return ApplicationServices.Current.ReadSiteContentBytes(relativePath)
        End Function
        
        Public Overloads Shared Function WriteAllBytes(ByVal relativePath As String, ByVal data() As Byte) As Integer
            Return WriteAllBytes(relativePath, MimeMapping.GetMimeMapping(System.IO.Path.GetFileName(relativePath)), data)
        End Function
        
        Public Overloads Shared Function WriteAllBytes(ByVal relativePath As String, ByVal contentType As String, ByVal data() As Byte) As Integer
            Dim services = ApplicationServices.Current
            Dim values = ToValues(relativePath, contentType, true)
            values.Add(New FieldValue(services.SiteContentFieldName(SiteContentFields.Data), data))
            values.Add(New FieldValue(services.SiteContentFieldName(SiteContentFields.Length), Nothing))
            If (Not (data) Is Nothing) Then
                values.Last().NewValue = data.Length
                values.Last().Modified = true
            End If
            Return Write(values).RowsAffected
        End Function
        
        Public Shared Function ReadAllText(ByVal relativePath As String) As String
            Return ApplicationServices.Current.ReadSiteContentString(relativePath)
        End Function
        
        Public Shared Function ReadJson(ByVal relativePath As String) As JObject
            Dim result = ReadAllText(relativePath)
            If (Not (String.IsNullOrEmpty(result)) AndAlso (result(0) = Global.Microsoft.VisualBasic.ChrW(123))) Then
                Return JObject.Parse(result)
            End If
            Return New JObject()
        End Function
        
        Public Overloads Shared Function WriteAllText(ByVal relativePath As String, ByVal text As String) As Integer
            Return WriteAllText(relativePath, "text/plain", text)
        End Function
        
        Public Overloads Shared Function WriteAllText(ByVal relativePath As String, ByVal contentType As String, ByVal text As String) As Integer
            Dim values = ToValues(relativePath, contentType, true)
            values.Add(New FieldValue(ApplicationServices.Current.SiteContentFieldName(SiteContentFields.Text), text))
            Return Write(values).RowsAffected
        End Function
        
        Public Shared Function WriteJson(ByVal relativePath As String, ByVal json As JObject) As Integer
            Return WriteAllText(relativePath, "application/json", json.ToString())
        End Function
        
        Public Shared Function Write(ByVal values As List(Of FieldValue)) As ActionResult
            'find Data, FileName, and ContentType field values
            Dim dataFieldName = ApplicationServices.Current.SiteContentFieldName(SiteContentFields.Data)
            Dim fileNameFieldName = ApplicationServices.Current.SiteContentFieldName(SiteContentFields.DataFileName)
            Dim contentTypeFieldName = ApplicationServices.Current.SiteContentFieldName(SiteContentFields.DataContentType)
            Dim dataFieldValue As FieldValue = Nothing
            Dim fileNameFieldValue As FieldValue = Nothing
            Dim contentTypeFieldValue As FieldValue = Nothing
            For Each fvo in values
                If (fvo.Name = dataFieldName) Then
                    dataFieldValue = fvo
                End If
                If (fvo.Name = fileNameFieldName) Then
                    fileNameFieldValue = fvo
                End If
                If (fvo.Name = contentTypeFieldName) Then
                    contentTypeFieldValue = fvo
                End If
            Next
            'remove "Data" field from the values. We will use Blob.Write to persist the data
            If (Not (dataFieldValue) Is Nothing) Then
                values.Remove(dataFieldValue)
            End If
            ' Insert or Update the record
            Dim args = New ActionArgs()
            args.Controller = ApplicationServices.Current.GetSiteContentControllerName()
            args.View = "createForm1"
            args.Values = values.ToArray()
            args.LastCommandName = "New"
            args.CommandName = "Insert"
            args.IgnoreBusinessRules = true
            If (Not (values(0).OldValue) Is Nothing) Then
                args.View = "editForm1"
                args.LastCommandName = Nothing
                args.CommandName = "Update"
            End If
            Dim result As ActionResult = Nothing
            Dim access = Controller.GrantFullAccess("")
            Try 
                Dim c = ControllerFactory.CreateDataController()
                result = c.Execute(args.Controller, args.View, args)
                result.RaiseExceptionIfErrors()
                ' If there is Data field, then write it with Blob.Write instead. This will ensure that adapters are correctly engaged.
                If (Not (dataFieldValue) Is Nothing) Then
                    Dim dataField = CType(c,DataControllerBase).CreateViewPage().FindField(dataFieldName)
                    Dim blobHandler = dataField.OnDemandHandler
                    Dim blobKey = values(0).Value
                    Blob.Write(blobHandler, blobKey, fileNameFieldValue.Value.ToString(), contentTypeFieldValue.Value.ToString(), CType(dataFieldValue.Value,Byte()))
                End If
            Finally
                Controller.RevokeFullAccess(access)
            End Try
            Return result
        End Function
        
        Public Shared Function Delete(ByVal relativePath As String) As Integer
            Dim services = ApplicationServices.Current
            Dim values = ToValues(relativePath, Nothing, false)
            Dim keys = New List(Of String)()
            For Each file in services.ReadSiteContent(CType(values(2).Value,String), CType(values(1).Value,String))
                keys.Add(file.Id.ToString())
            Next
            If (keys.Count > 0) Then
                Dim args = New ActionArgs()
                args.Controller = services.GetSiteContentControllerName()
                args.View = "grid1"
                args.Values = New FieldValue() {New FieldValue(values(0).Name, keys(0), keys(0))}
                args.SelectedValues = keys.ToArray()
                args.CommandName = "Delete"
                args.IgnoreBusinessRules = true
                Dim access = Controller.GrantFullAccess("")
                Try 
                    Dim c = ControllerFactory.CreateDataController()
                    Dim result = c.Execute(args.Controller, args.View, args)
                    result.RaiseExceptionIfErrors()
                    Return result.RowsAffected
                Finally
                    Controller.RevokeFullAccess(access)
                End Try
            End If
            Return 0
        End Function
        
        Public Shared Function Exists(ByVal relativePath As String) As Boolean
            Return (ApplicationServices.Current.ReadSiteContent(relativePath).Length > 0)
        End Function
        
        Private Shared Function ToValues(ByVal relativePath As String, ByVal contentType As String, ByVal checkForExisting As Boolean) As List(Of FieldValue)
            Dim services = ApplicationServices.Current
            Dim name = relativePath
            Dim path As String = Nothing
            Dim index = relativePath.LastIndexOf("/")
            If (index >= 0) Then
                name = relativePath.Substring((index + 1))
                path = relativePath.Substring(0, index)
            End If
            Dim list = New List(Of FieldValue)()
            list.Add(New FieldValue(services.SiteContentFieldName(SiteContentFields.SiteContentId)))
            list.Add(New FieldValue(services.SiteContentFieldName(SiteContentFields.DataFileName), name))
            list.Add(New FieldValue(services.SiteContentFieldName(SiteContentFields.Path), path))
            list.Add(New FieldValue(services.SiteContentFieldName(SiteContentFields.DataContentType)))
            If checkForExisting Then
                Dim file = services.ReadSiteContent(relativePath)
                If (Not (file) Is Nothing) Then
                    list(0).OldValue = file.Id
                    list(0).Modified = false
                    list(1).OldValue = file.Name
                    list(2).OldValue = file.Path
                    list(3).OldValue = file.ContentType
                End If
            End If
            If Not (String.IsNullOrEmpty(contentType)) Then
                list(3).NewValue = contentType
                list(3).Modified = true
            End If
            Return list
        End Function
        
        Public Overrides Function ToString() As String
            Return String.Format("{0}/{1}", Path, Name)
        End Function
    End Class
    
    Public Class SiteContentFileList
        Inherits List(Of SiteContentFile)
    End Class
    
    Partial Public Class ApplicationServices
        Inherits EnterpriseApplicationServices
        
        Public Shared CustomCodeAssemblyName As String = Nothing
        
        Public Shared ReadOnly Property CombinedResourceType() As String
            Get
                Dim t = String.Empty
                If Not (AuthorizationIsSupported) Then
                    t = (t + "_noauth")
                End If
                Dim addonFile = Regex.Match(HttpContext.Current.Request.Url.LocalPath, "^\/(_\w+)")
                If addonFile.Success Then
                    t = (t + addonFile.Groups(1).Value)
                End If
                Return t
            End Get
        End Property
        
        Public Shared ReadOnly Property HomePageUrl() As String
            Get
                Return Create().UserHomePageUrl()
            End Get
        End Property
        
        Public Shared Function StringToType(ByVal typeName As String) As System.Type
            Dim qualifiedTypeName = typeName
            If Not (String.IsNullOrEmpty(CustomCodeAssemblyName)) Then
                qualifiedTypeName = (qualifiedTypeName  _
                            + ("," + CustomCodeAssemblyName))
            End If
            Dim t = Type.GetType(qualifiedTypeName)
            If (t Is Nothing) Then
                t = Type.GetType(typeName)
            End If
            Return t
        End Function
        
        Public Shared Function CreateInstance(ByVal typeName As String) As Object
            Return Activator.CreateInstance(StringToType(typeName))
        End Function
        
        Public Shared Sub Initialize()
            Dim appFrameworkConfigTypeName As String = Nothing
            'figure the name of the custom code assembly
            Dim customCodeAssembly = GetType(ApplicationServices).Assembly
            If (customCodeAssembly.GetName().Name = "FreeTrial") Then
                For Each a in AppDomain.CurrentDomain.GetAssemblies()
                    If a.FullName.StartsWith("App_Code") Then
                        customCodeAssembly = a
                        Exit For
                    End If
                Next
            End If
            CustomCodeAssemblyName = customCodeAssembly.FullName
            'find the full name of AppFrameworkConfig class
            For Each t in customCodeAssembly.GetTypes()
                If (t.Name = "AppFrameworkConfig") Then
                    appFrameworkConfigTypeName = t.FullName
                    Exit For
                End If
            Next
            'initialize external components of the framework
            Dim frameworkConfig = CreateInstance(appFrameworkConfigTypeName)
            If (Not (frameworkConfig) Is Nothing) Then
                frameworkConfig.GetType().InvokeMember("Initialize", (BindingFlags.InvokeMethod Or (BindingFlags.Instance Or BindingFlags.Public)), Nothing, frameworkConfig, Nothing)
            End If
            For Each className in New String() {"AppBuilder", "AssistantUI", "ContentMaker", "OfflineSync", "Survey"}
                Try 
                    Dim addonType = Type.GetType(String.Format("CodeOnTime.Addons.{0},addon.{0}", className))
                    If (Not (addonType) Is Nothing) Then
                        Addons.Add(Activator.CreateInstance(addonType))
                    End If
                Catch __exception As Exception
                End Try
            Next
            'register service routes and map handlers
            Create().RegisterServices()
        End Sub
        
        Public Shared Function Login(ByVal username As String, ByVal password As String, ByVal createPersistentCookie As Boolean) As Object
            Return Create().AuthenticateUser(username, password, createPersistentCookie)
        End Function
        
        Public Shared Sub Logout()
            Create().UserLogout()
        End Sub
        
        Public Shared Function Roles() As String()
            Return Create().UserRoles()
        End Function
        
        Public Shared Function Themes() As JObject
            Return Create().UserThemes()
        End Function
        
        Public Overloads Shared Function OAuthGetAuthorizationUrl(ByVal provider As String) As String
            Return OAuthGetAuthorizationUrl(provider, Nothing)
        End Function
        
        Public Overloads Shared Function OAuthGetAuthorizationUrl(ByVal provider As String, ByVal state As String) As String
            Dim authorizationUrl As String = Nothing
            Return authorizationUrl
        End Function
    End Class
    
    Public Class AddonRouteIgnoreConstraint
        
        Public ReadOnly Property PathInfo() As String
            Get
                Return "^(?!daf\/add\.min\.(js|css)$).+"
            End Get
        End Property
    End Class
    
    Public Enum ApplicationFeature
        
        DynamicAccessControlList
        
        DynamicControllerCustomization
        
        WorkflowRegister
    End Enum
    
    Public Class ApplicationServicesBase
        
        Public Shared Addons As List(Of Object) = New List(Of Object)()
        
        Public Shared EnableMobileClient As Boolean = true
        
        Private m_DefaultSettings As JObject
        
        Private Shared m_EnableCombinedCss As Boolean
        
        Private Shared m_EnableMinifiedCss As Boolean = true
        
        Public Shared FrameworkSiteContentControllerName As String = String.Empty
        
        Public Shared NameValueListRegex As Regex = New Regex("^\s*(?'Name'\w+)\s*=\s*(?'Value'[\S\s]+?)\s*$", RegexOptions.Multiline)
        
        Public Shared SystemResourceRegex As Regex = New Regex("~/((sys/)|(views/)|(controllers/)|(permissions/)|(reports/)|((site|touch\-setting"& _ 
                "s|acl)\.\w+))", RegexOptions.IgnoreCase)
        
        Public Shared FrameworkAppName As String = Nothing
        
        Public Shared AuthorizationIsSupported As Boolean = true
        
        Private m_UserTheme As String
        
        Private m_UserAccent As String
        
        Public Shared CssUrlRegex As Regex = New Regex("(?'Header'\burl\s*\(\s*(\""|\')?)(?'Name'[\w/\.]+)(?'Symbol'\S)")
        
        Public Shared DefaultExcludeScriptRegex As Regex = New Regex("^(daf\\|sys\\|lib\\|surveys\\|_references\.js)|((.+?)\.(\w\w(\-\w+)*)\.js$)")
        
        Public Shared RequestHandlers As SortedDictionary(Of String, ServiceRequestHandler) = New SortedDictionary(Of String, ServiceRequestHandler)()
        
        Public Shared ViewPageCompressRegex As Regex = New Regex("((""(DefaultValue)""\:(""[\s\S]*?""))|(""(Items|Pivots|Fields|Views|ActionGroups|Categ"& _ 
                "ories|Filter|Expressions|Errors)""\:(\[\]))|(""(Len|CategoryIndex|Rows|Columns|Sea"& _ 
                "rch|ItemsPageSize|Aggregate|OnDemandStyle|TextMode|MaskType|AutoCompletePrefixLe"& _ 
                "ngth|DataViewPageSize|PageOffset)""\:(0))|(""(CausesValidation|AllowQBE|AllowSorti"& _ 
                "ng|FormatOnClient|HtmlEncode|RequiresMetaData|RequiresRowCount|ShowInSelector|Da"& _ 
                "taViewShow(ActionBar|Description|ViewSelector|PageSize|SearchBar|QuickFind))""\:("& _ 
                "true))|(""(IsPrimaryKey|ReadOnly|HasDefaultValue|Hidden|AllowLEV|AllowNulls|OnDem"& _ 
                "and|IsMirror|Calculated|CausesCalculate|IsVirtual|AutoSelect|SearchOnStart|ShowI"& _ 
                "nSummary|ItemsLetters|WhenKeySelected|RequiresSiteContentText|RequiresPivot|Requ"& _ 
                "iresAggregates|Floating|Collapsed|Label|SupportsCaching|AllowDistinctFieldInFilt"& _ 
                "er|Flat|RequiresMetaData|RequiresRowCount|Distinct|(DataView(ShowInSummary|Multi"& _ 
                "Select|ShowModalForms|SearchByFirstLetter|SearchOnStart|ShowRowNumber|AutoHighli"& _ 
                "ghtFirstRow|AutoSelectFirstRow)))""\:(false))|(""(AliasName|Tag|FooterText|ToolTip"& _ 
                "|Watermark|DataFormatString|Copy|HyperlinkFormatString|SourceFields|SearchOption"& _ 
                "s|ItemsDataController|ItemsTargetController|ItemsDataView|ItemsDataValueField|It"& _ 
                "emsDataTextField|ItemsStyle|ItemsNewDataView|OnDemandHandler|Mask|ContextFields|"& _ 
                "Formula|Flow|Label|Configuration|Editor|ItemsDescription|Group|CommandName|Comma"& _ 
                "ndArgument|HeaderText|Description|CssClass|Confirmation|Notify|Key|WhenLastComma"& _ 
                "ndName|WhenLastCommandArgument|WhenClientScript|WhenTag|WhenHRef|WhenView|PivotD"& _ 
                "efinitions|Aggregates|PivotDefinitions|Aggregates|ViewType|LastView|StatusBar|Ic"& _ 
                "ons|LEVs|QuickFindHint|InnerJoinPrimaryKey|SystemFilter|DistinctValueFieldName|C"& _ 
                "lientScript|FirstLetters|SortExpression|Template|Tab|Wizard|InnerJoinForeignKey|"& _ 
                "Expressions|ViewHeaderText|ViewLayout|GroupExpression|FieldFilter|Wrap|Tags|Tag|"& _ 
                "Id|Filter|(DataView(Id|FilterSource|Controller|FilterFields|ShowActionButtons|Sh"& _ 
                "owPager)))""\:(""\s*""|null))|(""Type"":""String"")),?")
        
        Public Shared ViewPageCompress2Regex As Regex = New Regex(",\}(,|])")
        
        Public Overridable ReadOnly Property DefaultSettings() As JObject
            Get
                If (m_DefaultSettings Is Nothing) Then
                    m_DefaultSettings = CType(HttpContext.Current.Items("touch-settings.json"),JObject)
                    If (m_DefaultSettings Is Nothing) Then
                        m_DefaultSettings = CType(HttpContext.Current.Cache("touch-settings.json"),JObject)
                        If (m_DefaultSettings Is Nothing) Then
                            Dim json = "{}"
                            Dim filePath = HttpContext.Current.Server.MapPath("~/touch-settings.json")
                            If File.Exists(filePath) Then
                                json = File.ReadAllText(filePath)
                            End If
                            m_DefaultSettings = JObject.Parse(json)
                            EnsureJsonProperty(m_DefaultSettings, "appName", ApplicationServices.Current.Name)
                            EnsureJsonProperty(m_DefaultSettings, "map.apiKey", MapsApiIdentifier)
                            EnsureJsonProperty(m_DefaultSettings, "charts.maxPivotRowCount", MaxPivotRowCount)
                            EnsureJsonProperty(m_DefaultSettings, "ui.theme.name", "Light")
                            Dim ui = CType(m_DefaultSettings("ui"),JObject)
                            EnsureJsonProperty(ui, "theme.accent", "Aquarium")
                            EnsureJsonProperty(ui, "displayDensity.mobile", "Auto")
                            EnsureJsonProperty(ui, "displayDensity.desktop", "Condensed")
                            EnsureJsonProperty(ui, "list.labels.display", "DisplayedBelow")
                            EnsureJsonProperty(ui, "list.initialMode", "SeeAll")
                            EnsureJsonProperty(ui, "menu.location", "toolbar")
                            EnsureJsonProperty(ui, "actions.promote", true)
                            EnsureJsonProperty(ui, "smartDates", true)
                            EnsureJsonProperty(ui, "transitions.style", "")
                            EnsureJsonProperty(ui, "sidebar.when", "Landscape")
                            EnsureJsonProperty(m_DefaultSettings, "help.enabled", true)
                            HttpContext.Current.Cache.Add("touch-settings.json", m_DefaultSettings, New CacheDependency(New String() {filePath, HttpContext.Current.Server.MapPath("~/web.config")}), Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Normal, Nothing)
                        End If
                        HttpContext.Current.Items("touch-settings.json") = m_DefaultSettings
                    End If
                End If
                Return m_DefaultSettings
            End Get
        End Property
        
        Public Shared Property EnableCombinedCss() As Boolean
            Get
                Return m_EnableCombinedCss
            End Get
            Set
                m_EnableCombinedCss = value
            End Set
        End Property
        
        Public Shared Property EnableMinifiedCss() As Boolean
            Get
                Return m_EnableMinifiedCss
            End Get
            Set
                m_EnableMinifiedCss = value
            End Set
        End Property
        
        Public Shared ReadOnly Property IsSiteContentEnabled() As Boolean
            Get
                Return Not (String.IsNullOrEmpty(SiteContentControllerName))
            End Get
        End Property
        
        Public Shared ReadOnly Property SiteContentControllerName() As String
            Get
                Return Create().GetSiteContentControllerName()
            End Get
        End Property
        
        Public Shared ReadOnly Property SiteContentEditors() As String()
            Get
                Return Create().GetSiteContentEditors()
            End Get
        End Property
        
        Public Shared ReadOnly Property SiteContentDevelopers() As String()
            Get
                Return Create().GetSiteContentDevelopers()
            End Get
        End Property
        
        Public Shared ReadOnly Property SuperUsers() As String()
            Get
                Return Create().GetSuperUsers()
            End Get
        End Property
        
        Public Shared ReadOnly Property IsContentEditor() As Boolean
            Get
                For Each r in Create().GetSiteContentEditors()
                    If DataControllerBase.UserIsInRole(r) Then
                        Return true
                    End If
                Next
                Return false
            End Get
        End Property
        
        Public Shared ReadOnly Property IsDeveloper() As Boolean
            Get
                For Each r in Create().GetSiteContentDevelopers()
                    If DataControllerBase.UserIsInRole(r) Then
                        Return true
                    End If
                Next
                Return false
            End Get
        End Property
        
        Public Shared ReadOnly Property IsSuperUser() As Boolean
            Get
                For Each r in Create().GetSuperUsers()
                    If DataControllerBase.UserIsInRole(r) Then
                        Return true
                    End If
                Next
                Return false
            End Get
        End Property
        
        Public Shared ReadOnly Property IsSafeMode() As Boolean
            Get
                Dim request = HttpContext.Current.Request
                Dim test = request.UrlReferrer
                If (test Is Nothing) Then
                    test = request.Url
                End If
                Return ((test Is Nothing) AndAlso (test.ToString().Contains("_safemode=true") AndAlso DataControllerBase.UserIsInRole(SiteContentDevelopers)))
            End Get
        End Property
        
        Public Overridable ReadOnly Property ScheduleCacheDuration() As Integer
            Get
                Return 20
            End Get
        End Property
        
        Public Overridable ReadOnly Property Realm() As String
            Get
                Return DisplayName
            End Get
        End Property
        
        Public Overridable ReadOnly Property Name() As String
            Get
                Return FrameworkAppName
            End Get
        End Property
        
        Public Overridable ReadOnly Property DisplayName() As String
            Get
                Dim result = Name
                If IsTouchClient Then
                    Dim settings = DefaultSettings
                    Dim appName = Convert.ToString(settings("appName"))
                    If Not (String.IsNullOrEmpty(appName)) Then
                        result = appName
                    End If
                End If
                Return result
            End Get
        End Property
        
        Public Shared ReadOnly Property MapsApiIdentifier() As String
            Get
                If ((Not (HttpContext.Current) Is Nothing) AndAlso (HttpContext.Current.Request.Headers("X-Cot-Manifest-Request") = "true")) Then
                    Return WebConfigurationManager.AppSettings("MapsApiIdentifierMobile")
                End If
                Return WebConfigurationManager.AppSettings("MapsApiIdentifier")
            End Get
        End Property
        
        Public Overridable ReadOnly Property MaxPivotRowCount() As Integer
            Get
                Return 250000
            End Get
        End Property
        
        Public Shared ReadOnly Property Current() As ApplicationServices
            Get
                Return Create()
            End Get
        End Property
        
        Public Shared ReadOnly Property IsTouchClient() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overridable ReadOnly Property UserTheme() As String
            Get
                If String.IsNullOrEmpty(m_UserTheme) Then
                    LoadTheme()
                End If
                Return m_UserTheme
            End Get
        End Property
        
        Public Overridable ReadOnly Property UserAccent() As String
            Get
                If String.IsNullOrEmpty(m_UserAccent) Then
                    LoadTheme()
                End If
                Return m_UserAccent
            End Get
        End Property
        
        Public Overridable ReadOnly Property EnableCors() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overridable Function Supports(ByVal feature As ApplicationFeature) As Boolean
            Return false
        End Function
        
        Public Shared Function Settings(ByVal selector As String) As JToken
            Return SelectFrom(Current.DefaultSettings, selector)
        End Function
        
        Public Shared Function SelectFrom(ByVal json As JToken, ByVal selector As String) As JToken
            Dim path = Regex.Split(selector, "\.")
            Dim i = 0
            Do While (i < path.Length)
                json = json(path(i))
                If (json Is Nothing) Then
                    Exit Do
                End If
                i = (i + 1)
            Loop
            Return json
        End Function
        
        Public Overridable Function GetNavigateUrl() As String
            Return Nothing
        End Function
        
        Public Shared Sub VerifyUrl()
            Dim navigateUrl = Create().GetNavigateUrl()
            If Not (String.IsNullOrEmpty(navigateUrl)) Then
                Dim current = HttpContext.Current
                If Not (VirtualPathUtility.ToAbsolute(navigateUrl).Equals(current.Request.RawUrl, StringComparison.CurrentCultureIgnoreCase)) Then
                    current.Response.Redirect(navigateUrl)
                End If
            End If
        End Sub
        
        Public Overridable Sub RegisterServices()
            CreateStandardMembershipAccounts()
            Dim routes = RouteTable.Routes
            RegisterIgnoredRoutes(routes)
            RegisterContentServices(routes)
            'Register service request handlers
            RequestHandlers.Add("getpage", New GetPageServiceRequestHandler())
            RequestHandlers.Add("getpagelist", New GetPageListServiceRequestHandler())
            RequestHandlers.Add("getlistofvalues", New GetListOfValuesServiceRequestHandler())
            RequestHandlers.Add("execute", New ExecuteServiceRequestHandler())
            RequestHandlers.Add("executeandgetpage", New ExecuteAndGetPageServiceRequestHandler())
            RequestHandlers.Add("executelist", New ExecuteListServiceRequestHandler())
            RequestHandlers.Add("getcompletionlist", New GetCompletionListServiceRequestHandler())
            RequestHandlers.Add("login", New LoginServiceRequestHandler())
            RequestHandlers.Add("logout", New LogoutServiceRequestHandler())
            RequestHandlers.Add("roles", New RolesServiceRequestHandler())
            RequestHandlers.Add("themes", New ThemesServiceRequestHandler())
            RequestHandlers.Add("savepermalink", New SavePermalinkServiceRequestHandler())
            RequestHandlers.Add("encodepermalink", New EncodePermalinkServiceRequestHandler())
            RequestHandlers.Add("listallpermalinks", New ListAllPermalinksServiceRequestHandler())
            RequestHandlers.Add("getsurvey", New GetSurveyServiceRequestHandler())
            RequestHandlers.Add("addon", New AddonServiceRequestHandler())
            RequestHandlers.Add("getidentity", New GetIdentityServiceRequestHandler())
            RequestHandlers.Add("getmanifest", New GetManifestServiceRequestHandler())
            RequestHandlers.Add("getmanifestfile", New GetManifestFileServiceRequestHandler())
            RequestHandlers.Add("getcontrollerlist", New GetControllerListServiceRequestHandler())
            RequestHandlers.Add("commit", New CommitServiceRequestHandler())
        End Sub
        
        Public Shared Sub Start()
            Current.InstanceStart()
        End Sub
        
        Protected Overridable Sub InstanceStart()
            Norwindt.Services.ApplicationServices.Initialize()
        End Sub
        
        Public Shared Sub [Stop]()
            Current.InstanceStop()
        End Sub
        
        Protected Overridable Sub InstanceStop()
        End Sub
        
        Public Shared Sub SessionStart()
            'The line below will prevent intermittent error “Session state has created a session id,
            'but cannot save it because the response was already flushed by the application.”
            Dim sessionId = HttpContext.Current.Session.SessionID
            Current.UserSessionStart()
        End Sub
        
        Protected Overridable Sub UserSessionStart()
        End Sub
        
        Public Shared Sub SessionStop()
            Current.UserSessionStop()
        End Sub
        
        Protected Overridable Sub UserSessionStop()
        End Sub
        
        Public Shared Sub [Error]()
            Dim context = HttpContext.Current
            If (Not (context) Is Nothing) Then
                Current.HandleError(context, context.Server.GetLastError())
            End If
        End Sub
        
        Protected Overridable Sub HandleError(ByVal context As HttpContext, ByVal er As Exception)
        End Sub
        
        Public Overridable Function HandleException(ByVal result As JObject, ByVal ex As Exception) As Object
            Do While (Not (ex.InnerException) Is Nothing)
                ex = ex.InnerException
            Loop
            Dim er = New ServiceRequestError()
            er.Message = ex.Message
            er.ExceptionType = ex.GetType().ToString()
            Dim current = HttpContext.Current
            If (current.Request.Url.Host.Equals("localhost") OrElse Not (HttpContext.Current.IsCustomErrorEnabled)) Then
                er.StackTrace = ex.StackTrace
            End If
            Return er
        End Function
        
        Public Overridable Sub RegisterContentServices(ByVal routes As RouteCollection)
            GenericRoute.Map(routes, New PlaceholderHandler(), "placeholder/{FileName}")
            routes.MapPageRoute("SiteContent", "{*url}", "~/Site.aspx")
            routes.MapPageRoute("DataControllerService", "{*url}", AquariumExtenderBase.DefaultServicePath)
        End Sub
        
        Public Overridable Sub RegisterIgnoredRoutes(ByVal routes As RouteCollection)
            routes.Ignore("{handler}.ashx")
            routes.Ignore("favicon.ico")
            routes.Ignore("controlhost.aspx")
            routes.Ignore("charthost.aspx")
            routes.Ignore("{resource}.axd/{*pathInfo}")
            routes.Ignore("daf/{service}/{*methodName}")
            routes.Ignore("app_themes/{themeFolder}/{file}")
            routes.Ignore("{id}/arterySignalR/{*pathInfo}")
            If Not (IsSiteContentEnabled) Then
                routes.Ignore("images/{*pathInfo}")
                routes.Ignore("documents/{*pathInfo}")
                routes.Ignore("download/{*pathInfo}")
            End If
            routes.Ignore("css/{*pathInfo}", New AddonRouteIgnoreConstraint())
            routes.Ignore("js/{*pathInfo}", New AddonRouteIgnoreConstraint())
            routes.Ignore("services/{*pathInfo}")
        End Sub
        
        Public Overloads Shared Function LoadContent() As SortedDictionary(Of String, String)
            Dim content = New SortedDictionary(Of String, String)()
            Create().LoadContent(HttpContext.Current.Request, HttpContext.Current.Response, content)
            Dim rawContent As String = Nothing
            If content.TryGetValue("File", rawContent) Then
                'find the head
                Dim headMatch = Regex.Match(rawContent, "<head>([\s\S]+?)</head>")
                If headMatch.Success Then
                    Dim head = headMatch.Groups(1).Value
                    head = Regex.Replace(head, "\s*<meta charset="".+""\s*/?>\s*", String.Empty)
                    content("Head") = Regex.Replace(head, "\s*<title>([\S\s]*?)</title>\s*", String.Empty)
                    'find the title
                    Dim titleMatch = Regex.Match(head, "<title>(?'Title'[\S\s]+?)</title>")
                    If titleMatch.Success Then
                        Dim title = titleMatch.Groups("Title").Value
                        content("PageTitle") = title
                        content("PageTitleContent") = title
                    End If
                    'find "about"
                    Dim aboutMatch = Regex.Match(head, "<meta\s+name\s*=\s*""description""\s+content\s*=\s*""([\s\S]+?)""\s*/>")
                    If aboutMatch.Success Then
                        content("About") = HttpUtility.HtmlDecode(aboutMatch.Groups(1).Value)
                    End If
                End If
                'find the body
                Dim bodyMatch = Regex.Match(rawContent, "<body(?'Attr'[\s\S]*?)>(?'Body'[\s\S]+?)</body>")
                If bodyMatch.Success Then
                    content("PageContent") = EnrichData(bodyMatch.Groups("Body").Value)
                    content("BodyAttributes") = bodyMatch.Groups("Attr").Value
                Else
                    content("PageContent") = EnrichData(rawContent)
                End If
            End If
            Return content
        End Function
        
        Public Shared Function EnrichData(ByVal body As String) As String
            If Not (Regex.IsMatch(body, "<div[\s\S]+?(data-(app-role|controller|user-control|placeholder))\s*=""[\s\S]+?>")) Then
                body = String.Format("<div data-app-role=""page"" data-content-framework=""bootstrap"">{0}</div>", body)
            End If
            Return Regex.Replace(body, "(<script[^>]*(data-)?type=""(\$app\.)?execute""[^>]*>(?<Script>(.|\n)*?)<\/script>)"& _ 
                    "", AddressOf DoEnrichData)
        End Function
        
        Private Shared Function DoEnrichData(ByVal m As Match) As String
            Try 
                Dim json = m.Groups("Script").Value.Trim().Trim(Global.Microsoft.VisualBasic.ChrW(41), Global.Microsoft.VisualBasic.ChrW(40), Global.Microsoft.VisualBasic.ChrW(59))
                Dim obj = JObject.Parse(json)
                Dim request = New PageRequest()
                request.Controller = CType(obj("controller"),String)
                request.View = CType(obj("view"),String)
                request.PageIndex = Convert.ToInt32(obj("pageIndex"))
                request.PageSize = Convert.ToInt32(obj("pageSize"))
                If (request.PageSize = 0) Then
                    request.PageSize = 100
                End If
                request.SortExpression = CType(obj("sortExpression"),String)
                Dim metadataFilter = CType(obj("metadataFilter"),JArray)
                If (Not (metadataFilter) Is Nothing) Then
                    request.MetadataFilter = metadataFilter.ToObject(Of String())()
                Else
                    request.MetadataFilter = New String() {"fields"}
                End If
                request.RequiresMetaData = true
                Dim page = ControllerFactory.CreateDataController().GetPage(request.Controller, request.View, request)
                Dim output = ApplicationServices.CompressViewPageJsonOutput(JsonConvert.SerializeObject(page))
                Dim doFormat = obj("format")
                If (doFormat Is Nothing) Then
                    doFormat = "true"
                End If
                Dim id = obj("id")
                If (id Is Nothing) Then
                    id = request.Controller
                End If
                Return String.Format("<script>$app.data({{""id"":""{0}"",""format"":{1},""d"":{2}}});</script>", id, Convert.ToBoolean(doFormat).ToString().ToLower(), output)
            Catch ex As Exception
                Return (("<div class=""well text-danger"">" + ex.Message)  _
                            + "</div>")
            End Try
        End Function
        
        Public Overridable Function GetSiteContentControllerName() As String
            Return String.Empty
        End Function
        
        Public Overridable Function GetSiteContentViewId() As String
            Return "editForm1"
        End Function
        
        Public Overridable Function GetSiteContentEditors() As String()
            Return New String() {"Administrators", "Content Editors", "Developers"}
        End Function
        
        Public Overridable Function GetSiteContentDevelopers() As String()
            Return New String() {"Administrators", "Developers"}
        End Function
        
        Public Overridable Function GetSuperUsers() As String()
            Return New String() {"Administrators"}
        End Function
        
        Public Overridable Sub AfterAction(ByVal args As ActionArgs, ByVal result As ActionResult)
        End Sub
        
        Public Overridable Sub BeforeAction(ByVal args As ActionArgs, ByVal result As ActionResult)
            If SiteContentControllerName.Equals(args.Controller, StringComparison.OrdinalIgnoreCase) Then
                If (args.CommandName = "Insert") Then
                    Dim createdDateFieldName = SiteContentFieldName(SiteContentFields.CreatedDate)
                    Dim createdDate = args.SelectFieldValueObject(createdDateFieldName)
                    If (createdDate Is Nothing) Then
                        args.AddValue(New FieldValue(createdDateFieldName, DateTime.Now))
                    Else
                        If (createdDate.Value Is Nothing) Then
                            createdDate.NewValue = DateTime.Now
                            createdDate.Modified = true
                        End If
                    End If
                End If
                If ((args.CommandName = "Insert") OrElse (args.CommandName = "Update")) Then
                    Dim modifiedDateFieldName = SiteContentFieldName(SiteContentFields.ModifiedDate)
                    Dim modifiedDate = args.SelectFieldValueObject(modifiedDateFieldName)
                    If (modifiedDate Is Nothing) Then
                        args.AddValue(New FieldValue(modifiedDateFieldName, DateTime.Now))
                    Else
                        modifiedDate.NewValue = DateTime.Now
                        modifiedDate.Modified = true
                    End If
                End If
                If (Not (args.IgnoreBusinessRules) AndAlso AuthorizationIsSupported) Then
                    Dim userIsDeveloper = IsDeveloper
                    If ((Not (IsContentEditor) OrElse Not (userIsDeveloper)) OrElse (args.Values Is Nothing)) Then
                        Throw New HttpException(403, "Forbidden")
                    End If
                    Dim id = args.SelectFieldValueObject(SiteContentFieldName(SiteContentFields.SiteContentId))
                    Dim path = args.SelectFieldValueObject(SiteContentFieldName(SiteContentFields.Path))
                    Dim fileName = args.SelectFieldValueObject(SiteContentFieldName(SiteContentFields.DataFileName))
                    Dim text = args.SelectFieldValueObject(SiteContentFieldName(SiteContentFields.Text))
                    'verify "Path" access
                    If ((path Is Nothing) OrElse (fileName Is Nothing)) Then
                        Throw New HttpException(403, "Forbidden")
                    End If
                    If (((Not (path.Value) Is Nothing) AndAlso path.Value.ToString().StartsWith("sys/", StringComparison.CurrentCultureIgnoreCase)) AndAlso Not (userIsDeveloper)) Then
                        Throw New HttpException(403, "Forbidden")
                    End If
                    If (((Not (path.OldValue) Is Nothing) AndAlso path.OldValue.ToString().StartsWith("sys/", StringComparison.CurrentCultureIgnoreCase)) AndAlso Not (userIsDeveloper)) Then
                        Throw New HttpException(403, "Forbidden")
                    End If
                    'convert and parse "Text" as needed
                    If ((Not (text) Is Nothing) AndAlso Not ((args.CommandName = "Delete"))) Then
                        Dim s = Convert.ToString(text.Value)
                        If (s = "$Text") Then
                            Dim fullPath = Convert.ToString(path.Value)
                            If Not (String.IsNullOrEmpty(fullPath)) Then
                                fullPath = (fullPath + "/")
                            End If
                            fullPath = (fullPath + Convert.ToString(fileName.Value))
                            If Not (fullPath.StartsWith("/")) Then
                                fullPath = ("/" + fullPath)
                            End If
                            If Not (fullPath.EndsWith(".html", StringComparison.CurrentCultureIgnoreCase)) Then
                                fullPath = (fullPath + ".html")
                            End If
                            Dim physicalPath = HttpContext.Current.Server.MapPath(("~" + fullPath))
                            If Not (File.Exists(physicalPath)) Then
                                physicalPath = HttpContext.Current.Server.MapPath(("~" + fullPath.Replace("-", String.Empty)))
                                If Not (File.Exists(physicalPath)) Then
                                    physicalPath = Nothing
                                End If
                            End If
                            If Not (String.IsNullOrEmpty(physicalPath)) Then
                                text.NewValue = File.ReadAllText(physicalPath)
                            End If
                        End If
                    End If
                End If
            End If
        End Sub
        
        Public Overridable Function SiteContentFieldName(ByVal field As SiteContentFields) As String
            Return field.ToString()
        End Function
        
        Public Overridable Function ReadSiteContentString(ByVal relativePath As String) As String
            Dim data = ReadSiteContentBytes(relativePath)
            If (data Is Nothing) Then
                Return Nothing
            End If
            Return Encoding.UTF8.GetString(data)
        End Function
        
        Public Overridable Function ReadSiteContentBytes(ByVal relativePath As String) As Byte()
            Dim f = ReadSiteContent(relativePath)
            If (f Is Nothing) Then
                Return Nothing
            End If
            Return f.Data
        End Function
        
        Public Overloads Overridable Function ReadSiteContent(ByVal relativePath As String) As SiteContentFile
            Dim context = HttpContext.Current
            Dim f = CType(context.Items(relativePath),SiteContentFile)
            If (f Is Nothing) Then
                f = CType(context.Cache(relativePath),SiteContentFile)
            End If
            If (f Is Nothing) Then
                Dim path = relativePath
                Dim fileName = relativePath
                Dim index = relativePath.LastIndexOf("/")
                If (index >= 0) Then
                    fileName = path.Substring((index + 1))
                    path = relativePath.Substring(0, index)
                Else
                    path = Nothing
                End If
                Dim files = ReadSiteContent(path, fileName, 1)
                If (files.Count = 1) Then
                    f = files(0)
                    context.Items(relativePath) = f
                    If (f.CacheDuration > 0) Then
                        context.Cache.Add(relativePath, f, Nothing, DateTime.Now.AddSeconds(f.CacheDuration), Cache.NoSlidingExpiration, CacheItemPriority.Normal, Nothing)
                    End If
                End If
            End If
            Return f
        End Function
        
        Public Overloads Overridable Function ReadSiteContent(ByVal relativePath As String, ByVal fileName As String) As SiteContentFileList
            Return ReadSiteContent(relativePath, fileName, Int32.MaxValue)
        End Function
        
        Public Overloads Overridable Function ReadSiteContent(ByVal relativePath As String, ByVal fileName As String, ByVal maxCount As Integer) As SiteContentFileList
            Dim result = New SiteContentFileList()
            If IsSafeMode Then
                Return result
            End If
            'prepare a filter
            Dim dataFileNameField = SiteContentFieldName(SiteContentFields.DataFileName)
            Dim pathField = SiteContentFieldName(SiteContentFields.Path)
            Dim filter = New List(Of String)()
            Dim pathFilter As String = Nothing
            If Not (String.IsNullOrEmpty(relativePath)) Then
                pathFilter = "{0}:={1}"
                Dim firstWildcardIndex = relativePath.IndexOf("%")
                If (firstWildcardIndex >= 0) Then
                    Dim lastWildcardIndex = relativePath.LastIndexOf("%")
                    pathFilter = "{0}:$contains${1}"
                    If (firstWildcardIndex = lastWildcardIndex) Then
                        If (firstWildcardIndex = 0) Then
                            pathFilter = "{0}:$endswith${1}"
                            relativePath = relativePath.Substring(1)
                        Else
                            If (lastWildcardIndex = (relativePath.Length - 1)) Then
                                pathFilter = "{0}:$beginswith${1}"
                                relativePath = relativePath.Substring(0, lastWildcardIndex)
                            End If
                        End If
                    End If
                End If
            Else
                pathFilter = "{0}:=null"
            End If
            Dim fileNameFilter As String = Nothing
            If (Not (String.IsNullOrEmpty(fileName)) AndAlso Not ((fileName = "%"))) Then
                fileNameFilter = "{0}:={1}"
                Dim firstWildcardIndex = fileName.IndexOf("%")
                If (firstWildcardIndex >= 0) Then
                    Dim lastWildcardIndex = fileName.LastIndexOf("%")
                    fileNameFilter = "{0}:$contains${1}"
                    If (firstWildcardIndex = lastWildcardIndex) Then
                        If (firstWildcardIndex = 0) Then
                            fileNameFilter = "{0}:$endswith${1}"
                            fileName = fileName.Substring(1)
                        Else
                            If (lastWildcardIndex = (fileName.Length - 1)) Then
                                fileNameFilter = "{0}:$beginswith${1}"
                                fileName = fileName.Substring(0, lastWildcardIndex)
                            End If
                        End If
                    End If
                End If
            End If
            If (Not (String.IsNullOrEmpty(pathFilter)) OrElse Not (String.IsNullOrEmpty(fileNameFilter))) Then
                filter.Add("_match_:$all$")
                If Not (String.IsNullOrEmpty(pathFilter)) Then
                    filter.Add(String.Format(pathFilter, pathField, DataControllerBase.ValueToString(relativePath)))
                End If
                If (Not ((fileName = Nothing)) AndAlso Not ((fileName = "%"))) Then
                    filter.Add(String.Format(fileNameFilter, dataFileNameField, DataControllerBase.ValueToString(fileName)))
                    If (String.IsNullOrEmpty(Path.GetExtension(fileName)) AndAlso (String.IsNullOrEmpty(relativePath) OrElse (Not (relativePath.StartsWith("sys/", StringComparison.OrdinalIgnoreCase)) OrElse relativePath.StartsWith("sys/controls", StringComparison.OrdinalIgnoreCase)))) Then
                        filter.Add("_match_:$all$")
                        If Not (String.IsNullOrEmpty(pathFilter)) Then
                            filter.Add(String.Format(pathFilter, pathField, DataControllerBase.ValueToString(relativePath)))
                        End If
                        filter.Add(String.Format(fileNameFilter, dataFileNameField, DataControllerBase.ValueToString((Path.GetFileNameWithoutExtension(fileName).Replace("-", String.Empty) + ".html"))))
                    End If
                End If
            End If
            ' determine user identity
            Dim context = HttpContext.Current
            Dim userName = String.Empty
            Dim isAuthenticated = false
            Dim user = context.User
            If (Not (user) Is Nothing) Then
                userName = user.Identity.Name.ToLower()
                isAuthenticated = user.Identity.IsAuthenticated
            End If
            'enumerate site content files
            Dim r = New PageRequest()
            r.Controller = GetSiteContentControllerName()
            r.View = GetSiteContentViewId()
            r.RequiresSiteContentText = true
            r.PageSize = Int32.MaxValue
            r.Filter = filter.ToArray()
            Dim blobsToResolve = New SortedDictionary(Of String, SiteContentFile)()
            Dim access = Controller.GrantFullAccess("")
            Try 
                Dim engine = ControllerFactory.CreateDataEngine()
                Dim controller = CType(engine,DataControllerBase)
                Dim reader = engine.ExecuteReader(r)
                'verify optional SiteContent fields
                Dim fieldDictionary = New SortedDictionary(Of String, String)()
                Dim i = 0
                Do While (i < reader.FieldCount)
                    Dim fieldName = reader.GetName(i)
                    fieldDictionary(fieldName) = fieldName
                    i = (i + 1)
                Loop
                Dim rolesField As String = Nothing
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.Roles), rolesField)
                Dim usersField As String = Nothing
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.Users), usersField)
                Dim roleExceptionsField As String = Nothing
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.RoleExceptions), roleExceptionsField)
                Dim userExceptionsField As String = Nothing
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.UserExceptions), userExceptionsField)
                Dim cacheProfileField As String = Nothing
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.CacheProfile), cacheProfileField)
                Dim scheduleField As String = Nothing
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.Schedule), scheduleField)
                Dim scheduleExceptionsField As String = Nothing
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.ScheduleExceptions), scheduleExceptionsField)
                Dim createdDateField = String.Empty
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.CreatedDate), createdDateField)
                Dim modifiedDateField = String.Empty
                fieldDictionary.TryGetValue(SiteContentFieldName(SiteContentFields.ModifiedDate), modifiedDateField)
                Dim dataField = controller.CreateViewPage().FindField(SiteContentFieldName(SiteContentFields.Data))
                Dim blobHandler = dataField.OnDemandHandler
                Dim wr = WorkflowRegister.GetCurrent(relativePath)
                'read SiteContent files
                Do While reader.Read()
                    'verify user access rights
                    Dim include = true
                    If Not (String.IsNullOrEmpty(rolesField)) Then
                        Dim roles = Convert.ToString(reader(rolesField))
                        If (Not (String.IsNullOrEmpty(roles)) AndAlso Not ((roles = "?"))) Then
                            If ((roles = "*") AndAlso Not (isAuthenticated)) Then
                                include = false
                            Else
                                If (Not (isAuthenticated) OrElse (Not ((roles = "*")) AndAlso Not (DataControllerBase.UserIsInRole(roles)))) Then
                                    include = false
                                End If
                            End If
                        End If
                    End If
                    If (include AndAlso Not (String.IsNullOrEmpty(usersField))) Then
                        Dim users = Convert.ToString(reader(usersField))
                        If (Not (String.IsNullOrEmpty(users)) AndAlso (Array.IndexOf(users.ToLower().Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries), userName) = -1)) Then
                            include = false
                        End If
                    End If
                    If (include AndAlso Not (String.IsNullOrEmpty(roleExceptionsField))) Then
                        Dim roleExceptions = Convert.ToString(reader(roleExceptionsField))
                        If (Not (String.IsNullOrEmpty(roleExceptions)) AndAlso (isAuthenticated AndAlso ((roleExceptions = "*") OrElse DataControllerBase.UserIsInRole(roleExceptions)))) Then
                            include = false
                        End If
                    End If
                    If (include AndAlso Not (String.IsNullOrEmpty(userExceptionsField))) Then
                        Dim userExceptions = Convert.ToString(reader(userExceptionsField))
                        If (Not (String.IsNullOrEmpty(userExceptions)) AndAlso Not ((Array.IndexOf(userExceptions.ToLower().Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries), userName) = -1))) Then
                            include = false
                        End If
                    End If
                    Dim physicalName = Convert.ToString(reader(dataFileNameField))
                    Dim physicalPath = Convert.ToString(reader(SiteContentFieldName(SiteContentFields.Path)))
                    'check if the content object is a part of a workflow
                    If (((Not (wr) Is Nothing) AndAlso wr.Enabled) AndAlso Not (wr.IsMatch(physicalPath, physicalName))) Then
                        include = false
                    End If
                    Dim schedule As String = Nothing
                    Dim scheduleExceptions As String = Nothing
                    'check if the content object is on schedule
                    If (include AndAlso (String.IsNullOrEmpty(physicalPath) OrElse Not (physicalPath.StartsWith("sys/schedules/")))) Then
                        If Not (String.IsNullOrEmpty(scheduleField)) Then
                            schedule = Convert.ToString(reader(scheduleField))
                        End If
                        If Not (String.IsNullOrEmpty(scheduleExceptionsField)) Then
                            scheduleExceptions = Convert.ToString(reader(scheduleExceptionsField))
                        End If
                        If (Not (String.IsNullOrEmpty(schedule)) OrElse Not (String.IsNullOrEmpty(scheduleExceptions))) Then
                            Dim scheduleStatusKey = String.Format("ScheduleStatus|{0}|{1}", schedule, scheduleExceptions)
                            Dim status = CType(context.Items(scheduleStatusKey),ScheduleStatus)
                            If (status Is Nothing) Then
                                status = CType(context.Cache(scheduleStatusKey),ScheduleStatus)
                            End If
                            Dim scheduleStatusChanged = false
                            If (status Is Nothing) Then
                                If (Not (String.IsNullOrEmpty(schedule)) AndAlso Not (schedule.Contains("+"))) Then
                                    schedule = ReadSiteContentString(("sys/schedules%/" + schedule))
                                End If
                                If (Not (String.IsNullOrEmpty(scheduleExceptions)) AndAlso Not (scheduleExceptions.Contains("+"))) Then
                                    scheduleExceptions = ReadSiteContentString(("sys/schedules%/" + scheduleExceptions))
                                End If
                                If (Not (String.IsNullOrEmpty(schedule)) OrElse Not (String.IsNullOrEmpty(scheduleExceptions))) Then
                                    status = Scheduler.Test(schedule, scheduleExceptions)
                                Else
                                    status = New ScheduleStatus()
                                    status.Success = true
                                    status.NextTestDate = DateTime.MaxValue
                                End If
                                context.Items(scheduleStatusKey) = status
                                scheduleStatusChanged = true
                            Else
                                If (DateTime.Now > status.NextTestDate) Then
                                    status = Scheduler.Test(status.Schedule, status.Exceptions)
                                    context.Items(scheduleStatusKey) = status
                                    scheduleStatusChanged = true
                                End If
                            End If
                            If scheduleStatusChanged Then
                                context.Cache.Add(scheduleStatusKey, status, Nothing, DateTime.Now.AddSeconds(ScheduleCacheDuration), Cache.NoSlidingExpiration, CacheItemPriority.Normal, Nothing)
                            End If
                            If Not (status.Success) Then
                                include = false
                            End If
                        End If
                    End If
                    'create a file instance
                    If include Then
                        Dim siteContentIdField = SiteContentFieldName(SiteContentFields.SiteContentId)
                        Dim f = New SiteContentFile()
                        f.Id = reader(siteContentIdField)
                        f.Name = fileName
                        f.PhysicalName = physicalName
                        If (String.IsNullOrEmpty(f.Name) OrElse f.Name.Contains("%")) Then
                            f.Name = f.PhysicalName
                        End If
                        f.Path = physicalPath
                        If Not (String.IsNullOrEmpty(createdDateField)) Then
                            Dim createdDate = reader(createdDateField)
                            If Not (DBNull.Value.Equals(createdDate)) Then
                                f.CreatedDate = CType(createdDate,DateTime)
                            End If
                        End If
                        If Not (String.IsNullOrEmpty(modifiedDateField)) Then
                            Dim modifiedDate = reader(modifiedDateField)
                            If Not (DBNull.Value.Equals(modifiedDate)) Then
                                f.ModifiedDate = CType(modifiedDate,DateTime)
                            End If
                        End If
                        f.ContentType = Convert.ToString(reader(SiteContentFieldName(SiteContentFields.DataContentType)))
                        f.Schedule = schedule
                        f.ScheduleExceptions = scheduleExceptions
                        If Not (String.IsNullOrEmpty(cacheProfileField)) Then
                            Dim cacheProfile = Convert.ToString(reader(cacheProfileField))
                            If Not (String.IsNullOrEmpty(cacheProfile)) Then
                                f.CacheProfile = cacheProfile
                                cacheProfile = ReadSiteContentString(("sys/cache-profiles/" + cacheProfile))
                                If Not (String.IsNullOrEmpty(cacheProfile)) Then
                                    Dim m = NameValueListRegex.Match(cacheProfile)
                                    Do While m.Success
                                        Dim n = m.Groups("Name").Value.ToLower()
                                        Dim v = m.Groups("Value").Value
                                        If (n = "duration") Then
                                            Dim duration = 0
                                            If Int32.TryParse(v, duration) Then
                                                f.CacheDuration = duration
                                                f.CacheLocation = HttpCacheability.ServerAndPrivate
                                            End If
                                        Else
                                            If (n = "location") Then
                                                Try 
                                                    f.CacheLocation = CType(TypeDescriptor.GetConverter(GetType(HttpCacheability)).ConvertFromString(v),HttpCacheability)
                                                Catch __exception As Exception
                                                End Try
                                            Else
                                                If (n = "varybyheaders") Then
                                                    f.CacheVaryByHeaders = v.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44), Global.Microsoft.VisualBasic.ChrW(59)}, StringSplitOptions.RemoveEmptyEntries)
                                                Else
                                                    If (n = "varybyparams") Then
                                                        f.CacheVaryByParams = v.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44), Global.Microsoft.VisualBasic.ChrW(59)}, StringSplitOptions.RemoveEmptyEntries)
                                                    Else
                                                        If (n = "nostore") Then
                                                            f.CacheNoStore = (v.ToLower() = "true")
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        End If
                                        m = m.NextMatch()
                                    Loop
                                End If
                            End If
                        End If
                        Dim textString = reader(SiteContentFieldName(SiteContentFields.Text))
                        If (DBNull.Value.Equals(textString) OrElse Not (f.IsText)) Then
                            Dim blobKey = String.Format("{0}=o|{1}", blobHandler, f.Id)
                            If (f.CacheDuration > 0) Then
                                f.Data = CType(HttpContext.Current.Cache(blobKey),Byte())
                            End If
                            If (f.Data Is Nothing) Then
                                blobsToResolve(blobKey) = f
                            End If
                        Else
                            If String.IsNullOrEmpty(f.ContentType) Then
                                If Regex.IsMatch(CType(textString,String), "</\w+\s*>") Then
                                    f.ContentType = "text/xml"
                                Else
                                    f.ContentType = "text/plain"
                                End If
                            End If
                            f.Data = Encoding.UTF8.GetBytes(CType(textString,String))
                        End If
                        result.Add(f)
                        If (result.Count = maxCount) Then
                            Exit Do
                        End If
                    End If
                Loop
                reader.Close()
            Finally
                Controller.RevokeFullAccess(access)
            End Try
            For Each blobKey in blobsToResolve.Keys
                Dim f = blobsToResolve(blobKey)
                'download blob content
                Try 
                    access = Controller.GrantFullAccess("")
                    Try 
                        f.Data = Blob.Read(blobKey)
                    Finally
                        Controller.RevokeFullAccess(access)
                    End Try
                    If (f.CacheDuration > 0) Then
                        HttpContext.Current.Cache.Add(blobKey, f.Data, Nothing, DateTime.Now.AddSeconds(f.CacheDuration), Cache.NoSlidingExpiration, CacheItemPriority.Normal, Nothing)
                    End If
                Catch ex As Exception
                    f.Error = ex.Message
                End Try
            Next
            Return result
        End Function
        
        Public Overridable Function IsSystemResource(ByVal request As HttpRequest) As Boolean
            Return SystemResourceRegex.IsMatch(request.AppRelativeCurrentExecutionFilePath)
        End Function
        
        Public Overridable Function AddScripts() As String
            If (Addons.Count = 0) Then
                Return String.Empty
            End If
            Dim sb = New StringBuilder()
            For Each addon in Addons
                sb.Append(CType(addon.GetType().GetMethod("Script").Invoke(addon, Nothing),String))
            Next
            Return sb.ToString()
        End Function
        
        Public Overridable Function AddStyleSheets() As String
            If (Addons.Count = 0) Then
                Return String.Empty
            End If
            Dim sb = New StringBuilder()
            For Each addon in Addons
                sb.Append(CType(addon.GetType().GetMethod("StyleSheet").Invoke(addon, Nothing),String))
            Next
            Return sb.ToString()
        End Function
        
        Public Overloads Overridable Sub LoadContent(ByVal request As HttpRequest, ByVal response As HttpResponse, ByVal content As SortedDictionary(Of String, String))
            If IsSystemResource(request) Then
                Return
            End If
            If (request.Url.LocalPath = "/js/daf/add.min.js") Then
                response.Cache.SetExpires(DateTime.Now.AddMonths(1))
                response.Cache.SetCacheability(HttpCacheability.Public)
                response.ContentType = "text/javascript"
                response.Write(AddScripts())
                Try 
                    response.Flush()
                Catch __exception As Exception
                End Try
                response.End()
            End If
            If (request.Url.LocalPath = "/css/daf/add.min.css") Then
                response.Cache.SetExpires(DateTime.Now.AddMonths(1))
                response.Cache.SetCacheability(HttpCacheability.Public)
                response.ContentType = "text/css"
                response.Write(AddStyleSheets())
                Try 
                    response.Flush()
                Catch __exception As Exception
                End Try
                response.End()
            End If
            Dim text As String = Nothing
            Dim tryFileSystem = true
            Dim addonPage = Regex.Match(request.Url.LocalPath, "^\/_(\w+)")
            If addonPage.Success Then
                For Each addon in Addons
                    If addon.GetType().Name.Equals(addonPage.Groups(1).Value, StringComparison.OrdinalIgnoreCase) Then
                        text = CType(addon.GetType().GetMethod("Page").Invoke(addon, Nothing),String)
                        tryFileSystem = false
                        Exit For
                    End If
                Next
            End If
            If (IsSiteContentEnabled AndAlso tryFileSystem) Then
                Dim fileName = HttpUtility.UrlDecode(request.Url.Segments((request.Url.Segments.Length - 1)))
                Dim path = request.CurrentExecutionFilePath.Substring(request.ApplicationPath.Length)
                If ((fileName = "/") AndAlso String.IsNullOrEmpty(path)) Then
                    fileName = "index"
                Else
                    If Not (String.IsNullOrEmpty(path)) Then
                        path = path.Substring(0, (path.Length - fileName.Length))
                        If path.EndsWith("/") Then
                            path = path.Substring(0, (path.Length - 1))
                        End If
                    End If
                End If
                If String.IsNullOrEmpty(path) Then
                    path = Nothing
                End If
                Dim files = ReadSiteContent(path, fileName, 1)
                If (files.Count > 0) Then
                    Dim f = files(0)
                    If (f.ContentType = "text/html") Then
                        text = f.Text
                        tryFileSystem = false
                    Else
                        If (f.CacheDuration > 0) Then
                            Dim expires = DateTime.Now.AddSeconds(f.CacheDuration)
                            response.Cache.SetExpires(expires)
                            response.Cache.SetCacheability(f.CacheLocation)
                            If (Not (f.CacheVaryByParams) Is Nothing) Then
                                For Each header in f.CacheVaryByParams
                                    response.Cache.VaryByParams(header) = true
                                Next
                            End If
                            If (Not (f.CacheVaryByHeaders) Is Nothing) Then
                                For Each header in f.CacheVaryByHeaders
                                    response.Cache.VaryByHeaders(header) = true
                                Next
                            End If
                            If f.CacheNoStore Then
                                response.Cache.SetNoStore()
                            End If
                        End If
                        response.ContentType = f.ContentType
                        response.AddHeader("Content-Disposition", ("filename=" + HttpUtility.UrlEncode(f.PhysicalName)))
                        response.OutputStream.Write(f.Data, 0, f.Data.Length)
                        Try 
                            response.Flush()
                        Catch __exception As Exception
                        End Try
                        response.End()
                    End If
                End If
            End If
            If tryFileSystem Then
                Dim filePath = request.PhysicalPath
                Dim fileExtension = Path.GetExtension(filePath)
                If (Not ((fileExtension.ToLower() = ".html")) AndAlso File.Exists(filePath)) Then
                    Dim fileName = Path.GetFileName(filePath)
                    response.AddHeader("Content-Disposition", ("filename=" + HttpUtility.UrlEncode(fileName)))
                    response.ContentType = MimeMapping.GetMimeMapping(fileName)
                    Dim expires = DateTime.Now.AddSeconds(((60 * 60)  _
                                    * 24))
                    response.Cache.SetExpires(expires)
                    response.Cache.SetCacheability(HttpCacheability.Public)
                    Dim data = File.ReadAllBytes(filePath)
                    response.OutputStream.Write(data, 0, data.Length)
                    Try 
                        response.Flush()
                    Catch __exception As Exception
                    End Try
                    response.End()
                End If
                If Not (String.IsNullOrEmpty(fileExtension)) Then
                    filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath))
                End If
                filePath = (filePath + ".html")
                If File.Exists(filePath) Then
                    text = File.ReadAllText(filePath)
                Else
                    If Path.GetFileNameWithoutExtension(filePath).Contains("-") Then
                        filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Replace("-", String.Empty))
                        If File.Exists(filePath) Then
                            text = File.ReadAllText(filePath)
                        End If
                    End If
                End If
                If (Not (text) Is Nothing) Then
                    text = Localizer.Replace("Pages", filePath, text)
                End If
            End If
            If (Not (text) Is Nothing) Then
                text = Regex.Replace(text, "<!--[\s\S]+?-->\s*", String.Empty)
                content("File") = text
            End If
        End Sub
        
        Public Overridable Sub CreateStandardMembershipAccounts()
        End Sub
        
        Public Overridable Function RequiresAuthentication(ByVal request As HttpRequest) As Boolean
            If request.Path.EndsWith("Export.ashx", StringComparison.CurrentCultureIgnoreCase) Then
                Dim formToken = HttpContext.Current.Request.Params("t")
                If (String.IsNullOrEmpty(formToken) OrElse Not (ValidateToken(formToken))) Then
                    Return true
                End If
            End If
            Return false
        End Function
        
        Public Overridable Function AuthenticateRequest(ByVal context As HttpContext) As Boolean
            Return false
        End Function
        
        Public Overridable Sub RedirectToLoginPage()
            FormsAuthentication.RedirectToLoginPage()
        End Sub
        
        Public Overridable Function AuthenticateUser(ByVal username As String, ByVal password As String, ByVal createPersistentCookie As Boolean) As Object
            Dim response = HttpContext.Current.Response
            If password.StartsWith("token:") Then
                'validate token login
                Try 
                    Dim key = password.Substring(6)
                    Dim ticket = FormsAuthentication.Decrypt(key)
                    If (ValidateTicket(ticket) AndAlso (Not (String.IsNullOrEmpty(ticket.UserData)) AndAlso Regex.IsMatch(ticket.UserData, "^(REFRESHONLY$|OAUTH:)"))) Then
                        Dim user = Membership.GetUser(ticket.Name)
                        If ((Not (user) Is Nothing) AndAlso (user.IsApproved AndAlso Not (user.IsLockedOut))) Then
                            InvalidateTicket(ticket)
                            FormsAuthentication.SetAuthCookie(user.UserName, createPersistentCookie)
                            Return CreateTicket(user, key)
                        End If
                    End If
                Catch __exception As Exception
                End Try
            Else
                'login user
                If UserLogin(username, password, createPersistentCookie) Then
                    FormsAuthentication.SetAuthCookie(username, createPersistentCookie)
                    Dim user = Membership.GetUser(username)
                    If (Not (user) Is Nothing) Then
                        Return CreateTicket(user, Nothing)
                    End If
                End If
            End If
            Return false
        End Function
        
        Public Overridable Function CreateTicket(ByVal user As MembershipUser, ByVal refreshToken As String) As UserTicket
            Dim accessTimeout = 15
            Dim refreshTimeout = (60  _
                        * (24 * 7))
            Dim jTimeout = TryGetJsonProperty(DefaultSettings, "membership.accountManager.accessTokenDuration")
            If (Not (jTimeout) Is Nothing) Then
                accessTimeout = CType(jTimeout,Integer)
            End If
            jTimeout = TryGetJsonProperty(DefaultSettings, "membership.accountManager.refreshTokenDuration")
            If (Not (jTimeout) Is Nothing) Then
                refreshTimeout = CType(jTimeout,Integer)
            End If
            Dim userData = String.Empty
            Dim accessTicket = New FormsAuthenticationTicket(1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(accessTimeout), false, userData)
            If String.IsNullOrEmpty(refreshToken) Then
                Dim refreshTicket = New FormsAuthenticationTicket(1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(refreshTimeout), false, "REFRESHONLY")
                refreshToken = FormsAuthentication.Encrypt(refreshTicket)
            End If
            Return New UserTicket(user, FormsAuthentication.Encrypt(accessTicket), refreshToken)
        End Function
        
        Public Overridable Function ValidateTicket(ByVal ticket As FormsAuthenticationTicket) As Boolean
            Return Not (((ticket Is Nothing) OrElse (ticket.Expired OrElse String.IsNullOrEmpty(ticket.Name))))
        End Function
        
        Public Overridable Sub InvalidateTicket(ByVal ticket As FormsAuthenticationTicket)
        End Sub
        
        Public Overridable Function ValidateToken(ByVal accessToken As String) As Boolean
            Try 
                Dim ticket = FormsAuthentication.Decrypt(accessToken)
                If (ticket.UserData = "REFRESHONLY") Then
                    Return false
                End If
                If ValidateTicket(ticket) Then
                    HttpContext.Current.User = New RolePrincipal(New FormsIdentity(New FormsAuthenticationTicket(ticket.Name, false, 10)))
                    Return true
                End If
            Catch __exception As Exception
            End Try
            Return false
        End Function
        
        Public Overridable Function UserLogin(ByVal username As String, ByVal password As String, ByVal createPersistentCookie As Boolean) As Boolean
            If Membership.ValidateUser(username, password) Then
                Return true
            Else
                Return false
            End If
        End Function
        
        Public Overridable Sub UserLogout()
            FormsAuthentication.SignOut()
        End Sub
        
        Public Overridable Function UserRoles() As String()
            Return Roles.GetRolesForUser()
        End Function
        
        Public Overridable Function UserThemes() As JObject
            Dim lists = New JObject()
            Dim themes = New JArray()
            Dim accents = New JArray()
            lists("themes") = themes
            lists("accents") = accents
            Dim themesPath = HttpContext.Current.Server.MapPath("~/css/themes")
            For Each f in Directory.GetFiles(themesPath, "touch-theme.*.json")
                Dim theme = JObject.Parse(File.ReadAllText(f))
                Dim t = New JObject()
                t("name") = theme("name")
                t("color") = theme("color")
                themes.Add(t)
            Next
            For Each f in Directory.GetFiles(themesPath, "touch-accent.*.json")
                Dim accent = JObject.Parse(File.ReadAllText(f))
                Dim a = New JObject()
                a("name") = accent("name")
                a("color") = accent("color")
                accents.Add(a)
            Next
            Return lists
        End Function
        
        Public Overridable Function UserSettings(ByVal p As Page) As JObject
            Dim settings = New JObject(DefaultSettings)
            If (settings("membership") Is Nothing) Then
                settings("membership") = New JObject()
            End If
            Dim userKey = String.Empty
            settings("membership")("enabled") = false
            settings("appInfo") = String.Join("|", New String() {DisplayName, HttpContext.Current.User.Identity.Name, userKey})
            If IsContentEditor Then
                settings("siteContent") = GetSiteContentControllerName()
                settings("siteContentPK") = SiteContentFieldName(SiteContentFields.SiteContentId)
            End If
            If (Not (p) Is Nothing) Then
                settings("rootUrl") = p.ResolveUrl("~")
            End If
            settings("ui")("theme")("name") = UserTheme
            settings("ui")("theme")("accent") = UserAccent
            For Each addon in Addons
                addon.GetType().GetMethod("Settings").Invoke(addon, New Object() {settings})
            Next
            If (Not (settings("server")) Is Nothing) Then
                settings.Remove("server")
            End If
            Return settings
        End Function
        
        Public Overridable Function UserHomePageUrl() As String
            If IsSiteContentEnabled Then
                Dim index = ReadSiteContent("index")
                If (Not (index) Is Nothing) Then
                    Return HttpContext.Current.Request.ApplicationPath
                End If
            End If
            Return "~/pages/home"
        End Function
        
        Public Overridable Function UserPictureString(ByVal user As MembershipUser) As String
            Try 
                Dim img = UserPictureImage(user)
                If (img Is Nothing) Then
                    img = UserPictureFromCMS(user)
                End If
                If (Not (img) Is Nothing) Then
                    If ((img.Width > 80) OrElse (img.Height > 80)) Then
                        Dim scale = (CType(img.Width,Single) / 80)
                        Dim height = CType((img.Height / scale),Integer)
                        Dim width = 80
                        If (img.Height < img.Width) Then
                            scale = (CType(img.Height,Single) / 80)
                            height = 80
                            width = CType((img.Width / scale),Integer)
                        End If
                        img = Blob.ResizeImage(img, width, height)
                    End If
                    Using stream = New MemoryStream()
                        img.Save(stream, ImageFormat.Bmp)
                        Dim bytes = stream.ToArray()
                        img.Dispose()
                        Return ("data:image/raw;base64," + Convert.ToBase64String(bytes))
                    End Using
                End If
            Catch __exception As Exception
            End Try
            Return String.Empty
        End Function
        
        Public Overridable Function UserPictureImage(ByVal user As MembershipUser) As Image
            Dim url = UserPictureUrl(user)
            If Not (String.IsNullOrEmpty(url)) Then
                Dim request = WebRequest.Create(url)
                Using stream = request.GetResponse().GetResponseStream()
                    Using ms = New MemoryStream()
                        stream.CopyTo(ms)
                        Return CType(New ImageConverter().ConvertFrom(ms.ToArray()),Image)
                    End Using
                End Using
            Else
                url = UserPictureFilePath(user)
                If Not (String.IsNullOrEmpty(url)) Then
                    Return Image.FromFile(url)
                End If
            End If
            Return Nothing
        End Function
        
        Public Overridable Function UserPictureFromCMS(ByVal user As MembershipUser) As Image
            Return Nothing
        End Function
        
        Public Overridable Function UserPictureFilePath(ByVal user As MembershipUser) As String
            Return Nothing
        End Function
        
        Public Overridable Function UserPictureUrl(ByVal user As MembershipUser) As String
            Return Nothing
        End Function
        
        Public Shared Function Create() As ApplicationServices
            Return New ApplicationServices()
        End Function
        
        Public Shared Function UserIsAuthorizedToAccessResource(ByVal path As String, ByVal roles As String) As Boolean
            Return Not (Create().ResourceAuthorizationIsRequired(path, roles))
        End Function
        
        Public Overridable Function ResourceAuthorizationIsRequired(ByVal path As String, ByVal roles As String) As Boolean
            If Not (AuthorizationIsSupported) Then
                Return false
            End If
            If (roles Is Nothing) Then
                roles = String.Empty
            Else
                roles = roles.Trim()
            End If
            Dim acl = AccessControlList.Current
            Dim appPage = Regex.Match(path, "pages/(.+?)(\?|$)", RegexOptions.IgnoreCase)
            If ((appPage.Success AndAlso Not (acl.PermissionGranted(PermissionKind.Page, appPage.Groups(1).Value))) AndAlso Not (path.Equals(FormsAuthentication.LoginUrl))) Then
                If Not (String.IsNullOrEmpty(roles)) Then
                    Dim roleList = Regex.Split(roles, "\s+|\s*,\s*")
                    Dim pageSupers = roleList.Intersect(SuperUsers).ToArray()
                    If ((pageSupers.Length > 0) AndAlso DataControllerBase.UserIsInRole(pageSupers)) Then
                        Return false
                    End If
                    Return true
                End If
                Return true
            End If
            Dim requiresAuthorization = false
            Dim isAuthenticated = HttpContext.Current.User.Identity.IsAuthenticated
            If Not (acl.Enabled) Then
                If (String.IsNullOrEmpty(roles) AndAlso Not (isAuthenticated)) Then
                    requiresAuthorization = true
                End If
                If (Not (String.IsNullOrEmpty(roles)) AndAlso Not ((roles = "?"))) Then
                    If (roles = "*") Then
                        If Not (isAuthenticated) Then
                            requiresAuthorization = true
                        End If
                    Else
                        If (Not (isAuthenticated) OrElse Not (DataControllerBase.UserIsInRole(roles))) Then
                            requiresAuthorization = true
                        End If
                    End If
                End If
            End If
            If (path = FormsAuthentication.LoginUrl) Then
                requiresAuthorization = false
            End If
            Return requiresAuthorization
        End Function
        
        Public Shared Sub RegisterStandardMembershipAccounts()
            If AuthorizationIsSupported Then
                'Create standard 'admin' and 'user' accounts.
                Dim admin = Membership.GetUser("admin")
                If ((Not (admin) Is Nothing) AndAlso admin.IsLockedOut) Then
                    admin.UnlockUser()
                End If
                Dim user = Membership.GetUser("user")
                If ((Not (user) Is Nothing) AndAlso user.IsLockedOut) Then
                    user.UnlockUser()
                End If
                If (Membership.GetUser("admin") Is Nothing) Then
                    Dim status As MembershipCreateStatus
                    admin = Membership.CreateUser("admin", "admin123%", "admin@Norwindt.com", "ASP.NET", "Code OnTime", true, status)
                    user = Membership.CreateUser("user", "user123%", "user@Norwindt.com", "ASP.NET", "Code OnTime", true, status)
                    Roles.CreateRole("Administrators")
                    Roles.CreateRole("Users")
                    Roles.AddUserToRole(admin.UserName, "Users")
                    Roles.AddUserToRole(user.UserName, "Users")
                    Roles.AddUserToRole(admin.UserName, "Administrators")
                End If
            End If
        End Sub
        
        Public Shared Sub RegisterCssLinks(ByVal p As Page)
            Dim l = New HtmlLink()
            l.ID = "NorwindtTheme"
            l.Attributes.Add("type", "text/css")
            l.Attributes.Add("rel", "stylesheet")
            p.Header.Controls.Add(CType(l,Control))
            Dim services = ApplicationServices.Current
            Dim jqmCss = String.Format("jquery.mobile-{0}.min.css", ApplicationServices.JqmVersion)
            l.Href = ("~/css/sys/" + jqmCss)
            Dim meta = New HtmlMeta()
            meta.Attributes("name") = "viewport"
            meta.Attributes("content") = "width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"
            p.Header.Controls.AddAt(0, meta)
            If ApplicationServices.EnableCombinedCss Then
                l.Href = p.ResolveUrl(String.Format("~/appservices/stylesheet-{0}.min.css?_t={1}.{2}&_r={3}&_cf=", ApplicationServices.Version, services.UserTheme, services.UserAccent, ApplicationServices.CombinedResourceType))
                l.Attributes("class") = "app-theme"
            Else
                For Each stylesheet in services.EnumerateTouchUIStylesheets()
                    Dim cssName = Path.GetFileName(stylesheet)
                    If (Not (cssName.StartsWith("jquery.mobile")) AndAlso Not (cssName.StartsWith("bootstrap"))) Then
                        Dim cssLink = New HtmlLink()
                        cssLink.Href = String.Format("{0}?{1}", stylesheet.Replace(Global.Microsoft.VisualBasic.ChrW(92), Global.Microsoft.VisualBasic.ChrW(47)), ApplicationServices.Version)
                        If cssName.StartsWith("touch-theme.") Then
                            cssLink.Attributes("class") = "app-theme"
                        End If
                        cssLink.Attributes("type") = "text/css"
                        cssLink.Attributes("rel") = "stylesheet"
                        p.Header.Controls.Add(cssLink)
                    End If
                Next
            End If
            Dim removeList = New List(Of Control)()
            For Each c2 in p.Header.Controls
                If TypeOf c2 Is HtmlLink Then
                    l = CType(c2,HtmlLink)
                    If l.Href.Contains("App_Themes/") Then
                        removeList.Add(l)
                    End If
                End If
            Next
            For Each c2 in removeList
                p.Header.Controls.Remove(c2)
            Next
        End Sub
        
        Private Sub LoadTheme()
            Dim theme = String.Empty
            If (Not (HttpContext.Current) Is Nothing) Then
                Dim themeCookie = HttpContext.Current.Request.Cookies((".COTTHEME" + BusinessRules.UserName))
                If (Not (themeCookie) Is Nothing) Then
                    theme = themeCookie.Value
                End If
            End If
            If (Not (String.IsNullOrEmpty(theme)) AndAlso theme.Contains(Global.Microsoft.VisualBasic.ChrW(46))) Then
                theme = theme.Replace(" ", String.Empty)
                Dim parts = theme.Split(Global.Microsoft.VisualBasic.ChrW(46))
                m_UserTheme = parts(0)
                m_UserAccent = parts(1)
            Else
                m_UserTheme = CType(DefaultSettings("ui")("theme")("name"),String)
                m_UserAccent = CType(DefaultSettings("ui")("theme")("accent"),String)
            End If
        End Sub
        
        Protected Overridable Function AllowTouchUIStylesheet(ByVal name As String) As Boolean
            Return Not (Regex.IsMatch(name, "^(touch|bootstrap|jquery\.mobile)"))
        End Function
        
        Public Overridable Function EnumerateTouchUIStylesheets() As List(Of String)
            Dim stylesheets = New List(Of String)()
            Dim ext = ".min.css"
            If Not (EnableMinifiedCss) Then
                ext = ".css"
            End If
            stylesheets.Add(String.Format("~\css\sys\jquery.mobile-{0}{1}", ApplicationServices.JqmVersion, ext))
            stylesheets.Add(("~\css\daf\touch" + ext))
            stylesheets.Add(("~\css\daf\touch-charts" + ext))
            stylesheets.Add(("~\css\sys\bootstrap" + ext))
            stylesheets.Add(String.Format("~\appservices\touch-theme.{0}.{1}.css", UserTheme, UserAccent))
            If Not (String.IsNullOrEmpty(AddStyleSheets())) Then
                stylesheets.Add("~\css\daf\add.min.css")
            End If
            'enumerate custom css files
            Dim customCss = CType(HttpRuntime.Cache("IncludedCss"),List(Of String))
            If (customCss Is Nothing) Then
                customCss = New List(Of String)()
                Dim cssPath = Path.Combine(HttpRuntime.AppDomainAppPath, "css")
                Dim dep As CacheDependency = Nothing
                If Directory.Exists(cssPath) Then
                    dep = New FolderCacheDependency(cssPath, "*.css")
                    Dim ignorePath = Path.Combine(cssPath, "_ignore.txt")
                    Dim ignoreRegex As Regex = Nothing
                    If File.Exists(ignorePath) Then
                        ignoreRegex = BuildSearchPathRegex(File.ReadAllLines(ignorePath))
                    End If
                    For Each filePath in Directory.EnumerateFiles(cssPath, "*.css", SearchOption.AllDirectories)
                        Dim css = Path.GetFileName(filePath)
                        Dim relativePath = ("~\" + filePath.Substring(HttpRuntime.AppDomainAppPath.Length))
                        If (AllowTouchUIStylesheet(css) AndAlso ((ignoreRegex Is Nothing) OrElse Not (ignoreRegex.IsMatch(relativePath.Substring(2))))) Then
                            If Not (css.EndsWith(".min.css")) Then
                                customCss.Add(relativePath)
                            Else
                                Dim index = customCss.IndexOf((css.Substring(0, (css.Length - 7)) + "css"))
                                If (index > -1) Then
                                    customCss(index) = relativePath
                                Else
                                    customCss.Add(relativePath)
                                End If
                            End If
                        End If
                    Next
                End If
                HttpRuntime.Cache.Add("IncludedCss", customCss, dep, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, Nothing)
            End If
            stylesheets.AddRange(customCss)
            Return stylesheets
        End Function
        
        Private Shared Function DoReplaceCssUrl(ByVal m As Match) As String
            Dim header = m.Groups("Header").Value
            Dim name = m.Groups("Name").Value
            Dim symbol = m.Groups("Symbol").Value
            If (((name = "data") OrElse name.StartsWith("http")) AndAlso (symbol = ":")) Then
                Return m.Value
            End If
            Dim appPath = HttpContext.Current.Request.ApplicationPath
            If Not (appPath.EndsWith("/")) Then
                appPath = (appPath + "/")
            End If
            name = Regex.Replace(name, "^(\.\.\/)+", appPath)
            Return (header  _
                        + (name + symbol))
        End Function
        
        Public Shared Function CombineTouchUIStylesheets(ByVal context As HttpContext) As String
            Dim response = context.Response
            Dim cache = response.Cache
            cache.SetCacheability(HttpCacheability.Public)
            cache.VaryByHeaders("User-Agent") = true
            cache.SetOmitVaryStar(true)
            cache.SetExpires(DateTime.Now.AddDays(365))
            cache.SetValidUntilExpires(true)
            cache.SetLastModifiedFromFileDependencies()
            'combine scripts
            Dim contentFramework = context.Request.QueryString("_cf")
            Dim includeBootstrap = (contentFramework = "bootstrap")
            Dim sb = New StringBuilder()
            Dim services = Create()
            For Each stylesheet in services.EnumerateTouchUIStylesheets()
                Dim cssName = Path.GetFileName(stylesheet)
                If (includeBootstrap OrElse Not (cssName.StartsWith("bootstrap"))) Then
                    If cssName.StartsWith("touch-theme.") Then
                        sb.AppendLine(StylesheetGenerator.Compile(cssName))
                    Else
                        Dim data As String = Nothing
                        If (stylesheet = "~\css\daf\add.min.css") Then
                            data = ApplicationServices.Current.AddStyleSheets()
                        Else
                            data = File.ReadAllText(HttpContext.Current.Server.MapPath(stylesheet))
                        End If
                        data = CssUrlRegex.Replace(data, AddressOf DoReplaceCssUrl)
                        If Not (data.Contains("@import url")) Then
                            sb.AppendLine(data)
                        Else
                            sb.Insert(0, data)
                        End If
                    End If
                End If
            Next
            Return sb.ToString()
        End Function
        
        Public Overridable Sub ConfigureScripts(ByVal scripts As List(Of ScriptReference))
            Dim jsPath = Path.Combine(HttpRuntime.AppDomainAppPath, "js")
            Dim includedScripts = CType(HttpRuntime.Cache("IncludedScripts"),List(Of String))
            If (includedScripts Is Nothing) Then
                includedScripts = New List(Of String)()
                Dim dep As CacheDependency = Nothing
                If Directory.Exists(jsPath) Then
                    dep = New FolderCacheDependency(jsPath, "*.js")
                    Dim ignorePath = Path.Combine(jsPath, "_ignore.txt")
                    Dim ignoreRegex As Regex = Nothing
                    If File.Exists(ignorePath) Then
                        ignoreRegex = BuildSearchPathRegex(File.ReadAllLines(ignorePath))
                    End If
                    For Each file in Directory.EnumerateFiles(jsPath, "*.js", SearchOption.AllDirectories)
                        Dim relativeFile = file.Substring((jsPath.Length + 1))
                        If (((ignoreRegex Is Nothing) OrElse Not (ignoreRegex.IsMatch(relativeFile))) AndAlso Not (DefaultExcludeScriptRegex.IsMatch(relativeFile))) Then
                            includedScripts.Add(("~/" + file.Substring(HttpRuntime.AppDomainAppPath.Length).Replace("\", "/")))
                        End If
                    Next
                    Dim i = 0
                    Do While (i < includedScripts.Count)
                        Dim scriptName = includedScripts(i)
                        If scriptName.EndsWith(".min.js") Then
                            If AquariumExtenderBase.EnableMinifiedScript Then
                                scriptName = (scriptName.Substring(0, (scriptName.Length - 7)) + ".js")
                            End If
                            includedScripts.Remove(scriptName)
                        Else
                            i = (i + 1)
                        End If
                    Loop
                End If
                HttpRuntime.Cache.Add("IncludedScripts", includedScripts, dep, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, Nothing)
            End If
            For Each file in includedScripts
                scripts.Add(AquariumExtenderBase.CreateScriptReference(file))
            Next
        End Sub
        
        Function BuildSearchPathRegex(ByVal paths() As String) As Regex
            If (paths.Length = 0) Then
                Return Nothing
            End If
            Dim sb = New StringBuilder()
            For Each path in paths
                If Not ((sb.Length = 0)) Then
                    sb.Append("|")
                End If
                sb.AppendFormat("({0})", Regex.Escape(path.Trim().Replace("/", "\")).Replace("\*", ".*"))
            Next
            Return New Regex(sb.ToString())
        End Function
        
        Public Shared Sub CompressOutput(ByVal context As HttpContext, ByVal data As String)
            Dim request = context.Request
            Dim response = context.Response
            Dim acceptEncoding = request.Headers("Accept-Encoding")
            If Not (String.IsNullOrEmpty(acceptEncoding)) Then
                If acceptEncoding.Contains("gzip") Then
                    response.Filter = New GZipStream(response.Filter, CompressionMode.Compress)
                    response.AppendHeader("Content-Encoding", "gzip")
                Else
                    If acceptEncoding.Contains("deflate") Then
                        response.Filter = New DeflateStream(response.Filter, CompressionMode.Compress)
                        response.AppendHeader("Content-Encoding", "deflate")
                    End If
                End If
            End If
            Dim output = Encoding.UTF8.GetBytes(data)
            response.ContentEncoding = Encoding.Unicode
            response.AddHeader("Content-Length", output.Length.ToString())
            response.OutputStream.Write(output, 0, output.Length)
            Try 
                response.Flush()
            Catch __exception As Exception
            End Try
        End Sub
        
        Public Shared Sub HandleServiceRequest(ByVal context As HttpContext)
            Dim methodName = context.Request.AppRelativeCurrentExecutionFilePath.ToLowerInvariant()
            If methodName.StartsWith(AquariumExtenderBase.DefaultServicePath) Then
                methodName = methodName.Substring((AquariumExtenderBase.DefaultServicePath.Length + 1))
            Else
                If methodName.StartsWith(AquariumExtenderBase.AppServicePath) Then
                    methodName = methodName.Substring((AquariumExtenderBase.AppServicePath.Length + 1))
                End If
            End If
            Dim indexOfSlash = methodName.IndexOf("/")
            If Not ((indexOfSlash = -1)) Then
                methodName = methodName.Substring(0, indexOfSlash)
            End If
            If String.IsNullOrEmpty(methodName) Then
                Throw New HttpException(400, "Method not specified.")
            End If
            Dim handler As ServiceRequestHandler = Nothing
            If RequestHandlers.TryGetValue(methodName.ToLower(), handler) Then
                Dim args = RequestValidationService.ToJson(context)
                Dim result As Object = Nothing
                Dim allowedMethods = handler.AllowedMethods
                If ((Not (allowedMethods) Is Nothing) AndAlso Not (allowedMethods.Contains(context.Request.HttpMethod))) Then
                    Throw New HttpException(405, "This HTTP Method is not allowed.")
                End If
                If (handler.RequiresAuthentication AndAlso Not (context.Request.IsAuthenticated)) Then
                    Throw New HttpException(403, "Requires authentication.")
                End If
                Try 
                    result = handler.HandleRequest(New DataControllerService(), args)
                Catch rex As ServiceRequestRedirectException
                    result = New JObject()
                    CType(result,JObject)("RedirectUrl") = rex.RedirectUrl
                Catch ex As Exception
                    result = handler.HandleException(args, ex)
                End Try
                If (Not (result) Is Nothing) Then
                    context.Response.ContentType = "application/json; charset=utf-8"
                    Dim output = String.Format("{{""d"":{0}}}", JsonConvert.SerializeObject(result))
                    ApplicationServices.CompressOutput(context, CompressViewPageJsonOutput(output))
                End If
            Else
                Throw New HttpException(404, "Method not found.")
            End If
            context.Response.End()
        End Sub
        
        Public Shared Function CompressViewPageJsonOutput(ByVal output As String) As String
            Dim startIndex = 0
            Dim dataIndex = 0
            Dim lastIndex = 0
            Dim lastLength = output.Length
            Do While true
                startIndex = output.IndexOf("{""Controller"":", lastIndex, StringComparison.Ordinal)
                dataIndex = output.IndexOf(",""NewRow"":", lastIndex, StringComparison.Ordinal)
                If ((startIndex < 0) OrElse (dataIndex < 0)) Then
                    Exit Do
                End If
                Dim metadata = (output.Substring(0, startIndex) + ViewPageCompressRegex.Replace(output.Substring(startIndex, (dataIndex - startIndex)), String.Empty))
                If metadata.EndsWith(",") Then
                    metadata = metadata.Substring(0, (metadata.Length - 1))
                End If
                output = (ViewPageCompress2Regex.Replace(metadata, "}$1") + output.Substring(dataIndex))
                lastIndex = ((dataIndex + 10)  _
                            - (lastLength - output.Length))
                lastLength = output.Length
            Loop
            Return output
        End Function
        
        Public Shared Function ResolveClientUrl(ByVal relativeUrl As String) As String
            Dim request = HttpContext.Current.Request
            Dim root = (request.Url.Scheme  _
                        + (Uri.SchemeDelimiter + request.Url.Host))
            If Not (request.Url.IsDefaultPort) Then
                root = (root  _
                            + (":" + Convert.ToString(request.Url.Port)))
            End If
            If relativeUrl.StartsWith("~/") Then
                relativeUrl = relativeUrl.Substring(2)
            Else
                If relativeUrl.StartsWith("/") Then
                    relativeUrl = relativeUrl.Substring(1)
                Else
                    relativeUrl = (request.Url.AbsolutePath  _
                                + ("/" + relativeUrl))
                End If
            End If
            Dim appPath = request.ApplicationPath
            If Not (appPath.EndsWith("/")) Then
                appPath = (appPath + "/")
            End If
            Dim result = ((root + appPath)  _
                        + relativeUrl)
            If String.IsNullOrEmpty(relativeUrl) Then
                result = result.Substring(0, (result.Length - 1))
            End If
            Return result
        End Function
        
        Public Overridable Function CorsConfiguration(ByVal request As HttpRequest) As SortedDictionary(Of String, String)
            If EnableCors Then
                Dim list = New SortedDictionary(Of String, String)()
                Dim origin = request.Headers("Origin")
                If String.IsNullOrEmpty(origin) Then
                    origin = "*"
                End If
                list("Access-Control-Allow-Origin") = origin
                list("Access-Control-Allow-Methods") = "GET,POST"
                list("Access-Control-Allow-Credentials") = "true"
                list("Access-Control-Allow-Headers") = "content-type,authorization"
                Return list
            End If
            Return Nothing
        End Function
        
        Private Shared Sub EnsureJsonProperty(ByVal ptr As JObject, ByVal path As String, ByVal defaultValue As Object)
            If (defaultValue Is Nothing) Then
                defaultValue = String.Empty
            End If
            Dim parts = path.Split(Global.Microsoft.VisualBasic.ChrW(46))
            Dim counter = parts.Length
            For Each part in parts
                counter = (counter - 1)
                If (ptr(part) Is Nothing) Then
                    If Not ((counter = 0)) Then
                        ptr(part) = New JObject()
                    Else
                        ptr(part) = JToken.FromObject(defaultValue)
                    End If
                End If
                If Not ((counter = 0)) Then
                    ptr = CType(ptr(part),JObject)
                End If
            Next
        End Sub
        
        Public Shared Function TryGetJsonProperty(ByVal ptr As JObject, ByVal path As String) As JToken
            Dim parts = path.Split(Global.Microsoft.VisualBasic.ChrW(46))
            Dim temp As JToken = Nothing
            Dim i = 0
            Do While (i < (parts.Length - 1))
                temp = ptr(parts(i))
                If (Not (temp) Is Nothing) Then
                    ptr = CType(temp,JObject)
                Else
                    Return Nothing
                End If
                i = (i + 1)
            Loop
            Return ptr(parts((parts.Length - 1)))
        End Function
        
        Public Overridable Sub OAuthSetState(ByVal name As String, ByVal value As String)
        End Sub
        
        Public Overridable Sub OAuthSetUserObject(ByVal user As JObject)
        End Sub
        
        Public Overridable Sub OAuthSyncUser(ByVal user As MembershipUser)
        End Sub
        
        Public Overridable Function ValidateBlobAccess(ByVal context As HttpContext, ByVal handler As BlobHandlerInfo, ByVal ba As BlobAdapter, ByVal val As String) As Boolean
            'allow access to a requests from CMS
            If Blob.DirectAccessMode Then
                Return true
            End If
            Dim key = context.Request.Params("_validationKey")
            If (((ba Is Nothing) OrElse Not (ba.IsPublic)) AndAlso (Not (context.User.Identity.IsAuthenticated) AndAlso Not ((key = BlobAdapter.ValidationKey)))) Then
                Return Not (ApplicationServicesBase.AuthorizationIsSupported)
            End If
            'allow access to a request received from ReportViewer
            If (key = BlobAdapter.ValidationKey) Then
                Return true
            End If
            'confirm that the user is allowed to see the corresponding data row
            Dim pr = New PageRequest(0, 1, String.Empty, Nothing)
            Dim config = Controller.CreateConfigurationInstance([GetType](), handler.DataController)
            Dim iterator = config.Select("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
            Dim filter = New List(Of String)()
            Dim vals = val.Split(Global.Microsoft.VisualBasic.ChrW(124))
            Dim count = 0
            Dim fieldFilter = New List(Of String)()
            Do While iterator.MoveNext()
                Dim pk = iterator.Current.GetAttribute("name", String.Empty)
                filter.Add(String.Format("{0}:={1}", pk, vals(count)))
                fieldFilter.Add(pk)
                count = (count + 1)
            Loop
            pr.Filter = filter.ToArray()
            Dim fieldName = config.SelectSingleNode("/c:dataController/c:fields/c:field[@onDemandHandler='{0}']/@name", handler.Key).Value
            iterator = config.Select(String.Format("/c:dataController/c:views/c:view[c:dataFields/c:dataField/@fieldName='{0}' or c:c"& _ 
                        "ategories/c:category/c:dataFields/c:dataField/@fieldName='{0}']", fieldName), String.Empty)
            Dim view As String
            If iterator.MoveNext() Then
                view = iterator.Current.GetAttribute("id", String.Empty)
            Else
                view = Controller.GetSelectView(handler.DataController)
            End If
            fieldFilter.Add(fieldName)
            pr.FieldFilter = fieldFilter.ToArray()
            pr.RequiresMetaData = true
            pr.MetadataFilter = New String() {"fields"}
            Dim page = ControllerFactory.CreateDataController().GetPage(handler.DataController, view, pr)
            'make sure that exactly one row is returned and the number of fields in the output is exactly equal to the number of PK fields plus 1 (blob field)
            If ((page.Rows.Count = 0) OrElse (page.FindField(fieldName) Is Nothing)) Then
                Return false
            End If
            Return true
        End Function
    End Class
    
    Public Class AnonymousUserIdentity
        Inherits Object
        Implements IIdentity
        
        ReadOnly Property IIdentity_AuthenticationType() As String Implements IIdentity.AuthenticationType
            Get
                Return "None"
            End Get
        End Property
        
        ReadOnly Property IIdentity_IsAuthenticated() As Boolean Implements IIdentity.IsAuthenticated
            Get
                Return false
            End Get
        End Property
        
        ReadOnly Property IIdentity_Name() As String Implements IIdentity.Name
            Get
                Return String.Empty
            End Get
        End Property
    End Class
    
    Partial Public Class ApplicationSiteMapProvider
        Inherits ApplicationSiteMapProviderBase
    End Class
    
    Public Class ApplicationSiteMapProviderBase
        Inherits System.Web.XmlSiteMapProvider
        
        Public Overrides Function IsAccessibleToUser(ByVal context As HttpContext, ByVal node As SiteMapNode) As Boolean
            Dim device = node("Device")
            Dim isTouchUI = ApplicationServices.IsTouchClient
            If ((device = "touch") AndAlso Not (isTouchUI)) Then
                Return false
            End If
            If ((device = "desktop") AndAlso isTouchUI) Then
                Return false
            End If
            Return MyBase.IsAccessibleToUser(context, node)
        End Function
    End Class
    
    Partial Public Class PlaceholderHandler
        Inherits PlaceholderHandlerBase
    End Class
    
    Public Class PlaceholderHandlerBase
        Inherits Object
        Implements IHttpHandler
        
        Private Shared m_ImageSizeRegex As Regex = New Regex("((?'background'[a-zA-Z0-9]+?)-((?'textcolor'[a-zA-Z0-9]+?)-)?)?(?'width'[0-9]+?)("& _ 
                "x(?'height'[0-9]*))?\.[a-zA-Z][a-zA-Z][a-zA-Z]")
        
        Overridable ReadOnly Property IHttpHandler_IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return true
            End Get
        End Property
        
        Overridable Sub IHttpHandler_ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            'Get file name
            Dim routeValues = context.Request.RequestContext.RouteData.Values
            Dim fileName = CType(routeValues("FileName"),String)
            'Get extension
            Dim ext = Path.GetExtension(fileName)
            Dim format = ImageFormat.Png
            Dim contentType = "image/png"
            If (ext = ".jpg") Then
                format = ImageFormat.Jpeg
                contentType = "image/jpg"
            Else
                If (ext = ".gif") Then
                    format = ImageFormat.Gif
                    contentType = "image/jpg"
                End If
            End If
            'get width and height
            Dim regexMatch = m_ImageSizeRegex.Matches(fileName)(0)
            Dim widthCapture = regexMatch.Groups("width")
            Dim width = 500
            If Not ((widthCapture.Length = 0)) Then
                width = Convert.ToInt32(widthCapture.Value)
            End If
            If (width = 0) Then
                width = 500
            End If
            If (width > 4096) Then
                width = 4096
            End If
            Dim heightCapture = regexMatch.Groups("height")
            Dim height = width
            If Not ((heightCapture.Length = 0)) Then
                height = Convert.ToInt32(heightCapture.Value)
            End If
            If (height = 0) Then
                height = 500
            End If
            If (height > 4096) Then
                height = 4096
            End If
            'Get background and text colors
            Dim background = GetColor(regexMatch.Groups("background"), Color.LightGray)
            Dim textColor = GetColor(regexMatch.Groups("textcolor"), Color.Black)
            Dim fontSize = ((width + height)  _
                        / 50)
            If (fontSize < 10) Then
                fontSize = 10
            End If
            Dim font = New Font(FontFamily.GenericSansSerif, fontSize)
            'Get text
            Dim text = context.Request.QueryString("text")
            If String.IsNullOrEmpty(text) Then
                text = String.Format("{0} x {1}", width, height)
            End If
            'Get position for text
            Dim textSize As SizeF
            Using img = New Bitmap(1, 1)
                Dim textDrawing = Graphics.FromImage(img)
                textSize = textDrawing.MeasureString(text, font)
            End Using
            'Draw the image
            Using image = New Bitmap(width, height)
                Dim drawing = Graphics.FromImage(image)
                drawing.Clear(background)
                Using textBrush = New SolidBrush(textColor)
                    drawing.DrawString(text, font, textBrush, ((width - textSize.Width)  _
                                    / 2), ((height - textSize.Height)  _
                                    / 2))
                End Using
                drawing.Save()
                drawing.Dispose()
                'Return image
                Using stream = New MemoryStream()
                    image.Save(stream, format)
                    Dim cache = context.Response.Cache
                    cache.SetCacheability(HttpCacheability.Public)
                    cache.SetOmitVaryStar(true)
                    cache.SetExpires(DateTime.Now.AddDays(365))
                    cache.SetValidUntilExpires(true)
                    cache.SetLastModifiedFromFileDependencies()
                    context.Response.ContentType = contentType
                    context.Response.AddHeader("Content-Length", Convert.ToString(stream.Length))
                    context.Response.AddHeader("File-Name", fileName)
                    context.Response.BinaryWrite(stream.ToArray())
                    context.Response.OutputStream.Flush()
                End Using
            End Using
        End Sub
        
        Private Shared Function GetColor(ByVal colorName As Capture, ByVal defaultColor As Color) As Color
            Try 
                If (colorName.Length > 0) Then
                    Dim s = colorName.Value
                    If Regex.IsMatch(s, "^[0-9abcdef]{3,6}$") Then
                        s = ("#" + s)
                    End If
                    Return ColorTranslator.FromHtml(s)
                End If
            Catch __exception As Exception
            End Try
            Return defaultColor
        End Function
    End Class
    
    Public Class GenericRoute
        Inherits Object
        Implements IRouteHandler
        
        Private m_Handler As IHttpHandler
        
        Public Sub New(ByVal handler As IHttpHandler)
            MyBase.New
            m_Handler = handler
        End Sub
        
        Function IRouteHandler_GetHttpHandler(ByVal context As RequestContext) As IHttpHandler Implements IRouteHandler.GetHttpHandler
            Return m_Handler
        End Function
        
        Public Shared Sub Map(ByVal routes As RouteCollection, ByVal handler As IHttpHandler, ByVal url As String)
            Dim r = New Route(url, New GenericRoute(handler))
            r.Defaults = New RouteValueDictionary()
            r.Constraints = New RouteValueDictionary()
            routes.Add(r)
        End Sub
    End Class
    
    Public Class UserTicket
        
        <JsonProperty("name")>  _
        Public UserName As String
        
        <JsonProperty("email")>  _
        Public Email As String
        
        <JsonProperty("access_token")>  _
        Public AccessToken As String
        
        <JsonProperty("refresh_token")>  _
        Public RefreshToken As String
        
        <JsonProperty("picture")>  _
        Public Picture As String
        
        <JsonProperty("claims")>  _
        Public Claims As Dictionary(Of String, String) = New Dictionary(Of String, String)()
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal user As MembershipUser)
            MyBase.New
            UserName = user.UserName
            Email = user.Email
            Picture = ApplicationServices.Create().UserPictureString(user)
        End Sub
        
        Public Sub New(ByVal user As MembershipUser, ByVal accessToken As String, ByVal refreshToken As String)
            Me.New(user)
            Me.AccessToken = accessToken
            Me.RefreshToken = refreshToken
        End Sub
    End Class
    
    Public Class ManifestFile
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Name As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Path As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_MD5 As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Length As Integer
        
        Public Property Name() As String
            Get
                Return m_Name
            End Get
            Set
                m_Name = value
            End Set
        End Property
        
        Public Property Path() As String
            Get
                Return m_Path
            End Get
            Set
                m_Path = value
            End Set
        End Property
        
        Public Property MD5() As String
            Get
                Return m_MD5
            End Get
            Set
                m_MD5 = value
            End Set
        End Property
        
        Public Property Length() As Integer
            Get
                Return m_Length
            End Get
            Set
                m_Length = value
            End Set
        End Property
        
        Public Shared Function FromPath(ByVal relativePath As String) As ManifestFile
            Dim f = New ManifestFile()
            If relativePath.Contains("?") Then
                relativePath = relativePath.Substring(0, relativePath.IndexOf("?"))
            End If
            f.Path = relativePath.Replace(Global.Microsoft.VisualBasic.ChrW(92), Global.Microsoft.VisualBasic.ChrW(47)).Replace("~/", String.Empty)
            f.Name = System.IO.Path.GetFileName(f.Path)
            Dim fileBytes() As Byte = Nothing
            If (f.Path = "js/daf/add.min.js") Then
                fileBytes = Encoding.UTF8.GetBytes(ApplicationServices.Current.AddScripts())
            Else
                If (f.Path = "css/daf/add.min.css") Then
                    fileBytes = Encoding.UTF8.GetBytes(ApplicationServices.Current.AddStyleSheets())
                Else
                    fileBytes = File.ReadAllBytes(HttpContext.Current.Server.MapPath(("~/" + f.Path)))
                End If
            End If
            f.MD5 = ComputeHash(fileBytes)
            f.Length = fileBytes.Length
            Return f
        End Function
        
        Public Shared Function FromResource(ByVal resourceName As String) As ManifestFile
            Dim file = New ManifestFile()
            file.Name = resourceName
            file.Path = ("_resources/" + resourceName)
            Using s = ControllerConfigurationUtility.GetResourceStream(resourceName)
                Using ms = New MemoryStream()
                    s.CopyTo(ms)
                    file.MD5 = ComputeHash(ms.ToArray())
                    file.Length = Convert.ToInt32(ms.Length)
                End Using
            End Using
            Return file
        End Function
        
        Public Shared Function FromUrl(ByVal url As String) As ManifestFile
            url = url.Replace(Global.Microsoft.VisualBasic.ChrW(92), Global.Microsoft.VisualBasic.ChrW(47)).Replace("~/", String.Empty)
            Dim file = New ManifestFile()
            file.Path = url
            file.Name = System.IO.Path.GetFileName(file.Path)
            Using sw = New StringWriter()
                Dim handlerUrl = ("/" + url.Replace(".html", String.Empty).Replace(".htm", String.Empty))
                Dim page = GetContent(handlerUrl)
                For Each addon in ApplicationServicesBase.Addons
                    page = CType(addon.GetType().GetMethod("Content").Invoke(addon, New Object() {handlerUrl, page}),String)
                Next
                Dim bytes = Encoding.UTF8.GetBytes(page)
                file.MD5 = ComputeHash(bytes)
                file.Length = bytes.Length
            End Using
            Return file
        End Function
        
        Public Shared Function GetConfig(ByVal config As String) As ManifestFile
            Dim configBytes = Encoding.UTF8.GetBytes(config)
            Dim configFile = New ManifestFile()
            configFile.Path = "js/host/config.js"
            configFile.Name = "config.js"
            configFile.Length = configBytes.Length
            configFile.MD5 = ComputeHash(configBytes)
            Return configFile
        End Function
        
        Public Shared Function ComputeHash(ByVal data() As Byte) As String
            Dim prov = New MD5CryptoServiceProvider()
            Dim hashData = prov.ComputeHash(data)
            Dim sb = New StringBuilder()
            Dim i = 0
            Do While (i < hashData.Length)
                sb.Append(hashData(i).ToString("x2"))
                i = (i + 1)
            Loop
            Return sb.ToString()
        End Function
        
        Public Shared Function GetContent(ByVal url As String) As String
            Dim original = HttpContext.Current.Request
            Dim homePage = ApplicationServices.ResolveClientUrl(url)
            Dim request = WebRequest.CreateHttp(homePage)
            request.AutomaticDecompression = (DecompressionMethods.Deflate Or DecompressionMethods.GZip)
            For Each header As String in original.Headers
                If (Not (WebHeaderCollection.IsRestricted(header)) AndAlso Not ((header = "Cookie"))) Then
                    request.Headers(header) = original.Headers(header)
                End If
            Next
            request.Headers("X-Cot-Manifest-Request") = "true"
            Dim args = HttpContext.Current.Items("ServiceRequestHandler_args")
            If (Not (args) Is Nothing) Then
                request.Headers("X-Device-Id") = CType(CType(args,JObject)("deviceId"),String)
            End If
            Using sr = New StreamReader(request.GetResponse().GetResponseStream())
                Return sr.ReadToEnd()
            End Using
        End Function
    End Class
    
    Public Class StylesheetGenerator
        
        Private m_Template As String
        
        Private m_Theme As JObject
        
        Private m_Accent As JObject
        
        Private m_ThemeVariables As SortedDictionary(Of String, String) = New SortedDictionary(Of String, String)()
        
        Public Shared ThemeStylesheetRegex As Regex = New Regex("^touch-theme\.(?'Theme'\w+)\.((?'Accent'\w+)\.)?css$")
        
        Public Shared ThemeVariableRegex As Regex = New Regex("(?'Item'(?'Before'\w+:\s*)\/\*\s*(?'Name'(@[\w\.]+(,\s*)?)+)\s*\*\/(?'Value'.+?))"& _ 
                "(?'After'(!important)?;\s*)$", RegexOptions.Multiline)
        
        Public Sub New(ByVal theme As String, ByVal accent As String)
            MyBase.New
            Dim touchPath = HttpContext.Current.Server.MapPath("~/css")
            Dim css = Path.Combine(touchPath, "daf", "touch-theme.css")
            If File.Exists(css) Then
                m_Template = File.ReadAllText(css)
                Dim themeFile = Path.Combine(touchPath, "themes", ("touch-theme."  _
                                + (theme + ".json")))
                Dim accentFile = Path.Combine(touchPath, "themes", ("touch-accent."  _
                                + (accent + ".json")))
                If (File.Exists(themeFile) AndAlso File.Exists(accentFile)) Then
                    m_Accent = JObject.Parse(File.ReadAllText(accentFile))
                    m_Theme = JObject.Parse(File.ReadAllText(themeFile))
                End If
            End If
        End Sub
        
        Public Shared Function Compile(ByVal fileName As String) As String
            Dim m = ThemeStylesheetRegex.Match(fileName)
            If m.Success Then
                Return New StylesheetGenerator(m.Groups("Theme").Value, m.Groups("Accent").Value).ToString()
            End If
            Return String.Empty
        End Function
        
        Public Shared Function Minify(ByVal css As String) As String
            css = Regex.Replace(css, "[a-zA-Z]+#", "#")
            css = Regex.Replace(css, "[\n\r]+\s*", String.Empty)
            css = Regex.Replace(css, "\s\s+", " ")
            css = Regex.Replace(css, "\s?([:,;{}])\s?", "$1")
            css = css.Replace(";}", "}")
            css = Regex.Replace(css, "([\s:]0)(px|pt|%|em)", "$1")
            css = Regex.Replace(css, "/\*[\d\D]*?\*/", String.Empty)
            Return css
        End Function
        
        Public Overrides Function ToString() As String
            Dim result = m_Template
            If (Not (String.IsNullOrEmpty(m_Template)) AndAlso ((Not (m_Theme) Is Nothing) AndAlso (Not (m_Accent) Is Nothing))) Then
                result = ThemeVariableRegex.Replace(result, AddressOf DoReplaceThemeVariables)
            End If
            If ApplicationServices.EnableMinifiedCss Then
                result = Minify(result)
            End If
            Return result
        End Function
        
        Protected Function DoReplaceThemeVariables(ByVal m As Match) As String
            Dim variable = m.Groups("Name").Value
            Dim before = m.Groups("Before").Value
            Dim after = m.Groups("After").Value
            Dim parts = variable.Split(Global.Microsoft.VisualBasic.ChrW(44))
            Dim value As String = Nothing
            For Each part in parts
                If TryGetThemeVariable(part.Trim().Substring(1), value) Then
                    Exit For
                End If
            Next
            If String.IsNullOrEmpty(value) Then
                value = m.Groups("Value").Value
            End If
            If ApplicationServices.EnableMinifiedCss Then
                Return ((before + value)  _
                            + after)
            Else
                Return ((before  _
                            + (" /*" + variable))  _
                            + (("*/ " + value)  _
                            + after))
            End If
        End Function
        
        Protected Function TryGetThemeVariable(ByVal name As String, ByRef value As String) As Boolean
            If Not (m_ThemeVariables.TryGetValue(name, value)) Then
                Dim token As JToken = Nothing
                If name.StartsWith("theme.") Then
                    token = ApplicationServicesBase.TryGetJsonProperty(m_Accent, String.Join(".", "theme", m_Theme("name"), name.Substring(6)))
                    If ((token Is Nothing) OrElse (token.Type = JTokenType.Null)) Then
                        token = ApplicationServicesBase.TryGetJsonProperty(m_Theme, name.Substring(6))
                    End If
                Else
                    token = ApplicationServicesBase.TryGetJsonProperty(m_Accent, String.Join(".", "theme", m_Theme("name"), name))
                    If ((token Is Nothing) OrElse (token.Type = JTokenType.Null)) Then
                        token = ApplicationServicesBase.TryGetJsonProperty(m_Accent, name)
                    End If
                End If
                If ((Not (token) Is Nothing) AndAlso Not ((token.Type = JTokenType.Null))) Then
                    value = CType(token,String)
                End If
                m_ThemeVariables(name) = value
            End If
            Return Not (String.IsNullOrEmpty(value))
        End Function
    End Class
    
    Public Class FolderCacheDependency
        Inherits CacheDependency
        
        Private m_Watcher As FileSystemWatcher
        
        Public Sub New(ByVal dirName As String, ByVal filter As String)
            MyBase.New
            m_Watcher = New FileSystemWatcher(dirName, filter)
            m_Watcher.EnableRaisingEvents = true
            AddHandler m_Watcher.Changed, AddressOf Me.watcher_Changed
            AddHandler m_Watcher.Deleted, AddressOf Me.watcher_Changed
            AddHandler m_Watcher.Created, AddressOf Me.watcher_Changed
            AddHandler m_Watcher.Renamed, AddressOf Me.watcher_Renamed
        End Sub
        
        Sub watcher_Renamed(ByVal sender As Object, ByVal e As RenamedEventArgs)
            NotifyDependencyChanged(Me, e)
        End Sub
        
        Sub watcher_Changed(ByVal sender As Object, ByVal e As FileSystemEventArgs)
            NotifyDependencyChanged(Me, e)
        End Sub
    End Class
End Namespace
