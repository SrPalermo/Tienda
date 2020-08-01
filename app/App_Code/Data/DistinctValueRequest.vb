Imports System
Imports System.Web

Namespace Norwindt.Data
    
    Partial Public Class DistinctValueRequest
        Inherits DistinctValueRequestBase
    End Class
    
    Public Class DistinctValueRequestBase
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Tag As String
        
        Private m_FieldName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Filter() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ExternalFilter() As FieldValue
        
        Private m_LookupContextController As String
        
        Private m_LookupContextView As String
        
        Private m_LookupContextFieldName As String
        
        Private m_Controller As String
        
        Private m_View As String
        
        Private m_MaximumValueCount As Integer
        
        Private m_AllowFieldInFilter As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_QuickFindHint As String
        
        Public Sub New()
            MyBase.New
            If (Current Is Nothing) Then
                HttpContext.Current.Items("DistinctValueRequest_Current") = Me
            End If
        End Sub
        
        Public Property Tag() As String
            Get
                Return m_Tag
            End Get
            Set
                m_Tag = value
            End Set
        End Property
        
        Public Property FieldName() As String
            Get
                Return m_FieldName
            End Get
            Set
                m_FieldName = value
            End Set
        End Property
        
        Public Property Filter() As String()
            Get
                Return m_Filter
            End Get
            Set
                m_Filter = value
            End Set
        End Property
        
        Public Property ExternalFilter() As FieldValue()
            Get
                Return m_ExternalFilter
            End Get
            Set
                m_ExternalFilter = value
            End Set
        End Property
        
        Public Property LookupContextController() As String
            Get
                Return m_LookupContextController
            End Get
            Set
                m_LookupContextController = value
            End Set
        End Property
        
        Public Property LookupContextView() As String
            Get
                Return m_LookupContextView
            End Get
            Set
                m_LookupContextView = value
            End Set
        End Property
        
        Public Property LookupContextFieldName() As String
            Get
                Return m_LookupContextFieldName
            End Get
            Set
                m_LookupContextFieldName = value
            End Set
        End Property
        
        Public Property Controller() As String
            Get
                Return m_Controller
            End Get
            Set
                m_Controller = value
            End Set
        End Property
        
        Public Property View() As String
            Get
                Return m_View
            End Get
            Set
                m_View = value
            End Set
        End Property
        
        Public Shared ReadOnly Property Current() As DistinctValueRequest
            Get
                Return CType(HttpContext.Current.Items("DistinctValueRequest_Current"),DistinctValueRequest)
            End Get
        End Property
        
        Public Overridable Property MaximumValueCount() As Integer
            Get
                If (m_MaximumValueCount <= 0) Then
                    Return DataControllerBase.MaximumDistinctValues
                End If
                Return m_MaximumValueCount
            End Get
            Set
                m_MaximumValueCount = value
            End Set
        End Property
        
        Public Property AllowFieldInFilter() As Boolean
            Get
                Return m_AllowFieldInFilter
            End Get
            Set
                m_AllowFieldInFilter = value
            End Set
        End Property
        
        Public Property QuickFindHint() As String
            Get
                Return m_QuickFindHint
            End Get
            Set
                m_QuickFindHint = value
            End Set
        End Property
    End Class
End Namespace
