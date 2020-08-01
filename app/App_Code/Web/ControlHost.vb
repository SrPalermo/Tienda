Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.Security
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls

Namespace Norwindt.Web
    
    Public Class ControlHost
        Inherits System.Web.UI.Page
        
        Public Overrides Property Theme() As String
            Get
                Return MyBase.Theme
            End Get
            Set
                'Themes are not supported in editors.
            End Set
        End Property
        
        Protected Overrides Sub OnInit(ByVal e As EventArgs)
            Controls.Add(New LiteralControl(""&Global.Microsoft.VisualBasic.ChrW(10)&"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.or"& _ 
                        "g/TR/xhtml1/DTD/xhtml1-transitional.dtd"">"&Global.Microsoft.VisualBasic.ChrW(10)&"<html xmlns=""http://www.w3.org/1999/xh"& _ 
                        "tml"" style=""overflow: hidden"">"&Global.Microsoft.VisualBasic.ChrW(10)))
            Dim head = New HtmlHead()
            Controls.Add(head)
            head.Controls.Add(New LiteralControl(""&Global.Microsoft.VisualBasic.ChrW(10)&"    <script type=""text/javascript"">"&Global.Microsoft.VisualBasic.ChrW(10)&"        function pageLoad() {"&Global.Microsoft.VisualBasic.ChrW(10)&"            va"& _ 
                        "r m = location.href.match(/(\?|&)id=(.+?)(&|$)/);"&Global.Microsoft.VisualBasic.ChrW(10)&"            if (!(parent && pa"& _ 
                        "rent.window.Web) || !m) return;"&Global.Microsoft.VisualBasic.ChrW(10)&"            var elem = parent.window.$get(m[2]);"& _ 
                        ""&Global.Microsoft.VisualBasic.ChrW(10)&"            if (!elem) return;"&Global.Microsoft.VisualBasic.ChrW(10)&"            if (typeof (FieldEditor_SetValue) !="& _ 
                        "= ""undefined"")"&Global.Microsoft.VisualBasic.ChrW(10)&"                FieldEditor_SetValue(elem.value);"&Global.Microsoft.VisualBasic.ChrW(10)&"            els"& _ 
                        "e"&Global.Microsoft.VisualBasic.ChrW(10)&"                alert('The field editor does not implement ""FieldEditor_SetVal"& _ 
                        "ue"" function.');"&Global.Microsoft.VisualBasic.ChrW(10)&"            if (typeof (FieldEditor_GetValue) !== ""undefined"")"&Global.Microsoft.VisualBasic.ChrW(10)& _ 
                        "                parent.window.Web.DataView.Editors[elem.id] = { 'GetValue': Fiel"& _ 
                        "dEditor_GetValue, 'SetValue': FieldEditor_SetValue };"&Global.Microsoft.VisualBasic.ChrW(10)&"            else"&Global.Microsoft.VisualBasic.ChrW(10)&"         "& _ 
                        "       alert('The field editor does not implement ""FieldEditor_GetValue"" functio"& _ 
                        "n.');"&Global.Microsoft.VisualBasic.ChrW(10)&"        }"&Global.Microsoft.VisualBasic.ChrW(10)&"    </script>"&Global.Microsoft.VisualBasic.ChrW(10)))
            head.Controls.Add(New LiteralControl(""&Global.Microsoft.VisualBasic.ChrW(10)&"    <style type=""text/css"">"&Global.Microsoft.VisualBasic.ChrW(10)&"        .ajax__htmleditor_editor_container"&Global.Microsoft.VisualBasic.ChrW(10)&"        {"& _ 
                        ""&Global.Microsoft.VisualBasic.ChrW(10)&"            border-width:0px!important;"&Global.Microsoft.VisualBasic.ChrW(10)&"        }"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"        .ajax__htmleditor_ed"& _ 
                        "itor_bottomtoolbar"&Global.Microsoft.VisualBasic.ChrW(10)&"        {"&Global.Microsoft.VisualBasic.ChrW(10)&"            padding-top:2px!important;"&Global.Microsoft.VisualBasic.ChrW(10)&"        }"&Global.Microsoft.VisualBasic.ChrW(10)&"  "& _ 
                        "  </style>"))
            Controls.Add(New LiteralControl(""&Global.Microsoft.VisualBasic.ChrW(10)&"<body style=""margin: 0px; padding: 0px; background-color: #fff;"">"&Global.Microsoft.VisualBasic.ChrW(10)))
            Dim form = New HtmlForm()
            Controls.Add(form)
            Dim sm = New ScriptManager()
            sm.ScriptMode = ScriptMode.Release
            form.Controls.Add(sm)
            Dim controlName = Request.Params("control")
            Dim c As Control = Nothing
            If Not (String.IsNullOrEmpty(controlName)) Then
                Try 
                    c = LoadControl(String.Format("~/Controls/{0}.ascx", controlName))
                Catch __exception As Exception
                End Try
                If (Not (c) Is Nothing) Then
                    Dim editorAttributes = c.GetType().GetCustomAttributes(GetType(AquariumFieldEditorAttribute), true)
                    If (editorAttributes.Length = 0) Then
                        c = Nothing
                    End If
                Else
                    If (controlName = "RichEditor") Then
                    End If
                End If
            End If
            If (c Is Nothing) Then
                Throw New HttpException(404, String.Empty)
            Else
                form.Controls.Add(c)
                If Not (TypeOf c Is System.Web.UI.UserControl) Then
                    Me.ClientScript.RegisterClientScriptBlock([GetType](), "ClientScripts", String.Format("function FieldEditor_GetValue(){{return $find('{0}').get_content();}}"&Global.Microsoft.VisualBasic.ChrW(10)&"function Fi"& _ 
                                "eldEditor_SetValue(value) {{$find('{0}').set_content(value);}}", c.ClientID), true)
                End If
            End If
            Controls.Add(New LiteralControl(""&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"</body>"&Global.Microsoft.VisualBasic.ChrW(10)&"</html>"))
            MyBase.OnInit(e)
            EnableViewState = false
        End Sub
    End Class
End Namespace
