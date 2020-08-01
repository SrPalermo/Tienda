Imports Newtonsoft.Json.Linq
Imports Norwindt.Data
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Routing
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Services
    
    Partial Public Class EnterpriseApplicationServices
        Inherits EnterpriseApplicationServicesBase
    End Class
    
    Public Class EnterpriseApplicationServicesBase
        Inherits ApplicationServicesBase
        
        Public Shared AppServicesRegex As Regex = New Regex("/appservices/(?'Controller'\w+?)(/|$)", RegexOptions.IgnoreCase)
        
        Public Shared DynamicResourceRegex As Regex = New Regex("(\.js$|^_(invoke|authenticate)$)", RegexOptions.IgnoreCase)
        
        Public Shared DynamicWebResourceRegex As Regex = New Regex("\.(js|css)$", RegexOptions.IgnoreCase)
        
        Public Overrides Sub RegisterServices()
            RegisterREST()
            MyBase.RegisterServices()
            ServicePointManager.SecurityProtocol = (ServicePointManager.SecurityProtocol Or SecurityProtocolType.Tls12)
        End Sub
        
        Public Overridable Sub RegisterREST()
            Dim routes = RouteTable.Routes
            routes.RouteExistingFiles = true
            GenericRoute.Map(routes, New RepresentationalStateTransfer(), "appservices/{Controller}/{Segment1}/{Segment2}/{Segment3}/{Segment4}")
            GenericRoute.Map(routes, New RepresentationalStateTransfer(), "appservices/{Controller}/{Segment1}/{Segment2}/{Segment3}")
            GenericRoute.Map(routes, New RepresentationalStateTransfer(), "appservices/{Controller}/{Segment1}/{Segment2}")
            GenericRoute.Map(routes, New RepresentationalStateTransfer(), "appservices/{Controller}/{Segment1}")
            GenericRoute.Map(routes, New RepresentationalStateTransfer(), "appservices/{Controller}")
        End Sub
        
        Public Overrides Function RequiresAuthentication(ByVal request As HttpRequest) As Boolean
            Dim result = MyBase.RequiresAuthentication(request)
            If result Then
                Return true
            End If
            Dim m = AppServicesRegex.Match(request.Path)
            If m.Success Then
                Dim config As ControllerConfiguration = Nothing
                Try 
                    Dim controllerName = m.Groups("Controller").Value
                    If ((controllerName = "_authenticate") OrElse (controllerName = "saas")) Then
                        Return false
                    End If
                    If Not (DynamicResourceRegex.IsMatch(controllerName)) Then
                        config = DataControllerBase.CreateConfigurationInstance([GetType](), controllerName)
                    End If
                Catch __exception As Exception
                End Try
                If (config Is Nothing) Then
                    Return Not (DynamicWebResourceRegex.IsMatch(request.Path))
                End If
                Return RequiresRESTAuthentication(request, config)
            End If
            Return false
        End Function
        
        Public Overridable Function RequiresRESTAuthentication(ByVal request As HttpRequest, ByVal config As ControllerConfiguration) As Boolean
            Return UriRestConfig.RequiresAuthentication(request, config)
        End Function
    End Class
    
    Public Class ScheduleStatus
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Schedule As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Exceptions As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Success As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_NextTestDate As DateTime
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Expired As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Precision As String
        
        ''' The definition of the schedule.
        Public Overridable Property Schedule() As String
            Get
                Return m_Schedule
            End Get
            Set
                m_Schedule = value
            End Set
        End Property
        
        ''' The defintion of excepetions to the schedule. Exceptions are expressed as another schedule.
        Public Overridable Property Exceptions() As String
            Get
                Return m_Exceptions
            End Get
            Set
                m_Exceptions = value
            End Set
        End Property
        
        ''' True if the schedule is valid at this time.
        Public Overridable Property Success() As Boolean
            Get
                Return m_Success
            End Get
            Set
                m_Success = value
            End Set
        End Property
        
        ''' The next date and time when the schedule is invalid.
        Public Overridable Property NextTestDate() As DateTime
            Get
                Return m_NextTestDate
            End Get
            Set
                m_NextTestDate = value
            End Set
        End Property
        
        ''' True if the schedule has expired. For internal use only.
        Public Overridable Property Expired() As Boolean
            Get
                Return m_Expired
            End Get
            Set
                m_Expired = value
            End Set
        End Property
        
        ''' The precision of the schedule. For internal use only.
        Public Overridable Property Precision() As String
            Get
                Return m_Precision
            End Get
            Set
                m_Precision = value
            End Set
        End Property
    End Class
    
    Partial Public Class Scheduler
        Inherits SchedulerBase
    End Class
    
    Public Class SchedulerBase
        
        Public Shared NodeMatchRegex As Regex = New Regex("(?'Depth'\++)\s*(?'NodeType'\S+)\s*(?'Properties'[^\+]*)")
        
        Public Shared PropertyMatchRegex As Regex = New Regex("\s*(?'Name'[a-zA-Z]*)\s*[:=]?\s*(?'Value'.+?)(\n|;|$)")
        
        Private Shared m_NodeTypes() As String = New String() {"yearly", "monthly", "weekly", "daily", "once"}
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_TestDate As DateTime
        
        Public Overridable Property TestDate() As DateTime
            Get
                Return m_TestDate
            End Get
            Set
                m_TestDate = value
            End Set
        End Property
        
        Public Overridable ReadOnly Property UsePreciseProbe() As Boolean
            Get
                Return false
            End Get
        End Property
        
        ''' Check if a free form text schedule is valid now.
        Public Overloads Shared Function Test(ByVal schedule As String) As ScheduleStatus
            Return Test(schedule, Nothing, DateTime.Now)
        End Function
        
        ''' Check if a free form text schedule is valid on the testDate.
        Public Overloads Shared Function Test(ByVal schedule As String, ByVal testDate As DateTime) As ScheduleStatus
            Return Test(schedule, Nothing, testDate)
        End Function
        
        ''' Check if a free form text schedule with exceptions is valid now.
        Public Overloads Shared Function Test(ByVal schedule As String, ByVal exceptions As String) As ScheduleStatus
            Return Test(schedule, exceptions, DateTime.Now)
        End Function
        
        ''' Check if a free form text schedule with exceptions is valid on the testDate.
        Public Overloads Shared Function Test(ByVal schedule As String, ByVal exceptions As String, ByVal testDate As DateTime) As ScheduleStatus
            Dim s = New Scheduler()
            s.TestDate = testDate
            Dim status = s.CheckSchedule(schedule, exceptions)
            status.Schedule = schedule
            status.Exceptions = exceptions
            Return status
        End Function
        
        Public Overloads Overridable Function CheckSchedule(ByVal schedule As String) As ScheduleStatus
            Return CheckSchedule(StringToXml(schedule), Nothing)
        End Function
        
        Public Overloads Overridable Function CheckSchedule(ByVal schedule As String, ByVal exceptions As String) As ScheduleStatus
            Return CheckSchedule(StringToXml(schedule), StringToXml(exceptions))
        End Function
        
        ''' Check an XML schedule.
        Public Overloads Overridable Function CheckSchedule(ByVal schedule As Stream) As ScheduleStatus
            Return CheckSchedule(schedule, Nothing)
        End Function
        
        ''' Check an XML schedule with exceptions.
        Public Overloads Overridable Function CheckSchedule(ByVal schedule As Stream, ByVal exceptions As Stream) As ScheduleStatus
            Dim sched = New ScheduleStatus()
            sched.Precision = String.Empty
            Dim xSched = New ScheduleStatus()
            xSched.Precision = String.Empty
            Dim nav As XPathNavigator = Nothing
            Dim xNav As XPathNavigator = Nothing
            If ((schedule Is Nothing) OrElse schedule.Equals(Stream.Null)) Then
                sched.Success = true
            Else
                Dim doc = New XPathDocument(schedule)
                nav = doc.CreateNavigator()
                If (Not (nav.MoveToChild(XPathNodeType.Element)) OrElse Not ((nav.Name = "schedule"))) Then
                    sched.Success = true
                Else
                    CheckNode(nav, DateTime.Now, sched)
                End If
            End If
            If ((Not (exceptions) Is Nothing) AndAlso Not (exceptions.Equals(Stream.Null))) Then
                Dim xDoc = New XPathDocument(exceptions)
                xNav = xDoc.CreateNavigator()
                If (Not (xNav.MoveToChild(XPathNodeType.Element)) OrElse Not ((xNav.Name = "schedule"))) Then
                    xSched.Success = true
                Else
                    CheckNode(xNav, DateTime.Now, xSched)
                End If
            End If
            If xSched.Success Then
                sched.Success = false
            End If
            If UsePreciseProbe Then
                sched = ProbeScheduleExact(nav, xNav, sched, xSched)
            Else
                sched = ProbeSchedule(nav, xNav, sched, xSched)
            End If
            Return sched
        End Function
        
        ''' Converts plain text schedule format into XML stream.
        Private Function StringToXml(ByVal text As String) As Stream
            If String.IsNullOrEmpty(text) Then
                Return Nothing
            End If
            'check for shorthand "start"
            Dim testDate = DateTime.Now
            If DateTime.TryParse(text, testDate) Then
                String.Format("+once start: {0}", text)
            End If
            'compose XML document
            Dim doc = New XmlDocument()
            Dim dec = doc.CreateXmlDeclaration("1.0", Nothing, Nothing)
            doc.AppendChild(dec)
            Dim schedule = doc.CreateNode(XmlNodeType.Element, "schedule", Nothing)
            doc.AppendChild(schedule)
            'configure nodes
            Dim nodes = NodeMatchRegex.Matches(text)
            Dim lastNode = schedule
            Dim lastDepth = 0
            For Each node As Match in nodes
                Dim nodeType = node.Groups("NodeType").Value
                Dim depth = node.Groups("Depth").Value.Length
                Dim properties = node.Groups("Properties").Value
                If m_NodeTypes.Contains(nodeType) Then
                    Dim newNode = doc.CreateNode(XmlNodeType.Element, nodeType, Nothing)
                    Dim propertyMatches = PropertyMatchRegex.Matches(node.Groups("Properties").Value)
                    'populate attributes
                    For Each [property] As Match in propertyMatches
                        Dim name = [property].Groups("Name").Value.Trim()
                        Dim val = [property].Groups("Value").Value.Trim()
                        'group value
                        If String.IsNullOrEmpty(name) Then
                            name = "value"
                        End If
                        Dim attr = doc.CreateAttribute(name)
                        attr.Value = val
                        newNode.Attributes.Append(attr)
                    Next
                    'insert node
                    If (depth > lastDepth) Then
                        lastNode.AppendChild(newNode)
                    Else
                        If (depth < lastDepth) Then
                            Do While (Not ((lastNode.Name = "schedule")) AndAlso Not ((lastNode.Name = nodeType)))
                                lastNode = lastNode.ParentNode
                            Loop
                            If (lastNode.Name = nodeType) Then
                                lastNode = lastNode.ParentNode
                            End If
                            lastNode.AppendChild(newNode)
                        Else
                            lastNode.ParentNode.AppendChild(newNode)
                        End If
                    End If
                    lastNode = newNode
                    lastDepth = depth
                End If
            Next
            'save and return
            Dim stream = New MemoryStream()
            doc.Save(stream)
            stream.Position = 0
            Return stream
        End Function
        
        ''' Checks the current navigator if the current nodes define an active schedule. An empty schedule will set Match to true.
        Private Function CheckNode(ByVal nav As XPathNavigator, ByVal checkDate As DateTime, ByRef sched As ScheduleStatus) As Boolean
            If (nav Is Nothing) Then
                Return false
            End If
            sched.Precision = nav.Name
            If Not (nav.MoveToFirstChild()) Then
                'no schedule limitation
                sched.Success = true
                Return true
            End If
            Do While true
                'ignore comments
                If Not (nav.NodeType.Equals(XPathNodeType.Comment)) Then
                    Dim name = nav.Name
                    If (name = "once") Then
                        If CheckInterval(nav, checkDate) Then
                            sched.Success = true
                        End If
                    Else
                        If CheckInterval(nav, checkDate) Then
                            Dim value = nav.GetAttribute("value", String.Empty)
                            Dim every = nav.GetAttribute("every", String.Empty)
                            Dim check = 0
                            If (name = "yearly") Then
                                check = checkDate.Year
                            Else
                                If (name = "monthly") Then
                                    check = checkDate.Month
                                Else
                                    If (name = "weekly") Then
                                        check = GetWeekOfMonth(checkDate)
                                    Else
                                        If (name = "daily") Then
                                            check = CType(checkDate.DayOfWeek,Integer)
                                        End If
                                    End If
                                End If
                            End If
                            If CheckNumberInterval(value, check, every) Then
                                CheckNode(nav, checkDate, sched)
                            End If
                        End If
                    End If
                    'found a match
                    If (sched.Expired OrElse sched.Success) Then
                        Exit Do
                    End If
                End If
                'no more nodes
                If Not (nav.MoveToNext()) Then
                    Exit Do
                End If
            Loop
            Return sched.Success
        End Function
        
        ''' Checks to see if a series of comma-separated numbers and/or dash-separated intervals contain a specific number
        Private Function CheckNumberInterval(ByVal interval As String, ByVal number As Integer, ByVal every As String) As Boolean
            If String.IsNullOrEmpty(interval) Then
                Return true
            End If
            'process numbers and number ranges
            Dim strings = interval.Split(Global.Microsoft.VisualBasic.ChrW(44))
            Dim numbers = New List(Of Integer)()
            For Each s in strings
                If s.Contains(Global.Microsoft.VisualBasic.ChrW(45)) Then
                    Dim intervalString = s.Split(Global.Microsoft.VisualBasic.ChrW(45))
                    Dim interval1 = Convert.ToInt32(intervalString(0))
                    Dim interval2 = Convert.ToInt32(intervalString(1))
                    Dim i = interval1
                    Do While (i <= interval2)
                        numbers.Add(i)
                        i = (i + 1)
                    Loop
                Else
                    If Not (String.IsNullOrEmpty(s)) Then
                        numbers.Add(Convert.ToInt32(s))
                    End If
                End If
            Next
            numbers.Sort()
            'check if "every" used
            Dim everyNum = 1
            If Not (String.IsNullOrEmpty(every)) Then
                everyNum = Convert.ToInt32(every)
            End If
            If (everyNum > 1) Then
                'if "every" is greater than available numbers
                If (everyNum >= numbers.Count) Then
                    Return numbers.First().Equals(number)
                End If
                Dim allNumbers = New List(Of Integer)(numbers)
                numbers.Clear()
                Dim i = 0
                Do While (i <= (allNumbers.Count / everyNum))
                    numbers.Add(allNumbers.ElementAt((i * everyNum)))
                    i = (i + 1)
                Loop
            End If
            Return numbers.Contains(number)
        End Function
        
        ''' Checks to see if the current node's start and end attributes are valid.
        Private Function CheckInterval(ByVal nav As XPathNavigator, ByVal checkDate As DateTime) As Boolean
            Dim startDate = checkDate
            Dim endDate = checkDate
            If Not (DateTime.TryParse(nav.GetAttribute("start", String.Empty), startDate)) Then
                startDate = StartOfDay(TestDate)
            End If
            If Not (DateTime.TryParse(nav.GetAttribute("end", String.Empty), endDate)) Then
                endDate = DateTime.MaxValue
            End If
            If Not (((startDate <= checkDate) AndAlso (checkDate <= endDate))) Then
                Return false
            End If
            Return true
        End Function
        
        Private Function ProbeSchedule(ByVal document As XPathNavigator, ByVal exceptionsDocument As XPathNavigator, ByVal schedule As ScheduleStatus, ByVal exceptionsSchedule As ScheduleStatus) As ScheduleStatus
            Dim testSched = New ScheduleStatus()
            Dim testExceptionSched = New ScheduleStatus()
            Dim nextDate = DateTime.Now
            Dim initialState = schedule.Success
            Dim probeCount = 0
            Do While (probeCount <= 30)
                nextDate = nextDate.AddSeconds(1)
                'reset variables
                testSched.Success = false
                testSched.Expired = false
                document.MoveToRoot()
                document.MoveToFirstChild()
                If (Not (exceptionsDocument) Is Nothing) Then
                    exceptionsDocument.MoveToRoot()
                    exceptionsDocument.MoveToFirstChild()
                    testExceptionSched.Success = false
                    testExceptionSched.Expired = false
                End If
                Dim valid = (CheckNode(document, nextDate, testSched) AndAlso ((exceptionsDocument Is Nothing) OrElse Not (CheckNode(exceptionsDocument, nextDate, testExceptionSched))))
                If Not ((valid = initialState)) Then
                    Return schedule
                End If
                schedule.NextTestDate = nextDate
                probeCount = (probeCount + 1)
            Loop
            Return schedule
        End Function
        
        Private Function ProbeScheduleExact(ByVal document As XPathNavigator, ByVal exceptionsDocument As XPathNavigator, ByVal schedule As ScheduleStatus, ByVal exceptionsSchedule As ScheduleStatus) As ScheduleStatus
            Dim testSched = New ScheduleStatus()
            Dim testExceptionSched = New ScheduleStatus()
            Dim sign = 1
            Dim nextDate = DateTime.Now
            Dim initialState = schedule.Success
            Dim jump = 0
            If (schedule.Precision.Equals("daily") OrElse exceptionsSchedule.Precision.Equals("daily")) Then
                jump = (6 * 60)
            Else
                If (schedule.Precision.Equals("weekly") OrElse exceptionsSchedule.Precision.Equals("weekly")) Then
                    jump = (72 * 60)
                Else
                    If (schedule.Precision.Equals("monthly") OrElse exceptionsSchedule.Precision.Equals("monthly")) Then
                        jump = (360 * 60)
                    Else
                        If (schedule.Precision.Equals("yearly") OrElse exceptionsSchedule.Precision.Equals("yearly")) Then
                            jump = ((720 * 6)  _
                                        * 60)
                        Else
                            jump = (6 * 60)
                        End If
                    End If
                End If
            End If
            Dim probeCount = 1
            Do While (probeCount <= 20)
                'reset variables
                testSched.Success = false
                testSched.Expired = false
                document.MoveToRoot()
                document.MoveToFirstChild()
                If (Not (exceptionsDocument) Is Nothing) Then
                    exceptionsDocument.MoveToRoot()
                    exceptionsDocument.MoveToFirstChild()
                    testExceptionSched.Success = false
                    testExceptionSched.Expired = false
                End If
                'set next date to check
                nextDate = nextDate.AddMinutes((jump * sign))
                Dim valid = (CheckNode(document, nextDate, testSched) AndAlso ((exceptionsDocument Is Nothing) OrElse Not (CheckNode(exceptionsDocument, nextDate, testExceptionSched))))
                If (valid = initialState) Then
                    sign = 1
                Else
                    sign = -1
                End If
                'keep moving forward and expand jump if no border found, otherwise narrow jump
                If (sign = -1) Then
                    jump = (jump / 2)
                Else
                    jump = (jump * 2)
                    probeCount = (probeCount - 1)
                End If
                If (jump < 5) Then
                    jump = (jump + 1)
                End If
                'no border found
                If (nextDate > DateTime.Now.AddYears(5)) Then
                    Exit Do
                End If
                probeCount = (probeCount + 1)
            Loop
            schedule.NextTestDate = nextDate.AddMinutes((jump * -1))
            Return schedule
        End Function
        
        Private Function GetWeekOfMonth(ByVal [date] As DateTime) As Integer
            Dim beginningOfMonth = New DateTime([date].Year, [date].Month, 1)
            Do While Not (([date].Date.AddDays(1).DayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek))
                [date] = [date].AddDays(1)
            Loop
            Return (CType((CType([date].Subtract(beginningOfMonth).TotalDays,Double) / 7),Integer) + 1)
        End Function
        
        Private Function StartOfDay(ByVal [date] As DateTime) As DateTime
            Return New DateTime([date].Year, [date].Month, [date].Day, 0, 0, 0, 0)
        End Function
        
        Private Function EndOfDay(ByVal [date] As DateTime) As DateTime
            Return New DateTime([date].Year, [date].Month, [date].Day, 23, 59, 59, 999)
        End Function
    End Class
    
    Partial Public Class AutoFillGeocode
        Inherits AutoFillGeocodeBase
    End Class
    
    Public Class AutoFillGeocodeBase
        Inherits AutoFillAddress
        
        Protected Overrides Function CreateRequestUrl(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim pb = New List(Of String)()
            'latitude
            Dim lat = CType(autofill("latitude"),String)
            If Not (String.IsNullOrEmpty(lat)) Then
                pb.Add(lat)
            End If
            'longitude
            Dim lng = CType(autofill("longitude"),String)
            If Not (String.IsNullOrEmpty(lng)) Then
                pb.Add(lng)
            End If
            Return String.Format("https://maps.googleapis.com/maps/api/geocode/json?latlng={0}&key={1}", HttpUtility.UrlEncode(String.Join(",", pb.ToArray()).Replace(" ", "+")), ApplicationServicesBase.Settings("server.geocoding.google.key"))
        End Function
    End Class
    
    Partial Public Class AutoFillAddress
        Inherits AutoFillAddressBase
        
        Shared Sub New()
            Formats("address1_AU") = "{street_number} {route} {when /^\\d/ in subpremise then #}{subpremise}"
            Formats("address1_CA") = "{street_number} {route} {when /^\\d/ in subpremise then #}{subpremise}"
            Formats("address1_DE") = "{route}{when /./ in street_number then  }{street_number}{when /./ in subpremise t"& _ 
                "hen , }{when /./ in subpremise then subpremise}"
            Formats("address1_GB") = "{street_number} {route} {when /^\\d/ in subpremise then #}{subpremise}"
            Formats("address1_US") = "{street_number} {route} {when /^\d/ in subpremise then #}{subpremise}"
            Formats("address1") = "{route}{when /./ in street_number then , }{street_number}{when /./ in subpremise "& _ 
                "then , }{when /./ in subpremise then subpremise}"
            Formats("city") = "{postal_town,sublocality,neighborhood,locality}"
            Formats("postalcode") = "{postal_code}{when /./ in postal_code_suffix then -}{when /./ in postal_code_suff"& _ 
                "ix then postal_code_suffix}"
            Formats("region_ES") = "{administrative_area_level_1_long,administrative_area_level_2_long}"
            Formats("region_IT") = "{administrative_area_level_2,administrative_area_level_1}"
            Formats("region") = "{administrative_area_level_1,administrative_area_level_2}"
            Formats("country") = "{country_long}"
        End Sub
    End Class
    
    Public Class AutoFillAddressBase
        Inherits AutoFill
        
        Public Shared Formats As SortedDictionary(Of String, String) = New SortedDictionary(Of String, String)()
        
        Protected Overridable Function CreateRequestUrl(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim components = New List(Of String)()
            Dim pb = New List(Of String)()
            'address 1
            Dim addr1 = CType(autofill("address1"),String)
            If Not (String.IsNullOrEmpty(addr1)) Then
                pb.Add(addr1)
            End If
            'city
            Dim city = CType(autofill("city"),String)
            If Not (String.IsNullOrEmpty(city)) Then
                pb.Add(city)
            End If
            'region
            Dim region = CType(autofill("region"),String)
            If Not (String.IsNullOrEmpty(region)) Then
                pb.Add(region)
            End If
            'postalcode
            Dim postalCode = CType(autofill("postalcode"),String)
            If String.IsNullOrEmpty(postalCode) Then
                postalCode = CType(autofill.GetValue("componentpostalcode", StringComparison.OrdinalIgnoreCase),String)
            End If
            If Not (String.IsNullOrEmpty(postalCode)) Then
                components.Add(("postal_code:" + postalCode))
            End If
            'country
            Dim country = CType(autofill("country"),String)
            If Not (String.IsNullOrEmpty(country)) Then
                If ((country.Length > 2) AndAlso (Not (String.IsNullOrEmpty(postalCode)) OrElse Not (String.IsNullOrEmpty(region)))) Then
                    Dim allCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                    For Each ci in allCultures
                        Dim ri = New RegionInfo(ci.LCID)
                        If (ri.EnglishName.Equals(country, StringComparison.CurrentCultureIgnoreCase) OrElse ri.NativeName.Equals(country, StringComparison.CurrentCultureIgnoreCase)) Then
                            country = ri.TwoLetterISORegionName
                            Exit For
                        End If
                    Next
                End If
                If (country.Length = 2) Then
                    components.Add(("country:" + country))
                Else
                    pb.Add(country)
                End If
            End If
            Dim requestUrl = String.Format("https://maps.googleapis.com/maps/api/geocode/json?address={0}&key={1}", HttpUtility.UrlEncode(String.Join(",", pb.ToArray()).Replace(" ", "+")), ApplicationServicesBase.Settings("server.geocoding.google.key"))
            If (components.Count > 0) Then
                requestUrl = String.Format("{0}&components={1}", requestUrl, HttpUtility.UrlEncode(String.Join("|", components.ToArray()).Replace(" ", "+")))
            End If
            Return requestUrl
        End Function
        
        Protected Overrides Function Supports(ByVal autofill As JObject) As Boolean
            Dim enabled = ApplicationServicesBase.Settings("server.geocoding.google.address")
            Return ((enabled Is Nothing) OrElse CType(enabled,Boolean))
        End Function
        
        Protected Overrides Function Process(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As JToken
            Dim requestUrl = CreateRequestUrl(rules, autofill)
            Dim outputAddressList = New JArray()
            Using client = New WebClient()
                client.Headers("Accept-Language") = Language()
                Dim addressJson = JObject.Parse(Encoding.UTF8.GetString(client.DownloadData(requestUrl)))
                Dim addressList = CType(addressJson("results"),JArray)
                For Each address As JToken in addressList
                    Dim componentList = address("address_components")
                    Dim addressComponents = New JObject()
                    For Each component in componentList
                        Dim types = CType(component("types").ToObject(GetType(String())),String())
                        Dim shortName = CType(component("short_name"),String)
                        Dim longName = CType(component("long_name"),String)
                        For Each componentType in types
                            If Not ((componentType = "political")) Then
                                addressComponents(componentType) = shortName
                                addressComponents((componentType + "_long")) = longName
                            End If
                        Next
                    Next
                    Dim normalizedAddressComponents = New JObject()
                    For Each p in addressComponents
                        normalizedAddressComponents(p.Key.Replace("_", String.Empty)) = p.Value
                    Next
                    outputAddressList.Add(New JObject(New JProperty("name", address("formatted_address")), New JProperty("address1", Format("address1", addressComponents)), New JProperty("address2", Format("address2", addressComponents, CType(autofill("address2"),String))), New JProperty("address3", Format("address3", addressComponents, CType(autofill("address3"),String))), New JProperty("city", Format("city", addressComponents)), New JProperty("region", Format("region", addressComponents)), New JProperty("postalcode", Format("postalcode", addressComponents)), New JProperty("country", Format("country", addressComponents)), New JProperty("type", address("types")(0)), New JProperty("latitude", address("geometry")("location")("lat")), New JProperty("longitude", address("geometry")("location")("lng")), New JProperty("components", normalizedAddressComponents), New JProperty("rawAddress", address)))
                Next
            End Using
            Try 
                ConfirmResult(rules, outputAddressList)
            Catch __exception As Exception
                'do nothing
            End Try
            Return outputAddressList
        End Function
        
        Protected Overridable Function ToComponentValue(ByVal components As JObject, ByVal expression As String) As String
            Dim m = Regex.Match(expression, "\b(\w+?)\b")
            Do While m.Success
                Dim v = CType(components(m.Groups(1).Value),String)
                If (Not (v) Is Nothing) Then
                    Return v
                End If
                m = m.NextMatch()
            Loop
            Return Nothing
        End Function
        
        Protected Overloads Overridable Function Format(ByVal type As String, ByVal components As JObject) As String
            Return Format(type, components, String.Empty)
        End Function
        
        Protected Overloads Overridable Function Format(ByVal type As String, ByVal components As JObject, ByVal defaultValue As String) As String
            Dim s = String.Empty
            Dim country = CType(components("country"),String)
            If Not (Formats.TryGetValue((type  _
                            + ("_" + country)), s)) Then
                If Not (Formats.TryGetValue(type, s)) Then
                    Return defaultValue
                End If
            End If
            Do While true
                Dim m = Regex.Match(s, "\{(.+?)\}")
                If m.Success Then
                    Dim name = m.Groups(1).Value
                    Dim v As String = Nothing
                    Dim ift = Regex.Match(name, "^when\s\/(?'RegEx'.+?)/\sin\s(?'Component'.+?)\s+then\s(?'Result'.+)$")
                    If ift.Success Then
                        Dim componentValue = ToComponentValue(components, ift.Groups("Component").Value)
                        If (Not (componentValue) Is Nothing) Then
                            Dim test = New Regex(ift.Groups("RegEx").Value, RegexOptions.IgnoreCase)
                            If test.IsMatch(componentValue) Then
                                v = ToComponentValue(components, ift.Groups("Result").Value)
                                If (v Is Nothing) Then
                                    v = ift.Groups("Result").Value
                                End If
                            End If
                        End If
                    Else
                        v = ToComponentValue(components, name)
                    End If
                    If (v Is Nothing) Then
                        v = String.Empty
                    End If
                    s = (s.Substring(0, m.Index)  _
                                + (v + s.Substring((m.Index + m.Length))))
                Else
                    Exit Do
                End If
            Loop
            Return s.Trim()
        End Function
        
        Protected Overridable Sub ConfirmResult(ByVal rules As BusinessRulesBase, ByVal addresses As JArray)
            For Each address As JToken in addresses
                If (CType(address("components")("country"),String) = "US") Then
                    'try enhancing address by verifying it with USPS
                    Dim serialNo = CType(ApplicationServicesBase.Settings("server.geocoding.usps.serialNo"),String)
                    Dim userName = CType(ApplicationServicesBase.Settings("server.geocoding.usps.userName"),String)
                    Dim password = CType(ApplicationServicesBase.Settings("server.geocoding.usps.password"),String)
                    Dim address1 = CType(address("address1"),String)
                    If (Not (String.IsNullOrEmpty(userName)) AndAlso Not (String.IsNullOrEmpty(address1))) Then
                        Dim uspsRequest = New StringBuilder("<VERIFYADDRESS><COMMAND>ZIP1</COMMAND>")
                        uspsRequest.AppendFormat("<SERIALNO>{0}</SERIALNO>", serialNo)
                        uspsRequest.AppendFormat("<USER>{0}</USER>", userName)
                        uspsRequest.AppendFormat("<PASSWORD>{0}</PASSWORD>", password)
                        uspsRequest.Append("<ADDRESS0></ADDRESS0>")
                        uspsRequest.AppendFormat("<ADDRESS1>{0}</ADDRESS1>", address1)
                        uspsRequest.AppendFormat("<ADDRESS2>{0}</ADDRESS2>", address("address2"))
                        uspsRequest.AppendFormat("<ADDRESS3>{0},{1},{2}</ADDRESS3>", address("city"), address("region"), address("postalcode"))
                        uspsRequest.Append("</VERIFYADDRESS>")
                        Using client = New WebClient()
                            Dim uspsResponseText = client.DownloadString(("http://www.dial-a-zip.com/XML-Dial-A-ZIP/DAZService.asmx/MethodZIPValidate?input="& _ 
                                    "" + HttpUtility.UrlEncode(uspsRequest.ToString())))
                            Dim uspsResponse = New XPathDocument(New StringReader(uspsResponseText)).CreateNavigator().SelectSingleNode("/Dial-A-ZIP_Response")
                            If (Not (uspsResponse) Is Nothing) Then
                                address("address1") = uspsResponse.SelectSingleNode("AddrLine1").Value
                                address("address2") = uspsResponse.SelectSingleNode("AddrLine2").Value
                                address("city") = uspsResponse.SelectSingleNode("City").Value
                                address("region") = uspsResponse.SelectSingleNode("State").Value
                                address("postalcode") = (uspsResponse.SelectSingleNode("ZIP5").Value  _
                                            + ("-" + uspsResponse.SelectSingleNode("Plus4").Value))
                                address("components")("postalcode") = uspsResponse.SelectSingleNode("ZIP5").Value
                                address("components")("postalcodesuffix") = uspsResponse.SelectSingleNode("Plus4").Value
                                address("country") = address("country").ToString().ToUpper()
                            End If
                        End Using
                    End If
                End If
            Next
        End Sub
    End Class
    
    Public Class AutoFill
        
        Public Shared Handlers As SortedDictionary(Of String, AutoFill) = New SortedDictionary(Of String, AutoFill)()
        
        Shared Sub New()
            Handlers("address") = New AutoFillAddress()
            Handlers("geocode") = New AutoFillGeocode()
            Handlers("map") = New AutoFillMap()
        End Sub
        
        Public Shared Sub Evaluate(ByVal rules As BusinessRulesBase)
            Dim args = rules.Arguments
            If ((args.CommandName = "AutoFill") AndAlso Not (String.IsNullOrEmpty(rules.View))) Then
                Dim autofill = JObject.Parse(args.Trigger)
                Dim handler As AutoFill = Nothing
                If (Handlers.TryGetValue(CType(autofill("autofill"),String), handler) AndAlso handler.Supports(autofill)) Then
                    Dim result = handler.Process(rules, autofill)
                    rules.Result.Values.Add(New FieldValue("AutoFill", result.ToString()))
                End If
            End If
        End Sub
        
        Protected Overridable Function Process(ByVal rules As BusinessRulesBase, ByVal autoFill As JObject) As JToken
            Return Nothing
        End Function
        
        Protected Overridable Function Supports(ByVal autofill As JObject) As Boolean
            Return true
        End Function
        
        Protected Overridable Function Language() As String
            Return CultureInfo.CurrentUICulture.Name
        End Function
    End Class
    
    Partial Public Class AutoFillMap
        Inherits AutoFillMapBase
    End Class
    
    Public Class AutoFillMapBase
        Inherits AutoFill
        
        Protected Overridable Function ToSize(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim width = 0
            If (Not (autofill.Property("width")) Is Nothing) Then
                width = CType(autofill("width"),Integer)
            End If
            If (width < 180) Then
                width = 180
            End If
            Dim height = 0
            If (Not (autofill.Property("height")) Is Nothing) Then
                height = CType(autofill("height"),Integer)
            End If
            If (height < 180) Then
                height = 180
            End If
            Return String.Format("{0}x{1}", width, height)
        End Function
        
        Protected Overridable Function ToMapType(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim mapType = CType(autofill("mapType"),String)
            If String.IsNullOrEmpty(mapType) Then
                mapType = "roadmap"
            End If
            Return mapType
        End Function
        
        Protected Overridable Function ToScale(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim scale = CType(autofill("scale"),String)
            If String.IsNullOrEmpty(scale) Then
                scale = "1"
            End If
            Return scale
        End Function
        
        Protected Overridable Function ToZoom(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim zoom = CType(autofill("zoom"),String)
            If String.IsNullOrEmpty(zoom) Then
                zoom = "16"
            End If
            Return zoom
        End Function
        
        Protected Overridable Function ToMarkerSize(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim size = CType(autofill("markerSize"),String)
            If String.IsNullOrEmpty(size) Then
                size = "mid"
            End If
            Return size
        End Function
        
        Protected Overridable Function ToMarkerColor(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            Dim color = CType(autofill("markerColor"),String)
            If String.IsNullOrEmpty(color) Then
                color = "red"
            End If
            Return color
        End Function
        
        Protected Overridable Function ToMarkerList(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            'try lat & lng
            Dim lat = CType(autofill("latitude"),String)
            Dim lng = CType(autofill("longitude"),String)
            If (Not (String.IsNullOrEmpty(lat)) AndAlso Not (String.IsNullOrEmpty(lng))) Then
                Return String.Format("{0},{1}", lat, lng)
            Else
                Dim mb = New List(Of String)()
                'address 1
                Dim addr1 = CType(autofill("address1"),String)
                If Not (String.IsNullOrEmpty(addr1)) Then
                    mb.Add(addr1)
                End If
                'city
                Dim city = CType(autofill("city"),String)
                If Not (String.IsNullOrEmpty(city)) Then
                    mb.Add(city)
                End If
                'region
                Dim region = CType(autofill("region"),String)
                If Not (String.IsNullOrEmpty(region)) Then
                    mb.Add(region)
                End If
                'postalcode
                Dim postalCode = CType(autofill("postalcode"),String)
                If String.IsNullOrEmpty(postalCode) Then
                    postalCode = CType(autofill.GetValue("componentpostalcode", StringComparison.OrdinalIgnoreCase),String)
                End If
                If Not (String.IsNullOrEmpty(postalCode)) Then
                    mb.Add(postalCode)
                End If
                'country
                Dim country = CType(autofill("country"),String)
                If Not (String.IsNullOrEmpty(country)) Then
                    mb.Add(country)
                End If
                Return String.Join(",", mb.ToArray()).Replace(" ", "+")
            End If
        End Function
        
        Protected Overridable Function ToMarkers(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            'size:mid|color:red|San Francisco,CA|Oakland,CA|San Jose,CA
            Return HttpUtility.UrlEncode(String.Format("size:{0}|color:{1}|{2}", ToMarkerSize(rules, autofill), ToMarkerColor(rules, autofill), ToMarkerList(rules, autofill)))
        End Function
        
        Protected Overridable Function CreateRequestUrl(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As String
            'size=512x512&maptype=roadma&markers=size:mid|color:red|San Francisco,CA|Oakland,CA|San Jose,CA&key=737dk343kjfld83lkjfdlk
            Return String.Format("https://maps.googleapis.com/maps/api/staticmap?size={0}&scale={1}&maptype={2}&zoo"& _ 
                    "m={3}&markers={4}&key={5}", ToSize(rules, autofill), ToScale(rules, autofill), ToMapType(rules, autofill), ToZoom(rules, autofill), ToMarkers(rules, autofill), ApplicationServicesBase.Settings("server.geocoding.google.key"))
        End Function
        
        Protected Overrides Function Supports(ByVal autofill As JObject) As Boolean
            Dim enabled = ApplicationServicesBase.Settings("server.geocoding.google.map")
            Return ((enabled Is Nothing) OrElse CType(enabled,Boolean))
        End Function
        
        Protected Overrides Function Process(ByVal rules As BusinessRulesBase, ByVal autofill As JObject) As JToken
            Dim requestUrl = CreateRequestUrl(rules, autofill)
            Dim result = New JObject()
            Using client = New WebClient()
                client.Headers("Accept-Language") = Language()
                Dim data = Convert.ToBase64String(client.DownloadData(requestUrl))
                Dim contentType = client.ResponseHeaders(HttpResponseHeader.ContentType)
                If contentType.StartsWith("image") Then
                    result("image") = data
                    result("contentType") = contentType
                End If
            End Using
            Return result
        End Function
    End Class
End Namespace
