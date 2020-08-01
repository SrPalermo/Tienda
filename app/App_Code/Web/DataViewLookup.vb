Imports Norwindt.Data
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Configuration
Imports System.Data
Imports System.Linq
Imports System.Web
Imports System.Web.Security
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls
Imports System.Web.UI.WebControls.WebParts

Namespace Norwindt.Web
    
    <DefaultProperty("SelectedValue"),  _
     ControlValueProperty("SelectedValue"),  _
     DefaultEvent("SelectedValueChanged")>  _
    Public Class DataViewLookup
        Inherits System.Web.UI.Control
        Implements INamingContainer
        
        Private m_AutoPostBack As Boolean
        
        Private m_AllowCreateItems As Boolean = true
        
        Private m_DataController As String
        
        Private m_DataView As String
        
        Private m_DataValueField As String
        
        Private m_DataTextField As String
        
        Private m_Span As HtmlGenericControl
        
        Private m_Extender As DataViewExtender
        
        Public Sub New()
            MyBase.New
        End Sub
        
        <System.ComponentModel.Category("Behavior"),  _
         System.ComponentModel.DefaultValue(false)>  _
        Public Property AutoPostBack() As Boolean
            Get
                Return m_AutoPostBack
            End Get
            Set
                m_AutoPostBack = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(true)>  _
        Public Property AllowCreateItems() As Boolean
            Get
                Return m_AllowCreateItems
            End Get
            Set
                m_AllowCreateItems = value
            End Set
        End Property
        
        <System.ComponentModel.Browsable(false)>  _
        Public Property SelectedValue() As String
            Get
                Dim v = CType(ViewState("SelectedValue"),String)
                If (v Is Nothing) Then
                    v = String.Empty
                End If
                Return v
            End Get
            Set
                ViewState("SelectedValue") = value
            End Set
        End Property
        
        <System.ComponentModel.Category("Data")>  _
        Public Property DataController() As String
            Get
                Return m_DataController
            End Get
            Set
                m_DataController = value
            End Set
        End Property
        
        <System.ComponentModel.Category("Data")>  _
        Public Property DataView() As String
            Get
                Return m_DataView
            End Get
            Set
                m_DataView = value
            End Set
        End Property
        
        <System.ComponentModel.Category("Data")>  _
        Public Property DataValueField() As String
            Get
                Return m_DataValueField
            End Get
            Set
                m_DataValueField = value
            End Set
        End Property
        
        <System.ComponentModel.Category("Data")>  _
        Public Property DataTextField() As String
            Get
                Return m_DataTextField
            End Get
            Set
                m_DataTextField = value
            End Set
        End Property
        
        Protected Property LookupText() As String
            Get
                Dim text = CType(ViewState("LookupText"),String)
                If (String.IsNullOrEmpty(text) AndAlso Not (String.IsNullOrEmpty(SelectedValue))) Then
                    text = Controller.LookupText(DataController, String.Format("{0}:={1}", DataValueField, SelectedValue), DataTextField)
                    ViewState("LookupText") = text
                End If
                If String.IsNullOrEmpty(text) Then
                    text = "(select)"
                End If
                Return text
            End Get
            Set
                ViewState("LookupText") = value
            End Set
        End Property
        
        <System.ComponentModel.Category("Behavior"),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property Enabled() As Boolean
            Get
                Dim v = ViewState("Enabled")
                If (v Is Nothing) Then
                    Return true
                End If
                Return CType(v,Boolean)
            End Get
            Set
                ViewState("Enabled") = value
            End Set
        End Property
        
        <System.ComponentModel.Category("Accessibility"),  _
         System.ComponentModel.DefaultValue(0)>  _
        Public Property TabIndex() As Integer
            Get
                Dim v = ViewState("TabIndex")
                If (v Is Nothing) Then
                    Return 0
                End If
                Return CType(v,Integer)
            End Get
            Set
                ViewState("TabIndex") = value
            End Set
        End Property
        
        Public Event SelectedValueChanged As EventHandler(Of EventArgs)
        
        Protected Overridable Sub OnSelectedValueChanged(ByVal e As EventArgs)
            If (Not (Me.SelectedValueChangedEvent) Is Nothing) Then
                RaiseEvent SelectedValueChanged(Me, e)
            End If
        End Sub
        
        Protected Overrides Sub OnInit(ByVal e As EventArgs)
            MyBase.OnInit(e)
            If Not (DesignMode) Then
                m_Span = New HtmlGenericControl("span")
                m_Span.ID = "s"
                Controls.Add(m_Span)
                m_Extender = New DataViewExtender()
                m_Extender.ID = "e"
                m_Extender.TargetControlID = m_Span.ID
                Controls.Add(m_Extender)
            End If
        End Sub
        
        Protected Overrides Sub OnLoad(ByVal e As EventArgs)
            MyBase.OnLoad(e)
            If Page.IsPostBack Then
                Dim valueKey = (m_Extender.ClientID + "_Item0")
                If Page.Request.Form.AllKeys.Contains(valueKey) Then
                    SelectedValue = Page.Request.Form(valueKey)
                    LookupText = Page.Request.Form((m_Extender.ClientID + "_Text0"))
                    OnSelectedValueChanged(EventArgs.Empty)
                End If
            End If
        End Sub
        
        Protected Overrides Sub OnPreRender(ByVal e As EventArgs)
            MyBase.OnPreRender(e)
            m_Span.InnerHtml = String.Format("<table cellpadding=""0"" cellspacing=""0"" class=""DataViewLookup""><tr><td>{0}</td></t"& _ 
                    "r></table>", HttpUtility.HtmlEncode(LookupText))
            m_Extender.Controller = DataController
            m_Extender.View = DataView
            m_Extender.LookupValue = SelectedValue
            m_Extender.LookupText = LookupText
            m_Extender.AllowCreateLookupItems = AllowCreateItems
            m_Extender.Enabled = Enabled
            m_Extender.TabIndex = TabIndex
            If AutoPostBack Then
                m_Extender.LookupPostBackExpression = Page.ClientScript.GetPostBackEventReference(Me, Nothing)
            End If
        End Sub
        
        Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)
            If (Not (Site) Is Nothing) Then
                writer.RenderBeginTag(HtmlTextWriterTag.Span)
                writer.Write("DataViewLookup (")
                writer.Write(DataValueField)
                writer.Write("=>")
                writer.Write(DataController)
                If Not (String.IsNullOrEmpty("DataView")) Then
                    writer.Write(", ")
                    writer.Write(DataView)
                End If
                writer.Write(")")
                writer.RenderEndTag()
            Else
                MyBase.Render(writer)
            End If
            'add a hidden field to support UpdatePanel with partial rendering
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden")
            writer.AddAttribute(HtmlTextWriterAttribute.Id, ClientID)
            writer.RenderBeginTag(HtmlTextWriterTag.Input)
            writer.RenderEndTag()
        End Sub
        
        Public Sub Clear()
            SelectedValue = Nothing
            LookupText = Nothing
        End Sub
    End Class
End Namespace
