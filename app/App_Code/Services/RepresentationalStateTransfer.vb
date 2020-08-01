Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Norwindt.Data
Imports Norwindt.Handlers
Imports Norwindt.Security
Imports Norwindt.Web
Imports System
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Web
Imports System.Web.Routing
Imports System.Web.Security
Imports System.Web.UI
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Services
    
    Public Class UriRestConfig
        
        Private m_Uri As Regex
        
        Private m_Properties As SortedDictionary(Of String, String)
        
        Public Shared SupportedJSONContentTypes() As String = New String() {"application/json", "text/javascript", "application/javascript", "application/ecmascript", "application/x-ecmascript"}
        
        Public Sub New(ByVal uri As String)
            MyBase.New
            m_Uri = New Regex(uri, RegexOptions.IgnoreCase)
            m_Properties = New SortedDictionary(Of String, String)()
        End Sub
        
        Public Default Property Item(ByVal propertyName As String) As String
            Get
                Dim result As String = Nothing
                m_Properties.TryGetValue(propertyName.ToLower(), result)
                Return result
            End Get
            Set
                If Not (String.IsNullOrEmpty(value)) Then
                    value = value.Trim()
                End If
                m_Properties(propertyName.ToLower()) = value
            End Set
        End Property
        
        Public Shared Function Enumerate(ByVal config As ControllerConfiguration) As List(Of UriRestConfig)
            Dim list = New List(Of UriRestConfig)()
            Dim restConfigNode = config.SelectSingleNode("/c:dataController/c:restConfig")
            If (Not (restConfigNode) Is Nothing) Then
                Dim urc As UriRestConfig = Nothing
                'configuration regex: ^\s*(?'Property'\w+)\s*(:|=)\s*(?'Value'.+?)\s*$
                Dim m = Regex.Match(restConfigNode.Value, "^\s*(?'Property'\w+)\s*(:|=)\s*(?'Value'.+?)\s*$", (RegexOptions.IgnoreCase Or RegexOptions.Multiline))
                Do While m.Success
                    Dim propertyName = m.Groups("Property").Value
                    Dim propertyValue = m.Groups("Value").Value
                    If propertyName.Equals("Uri", StringComparison.CurrentCultureIgnoreCase) Then
                        Try 
                            urc = New UriRestConfig(propertyValue)
                            list.Add(urc)
                        Catch __exception As Exception
                        End Try
                    Else
                        If (Not (urc) Is Nothing) Then
                            urc(propertyName) = propertyValue
                        End If
                    End If
                    m = m.NextMatch()
                Loop
            End If
            Return list
        End Function
        
        Public Overridable Function IsMatch(ByVal request As HttpRequest) As Boolean
            Return m_Uri.IsMatch(request.Path)
        End Function
        
        Public Shared Function RequiresAuthentication(ByVal request As HttpRequest, ByVal config As ControllerConfiguration) As Boolean
            For Each urc in Enumerate(config)
                If (urc.IsMatch(request) AndAlso (urc("Users") = "?")) Then
                    Return false
                End If
            Next
            Return true
        End Function
        
        Public Shared Function IsAuthorized(ByVal request As HttpRequest, ByVal config As ControllerConfiguration) As Boolean
            If (request.AcceptTypes Is Nothing) Then
                Return false
            End If
            For Each urc in Enumerate(config)
                If urc.IsMatch(request) Then
                    'verify HTTP method
                    Dim httpMethod = urc("Method")
                    If Not (String.IsNullOrEmpty(httpMethod)) Then
                        Dim methodList = Regex.Split(httpMethod, "(\s*,\s*)")
                        If Not (methodList.Contains(request.HttpMethod)) Then
                            Return false
                        End If
                    End If
                    'verify user identity
                    Dim users = urc("Users")
                    If (Not (String.IsNullOrEmpty(users)) AndAlso Not ((users = "?"))) Then
                        If Not (HttpContext.Current.User.Identity.IsAuthenticated) Then
                            Return false
                        End If
                        If Not ((users = "*")) Then
                            Dim userList = Regex.Split(users, "(\s*,\s*)")
                            If Not (userList.Contains(HttpContext.Current.User.Identity.Name)) Then
                                Return false
                            End If
                        End If
                    End If
                    'verify user roles
                    Dim roles = urc("Roles")
                    If (Not (String.IsNullOrEmpty(roles)) AndAlso Not (DataControllerBase.UserIsInRole(roles))) Then
                        Return false
                    End If
                    'verify SSL, Xml, and JSON constrains
                    If (true.ToString().Equals(urc("Ssl"), StringComparison.OrdinalIgnoreCase) AndAlso Not (request.IsSecureConnection)) Then
                        Return false
                    End If
                    If (false.ToString().Equals(urc("Xml"), StringComparison.OrdinalIgnoreCase) AndAlso Not (IsJSONRequest(request))) Then
                        Return false
                    End If
                    If (false.ToString().Equals(urc("Json"), StringComparison.OrdinalIgnoreCase) AndAlso IsJSONRequest(request)) Then
                        Return false
                    End If
                    Return true
                End If
            Next
            Return false
        End Function
        
        Public Shared Function TypeOfJSONRequest(ByVal request As HttpRequest) As String
            If (((request.QueryString("_dataType") = "json") OrElse Not (String.IsNullOrEmpty(request.QueryString("_instance")))) OrElse Not (String.IsNullOrEmpty(request.QueryString("callback")))) Then
                Return "application/javascript"
            End If
            If (Not (request.AcceptTypes) Is Nothing) Then
                For Each t in request.AcceptTypes
                    Dim typeIndex = Array.IndexOf(UriRestConfig.SupportedJSONContentTypes, t)
                    If Not ((typeIndex = -1)) Then
                        Return t
                    End If
                Next
            End If
            Return Nothing
        End Function
        
        Public Shared Function IsJSONRequest(ByVal request As HttpRequest) As Boolean
            Return Not (String.IsNullOrEmpty(TypeOfJSONRequest(request)))
        End Function
        
        Public Shared Function IsJSONPRequest(ByVal request As HttpRequest) As Boolean
            Dim t = TypeOfJSONRequest(request)
            Return (Not (String.IsNullOrEmpty(t)) AndAlso Not ((t = SupportedJSONContentTypes(0))))
        End Function
    End Class
    
    Partial Public Class RepresentationalStateTransfer
        Inherits RepresentationalStateTransferBase
    End Class
    
    Public Class RepresentationalStateTransferBase
        Inherits Object
        Implements IHttpHandler, System.Web.SessionState.IRequiresSessionState
        
        Public Shared JsonDateRegex As Regex = New Regex("""\\/Date\((\-?\d+)\)\\/""")
        
        Public Shared ScriptResourceRegex As Regex = New Regex("^(?'ScriptName'[\w\-]+?)(\-(?'Version'[\.\d]+))?(\.(?'Culture'[\w\-]+?))?(\.(?'Ac"& _ 
                "cent'\w+))?\.(?'Extension'js|css)", RegexOptions.IgnoreCase)
        
        Public Shared CultureJavaScriptRegex As Regex = New Regex("//<\!\[CDATA\[\s+(?'JavaScript'var __cultureInfo[\s\S]*?)//\]\]>")
        
        Public Shared NumericTypes() As String = New String() {"SByte", "Byte", "Int16", "Int32", "UInt32", "Int64", "Single", "Double", "Decimal", "Currency"}
        
        Overridable ReadOnly Property IHttpHandler_IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return true
            End Get
        End Property
        
        Protected Overridable ReadOnly Property HttpMethod() As String
            Get
                Dim request = HttpContext.Current.Request
                Dim requestType = request.HttpMethod
                If ((requestType = "GET") AndAlso Not (String.IsNullOrEmpty(request("callback")))) Then
                    Dim t = request.QueryString("_type")
                    If Not (String.IsNullOrEmpty(t)) Then
                        requestType = t
                    End If
                End If
                Return requestType
            End Get
        End Property
        
        Overridable Sub IHttpHandler_ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            CultureManager.Initialize()
            Dim routeValues = context.Request.RequestContext.RouteData.Values
            Dim controllerName = CType(routeValues("Controller"),String)
            If String.IsNullOrEmpty(controllerName) Then
                controllerName = context.Request.QueryString("_controller")
            End If
            Dim output = context.Response.OutputStream
            Dim contentType = "text/xml"
            Dim json = UriRestConfig.IsJSONRequest(context.Request)
            If json Then
                contentType = (UriRestConfig.TypeOfJSONRequest(context.Request) + "; charset=utf-8")
            End If
            context.Response.ContentType = contentType
            Try 
                If (controllerName = "saas") Then
                    context.Response.StatusCode = 404
                Else
                    If (controllerName = "_authenticate") Then
                        AuthenticateSaaS(context)
                    Else
                        Dim script = ScriptResourceRegex.Match(controllerName)
                        Dim scriptName = script.Groups("ScriptName").Value
                        Dim isSaaS = (scriptName = "factory")
                        Dim isCombinedScript = (scriptName = "combined")
                        Dim isStylesheet = (scriptName = "stylesheet")
                        If ((isStylesheet OrElse (scriptName = "touch-theme")) AndAlso (script.Groups("Extension").Value = "css")) Then
                            context.Response.ContentType = "text/css"
                            Dim css = String.Empty
                            If isStylesheet Then
                                css = ApplicationServices.CombineTouchUIStylesheets(context)
                            Else
                                css = StylesheetGenerator.Compile(controllerName)
                            End If
                            ApplicationServices.CompressOutput(context, css)
                        Else
                            If ((isSaaS OrElse isCombinedScript) AndAlso (HttpMethod = "GET")) Then
                                CombineScripts(context, isSaaS, scriptName, script.Groups("Culture").Value, script.Groups("Version").Value)
                            Else
                                If Regex.IsMatch(HttpMethod, "^(GET|POST|DELETE|PUT)$") Then
                                    PerformRequest(context, output, json, controllerName)
                                Else
                                    context.Response.StatusCode = 400
                                End If
                            End If
                        End If
                    End If
                End If
            Catch er As Exception
                If Not ((context.Response.StatusCode = 302)) Then
                    context.Response.ContentType = "text/xml"
                    context.Response.Clear()
                    Dim writer = CreateXmlWriter(output)
                    RenderException(context, er, writer)
                    writer.Close()
                    context.Response.StatusCode = 400
                End If
            End Try
        End Sub
        
        Protected Overridable Sub CombineScripts(ByVal context As HttpContext, ByVal isSaaS As Boolean, ByVal scriptName As String, ByVal culture As String, ByVal version As String)
            Dim request = context.Request
            Dim response = context.Response
            If Not (isSaaS) Then
                Dim cache = response.Cache
                cache.SetCacheability(HttpCacheability.Public)
                cache.VaryByParams("_touch") = true
                cache.VaryByHeaders("User-Agent") = true
                cache.SetOmitVaryStar(true)
                cache.SetExpires(DateTime.Now.AddDays(365))
                cache.SetValidUntilExpires(true)
                cache.SetLastModifiedFromFileDependencies()
            End If
            If isSaaS Then
                If Not (String.IsNullOrEmpty(culture)) Then
                    Try 
                        Thread.CurrentThread.CurrentCulture = New CultureInfo(culture)
                        Thread.CurrentThread.CurrentUICulture = New CultureInfo(culture)
                    Catch __exception As Exception
                    End Try
                End If
            End If
            Dim sb = New StringBuilder()
            Dim baseUrl = String.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, request.ApplicationPath)
            Dim scripts = AquariumExtenderBase.StandardScripts(true)
            For Each sr in scripts
                Dim add = true
                Dim path = sr.Path
                Dim index = path.IndexOf("?")
                If (index > 0) Then
                    path = path.Substring(0, index)
                    If path.EndsWith("_System.js") Then
                        add = Not ((request.QueryString("jquery") = "false"))
                    Else
                        If (path.Contains("daf-membership") AndAlso Not (ApplicationServicesBase.AuthorizationIsSupported)) Then
                            add = false
                        End If
                    End If
                End If
                If add Then
                    Try 
                        Dim script As String
                        If path.Equals("~/js/daf/add.min.js") Then
                            script = ApplicationServices.Current.AddScripts()
                        Else
                            If String.IsNullOrEmpty(path) Then
                                script = New StreamReader([GetType]().Assembly.GetManifestResourceStream(sr.Name)).ReadToEnd()
                            Else
                                script = File.ReadAllText(context.Server.MapPath(path))
                            End If
                        End If
                        script = script.Replace(" sourceMappingURL=", " sourceMappingURL=../js/")
                        sb.AppendLine(script)
                        If Not (script.EndsWith(";")) Then
                            sb.Append(";")
                        End If
                    Catch ex As Exception
                        sb.AppendFormat("alert('{0}');", BusinessRules.JavaScriptString(String.Format("Unable to load {0}{1}:"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{2}", path, sr.Name, ex.Message)))
                    End Try
                End If
            Next
            If isSaaS Then
                If ApplicationServices.IsTouchClient Then
                    sb.AppendFormat(String.Format("$('<link></link>').appendTo($('head')).attr({{ href: '{0}/css//jquery.mobile-{1}."& _ 
                                "min.css', type: 'text/css', rel: 'stylesheet' }});", ApplicationServices.JqmVersion), baseUrl, ApplicationServices.JqmVersion)
                Else
                    sb.AppendFormat(String.Format("$('<link></link>').appendTo($('head')).attr({{ href: '{0}/App_Themes/Norwindt/_Th"& _ 
                                "eme_Aquarium.css?{0}', type: 'text/css', rel: 'stylesheet' }});", ApplicationServices.Version), baseUrl)
                End If
                Try 
                    Dim blankPage = New StringBuilder()
                    Dim sw = New StringWriter(blankPage)
                    context.Server.Execute("~/default.aspx?_page=_blank", sw)
                    sw.Flush()
                    sw.Close()
                    Dim cultureJS = CultureJavaScriptRegex.Match(blankPage.ToString())
                    If cultureJS.Success Then
                        sb.AppendLine(cultureJS.Groups("JavaScript").Value)
                        sb.AppendLine("Sys.CultureInfo.CurrentCulture=__cultureInfo;")
                    End If
                Catch __exception As Exception
                End Try
                sb.AppendFormat("var __targetFramework='4.6';__tf=4.0;__cothost='appfactory';__appInfo='Norwintd|{"& _ 
                        "0}';", BusinessRules.JavaScriptString(context.User.Identity.Name))
                sb.AppendFormat("Sys.Application.add_init(function() {{ Web.DataView._run('{0}','{0}/Services/Data"& _ 
                        "ControllerService.asmx', {1}) }});", baseUrl, context.User.Identity.IsAuthenticated.ToString().ToLower())
            End If
            context.Response.ContentType = "application/javascript"
            ApplicationServices.CompressOutput(context, sb.ToString())
        End Sub
        
        Protected Overridable Sub AuthenticateSaaS(ByVal context As HttpContext)
            Dim request = context.Request
            Dim response = context.Response
            Dim args = request.Params("args")
            Dim result = New StringBuilder(String.Format("{0}(", request.QueryString("callback")))
            Dim resultObject As Object = false
            Dim login = JsonConvert.DeserializeObject(Of String())(args)
            resultObject = ApplicationServices.Login(CType(login(0),String), CType(login(1),String), false)
            result.Append(JsonConvert.SerializeObject(resultObject))
            result.Append(")")
            Dim jsonp = result.ToString()
            response.Write(jsonp)
        End Sub
        
        Private Function DoReplaceDateTicks(ByVal m As Match) As String
            Return String.Format("new Date({0})", m.Groups(1).Value)
        End Function
        
        Friend Overridable Function CreateXmlWriter(ByVal output As Stream) As XmlWriter
            Dim settings = New XmlWriterSettings()
            settings.CloseOutput = false
            settings.Indent = true
            Dim writer = XmlWriter.Create(output, settings)
            Return writer
        End Function
        
        Friend Overridable Sub RenderException(ByVal context As HttpContext, ByVal er As Exception, ByVal writer As XmlWriter)
            If (Not (er) Is Nothing) Then
                writer.WriteStartElement("error")
                writer.WriteElementString("message", er.Message)
                writer.WriteElementString("type", er.GetType().ToString())
                If (context.Request.UserHostName = "::1") Then
                    writer.WriteStartElement("stackTrace")
                    writer.WriteCData(er.StackTrace)
                    writer.WriteEndElement()
                    RenderException(context, er.InnerException, writer)
                End If
                writer.WriteEndElement()
            End If
        End Sub
        
        Protected Function SelectView(ByVal config As ControllerConfiguration, ByVal viewId As String) As XPathNavigator
            Return config.SelectSingleNode("/c:dataController/c:views/c:view[@id='{0}']", viewId)
        End Function
        
        Protected Function SelectDataField(ByVal config As ControllerConfiguration, ByVal viewId As String, ByVal fieldName As String) As XPathNavigator
            Return config.SelectSingleNode("/c:dataController/c:views/c:view[@id='{0}']/.//c:dataField[@fieldName='{1}' or @a"& _ 
                    "liasFieldName='{1}']", viewId, fieldName)
        End Function
        
        Protected Function SelectField(ByVal config As ControllerConfiguration, ByVal name As String) As XPathNavigator
            Return config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='{0}']", name)
        End Function
        
        Protected Function SelectActionGroup(ByVal config As ControllerConfiguration, ByVal actionGroupId As String) As XPathNavigator
            Return config.SelectSingleNode("/c:dataController/c:actions/c:actionGroup[@id='{0}']", actionGroupId)
        End Function
        
        Protected Function SelectAction(ByVal config As ControllerConfiguration, ByVal actionGroupId As String, ByVal actionId As String) As XPathNavigator
            Return config.SelectSingleNode("/c:dataController/c:actions/c:actionGroup[@id='{0}']/c:action[@id='{1}']", actionGroupId, actionId)
        End Function
        
        Private Function VerifyActionSegments(ByVal config As ControllerConfiguration, ByVal actionGroupId As String, ByVal actionId As String, ByVal keyIsAvailable As Boolean) As Boolean
            Dim result = true
            If (Not (SelectActionGroup(config, actionGroupId)) Is Nothing) Then
                Dim actionNode = SelectAction(config, actionGroupId, actionId)
                If (actionNode Is Nothing) Then
                    result = false
                Else
                    If (Not (keyIsAvailable) AndAlso ((actionNode.GetAttribute("whenKeySelected", String.Empty) = "true") OrElse Regex.IsMatch(actionNode.GetAttribute("commandName", String.Empty), "^(Update|Delete)$"))) Then
                        result = false
                    End If
                End If
            Else
                result = false
            End If
            Return result
        End Function
        
        Private Sub AnalyzeRouteValues(ByVal request As HttpRequest, ByVal response As HttpResponse, ByVal isHttpGetMethod As Boolean, ByVal config As ControllerConfiguration, ByRef view As String, ByRef key As String, ByRef fieldName As String, ByRef actionGroupId As String, ByRef actionId As String, ByRef commandName As String)
            Dim routeValues = request.RequestContext.RouteData.Values
            Dim segment1 = CType(routeValues("Segment1"),String)
            Dim segment2 = CType(routeValues("Segment2"),String)
            Dim segment3 = CType(routeValues("Segment3"),String)
            Dim segment4 = CType(routeValues("Segment4"),String)
            view = Nothing
            key = Nothing
            fieldName = Nothing
            actionGroupId = Nothing
            actionId = Nothing
            commandName = Nothing
            If Not (String.IsNullOrEmpty(segment1)) Then
                If (Not (SelectView(config, segment1)) Is Nothing) Then
                    view = segment1
                    If isHttpGetMethod Then
                        key = segment2
                        fieldName = segment3
                    Else
                        If VerifyActionSegments(config, segment2, segment3, false) Then
                            actionGroupId = segment2
                            actionId = segment3
                        Else
                            If String.IsNullOrEmpty(segment2) Then
                                If Not ((HttpMethod = "POST")) Then
                                    response.StatusCode = 404
                                End If
                            Else
                                key = segment2
                                If VerifyActionSegments(config, segment3, segment4, true) Then
                                    actionGroupId = segment3
                                    actionId = segment4
                                Else
                                    If Not (((HttpMethod = "PUT") OrElse (HttpMethod = "DELETE"))) Then
                                        response.StatusCode = 404
                                    End If
                                End If
                            End If
                        End If
                    End If
                Else
                    If isHttpGetMethod Then
                        key = segment1
                        fieldName = segment2
                    Else
                        If VerifyActionSegments(config, segment1, segment2, false) Then
                            actionGroupId = segment1
                            actionId = segment2
                        Else
                            If String.IsNullOrEmpty(segment1) Then
                                response.StatusCode = 404
                            Else
                                key = segment1
                                If VerifyActionSegments(config, segment2, segment3, true) Then
                                    actionGroupId = segment2
                                    actionId = segment3
                                Else
                                    If Not (((HttpMethod = "PUT") OrElse (HttpMethod = "DELETE"))) Then
                                        response.StatusCode = 404
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            Else
                view = request.QueryString("_view")
                key = request.QueryString("_key")
                fieldName = request.QueryString("_fieldName")
                If Not (isHttpGetMethod) Then
                    actionGroupId = request.QueryString("_actionId")
                End If
            End If
            If Not (isHttpGetMethod) Then
                Dim actionNode = SelectAction(config, actionGroupId, actionId)
                If (Not (actionNode) Is Nothing) Then
                    commandName = actionNode.GetAttribute("commandName", String.Empty)
                Else
                    commandName = HttpMethodToCommandName(request)
                End If
            End If
        End Sub
        
        Private Function HttpMethodToCommandName(ByVal request As HttpRequest) As String
            If (HttpMethod = "POST") Then
                Return "Insert"
            End If
            If (HttpMethod = "PUT") Then
                Return "Update"
            End If
            If (HttpMethod = "DELETE") Then
                Return "Delete"
            End If
            Return Nothing
        End Function
        
        Protected Overridable Function AuthorizeRequest(ByVal request As HttpRequest, ByVal config As ControllerConfiguration) As Boolean
            Return UriRestConfig.IsAuthorized(request, config)
        End Function
        
        Private Sub PerformRequest(ByVal context As HttpContext, ByVal output As Stream, ByVal json As Boolean, ByVal controllerName As String)
            Dim request = context.Request
            Dim response = context.Response
            Dim config As ControllerConfiguration = Nothing
            Try 
                config = DataControllerBase.CreateConfigurationInstance([GetType](), controllerName)
            Catch __exception As Exception
                response.StatusCode = 404
                Return
            End Try
            If Not (AuthorizeRequest(request, config)) Then
                response.StatusCode = 404
                Return
            End If
            'analyze route segments
            Dim isHttpGetMethod = (HttpMethod = "GET")
            Dim view As String = Nothing
            Dim key As String = Nothing
            Dim fieldName As String = Nothing
            Dim actionGroupId As String = Nothing
            Dim actionId As String = Nothing
            Dim commandName As String = Nothing
            AnalyzeRouteValues(request, response, isHttpGetMethod, config, view, key, fieldName, actionGroupId, actionId, commandName)
            If (response.StatusCode = 404) Then
                Return
            End If
            Dim keyIsAvailable = Not (String.IsNullOrEmpty(key))
            If String.IsNullOrEmpty(view) Then
                If isHttpGetMethod Then
                    view = Controller.GetSelectView(controllerName)
                Else
                    If (commandName = "Insert") Then
                        view = Controller.GetInsertView(controllerName)
                    Else
                        If (commandName = "Update") Then
                            view = Controller.GetUpdateView(controllerName)
                        Else
                            If (commandName = "Delete") Then
                                view = Controller.GetDeleteView(controllerName)
                            End If
                        End If
                    End If
                End If
            End If
            If (SelectView(config, view) Is Nothing) Then
                response.StatusCode = 404
                Return
            End If
            Dim dataFieldNode As XPathNavigator = Nothing
            Dim fieldNode As XPathNavigator = Nothing
            If Not (String.IsNullOrEmpty(fieldName)) Then
                dataFieldNode = SelectDataField(config, view, fieldName)
                fieldNode = SelectField(config, fieldName)
                If ((dataFieldNode Is Nothing) OrElse (fieldNode Is Nothing)) Then
                    response.StatusCode = 404
                    Return
                End If
            End If
            'create a filter
            Dim filter = New List(Of String)()
            'process key fields
            If keyIsAvailable Then
                Dim values = key.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries)
                Dim keyIterator = config.Select("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
                Dim index = 0
                Do While keyIterator.MoveNext()
                    filter.Add(String.Format("{0}:={1}", keyIterator.Current.GetAttribute("name", String.Empty), values(index)))
                    index = (index + 1)
                Loop
            End If
            'process quick find
            Dim quickFind = request.Params("_q")
            If Not (String.IsNullOrEmpty(quickFind)) Then
                filter.Add(String.Format("{0}:~{1}", config.SelectSingleNode("/c:dataController/c:views/c:view[@id='{0}']/.//c:dataField[1]/@fieldName", view).Value, quickFind))
            End If
            'process filter parameters
            If Not (keyIsAvailable) Then
                For Each filterName As String in request.Params.Keys
                    If (Not (SelectDataField(config, view, filterName)) Is Nothing) Then
                        filter.Add(String.Format("{0}:={1}", filterName, request.Params(filterName)))
                    Else
                        Dim m = BusinessRules.SqlFieldFilterOperationRegex.Match(filterName)
                        Dim filterFieldName = m.Groups("Name").Value
                        If (m.Success AndAlso (Not (SelectDataField(config, view, filterFieldName)) Is Nothing)) Then
                            Dim operation = m.Groups("Operation").Value
                            Dim filterOperation = CType(TypeDescriptor.GetConverter(GetType(RowFilterOperation)).ConvertFromString(operation),RowFilterOperation)
                            Dim filterValue = request.Params(filterName)
                            If ((filterOperation = RowFilterOperation.Includes) OrElse (filterOperation = RowFilterOperation.DoesNotInclude)) Then
                                filterValue = Regex.Replace(filterValue, ",", "$or$")
                            Else
                                If (filterOperation = RowFilterOperation.Between) Then
                                    filterValue = Regex.Replace(filterValue, ",", "$and$")
                                End If
                            End If
                            filter.Add(String.Format("{0}:{1}{2}", filterFieldName, RowFilterAttribute.ComparisonOperations(Convert.ToInt32(filterOperation)), filterValue))
                        End If
                    End If
                Next
            End If
            'execute request
            If isHttpGetMethod Then
                If (Not (fieldNode) Is Nothing) Then
                    Dim style = "o"
                    If (request.QueryString("_style") = "Thumbnail") Then
                        style = "t"
                    End If
                    Dim blobPath = String.Format("~/Blob.ashx?{0}={1}|{2}", fieldNode.GetAttribute("onDemandHandler", String.Empty), style, key)
                    context.RewritePath(blobPath)
                    Dim blobHandler = New Blob()
                    CType(blobHandler,IHttpHandler).ProcessRequest(context)
                Else
                    ExecuteHttpGetRequest(request, response, output, json, controllerName, view, filter, keyIsAvailable)
                End If
            Else
                ExecuteActionRequest(request, response, output, json, config, controllerName, view, key, filter, actionGroupId, actionId)
            End If
        End Sub
        
        Private Sub ExecuteActionRequest(ByVal request As HttpRequest, ByVal response As HttpResponse, ByVal output As Stream, ByVal json As Boolean, ByVal config As ControllerConfiguration, ByVal controllerName As String, ByVal view As String, ByVal key As String, ByVal filter As List(Of String), ByVal actionGroupId As String, ByVal actionId As String)
            Dim actionNode = SelectAction(config, actionGroupId, actionId)
            Dim commandName = HttpMethodToCommandName(request)
            Dim commandArgument = String.Empty
            Dim lastCommandName = String.Empty
            If (actionNode Is Nothing) Then
                If String.IsNullOrEmpty(commandName) Then
                    response.StatusCode = 404
                    Return
                End If
            Else
                commandName = actionNode.GetAttribute("commandName", String.Empty)
                commandArgument = actionNode.GetAttribute("commandArgument", String.Empty)
                lastCommandName = actionNode.GetAttribute("whenLastCommandName", String.Empty)
            End If
            'prepare action arguments
            Dim args = New ActionArgs()
            args.Controller = controllerName
            args.View = view
            args.CommandName = commandName
            args.CommandArgument = commandArgument
            args.LastCommandName = lastCommandName
            args.Filter = filter.ToArray()
            args.SortExpression = request.QueryString("_sortExpression")
            Dim selectedValues = request.Params("_selectedValues")
            If Not (String.IsNullOrEmpty(selectedValues)) Then
                args.SelectedValues = selectedValues.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries)
            End If
            args.Trigger = request.Params("_trigger")
            args.Path = String.Format("{0}/{1}", actionGroupId, actionId)
            Dim form = request.Form
            If (request.HttpMethod = "GET") Then
                form = request.QueryString
            End If
            Dim values = New List(Of FieldValue)()
            For Each fieldName As String in form.Keys
                Dim field = SelectField(config, fieldName)
                Dim dataField = SelectDataField(config, view, fieldName)
                If (Not (field) Is Nothing) Then
                    Dim oldValue As Object = form((fieldName + "_OldValue"))
                    Dim value As Object = form(fieldName)
                    'try parsing the values
                    Dim dataFormatString As String = Nothing
                    If (Not (dataField) Is Nothing) Then
                        dataFormatString = dataField.GetAttribute("dataFormatString", String.Empty)
                    End If
                    If String.IsNullOrEmpty(dataFormatString) Then
                        dataFormatString = field.GetAttribute("dataFormatString", String.Empty)
                    End If
                    If (Not (String.IsNullOrEmpty(dataFormatString)) AndAlso Not (dataFormatString.StartsWith("{"))) Then
                        dataFormatString = String.Format("{{0:{0}}}", dataFormatString)
                    End If
                    Dim fieldType = field.GetAttribute("type", String.Empty)
                    If NumericTypes.Contains(fieldType) Then
                        Dim d As Double
                        If [Double].TryParse(CType(value,String), NumberStyles.Any, CultureInfo.CurrentUICulture, d) Then
                            value = d
                        End If
                        If [Double].TryParse(CType(oldValue,String), NumberStyles.Any, CultureInfo.CurrentUICulture, d) Then
                            oldValue = d
                        End If
                    Else
                        If (fieldType = "DateTime") Then
                            Dim dt As DateTime
                            If Not (String.IsNullOrEmpty(dataFormatString)) Then
                                If DateTime.TryParseExact(CType(value,String), dataFormatString, CultureInfo.CurrentUICulture, DateTimeStyles.None, dt) Then
                                    value = dt
                                End If
                                If DateTime.TryParseExact(CType(oldValue,String), dataFormatString, CultureInfo.CurrentUICulture, DateTimeStyles.None, dt) Then
                                    oldValue = dt
                                End If
                            Else
                                If DateTime.TryParse(CType(value,String), dt) Then
                                    value = dt
                                End If
                                If DateTime.TryParse(CType(oldValue,String), dt) Then
                                    oldValue = dt
                                End If
                            End If
                        End If
                    End If
                    'create a field value
                    Dim fvo As FieldValue = Nothing
                    If (Not (oldValue) Is Nothing) Then
                        fvo = New FieldValue(fieldName, oldValue, value)
                    Else
                        fvo = New FieldValue(fieldName, value)
                    End If
                    'figure if the field is read-only
                    Dim isReadOnly = (field.GetAttribute("readOnly", String.Empty) = "true")
                    Dim writeRoles = field.GetAttribute("writeRoles", String.Empty)
                    If (Not (String.IsNullOrEmpty(writeRoles)) AndAlso Not (DataControllerBase.UserIsInRole(writeRoles))) Then
                        isReadOnly = true
                    End If
                    If (dataField Is Nothing) Then
                        isReadOnly = true
                    End If
                    fvo.ReadOnly = isReadOnly
                    'add field value to the list
                    values.Add(fvo)
                End If
            Next
            Dim keyIndex = 0
            Dim keyIterator = config.Select("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
            Do While keyIterator.MoveNext()
                Dim fieldName = keyIterator.Current.GetAttribute("name", String.Empty)
                For Each fvo in values
                    If (fvo.Name = fieldName) Then
                        fieldName = Nothing
                        If ((fvo.OldValue Is Nothing) AndAlso ((commandName = "Update") OrElse (commandName = "Delete"))) Then
                            fvo.OldValue = fvo.NewValue
                            fvo.Modified = false
                        End If
                        Exit For
                    End If
                Next
                If Not (String.IsNullOrEmpty(fieldName)) Then
                    Dim oldValue As String = Nothing
                    If Not (String.IsNullOrEmpty(key)) Then
                        Dim keyValues = key.Split(New Char() {Global.Microsoft.VisualBasic.ChrW(44)}, StringSplitOptions.RemoveEmptyEntries)
                        If (keyIndex < keyValues.Length) Then
                            oldValue = keyValues(keyIndex)
                        End If
                    End If
                    values.Add(New FieldValue(fieldName, oldValue, oldValue))
                End If
                keyIndex = (keyIndex + 1)
            Loop
            args.Values = values.ToArray()
            'execute action
            Dim controllerInstance = ControllerFactory.CreateDataController()
            Dim result = controllerInstance.Execute(controllerName, view, args)
            'redirect response location if success or error url has been specified
            Dim successUrl = request.Params("_successUrl")
            Dim errorUrl = request.Params("_errorUrl")
            If ((result.Errors.Count = 0) AndAlso Not (String.IsNullOrEmpty(successUrl))) Then
                response.RedirectLocation = successUrl
                response.StatusCode = 301
                Return
            End If
            If ((result.Errors.Count > 0) AndAlso Not (String.IsNullOrEmpty(errorUrl))) Then
                If errorUrl.Contains("?") Then
                    errorUrl = (errorUrl + "&")
                Else
                    errorUrl = (errorUrl + "?")
                End If
                errorUrl = String.Format("{0}_error={1}", errorUrl, HttpUtility.UrlEncode(result.Errors(0)))
                response.RedirectLocation = errorUrl
                response.StatusCode = 301
                Return
            End If
            If json Then
                Dim sw = CreateStreamWriter(request, response, output)
                BeginResponsePadding(request, sw)
                sw.Write("{{""rowsAffected"":{0}", result.RowsAffected)
                If ((Not (result.Errors) Is Nothing) AndAlso (result.Errors.Count > 0)) Then
                    sw.Write(",""errors"":[")
                    Dim first = true
                    For Each er in result.Errors
                        If first Then
                            first = false
                        Else
                            sw.Write(",")
                        End If
                        sw.Write("{{""message"":""{0}""}}", BusinessRules.JavaScriptString(er))
                    Next
                    sw.Write("]")
                End If
                If Not (String.IsNullOrEmpty(result.ClientScript)) Then
                    sw.Write(",""clientScript"":""{0}""", BusinessRules.JavaScriptString(result.ClientScript))
                End If
                If Not (String.IsNullOrEmpty(result.NavigateUrl)) Then
                    sw.Write(",""navigateUrl"":""{0}""", BusinessRules.JavaScriptString(result.NavigateUrl))
                End If
                If (Not (result.Values) Is Nothing) Then
                    For Each fvo in result.Values
                        sw.Write(",""{0}"":", fvo.Name)
                        WriteJSONValue(sw, fvo.Value, Nothing)
                    Next
                End If
                sw.Write("}")
                EndResponsePadding(request, sw)
                sw.Close()
            Else
                Dim writer = CreateXmlWriter(output)
                writer.WriteStartDocument()
                writer.WriteStartElement("result")
                writer.WriteAttributeString("rowsAffected", result.RowsAffected.ToString())
                If ((Not (result.Errors) Is Nothing) AndAlso (result.Errors.Count > 0)) Then
                    writer.WriteStartElement("errors")
                    For Each er in result.Errors
                        writer.WriteStartElement("error")
                        writer.WriteAttributeString("message", er)
                        writer.WriteEndElement()
                    Next
                    writer.WriteEndElement()
                End If
                If Not (String.IsNullOrEmpty(result.ClientScript)) Then
                    writer.WriteAttributeString("clientScript", result.ClientScript)
                End If
                If Not (String.IsNullOrEmpty(result.NavigateUrl)) Then
                    writer.WriteAttributeString("navigateUrl", result.NavigateUrl)
                End If
                If (Not (result.Values) Is Nothing) Then
                    For Each fvo in result.Values
                        writer.WriteElementString(fvo.Name, Convert.ToString(fvo.Value))
                    Next
                End If
                writer.WriteEndElement()
                writer.WriteEndDocument()
                writer.Close()
            End If
        End Sub
        
        Protected Overridable Sub WriteJSONValue(ByVal writer As StreamWriter, ByVal v As Object, ByVal field As DataField)
            Dim dataFormatString As String = Nothing
            If (Not (field) Is Nothing) Then
                dataFormatString = field.DataFormatString
            End If
            If (v Is Nothing) Then
                writer.Write("null")
            Else
                If TypeOf v Is String Then
                    writer.Write("""{0}""", BusinessRules.JavaScriptString(CType(v,String)))
                Else
                    If TypeOf v Is DateTime Then
                        writer.Write("""{0}""", ConvertDateToJSON(CType(v,DateTime), dataFormatString))
                    Else
                        If TypeOf v Is Guid Then
                            writer.Write("""{0}""", BusinessRules.JavaScriptString(v.ToString()))
                        Else
                            If TypeOf v Is Boolean Then
                                writer.Write(v.ToString().ToLower())
                            Else
                                If Not (String.IsNullOrEmpty(dataFormatString)) Then
                                    writer.Write("""{0}""", ConvertValueToJSON(v, dataFormatString))
                                Else
                                    writer.Write(ConvertValueToJSON(v, Nothing))
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End Sub
        
        Protected Overridable Sub ExecuteHttpGetRequest(ByVal request As HttpRequest, ByVal response As HttpResponse, ByVal output As Stream, ByVal json As Boolean, ByVal controllerName As String, ByVal view As String, ByVal filter As List(Of String), ByVal keyIsAvailable As Boolean)
            'prepare a page request
            Dim pageSize As Integer
            Integer.TryParse(request.QueryString("_pageSize"), pageSize)
            If (pageSize = 0) Then
                pageSize = 100
            End If
            Dim pageIndex As Integer
            Integer.TryParse(request.QueryString("_pageIndex"), pageIndex)
            Dim r = New PageRequest()
            r.Controller = controllerName
            r.View = view
            r.RequiresMetaData = true
            r.PageSize = pageSize
            r.PageIndex = pageIndex
            r.Filter = filter.ToArray()
            r.RequiresRowCount = ((pageIndex = 0) AndAlso Not (keyIsAvailable))
            r.SortExpression = request.QueryString("_sortExpression")
            'request the data
            Dim controllerInstance = ControllerFactory.CreateDataController()
            Dim page = controllerInstance.GetPage(r.Controller, r.View, r)
            If (keyIsAvailable AndAlso (page.Rows.Count = 0)) Then
                response.StatusCode = 404
                Return
            End If
            'stream out the data
            Dim writer As XmlWriter = Nothing
            Dim sw As StreamWriter = Nothing
            If json Then
                sw = CreateStreamWriter(request, response, output)
                BeginResponsePadding(request, sw)
                If Not (keyIsAvailable) Then
                    sw.Write("{")
                    If r.RequiresRowCount Then
                        sw.Write("""totalRowCount"":{0},", page.TotalRowCount)
                    End If
                    sw.Write("""pageSize"":{0},""pageIndex"":{1},""rowCount"":{2},", page.PageSize, page.PageIndex, page.Rows.Count)
                    sw.Write("""{0}"":[", controllerName)
                End If
            Else
                writer = CreateXmlWriter(output)
                writer.WriteStartDocument()
                writer.WriteStartElement(controllerName)
                If r.RequiresRowCount Then
                    writer.WriteAttributeString("totalRowCount", page.TotalRowCount.ToString())
                End If
                If Not (keyIsAvailable) Then
                    writer.WriteAttributeString("pageSize", page.PageSize.ToString())
                    writer.WriteAttributeString("pageIndex", page.PageIndex.ToString())
                    writer.WriteAttributeString("rowCount", page.Rows.Count.ToString())
                    writer.WriteStartElement("items")
                End If
            End If
            Dim firstRow = true
            For Each field in page.Fields
                If (Not (String.IsNullOrEmpty(field.DataFormatString)) AndAlso Not (field.DataFormatString.StartsWith("{"))) Then
                    field.DataFormatString = String.Format("{{0:{0}}}", field.DataFormatString)
                End If
            Next
            For Each row in page.Rows
                Dim index = 0
                If json Then
                    If firstRow Then
                        firstRow = false
                    Else
                        sw.Write(",")
                    End If
                    sw.Write("{")
                Else
                    If Not (keyIsAvailable) Then
                        writer.WriteStartElement("item")
                    End If
                End If
                Dim firstField = true
                For Each field in page.Fields
                    If json Then
                        If firstField Then
                            firstField = false
                        Else
                            sw.Write(",")
                        End If
                        sw.Write("""{0}"":", field.Name)
                        WriteJSONValue(sw, row(index), field)
                    Else
                        Dim v = row(index)
                        If (Not (v) Is Nothing) Then
                            Dim s As String = Nothing
                            If Not (String.IsNullOrEmpty(field.DataFormatString)) Then
                                s = String.Format(field.DataFormatString, v)
                            Else
                                s = Convert.ToString(v)
                            End If
                            writer.WriteAttributeString(field.Name, s)
                        End If
                    End If
                    index = (index + 1)
                Next
                If json Then
                    sw.Write("}")
                Else
                    If Not (keyIsAvailable) Then
                        writer.WriteEndElement()
                    End If
                End If
                If keyIsAvailable Then
                    Exit For
                End If
            Next
            If json Then
                If Not (keyIsAvailable) Then
                    sw.Write("]}")
                End If
                EndResponsePadding(request, sw)
                sw.Close()
            Else
                If Not (keyIsAvailable) Then
                    writer.WriteEndElement()
                End If
                writer.WriteEndElement()
                writer.WriteEndDocument()
                writer.Close()
            End If
        End Sub
        
        Protected Overridable Function ConvertValueToJSON(ByVal v As Object, ByVal dataFormatString As String) As String
            If String.IsNullOrEmpty(dataFormatString) Then
                Return v.ToString()
            Else
                Return String.Format(dataFormatString, v)
            End If
        End Function
        
        Protected Overridable Function ConvertDateToJSON(ByVal dt As DateTime, ByVal dataFormatString As String) As String
            dt = dt.ToUniversalTime()
            If String.IsNullOrEmpty(dataFormatString) Then
                Return dt.ToString("F")
            Else
                Return String.Format(dataFormatString, dt)
            End If
        End Function
        
        Protected Overridable Sub BeginResponsePadding(ByVal request As HttpRequest, ByVal sw As StreamWriter)
            Dim callback = request.QueryString("callback")
            If Not (String.IsNullOrEmpty(callback)) Then
                sw.Write("{0}(", callback)
            Else
                If ((request.HttpMethod = "GET") AndAlso UriRestConfig.IsJSONPRequest(request)) Then
                    Dim instance = request.QueryString("_instance")
                    If String.IsNullOrEmpty(instance) Then
                        instance = CType(request.RequestContext.RouteData.Values("Controller"),String)
                    End If
                    sw.Write("Norwindt=typeof Norwindt=='undefined'?{{}}:Norwindt;Norwindt.{0}=", instance)
                End If
            End If
        End Sub
        
        Protected Overridable Sub EndResponsePadding(ByVal request As HttpRequest, ByVal sw As StreamWriter)
            Dim callback = request.QueryString("callback")
            If Not (String.IsNullOrEmpty(callback)) Then
                sw.Write(")")
            Else
                If ((request.HttpMethod = "GET") AndAlso UriRestConfig.IsJSONPRequest(request)) Then
                    sw.Write(";")
                End If
            End If
        End Sub
        
        Protected Overridable Function CreateStreamWriter(ByVal request As HttpRequest, ByVal response As HttpResponse, ByVal output As Stream) As StreamWriter
            Dim acceptEncoding = request.Headers("Accept-Encoding")
            If Not (String.IsNullOrEmpty(acceptEncoding)) Then
                Dim encodings = acceptEncoding.Split(Global.Microsoft.VisualBasic.ChrW(44))
                If encodings.Contains("gzip") Then
                    output = New GZipStream(output, CompressionMode.Compress)
                    response.AppendHeader("Content-Encoding", "gzip")
                Else
                    If encodings.Contains("deflate") Then
                        output = New DeflateStream(output, CompressionMode.Compress)
                        response.AppendHeader("Content-Encoding", "deflate")
                    End If
                End If
            End If
            Return New StreamWriter(output)
        End Function
    End Class
End Namespace
