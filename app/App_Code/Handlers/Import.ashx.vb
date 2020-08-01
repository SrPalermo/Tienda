Imports Norwindt.Data
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.UI

Namespace Norwindt.Handlers
    
    Public Class Import
        Inherits GenericHandlerBase
        Implements IHttpHandler, System.Web.SessionState.IRequiresSessionState
        
        ReadOnly Property IHttpHandler_IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return false
            End Get
        End Property
        
        Sub IHttpHandler_ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            Dim parentId = context.Request.Params("parentId")
            Dim controller = context.Request.Params("controller")
            Dim view = context.Request.Params("view")
            If (String.IsNullOrEmpty(parentId) OrElse (String.IsNullOrEmpty(controller) OrElse String.IsNullOrEmpty(view))) Then
                Throw New HttpException(404, String.Empty)
            End If
            Dim methodName As String = Nothing
            Dim data As String = Nothing
            Dim errors = New StringBuilder()
            If (context.Request.HttpMethod = "GET") Then
                methodName = "_initImportUpload"
            Else
                If ((context.Request.HttpMethod = "POST") AndAlso (context.Request.Files.Count > 0)) Then
                    methodName = "_finishImportUpload"
                    Dim tempFileName As String = Nothing
                    Try 
                        'save file to the temporary folder
                        Dim fileName = context.Request.Files(0).FileName
                        Dim extension = Path.GetExtension(fileName).ToLower()
                        tempFileName = Path.Combine(ImportProcessor.SharedTempPath, (Guid.NewGuid().ToString() + extension))
                        context.Request.Files(0).SaveAs(tempFileName)
                        'return response to the client
                        Dim ip = ImportProcessorFactory.Create(tempFileName)
                        Dim numberOfRecords = ip.CountRecords(tempFileName)
                        Dim availableImportFields = ip.CreateListOfAvailableFields(controller, view)
                        Dim fieldMap = ip.CreateInitialFieldMap(tempFileName, controller, view)
                        data = String.Format("" & ControlChars.CrLf &"                        " & ControlChars.CrLf &"<form>" & ControlChars.CrLf &"<input id=""NumberOfRecords"" type=""hidden"" val"& _ 
                                "ue=""{0}""/>" & ControlChars.CrLf &"<input id=""AvailableImportFields"" type=""hidden"" value=""{1}""/>" & ControlChars.CrLf &"<inpu"& _ 
                                "t id=""FieldMap"" type=""hidden"" value=""{2}""/><input id=""FileName"" type=""hidden"" va"& _ 
                                "lue=""{3}""/>" & ControlChars.CrLf &"</form>", numberOfRecords, HttpUtility.HtmlAttributeEncode(availableImportFields), HttpUtility.HtmlAttributeEncode(fieldMap), Path.GetFileName(tempFileName))
                    Catch [error] As Exception
                        Do While (Not ([error]) Is Nothing)
                            errors.AppendLine([error].Message)
                            [error] = [error].InnerException
                        Loop
                        data = String.Format("<form><input type=""hidden"" id=""Errors"" value=""{0}""/>", HttpUtility.HtmlAttributeEncode(errors.ToString()))
                        Try 
                            If File.Exists(tempFileName) Then
                                File.Delete(tempFileName)
                            End If
                        Finally
                            'release resources here
                        End Try
                    End Try
                Else
                    Throw New HttpException(404, String.Empty)
                End If
            End If
            'format response and send it to the client
            Dim responseTemplate = "" & ControlChars.CrLf &"          " & ControlChars.CrLf &"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""htt"& _ 
                "p://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">" & ControlChars.CrLf &"<html xmlns=""http://www."& _ 
                "w3.org/1999/xhtml""><head></head><body onload=""if (parent && parent.window.$find)"& _ 
                "parent.window.$find('{0}').{1}(document)"">{2}</body></html>"
            context.Response.ContentType = "text/html"
            context.Response.Write(String.Format(responseTemplate, parentId, methodName, data))
        End Sub
    End Class
End Namespace
