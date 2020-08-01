Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Threading
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.HtmlControls

Namespace Norwindt.Data
    
    Public Class CultureManager
        
        Public Const AutoDetectCulture As String = "Detect,Detect"
        
        Public Shared SupportedCultures() As String = New String() {"en-US,en-US"}
        
        Public Shared Sub Initialize()
            Dim ctx = HttpContext.Current
            If ((ctx Is Nothing) OrElse (Not (ctx.Items("CultureManager_Initialized")) Is Nothing)) Then
                Return
            End If
            ctx.Items("CultureManager_Initialized") = true
            Dim cultureCookie = ctx.Request.Cookies(".COTCULTURE")
            Dim culture As String = Nothing
            If (Not (cultureCookie) Is Nothing) Then
                culture = cultureCookie.Value
            End If
            If (String.IsNullOrEmpty(culture) OrElse (culture = CultureManager.AutoDetectCulture)) Then
                If (Not (ctx.Request.UserLanguages) Is Nothing) Then
                    For Each l in ctx.Request.UserLanguages
                        Dim languageInfo = l.Split(Global.Microsoft.VisualBasic.ChrW(59))
                        For Each c in SupportedCultures
                            If c.StartsWith(languageInfo(0)) Then
                                culture = c
                                Exit For
                            End If
                        Next
                        If (Not (culture) Is Nothing) Then
                            Exit For
                        End If
                    Next
                Else
                    culture = SupportedCultures(0)
                End If
            End If
            If Not (String.IsNullOrEmpty(culture)) Then
                Dim cultureIndex = Array.IndexOf(SupportedCultures, culture)
                If Not ((cultureIndex = -1)) Then
                    Dim ci = culture.Split(Global.Microsoft.VisualBasic.ChrW(44))
                    Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(ci(0))
                    Thread.CurrentThread.CurrentUICulture = New CultureInfo(ci(1))
                    If TypeOf ctx.Handler Is Page Then
                        Dim p = CType(ctx.Handler,Page)
                        p.Culture = ci(0)
                        p.UICulture = ci(1)
                        If (Not (cultureCookie) Is Nothing) Then
                            If (cultureCookie.Value = CultureManager.AutoDetectCulture) Then
                                cultureCookie.Expires = DateTime.Now.AddDays(-14)
                            Else
                                cultureCookie.Expires = DateTime.Now.AddDays(14)
                            End If
                            ctx.Response.AppendCookie(cultureCookie)
                        End If
                    End If
                End If
            End If
        End Sub
        
        Public Overloads Shared Function ResolveEmbeddedResourceName(ByVal resourceName As String, ByVal culture As String) As String
            Return ResolveEmbeddedResourceName(GetType(CultureManager).Assembly, resourceName, culture)
        End Function
        
        Public Overloads Shared Function ResolveEmbeddedResourceName(ByVal resourceName As String) As String
            Return ResolveEmbeddedResourceName(GetType(CultureManager).Assembly, resourceName, Thread.CurrentThread.CurrentUICulture.Name)
        End Function
        
        Public Overloads Shared Function ResolveEmbeddedResourceName(ByVal a As [Assembly], ByVal resourceName As String, ByVal culture As String) As String
            Dim extension = Path.GetExtension(resourceName)
            Dim fileName = Path.GetFileNameWithoutExtension(resourceName)
            Dim localizedResourceName = String.Format("{0}.{1}{2}", fileName, culture.Replace("-", "_"), extension)
            Dim mri = a.GetManifestResourceInfo(localizedResourceName)
            If (mri Is Nothing) Then
                If culture.Contains("-") Then
                    localizedResourceName = String.Format("{0}.{1}_{2}", fileName, culture.Substring(0, culture.LastIndexOf("-")).Replace("-", "_"), extension)
                Else
                    localizedResourceName = String.Format("{0}.{1}_{2}", fileName, culture, extension)
                End If
                mri = a.GetManifestResourceInfo(localizedResourceName)
            End If
            If (mri Is Nothing) Then
                localizedResourceName = resourceName
            End If
            Return localizedResourceName
        End Function
    End Class
    
    Public Class GenericHandlerBase
        
        Public Sub New()
            MyBase.New
            CultureManager.Initialize()
        End Sub
        
        Protected Overridable Function GenerateOutputFileName(ByVal args As ActionArgs, ByVal outputFileName As String) As String
            args.CommandArgument = args.CommandName
            args.CommandName = "FileName"
            Dim values = New List(Of FieldValue)()
            values.Add(New FieldValue("FileName", outputFileName))
            args.Values = values.ToArray()
            Dim result = ControllerFactory.CreateDataController().Execute(args.Controller, args.View, args)
            For Each v in result.Values
                If (v.Name = "FileName") Then
                    outputFileName = Convert.ToString(v.Value)
                    Exit For
                End If
            Next
            Return outputFileName
        End Function
        
        Protected Overridable Sub AppendDownloadTokenCookie()
            Dim context = HttpContext.Current
            Dim downloadToken = "APPFACTORYDOWNLOADTOKEN"
            Dim tokenCookie = context.Request.Cookies(downloadToken)
            If (Not (tokenCookie) Is Nothing) Then
                tokenCookie.Value = String.Format("{0},{1}", tokenCookie.Value, Guid.NewGuid())
                context.Response.AppendCookie(tokenCookie)
            End If
        End Sub
    End Class
End Namespace
