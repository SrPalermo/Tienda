Imports Microsoft.Reporting.WebForms
Imports Norwindt.Data
Imports System
Imports System.Data
Imports System.IO
Imports System.Web

Namespace Norwindt.Handlers
    
    Partial Public Class Report
        Inherits ReportBase
        
        Protected Overrides Function Render(ByVal request As PageRequest, ByVal table As DataTable, ByVal reportTemplate As String, ByVal reportFormat As String) As ReportData
            Dim context = HttpContext.Current
            Dim q = context.Request("q")
            'render a report using Microsoft Report Viewer
            Dim mimeType As String = Nothing
            Dim encoding As String = Nothing
            Dim fileNameExtension As String = Nothing
            Dim streams() As String = Nothing
            Dim warnings() As Warning = Nothing
            Dim data() As Byte = Nothing
            Using report = New LocalReport()
                report.EnableHyperlinks = true
                report.EnableExternalImages = true
                report.LoadReportDefinition(New StringReader(reportTemplate))
                report.DataSources.Add(New ReportDataSource(request.Controller, table))
                report.EnableExternalImages = true
                For Each p in report.GetParameters()
                    If (p.Name.Equals("FilterDetails") AndAlso Not (String.IsNullOrEmpty(request.FilterDetails))) Then
                        report.SetParameters(New ReportParameter("FilterDetails", request.FilterDetails))
                    End If
                    If p.Name.Equals("BaseUrl") Then
                        Dim baseUrl = String.Format("{0}://{1}{2}", context.Request.Url.Scheme, context.Request.Url.Authority, context.Request.ApplicationPath.TrimEnd(Global.Microsoft.VisualBasic.ChrW(47)))
                        report.SetParameters(New ReportParameter("BaseUrl", baseUrl))
                    End If
                    If (p.Name.Equals("Query") AndAlso Not (String.IsNullOrEmpty(q))) Then
                        report.SetParameters(New ReportParameter("Query", HttpUtility.UrlEncode(q)))
                    End If
                Next
                report.SetBasePermissionsForSandboxAppDomain(New System.Security.PermissionSet(System.Security.Permissions.PermissionState.Unrestricted))
                data = report.Render(reportFormat, Nothing, mimeType, encoding, fileNameExtension, streams, warnings)
            End Using
            Return New ReportData(data, mimeType, fileNameExtension, encoding)
        End Function
    End Class
End Namespace
