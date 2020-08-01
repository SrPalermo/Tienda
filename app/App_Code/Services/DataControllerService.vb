Imports Newtonsoft.Json.Linq
Imports Norwindt.Data
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Web
Imports System.Web.Script.Services
Imports System.Web.Services

Namespace Norwindt.Services
    
    Public Class DataControllerService
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Protected ReadOnly Property Permalinks() As List(Of String())
            Get
                Dim links = CType(HttpContext.Current.Session("Permalinks"),List(Of String()))
                If (links Is Nothing) Then
                    links = New List(Of String())()
                    HttpContext.Current.Session("Permalinks") = links
                End If
                Return links
            End Get
        End Property
        
        Public Function GetPage(ByVal controller As String, ByVal view As String, ByVal request As PageRequest) As ViewPage
            Return ControllerFactory.CreateDataController().GetPage(controller, view, request)
        End Function
        
        Public Function ExecuteList(ByVal requests() As ActionArgs) As ActionResult()
            Return CType(ControllerFactory.CreateDataController(),DataControllerBase).ExecuteList(requests)
        End Function
        
        Public Function GetPageList(ByVal requests() As PageRequest) As ViewPage()
            Return CType(ControllerFactory.CreateDataController(),DataControllerBase).GetPageList(requests)
        End Function
        
        Public Function GetListOfValues(ByVal controller As String, ByVal view As String, ByVal request As DistinctValueRequest) As Object()
            Return ControllerFactory.CreateDataController().GetListOfValues(controller, view, request)
        End Function
        
        Public Function Execute(ByVal controller As String, ByVal view As String, ByVal args As ActionArgs) As ActionResult
            Return ControllerFactory.CreateDataController().Execute(controller, view, args)
        End Function
        
        Public Function GetCompletionList(ByVal prefixText As String, ByVal count As Integer, ByVal contextKey As String) As String()
            Return ControllerFactory.CreateAutoCompleteManager().GetCompletionList(prefixText, count, contextKey)
        End Function
        
        Protected Function FindPermalink(ByVal link As String) As String()
            For Each entry in Permalinks
                If (entry(0) = link) Then
                    Return entry
                End If
            Next
            Return Nothing
        End Function
        
        Public Sub SavePermalink(ByVal link As String, ByVal html As String)
            Dim permalink = FindPermalink(link)
            If Permalinks.Contains(permalink) Then
                Permalinks.Remove(permalink)
            End If
            If Not (String.IsNullOrEmpty(html)) Then
                Permalinks.Insert(0, New String() {link, html})
            Else
                If (Permalinks.Count > 0) Then
                    Permalinks.RemoveAt(0)
                End If
            End If
            Do While (Permalinks.Count > 10)
                Permalinks.RemoveAt((Permalinks.Count - 1))
            Loop
        End Sub
        
        Public Function EncodePermalink(ByVal link As String, ByVal rooted As Boolean) As String
            Dim request = HttpContext.Current.Request
            If rooted Then
                Dim appPath = request.ApplicationPath
                If appPath.Equals("/") Then
                    appPath = String.Empty
                End If
                Return String.Format("{0}://{1}{2}/default.aspx?_link={3}", request.Url.Scheme, request.Url.Authority, appPath, HttpUtility.UrlEncode(StringEncryptor.ToString(link)))
            Else
                Dim linkSegments = link.Split(Global.Microsoft.VisualBasic.ChrW(63))
                Dim arguments = String.Empty
                If (linkSegments.Length > 1) Then
                    arguments = linkSegments(1)
                End If
                Return String.Format("{0}?_link={1}", linkSegments(0), HttpUtility.UrlEncode(StringEncryptor.FromString(arguments)))
            End If
        End Function
        
        Public Function ListAllPermalinks() As String()()
            Return Permalinks.ToArray()
        End Function
        
        Public Function GetSurvey(ByVal survey As String) As String
            Return ControllerFactory.GetSurvey(survey)
        End Function
        
        Public Function Login(ByVal username As String, ByVal password As String, ByVal createPersistentCookie As Boolean) As Object
            Return ApplicationServices.Login(username, password, createPersistentCookie)
        End Function
        
        Public Sub Logout()
            ApplicationServices.Logout()
        End Sub
        
        Public Function Roles() As String()
            Return ApplicationServices.Roles()
        End Function
        
        Public Function Themes() As Object
            Return ApplicationServices.Themes()
        End Function
        
        Public Overridable Function Invoke(ByVal method As String, ByVal args As JObject) As Object
            Dim handler As ServiceRequestHandler = Nothing
            Dim result As Object = Nothing
            If ApplicationServices.RequestHandlers.TryGetValue(method.ToLower(), handler) Then
                result = handler.HandleRequest(Me, args)
            End If
            Return result
        End Function
    End Class
End Namespace
