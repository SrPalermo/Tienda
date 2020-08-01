Imports Newtonsoft.Json
Imports Norwindt.Data
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Handlers
    
    Partial Public Class Export
        Inherits ExportBase
    End Class
    
    Public Class ExportBase
        Inherits GenericHandlerBase
        Implements IHttpHandler, System.Web.SessionState.IRequiresSessionState
        
        ReadOnly Property IHttpHandler_IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return true
            End Get
        End Property
        
        Sub IHttpHandler_ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            Dim fileName As String = Nothing
            Dim q = context.Request.Params("q")
            If Not (String.IsNullOrEmpty(q)) Then
                If q.Contains("{") Then
                    q = Convert.ToBase64String(Encoding.Default.GetBytes(q))
                    context.Response.Redirect(((context.Request.AppRelativeCurrentExecutionFilePath + "?q=")  _
                                    + ((HttpUtility.UrlEncode(q) + "&t=")  _
                                    + context.Request.Params("t"))))
                End If
                q = Encoding.Default.GetString(Convert.FromBase64String(q))
                Dim args = JsonConvert.DeserializeObject(Of ActionArgs)(q)
                'execute data export
                Dim controller = ControllerFactory.CreateDataController()
                'create an Excel Web Query
                If ((args.CommandName = "ExportRowset") AndAlso Not (context.Request.Url.AbsoluteUri.Contains("&d"))) Then
                    Dim webQueryUrl = ToClientUrl((context.Request.Url.AbsoluteUri + "&d=true"))
                    context.Response.Write(("Web" & ControlChars.CrLf &"1" & ControlChars.CrLf  + webQueryUrl))
                    context.Response.ContentType = "text/x-ms-iqy"
                    context.Response.AddHeader("Content-Disposition", String.Format(String.Format("attachment; filename={0}", GenerateOutputFileName(args, String.Format("{0}_{1}.iqy", args.Controller, args.View)))))
                    Return
                End If
                'export data in the requested format
                Dim result = controller.Execute(args.Controller, args.View, args)
                fileName = CType(result.Values(0).Value,String)
                'send file to output
                If File.Exists(fileName) Then
                    If (args.CommandName = "ExportCsv") Then
                        context.Response.ContentType = "text/csv"
                        context.Response.AddHeader("Content-Disposition", String.Format(String.Format("attachment; filename={0}", GenerateOutputFileName(args, String.Format("{0}_{1}.csv", args.Controller, args.View)))))
                        context.Response.Charset = "utf-8"
                        context.Response.Write(Convert.ToChar(65279))
                    Else
                        If (args.CommandName = "ExportRowset") Then
                            context.Response.ContentType = "text/xml"
                        Else
                            context.Response.ContentType = "application/rss+xml"
                        End If
                    End If
                    Dim reader = File.OpenText(fileName)
                    Do While Not (reader.EndOfStream)
                        Dim s = reader.ReadLine()
                        context.Response.Output.WriteLine(s)
                    Loop
                    reader.Close()
                    File.Delete(fileName)
                End If
            End If
            If String.IsNullOrEmpty(fileName) Then
                Throw New HttpException(404, String.Empty)
            End If
        End Sub
        
        Protected Overridable Function ToClientUrl(ByVal url As String) As String
            Return url
        End Function
    End Class
End Namespace
