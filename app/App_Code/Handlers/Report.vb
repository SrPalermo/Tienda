Imports Newtonsoft.Json
Imports Norwindt.Data
Imports Norwindt.Services
Imports Norwindt.Web
Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web

Namespace Norwindt.Handlers
    
    Public Class ReportData
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Data() As Byte
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_MimeType As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FileNameExtension As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Encoding As String
        
        Public Sub New(ByVal data() As Byte, ByVal mimeType As String, ByVal fileNameExtension As String, ByVal encoding As String)
            MyBase.New
            Me.Data = data
            Me.MimeType = mimeType
            Me.FileNameExtension = fileNameExtension
            Me.Encoding = encoding
        End Sub
        
        Public Property Data() As Byte()
            Get
                Return m_Data
            End Get
            Set
                m_Data = value
            End Set
        End Property
        
        Public Property MimeType() As String
            Get
                Return m_MimeType
            End Get
            Set
                m_MimeType = value
            End Set
        End Property
        
        Public Property FileNameExtension() As String
            Get
                Return m_FileNameExtension
            End Get
            Set
                m_FileNameExtension = value
            End Set
        End Property
        
        Public Property Encoding() As String
            Get
                Return m_Encoding
            End Get
            Set
                m_Encoding = value
            End Set
        End Property
    End Class
    
    ''' <summary>
    ''' A collection of parameters controlling the process or report generation.
    ''' </summary>
    Public Class ReportArgs
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Controller As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_View As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_TemplateName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Format As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FilterDetails As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SortExpression As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Filter() As FieldFilter
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_MimeType As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FileNameExtension As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Encoding As String
        
        Public Sub New()
            MyBase.New
            View = "grid1"
        End Sub
        
        ''' <summary>
        ''' The name of the data controller
        ''' </summary>
        Public Property Controller() As String
            Get
                Return m_Controller
            End Get
            Set
                m_Controller = value
            End Set
        End Property
        
        ''' <summary>
        ''' The ID of the view. Optional.
        ''' </summary>
        Public Property View() As String
            Get
                Return m_View
            End Get
            Set
                m_View = value
            End Set
        End Property
        
        ''' <summary>
        ''' The name of a custom RDLC template. Optional.
        ''' </summary>
        Public Property TemplateName() As String
            Get
                Return m_TemplateName
            End Get
            Set
                m_TemplateName = value
            End Set
        End Property
        
        ''' <summary>
        ''' Report output format. Supported values are Pdf, Word, Excel, and Tiff. The default value is Pdf. Optional.
        ''' </summary>
        Public Property Format() As String
            Get
                Return m_Format
            End Get
            Set
                m_Format = value
            End Set
        End Property
        
        ''' <summary>
        ''' Specifies a user-friendly description of the filter. The description is displayed on the automatically produced reports below the report header. Optional.
        ''' </summary>
        Public Property FilterDetails() As String
            Get
                Return m_FilterDetails
            End Get
            Set
                m_FilterDetails = value
            End Set
        End Property
        
        ''' <summary>
        ''' Sort expression that must be applied to the dataset prior to the report generation. Optional.
        ''' </summary>
        Public Property SortExpression() As String
            Get
                Return m_SortExpression
            End Get
            Set
                m_SortExpression = value
            End Set
        End Property
        
        ''' <summary>
        ''' A filter expression that must be applied to the dataset prior to the report generation. Optional.
        ''' </summary>
        Public Property Filter() As FieldFilter()
            Get
                Return m_Filter
            End Get
            Set
                m_Filter = value
            End Set
        End Property
        
        ''' <summary>
        ''' Specifies the MIME type of the report produced by Report.Execute() method.
        ''' </summary>
        Public Property MimeType() As String
            Get
                Return m_MimeType
            End Get
            Set
                m_MimeType = value
            End Set
        End Property
        
        ''' <summary>
        ''' Specifies the file name extension of the report produced by Report.Execute() method.
        ''' </summary>
        Public Property FileNameExtension() As String
            Get
                Return m_FileNameExtension
            End Get
            Set
                m_FileNameExtension = value
            End Set
        End Property
        
        ''' <summary>
        ''' Specifies the encoding of the report produced by Report.Execute() method.
        ''' </summary>
        Public Property Encoding() As String
            Get
                Return m_Encoding
            End Get
            Set
                m_Encoding = value
            End Set
        End Property
    End Class
    
    Public Class ReportBase
        Inherits GenericHandlerBase
        Implements IHttpHandler, System.Web.SessionState.IRequiresSessionState
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Arguments As ReportArgs
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Request As PageRequest
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_OutputStream As Stream
        
        Private Shared m_ValidationKeyRegex As Regex = New Regex("/Blob.ashx\?")
        
        Protected Property Arguments() As ReportArgs
            Get
                Return m_Arguments
            End Get
            Set
                m_Arguments = value
            End Set
        End Property
        
        Protected Property Request() As PageRequest
            Get
                Return m_Request
            End Get
            Set
                m_Request = value
            End Set
        End Property
        
        Protected Property OutputStream() As Stream
            Get
                Return m_OutputStream
            End Get
            Set
                m_OutputStream = value
            End Set
        End Property
        
        ReadOnly Property IHttpHandler_IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return false
            End Get
        End Property
        
        Sub IHttpHandler_ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            Dim c = context.Request("c")
            Dim q = context.Request("q")
            Dim request = Me.Request
            If ((request Is Nothing) AndAlso (String.IsNullOrEmpty(c) OrElse String.IsNullOrEmpty(q))) Then
                Throw New Exception("Invalid report request.")
            End If
            'create a data table for report
            Dim templateName As String = Nothing
            Dim aa As String = Nothing
            Dim reportFormat As String = Nothing
            If (request Is Nothing) Then
                request = JsonConvert.DeserializeObject(Of PageRequest)(q)
                templateName = context.Request.Form("a")
                aa = context.Request("aa")
            Else
                templateName = Me.Arguments.TemplateName
                reportFormat = Me.Arguments.Format
                request.FilterDetails = Me.Arguments.FilterDetails
            End If
            request.PageIndex = 0
            request.PageSize = Int32.MaxValue
            request.RequiresMetaData = true
            'try to generate a report via a business rule
            Dim args As ActionArgs = Nothing
            If Not (String.IsNullOrEmpty(aa)) Then
                args = JsonConvert.DeserializeObject(Of ActionArgs)(aa)
                Dim controller = ControllerFactory.CreateDataController()
                Dim result = controller.Execute(args.Controller, args.View, args)
                If Not (String.IsNullOrEmpty(result.NavigateUrl)) Then
                    AppendDownloadTokenCookie()
                    context.Response.Redirect(result.NavigateUrl)
                End If
                If result.Canceled Then
                    AppendDownloadTokenCookie()
                    Return
                End If
                result.RaiseExceptionIfErrors()
                'parse action data
                Dim actionData = New SortedDictionary(Of String, String)()
                CType(controller,DataControllerBase).Config.ParseActionData(args.Path, actionData)
                Dim filter = New List(Of String)()
                For Each name in actionData.Keys
                    Dim v = actionData(name)
                    If name.StartsWith("_") Then
                        If (name = "_controller") Then
                            request.Controller = v
                        End If
                        If (name = "_view") Then
                            request.View = v
                        End If
                        If (name = "_sortExpression") Then
                            request.SortExpression = v
                        End If
                        If (name = "_count") Then
                            request.PageSize = Convert.ToInt32(v)
                        End If
                        If (name = "_template") Then
                            templateName = v
                        End If
                    Else
                        If (v = "@Arguments_SelectedValues") Then
                            If (args.SelectedValues.Length > 0) Then
                                Dim sb = New StringBuilder()
                                For Each key in args.SelectedValues
                                    If (sb.Length > 0) Then
                                        sb.Append("$or$")
                                    End If
                                    sb.Append(key)
                                Next
                                filter.Add(String.Format("{0}:$in${1}", name, sb.ToString()))
                            Else
                                Return
                            End If
                        Else
                            If Regex.IsMatch(v, "^('|"").+('|"")$") Then
                                filter.Add(String.Format("{0}:={1}", name, v.Substring(1, (v.Length - 2))))
                            Else
                                If (Not (args.Values) Is Nothing) Then
                                    For Each fvo in args.Values
                                        If (fvo.Name = v) Then
                                            filter.Add(String.Format("{0}:={1}", name, fvo.Value))
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                    request.Filter = filter.ToArray()
                Next
            End If
            'load report definition
            Dim reportTemplate = Controller.CreateReportInstance(Nothing, templateName, request.Controller, request.View)
            Dim page = ControllerFactory.CreateDataController().GetPage(request.Controller, request.View, request)
            Dim table = page.ToDataTable()
            'insert validation key
            reportTemplate = m_ValidationKeyRegex.Replace(reportTemplate, String.Format("/Blob.ashx?_validationKey={0}&amp;", BlobAdapter.ValidationKey))
            'figure report output format
            If (Me.Arguments Is Nothing) Then
                Dim m = Regex.Match(c, "^(ReportAs|Report)(Pdf|Excel|Image|Word|)$")
                reportFormat = m.Groups(2).Value
            End If
            If String.IsNullOrEmpty(reportFormat) Then
                reportFormat = "Pdf"
            End If
            'render a report
            Dim report = Render(request, table, reportTemplate, reportFormat)
            If (Not (Me.Arguments) Is Nothing) Then
                Me.Arguments.MimeType = report.MimeType
                Me.Arguments.FileNameExtension = report.FileNameExtension
                Me.Arguments.Encoding = report.Encoding
                Me.OutputStream.Write(report.Data, 0, report.Data.Length)
            Else
                'send report data to the client
                context.Response.Clear()
                context.Response.ContentType = report.MimeType
                context.Response.AddHeader("Content-Length", report.Data.Length.ToString())
                AppendDownloadTokenCookie()
                Dim fileName = FormatFileName(context, request, report.FileNameExtension)
                If String.IsNullOrEmpty(fileName) Then
                    fileName = String.Format("{0}_{1}.{2}", request.Controller, request.View, report.FileNameExtension)
                    If (Not (args) Is Nothing) Then
                        fileName = GenerateOutputFileName(args, fileName)
                    End If
                End If
                context.Response.AddHeader("Content-Disposition", String.Format("attachment;filename={0}", fileName))
                context.Response.OutputStream.Write(report.Data, 0, report.Data.Length)
            End If
        End Sub
        
        Protected Overridable Function Render(ByVal request As PageRequest, ByVal table As DataTable, ByVal reportTemplate As String, ByVal reportFormat As String) As ReportData
            Return Nothing
        End Function
        
        Protected Overridable Function FormatFileName(ByVal context As HttpContext, ByVal request As PageRequest, ByVal extension As String) As String
            Return Nothing
        End Function
        
        ''' <summary>
        ''' Generates a report using the default or custom report template with optional sort expression and filter applied to the dataset.
        ''' </summary>
        ''' <param name="args">A collection of parameters that control the report generation.</param>
        ''' <returns>A binary array representing the report data.</returns>
        Public Shared Function Execute(ByVal args As ReportArgs) As Byte()
            Dim reportHandler = CType(ApplicationServices.CreateInstance("Norwindt.Handlers.Report"),ReportBase)
            Dim output = New MemoryStream()
            reportHandler.OutputStream = output
            reportHandler.Arguments = args
            Dim request = New PageRequest()
            reportHandler.Request = request
            request.Controller = args.Controller
            request.View = args.View
            request.SortExpression = args.SortExpression
            If (Not (args.Filter) Is Nothing) Then
                Dim dve = New DataViewExtender()
                dve.AssignStartupFilter(args.Filter)
                request.Filter = CType(dve.Properties("StartupFilter"),List(Of String)).ToArray()
            End If
            CType(reportHandler,IHttpHandler).ProcessRequest(HttpContext.Current)
            'return report data
            output.Position = 0
            Dim data((output.Length) - 1) As Byte
            output.Read(data, 0, data.Length)
            Return data
        End Function
    End Class
End Namespace
