Imports Norwindt.Data
Imports Norwindt.Services
Imports System
Imports System.Web

Namespace Norwindt.Handlers
    
    Partial Public Class Theme
        Inherits GenericHandlerBase
        Implements IHttpHandler, System.Web.SessionState.IRequiresSessionState
        
        ReadOnly Property IHttpHandler_IsReusable() As Boolean Implements IHttpHandler.IsReusable
            Get
                Return true
            End Get
        End Property
        
        Sub IHttpHandler_ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
            Dim theme = context.Request.QueryString("theme")
            Dim accent = context.Request.QueryString("accent")
            If (String.IsNullOrEmpty(theme) OrElse String.IsNullOrEmpty(accent)) Then
                Throw New HttpException(400, "Bad Request")
            End If
            Dim services = New ApplicationServices()
            Dim css = New StylesheetGenerator(theme, accent).ToString()
            context.Response.ContentType = "text/css"
            Dim cache = context.Response.Cache
            cache.SetCacheability(HttpCacheability.Public)
            cache.SetOmitVaryStar(true)
            cache.SetExpires(Date.Now.AddDays(365))
            cache.SetValidUntilExpires(true)
            cache.SetLastModifiedFromFileDependencies()
            ApplicationServices.CompressOutput(context, css)
        End Sub
    End Class
End Namespace
