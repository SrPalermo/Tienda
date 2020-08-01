Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Data.Common
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Caching
Imports System.Web.Configuration
Imports System.Web.Security
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Data
    
    <Serializable()>  _
    Public Class PageRequest
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Tag As String
        
        Private m_ViewType As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SupportsCaching As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RequiresFirstLetters As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DisableJSONCompatibility As Boolean
        
        Private m_PageIndex As Integer
        
        Private m_PageSize As Integer
        
        Private m_PageOffset As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SortExpression As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_GroupExpression As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Filter() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SystemFilter() As String
        
        Private m_FilterDetails As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SyncKey() As Object
        
        Private m_ContextKey As String
        
        Private m_FilterIsExternal As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RequiresMetaData As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FieldFilter() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_MetadataFilter() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RequiresSiteContentText As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RequiresPivot As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_PivotDefinitions As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RequiresRowCount As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DoesNotRequireData As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DoesNotRequireAggregates As Boolean
        
        Private m_Controller As String
        
        Private m_View As String
        
        Private m_LastView As String
        
        Private m_LookupContextController As String
        
        Private m_LookupContextView As String
        
        Private m_LookupContextFieldName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Inserting As Boolean
        
        Private m_LastCommandName As String
        
        Private m_LastCommandArgument As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ExternalFilter() As FieldValue
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_QuickFindHint As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_InnerJoinPrimaryKey As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_InnerJoinForeignKey As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Distinct As Boolean
        
        Public Sub New()
            MyBase.New
            If ((Not (HttpContext.Current) Is Nothing) AndAlso (Current Is Nothing)) Then
                HttpContext.Current.Items("PageRequest_Current") = Me
            End If
        End Sub
        
        Public Sub New(ByVal pageIndex As Integer, ByVal pageSize As Integer, ByVal sortExpression As String, ByVal filter() As String)
            MyBase.New
            Me.m_PageIndex = pageIndex
            Me.m_PageSize = pageSize
            Me.m_SortExpression = sortExpression
            Me.m_Filter = filter
        End Sub
        
        Public Property Tag() As String
            Get
                Return m_Tag
            End Get
            Set
                m_Tag = value
            End Set
        End Property
        
        Public Property ViewType() As String
            Get
                Return m_ViewType
            End Get
            Set
                m_ViewType = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property SupportsCaching() As Boolean
            Get
                Return m_SupportsCaching
            End Get
            Set
                m_SupportsCaching = value
            End Set
        End Property
        
        Public Property RequiresFirstLetters() As Boolean
            Get
                Return m_RequiresFirstLetters
            End Get
            Set
                m_RequiresFirstLetters = value
            End Set
        End Property
        
        Public Property DisableJSONCompatibility() As Boolean
            Get
                Return m_DisableJSONCompatibility
            End Get
            Set
                m_DisableJSONCompatibility = value
            End Set
        End Property
        
        Public Property PageIndex() As Integer
            Get
                Return m_PageIndex
            End Get
            Set
                m_PageIndex = value
            End Set
        End Property
        
        Public Property PageSize() As Integer
            Get
                Return m_PageSize
            End Get
            Set
                m_PageSize = value
            End Set
        End Property
        
        Public Property PageOffset() As Integer
            Get
                Return m_PageOffset
            End Get
            Set
                m_PageOffset = value
            End Set
        End Property
        
        Public Property SortExpression() As String
            Get
                Return m_SortExpression
            End Get
            Set
                m_SortExpression = value
            End Set
        End Property
        
        Public Property GroupExpression() As String
            Get
                Return m_GroupExpression
            End Get
            Set
                m_GroupExpression = value
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
        
        Public Property SystemFilter() As String()
            Get
                Return m_SystemFilter
            End Get
            Set
                m_SystemFilter = value
            End Set
        End Property
        
        Public Property FilterDetails() As String
            Get
                Return m_FilterDetails
            End Get
            Set
                m_FilterDetails = value
            End Set
        End Property
        
        Public Property SyncKey() As Object()
            Get
                Return m_SyncKey
            End Get
            Set
                m_SyncKey = value
            End Set
        End Property
        
        Public Property ContextKey() As String
            Get
                Return m_ContextKey
            End Get
            Set
                m_ContextKey = value
            End Set
        End Property
        
        Public Property FilterIsExternal() As Boolean
            Get
                Return m_FilterIsExternal
            End Get
            Set
                m_FilterIsExternal = value
            End Set
        End Property
        
        Public Property RequiresMetaData() As Boolean
            Get
                Return m_RequiresMetaData
            End Get
            Set
                m_RequiresMetaData = value
            End Set
        End Property
        
        Public Overridable Property FieldFilter() As String()
            Get
                Return m_FieldFilter
            End Get
            Set
                m_FieldFilter = value
            End Set
        End Property
        
        Public Overridable Property MetadataFilter() As String()
            Get
                Return m_MetadataFilter
            End Get
            Set
                m_MetadataFilter = value
            End Set
        End Property
        
        Public Property RequiresSiteContentText() As Boolean
            Get
                Return m_RequiresSiteContentText
            End Get
            Set
                m_RequiresSiteContentText = value
            End Set
        End Property
        
        Public Property RequiresPivot() As Boolean
            Get
                Return m_RequiresPivot
            End Get
            Set
                m_RequiresPivot = value
            End Set
        End Property
        
        Public Property PivotDefinitions() As String
            Get
                Return m_PivotDefinitions
            End Get
            Set
                m_PivotDefinitions = value
            End Set
        End Property
        
        Public Property RequiresRowCount() As Boolean
            Get
                Return m_RequiresRowCount
            End Get
            Set
                m_RequiresRowCount = value
            End Set
        End Property
        
        Public Property DoesNotRequireData() As Boolean
            Get
                Return m_DoesNotRequireData
            End Get
            Set
                m_DoesNotRequireData = value
            End Set
        End Property
        
        Public Property DoesNotRequireAggregates() As Boolean
            Get
                Return m_DoesNotRequireAggregates
            End Get
            Set
                m_DoesNotRequireAggregates = value
            End Set
        End Property
        
        Public Property Controller() As String
            Get
                Return m_Controller
            End Get
            Set
                m_Controller = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property View() As String
            Get
                Return m_View
            End Get
            Set
                m_View = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property LastView() As String
            Get
                Return m_LastView
            End Get
            Set
                m_LastView = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property LookupContextController() As String
            Get
                Return m_LookupContextController
            End Get
            Set
                m_LookupContextController = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property LookupContextView() As String
            Get
                Return m_LookupContextView
            End Get
            Set
                m_LookupContextView = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property LookupContextFieldName() As String
            Get
                Return m_LookupContextFieldName
            End Get
            Set
                m_LookupContextFieldName = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property Inserting() As Boolean
            Get
                Return m_Inserting
            End Get
            Set
                m_Inserting = value
            End Set
        End Property
        
        Public Property LastCommandName() As String
            Get
                Return m_LastCommandName
            End Get
            Set
                m_LastCommandName = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property LastCommandArgument() As String
            Get
                Return m_LastCommandArgument
            End Get
            Set
                m_LastCommandArgument = ControllerUtilities.ValidateName(value)
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
        
        Public Property QuickFindHint() As String
            Get
                Return m_QuickFindHint
            End Get
            Set
                m_QuickFindHint = value
            End Set
        End Property
        
        Public Property InnerJoinPrimaryKey() As String
            Get
                Return m_InnerJoinPrimaryKey
            End Get
            Set
                m_InnerJoinPrimaryKey = value
            End Set
        End Property
        
        Public Property InnerJoinForeignKey() As String
            Get
                Return m_InnerJoinForeignKey
            End Get
            Set
                m_InnerJoinForeignKey = value
            End Set
        End Property
        
        Public Property Distinct() As Boolean
            Get
                Return m_Distinct
            End Get
            Set
                m_Distinct = value
            End Set
        End Property
        
        Public ReadOnly Property IsModal() As Boolean
            Get
                Return (Not (String.IsNullOrEmpty(m_ContextKey)) AndAlso m_ContextKey.Contains("_ModalDataView"))
            End Get
        End Property
        
        Public Shared ReadOnly Property Current() As PageRequest
            Get
                Return CType(HttpContext.Current.Items("PageRequest_Current"),PageRequest)
            End Get
        End Property
        
        Public Sub AssignContext(ByVal controller As String, ByVal view As String, ByVal config As ControllerConfiguration)
            m_Controller = controller
            m_View = view
            Dim referrer = String.Empty
            If ((PageSize = 1000) AndAlso String.IsNullOrEmpty(Me.SortExpression)) Then
                'we are processing a request to retrieve static lookup data
                Dim sortExpressionNode = config.SelectSingleNode("c:dataController/c:views/c:view[@id='{0}']/@sortExpression", view)
                If ((Not (sortExpressionNode) Is Nothing) AndAlso Not (String.IsNullOrEmpty(sortExpressionNode.Value))) Then
                    Me.SortExpression = sortExpressionNode.Value
                End If
            End If
            If ((Not (HttpContext.Current) Is Nothing) AndAlso (Not (HttpContext.Current.Request.UrlReferrer) Is Nothing)) Then
                referrer = HttpContext.Current.Request.UrlReferrer.AbsolutePath
            End If
            Me.m_ContextKey = String.Format("{0}/{1}.{2}.{3}", referrer, controller, view, ContextKey)
        End Sub
    End Class
End Namespace
