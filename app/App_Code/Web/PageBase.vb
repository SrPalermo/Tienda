Imports Norwindt.Data
Imports Norwindt.Services
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.HtmlControls

Namespace Norwindt.Web
    
    Partial Public Class PageBase
        Inherits PageBaseCore
    End Class
    
    Public Class PageBaseCore
        Inherits System.Web.UI.Page
        
        Public Overridable ReadOnly Property Device() As String
            Get
                Return Nothing
            End Get
        End Property
        
        Protected Overrides Sub InitializeCulture()
            CultureManager.Initialize()
            MyBase.InitializeCulture()
        End Sub
        
        Protected Overrides Sub OnInit(ByVal e As EventArgs)
            MyBase.OnInit(e)
            ValidateUrlParameters()
            If Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft Then
                For Each c As Control in Controls
                    ChangeCurrentCultureTextFlowDirection(c)
                Next
            End If
            Dim mobileSwitch = Request.Params("_mobile")
            If String.IsNullOrEmpty(mobileSwitch) Then
                mobileSwitch = Request.Params("_touch")
            End If
            If (Not (mobileSwitch) Is Nothing) Then
                Dim cookie = New HttpCookie("appfactorytouchui", (mobileSwitch = "true").ToString().ToLower())
                If String.IsNullOrEmpty(mobileSwitch) Then
                    cookie.Expires = DateTime.Today.AddDays(-1)
                Else
                    cookie.Expires = DateTime.Now.AddDays(30)
                End If
                Response.AppendCookie(cookie)
                Response.Redirect(Request.CurrentExecutionFilePath)
            End If
            Dim isTouchUI = ApplicationServices.IsTouchClient
            If (((Device = "touch") AndAlso Not (isTouchUI)) OrElse ((Device = "desktop") AndAlso isTouchUI)) Then
                Response.Redirect("~/")
            End If
            ApplicationServices.VerifyUrl()
        End Sub
        
        Private Function ChangeCurrentCultureTextFlowDirection(ByVal c As Control) As Boolean
            If TypeOf c Is HtmlGenericControl Then
                Dim gc = CType(c,HtmlGenericControl)
                If (gc.TagName = "body") Then
                    gc.Attributes("dir") = "rtl"
                    gc.Attributes("class") = "RTL"
                    Return true
                End If
            Else
                For Each child As Control in c.Controls
                    Dim result = ChangeCurrentCultureTextFlowDirection(child)
                    If result Then
                        Return true
                    End If
                Next
            End If
            Return false
        End Function
        
        Protected Overridable Function HideUnauthorizedDataViews(ByVal content As String) As String
            Dim tryRoles = true
            Do While tryRoles
                Dim m = Regex.Match(content, "\s*\bdata-roles\s*=\s*""([\S\s]*?)""")
                tryRoles = m.Success
                If tryRoles Then
                    Dim stringAfter = content.Substring((m.Index + m.Length))
                    If DataControllerBase.UserIsInRole(m.Groups(1).Value) Then
                        content = (content.Substring(0, m.Index) + stringAfter)
                    Else
                        Dim startPos = content.Substring(0, m.Index).LastIndexOf("<div")
                        Dim closingDiv = Regex.Match(stringAfter, "</div>")
                        content = (content.Substring(0, startPos) + stringAfter.Substring((closingDiv.Index + closingDiv.Length)))
                    End If
                End If
            Loop
            Return content
        End Function
        
        Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)
            Dim sb = New StringBuilder()
            Dim tempWriter = New HtmlTextWriter(New StringWriter(sb))
            MyBase.Render(tempWriter)
            tempWriter.Flush()
            tempWriter.Close()
            Dim page = Norwindt.Data.Localizer.Replace("Pages", Path.GetFileName(Request.PhysicalPath), sb.ToString())
            If page.Contains("data-content-framework=""bootstrap""") Then
                If ApplicationServices.EnableCombinedCss Then
                    page = Regex.Replace(page, "_cf=""", "_cf=bootstrap""")
                Else
                    If ApplicationServices.IsTouchClient Then
                        page = Regex.Replace(page, "(<link\s+href=""[.\w\/]+?touch\-theme\..+?"".+?/>)", (("<link href=""" + ResolveClientUrl(("~/css/sys/bootstrap.css?" + ApplicationServices.Version)))  _
                                        + """ type=""text/css"" rel=""stylesheet"" />$1"))
                    Else
                        page = Regex.Replace(page, "\/>\s*<title>", (("/><link href=""" + ResolveClientUrl(("~/css/sys/bootstrap.css?" + ApplicationServices.Version)))  _
                                        + """ type=""text/css"" rel=""stylesheet"" /><title>"))
                    End If
                End If
            End If
            writer.Write(HideUnauthorizedDataViews(page))
        End Sub
        
        Protected Overridable Sub ValidateUrlParameters()
            Dim success = true
            Dim link = Page.Request("_link")
            If Not (String.IsNullOrEmpty(link)) Then
                Try 
                    link = StringEncryptor.FromString(link.Replace(" ", "+").Split(Global.Microsoft.VisualBasic.ChrW(44))(0))
                    If Not (link.Contains(Global.Microsoft.VisualBasic.ChrW(63))) Then
                        link = (Global.Microsoft.VisualBasic.ChrW(63) + link)
                    End If
                    Dim permalink = link.Split(Global.Microsoft.VisualBasic.ChrW(63))
                    ClientScript.RegisterClientScriptBlock([GetType](), "CommandLine", String.Format("var __dacl='{0}?{1}';", permalink(0), BusinessRules.JavaScriptString(permalink(1))), true)
                Catch __exception As Exception
                    success = false
                End Try
            End If
            If Not (success) Then
                Response.StatusCode = 403
                Response.End()
            End If
        End Sub
    End Class
    
    Partial Public Class ControlBase
        Inherits ControlBaseCore
    End Class
    
    Public Class ControlBaseCore
        Inherits System.Web.UI.UserControl
        
        Protected Overrides Sub OnInit(ByVal e As EventArgs)
            MyBase.OnInit(e)
        End Sub
        
        Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)
            Dim sb = New StringBuilder()
            Dim tempWriter = New HtmlTextWriter(New StringWriter(sb))
            MyBase.Render(tempWriter)
            tempWriter.Flush()
            tempWriter.Close()
            writer.Write(Norwindt.Data.Localizer.Replace("Pages", Path.GetFileName(Request.PhysicalPath), sb.ToString()))
        End Sub
        
        Public Shared Function LoadPageControl(ByVal placeholder As System.Web.UI.Control, ByVal pageName As String, ByVal developmentMode As Boolean) As System.Web.UI.Control
            Try 
                Dim page = placeholder.Page
                Dim basePath = "~"
                If Not (developmentMode) Then
                    basePath = "~/DesktopModules/Norwindt"
                End If
                Dim controlPath = String.Format("{0}/Pages/{1}.ascx", basePath, pageName)
                Dim c = page.LoadControl(controlPath)
                If (Not (c) Is Nothing) Then
                    placeholder.Controls.Clear()
                    placeholder.Controls.Add(New LiteralControl("<table style=""width:100%"" id=""PageBody"" class=""Hosted""><tr><td valign=""top"" id=""P"& _ 
                                "ageContent"">"))
                    placeholder.Controls.Add(c)
                    placeholder.Controls.Add(New LiteralControl("</td></tr></table>"))
                    Return c
                End If
            Catch __exception As Exception
            End Try
            Return Nothing
        End Function
    End Class
End Namespace
