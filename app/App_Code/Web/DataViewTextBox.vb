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

Namespace Norwindt.Web
    
    Public Class DataViewTextBox
        Inherits TextBox
        Implements IScriptControl
        
        Private m_DataController As String
        
        Private m_DataView As String
        
        Private m_DistinctValueFieldName As String
        
        Private m_MinimumPrefixLength As Integer
        
        Private m_CompletionInterval As Integer
        
        Public Sub New()
            MyBase.New()
            m_CompletionInterval = 500
            m_MinimumPrefixLength = 1
        End Sub
        
        <Category("Auto Complete")>  _
        Public Property DataController() As String
            Get
                Return m_DataController
            End Get
            Set
                m_DataController = value
            End Set
        End Property
        
        <Category("Auto Complete")>  _
        Public Property DataView() As String
            Get
                Return m_DataView
            End Get
            Set
                m_DataView = value
            End Set
        End Property
        
        <Category("Auto Complete")>  _
        Public Property DistinctValueFieldName() As String
            Get
                Return m_DistinctValueFieldName
            End Get
            Set
                m_DistinctValueFieldName = value
            End Set
        End Property
        
        <Category("Auto Complete"),  _
         DefaultValue(1)>  _
        Public Property MinimumPrefixLength() As Integer
            Get
                Return m_MinimumPrefixLength
            End Get
            Set
                m_MinimumPrefixLength = value
            End Set
        End Property
        
        <Category("Auto Complete"),  _
         DefaultValue(500)>  _
        Public Property CompletionInterval() As Integer
            Get
                Return m_CompletionInterval
            End Get
            Set
                m_CompletionInterval = value
            End Set
        End Property
        
        Protected Overrides Sub OnPreRender(ByVal e As EventArgs)
            MyBase.OnPreRender(e)
            Dim sm = ScriptManager.GetCurrent(Page)
            If (Not (sm) Is Nothing) Then
                sm.RegisterScriptControl(Me)
                sm.RegisterScriptDescriptors(Me)
            End If
        End Sub
        
        Function IScriptControl_GetScriptDescriptors() As IEnumerable(Of ScriptDescriptor) Implements IScriptControl.GetScriptDescriptors
            Dim descriptor = New ScriptBehaviorDescriptor("Sys.Extended.UI.AutoCompleteBehavior", ClientID)
            descriptor.AddProperty("id", ClientID)
            descriptor.AddProperty("completionInterval", CompletionInterval)
            descriptor.AddProperty("contextKey", String.Format("{0},{1},{2}", DataController, DataView, DistinctValueFieldName))
            descriptor.AddProperty("delimiterCharacters", ",;")
            descriptor.AddProperty("minimumPrefixLength", MinimumPrefixLength)
            descriptor.AddProperty("serviceMethod", "GetCompletionList")
            descriptor.AddProperty("servicePath", ResolveClientUrl("~/Services/DataControllerService.asmx"))
            descriptor.AddProperty("useContextKey", true)
            Return New ScriptBehaviorDescriptor() {descriptor}
        End Function
        
        Function IScriptControl_GetScriptReferences() As IEnumerable(Of ScriptReference) Implements IScriptControl.GetScriptReferences
            Dim scripts = New List(Of ScriptReference)()
            Return scripts
        End Function
    End Class
End Namespace
