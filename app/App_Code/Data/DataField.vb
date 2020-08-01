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
    
    Public Enum DataFieldMaskType
        
        None
        
        [Date]
        
        Number
        
        Time
        
        DateTime
    End Enum
    
    Public Enum DataFieldAggregate
        
        None
        
        Sum
        
        Count
        
        Average
        
        Max
        
        Min
    End Enum
    
    Public Enum OnDemandDisplayStyle
        
        Thumbnail
        
        Link
        
        Signature
    End Enum
    
    Public Enum TextInputMode
        
        Text
        
        Password
        
        RichText
        
        Note
        
        [Static]
    End Enum
    
    Public Enum FieldSearchMode
        
        [Default]
        
        Required
        
        Suggested
        
        Allowed
        
        Forbidden
    End Enum
    
    Public Class DataField
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Name As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AliasName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Tag As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Type As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Len As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Label As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_IsPrimaryKey As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ReadOnly As Boolean
        
        Private m_DefaultValue As String
        
        Private m_HeaderText As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FooterText As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ToolTip As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Watermark As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Hidden As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AllowQBE As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AllowSorting As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AllowLEV As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataFormatString As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Copy As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_HyperlinkFormatString As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_FormatOnClient As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SourceFields As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CategoryIndex As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AllowNulls As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Columns As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Rows As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_OnDemand As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Search As FieldSearchMode
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SearchOptions As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsDataController As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsDataView As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsDataValueField As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsDataTextField As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsStyle As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsPageSize As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsNewDataView As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsTargetController As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsLetters As Boolean
        
        Private m_Items As List(Of Object())
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Aggregate As DataFieldAggregate
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_OnDemandHandler As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_OnDemandStyle As OnDemandDisplayStyle
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_TextMode As TextInputMode
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_MaskType As DataFieldMaskType
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Mask As String
        
        Private m_ContextFields As String
        
        Private m_SelectExpression As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Formula As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ShowInSummary As Boolean
        
        Private m_IsMirror As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_HtmlEncode As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AutoCompletePrefixLength As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Calculated As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CausesCalculate As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_IsVirtual As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Configuration As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Editor As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AutoSelect As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SearchOnStart As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ItemsDescription As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewController As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewId As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewFilterSource As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewFilterFields As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewShowInSummary As Boolean
        
        Private m_DataViewShowActionBar As Boolean = true
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewShowActionButtons As String
        
        Private m_DataViewShowDescription As Boolean = true
        
        Private m_DataViewShowViewSelector As Boolean = true
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewShowModalForms As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewSearchByFirstLetter As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewSearchOnStart As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewPageSize As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewMultiSelect As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewShowPager As String
        
        Private m_DataViewShowPageSize As Boolean = true
        
        Private m_DataViewShowSearchBar As Boolean = true
        
        Private m_DataViewShowQuickFind As Boolean = true
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewShowRowNumber As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewAutoSelectFirstRow As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DataViewAutoHighlightFirstRow As Boolean
        
        Public Sub New()
            MyBase.New
            m_Items = New List(Of Object())()
            m_FormatOnClient = true
        End Sub
        
        Public Sub New(ByVal field As XPathNavigator, ByVal nm As IXmlNamespaceResolver)
            Me.New()
            Me.m_Name = field.GetAttribute("name", String.Empty)
            Me.m_Type = field.GetAttribute("type", String.Empty)
            Dim l = field.GetAttribute("length", String.Empty)
            If Not (String.IsNullOrEmpty(l)) Then
                m_Len = Convert.ToInt32(l)
            End If
            Me.m_Label = field.GetAttribute("label", String.Empty)
            Me.m_IsPrimaryKey = (field.GetAttribute("isPrimaryKey", String.Empty) = "true")
            Me.m_ReadOnly = (field.GetAttribute("readOnly", String.Empty) = "true")
            Me.m_OnDemand = (field.GetAttribute("onDemand", String.Empty) = "true")
            Me.m_DefaultValue = field.GetAttribute("default", String.Empty)
            Me.m_AllowNulls = Not ((field.GetAttribute("allowNulls", String.Empty) = "false"))
            Me.m_Hidden = (field.GetAttribute("hidden", String.Empty) = "true")
            Me.m_AllowQBE = Not ((field.GetAttribute("allowQBE", String.Empty) = "false"))
            Me.m_AllowLEV = (field.GetAttribute("allowLEV", String.Empty) = "true")
            Me.m_AllowSorting = Not ((field.GetAttribute("allowSorting", String.Empty) = "false"))
            Me.m_SourceFields = field.GetAttribute("sourceFields", String.Empty)
            Dim onDemandStyle = field.GetAttribute("onDemandStyle", String.Empty)
            If (onDemandStyle = "Link") Then
                Me.m_OnDemandStyle = OnDemandDisplayStyle.Link
            Else
                If (onDemandStyle = "Signature") Then
                    Me.m_OnDemandStyle = OnDemandDisplayStyle.Signature
                End If
            End If
            Me.m_OnDemandHandler = field.GetAttribute("onDemandHandler", String.Empty)
            Me.m_ContextFields = field.GetAttribute("contextFields", String.Empty)
            Me.m_SelectExpression = field.GetAttribute("select", String.Empty)
            Dim computed = (field.GetAttribute("computed", String.Empty) = "true")
            If computed Then
                m_Formula = CType(field.Evaluate("string(self::c:field/c:formula)", nm),String)
                If String.IsNullOrEmpty(m_Formula) Then
                    m_Formula = "null"
                End If
            End If
            Me.m_ShowInSummary = (field.GetAttribute("showInSummary", String.Empty) = "true")
            Me.m_HtmlEncode = Not ((field.GetAttribute("htmlEncode", String.Empty) = "false"))
            Me.m_Calculated = (field.GetAttribute("calculated", String.Empty) = "true")
            Me.m_CausesCalculate = (field.GetAttribute("causesCalculate", String.Empty) = "true")
            Me.m_IsVirtual = (field.GetAttribute("isVirtual", String.Empty) = "true")
            Me.m_Configuration = CType(field.Evaluate("string(self::c:field/c:configuration)", nm),String)
            Me.m_DataFormatString = field.GetAttribute("dataFormatString", String.Empty)
            m_FormatOnClient = Not ((field.GetAttribute("formatOnClient", String.Empty) = "false"))
            Dim editor = field.GetAttribute("editor", String.Empty)
            If Not (String.IsNullOrEmpty(editor)) Then
                m_Editor = editor
            End If
            Dim itemsNav = field.SelectSingleNode("c:items", nm)
            If (Not (itemsNav) Is Nothing) Then
                Me.ItemsDataController = itemsNav.GetAttribute("dataController", String.Empty)
                Me.ItemsTargetController = itemsNav.GetAttribute("targetController", String.Empty)
            End If
            Dim dataViewNav = field.SelectSingleNode("c:dataView", nm)
            If (Not (dataViewNav) Is Nothing) Then
                Me.DataViewController = dataViewNav.GetAttribute("controller", String.Empty)
                Me.DataViewId = dataViewNav.GetAttribute("view", String.Empty)
                Me.DataViewFilterSource = dataViewNav.GetAttribute("filterSource", String.Empty)
                Me.DataViewFilterFields = dataViewNav.GetAttribute("filterFields", String.Empty)
                m_AllowQBE = true
                m_AllowSorting = true
                m_Len = 0
                m_Columns = 0
                m_HtmlEncode = true
            End If
        End Sub
        
        Public Sub New(ByVal field As XPathNavigator, ByVal nm As IXmlNamespaceResolver, ByVal hidden As Boolean)
            Me.New(field, nm)
            Me.m_Hidden = hidden
        End Sub
        
        Public Sub New(ByVal field As DataField)
            Me.New()
            Me.m_IsMirror = true
            Me.m_Name = (field.Name + "_Mirror")
            Me.m_Type = field.Type
            Me.m_Len = field.Len
            Me.m_Label = field.Label
            Me.m_ReadOnly = true
            Me.m_AllowNulls = field.AllowNulls
            Me.m_AllowQBE = field.AllowQBE
            Me.m_AllowSorting = field.AllowSorting
            Me.m_AllowLEV = field.AllowLEV
            Me.m_DataFormatString = field.DataFormatString
            Me.m_Aggregate = field.Aggregate
            If Not (Me.m_DataFormatString.Contains("{")) Then
                Me.m_DataFormatString = String.Format("{{0:{0}}}", Me.m_DataFormatString)
            End If
            field.m_AliasName = Me.m_Name
            Me.FormatOnClient = false
            field.FormatOnClient = true
            field.DataFormatString = String.Empty
            Me.m_Hidden = true
        End Sub
        
        Public Property Name() As String
            Get
                Return m_Name
            End Get
            Set
                m_Name = value
            End Set
        End Property
        
        Public Property AliasName() As String
            Get
                Return m_AliasName
            End Get
            Set
                m_AliasName = value
            End Set
        End Property
        
        Public Property Tag() As String
            Get
                Return m_Tag
            End Get
            Set
                m_Tag = value
            End Set
        End Property
        
        Public Property Type() As String
            Get
                Return m_Type
            End Get
            Set
                m_Type = value
            End Set
        End Property
        
        Public Property Len() As Integer
            Get
                Return m_Len
            End Get
            Set
                m_Len = value
            End Set
        End Property
        
        Public Property Label() As String
            Get
                Return m_Label
            End Get
            Set
                m_Label = value
            End Set
        End Property
        
        Public Property IsPrimaryKey() As Boolean
            Get
                Return m_IsPrimaryKey
            End Get
            Set
                m_IsPrimaryKey = value
            End Set
        End Property
        
        Public Property [ReadOnly]() As Boolean
            Get
                Return m_ReadOnly
            End Get
            Set
                m_ReadOnly = value
            End Set
        End Property
        
        Public ReadOnly Property DefaultValue() As String
            Get
                Return m_DefaultValue
            End Get
        End Property
        
        Public ReadOnly Property HasDefaultValue() As Boolean
            Get
                Return Not (String.IsNullOrEmpty(m_DefaultValue))
            End Get
        End Property
        
        Public Property HeaderText() As String
            Get
                Return m_HeaderText
            End Get
            Set
                m_HeaderText = value
                If (Not (String.IsNullOrEmpty(value)) AndAlso String.IsNullOrEmpty(m_Label)) Then
                    m_Label = value
                End If
            End Set
        End Property
        
        Public Property FooterText() As String
            Get
                Return m_FooterText
            End Get
            Set
                m_FooterText = value
            End Set
        End Property
        
        Public Property ToolTip() As String
            Get
                Return m_ToolTip
            End Get
            Set
                m_ToolTip = value
            End Set
        End Property
        
        Public Property Watermark() As String
            Get
                Return m_Watermark
            End Get
            Set
                m_Watermark = value
            End Set
        End Property
        
        Public Property Hidden() As Boolean
            Get
                Return m_Hidden
            End Get
            Set
                m_Hidden = value
            End Set
        End Property
        
        Public Property AllowQBE() As Boolean
            Get
                Return m_AllowQBE
            End Get
            Set
                m_AllowQBE = value
            End Set
        End Property
        
        Public Property AllowSorting() As Boolean
            Get
                Return m_AllowSorting
            End Get
            Set
                m_AllowSorting = value
            End Set
        End Property
        
        Public Property AllowLEV() As Boolean
            Get
                Return m_AllowLEV
            End Get
            Set
                m_AllowLEV = value
            End Set
        End Property
        
        Public Property DataFormatString() As String
            Get
                Return m_DataFormatString
            End Get
            Set
                m_DataFormatString = value
            End Set
        End Property
        
        Public Property Copy() As String
            Get
                Return m_Copy
            End Get
            Set
                m_Copy = value
            End Set
        End Property
        
        Public Property HyperlinkFormatString() As String
            Get
                Return m_HyperlinkFormatString
            End Get
            Set
                m_HyperlinkFormatString = value
            End Set
        End Property
        
        Public Property FormatOnClient() As Boolean
            Get
                Return m_FormatOnClient
            End Get
            Set
                m_FormatOnClient = value
            End Set
        End Property
        
        Public Property SourceFields() As String
            Get
                Return m_SourceFields
            End Get
            Set
                m_SourceFields = value
            End Set
        End Property
        
        Public Property CategoryIndex() As Integer
            Get
                Return m_CategoryIndex
            End Get
            Set
                m_CategoryIndex = value
            End Set
        End Property
        
        Public Property AllowNulls() As Boolean
            Get
                Return m_AllowNulls
            End Get
            Set
                m_AllowNulls = value
            End Set
        End Property
        
        Public Property Columns() As Integer
            Get
                Return m_Columns
            End Get
            Set
                m_Columns = value
            End Set
        End Property
        
        Public Property Rows() As Integer
            Get
                Return m_Rows
            End Get
            Set
                m_Rows = value
            End Set
        End Property
        
        Public Property OnDemand() As Boolean
            Get
                Return m_OnDemand
            End Get
            Set
                m_OnDemand = value
            End Set
        End Property
        
        Public Property Search() As FieldSearchMode
            Get
                Return m_Search
            End Get
            Set
                m_Search = value
            End Set
        End Property
        
        Public Overridable Property SearchOptions() As String
            Get
                Return m_SearchOptions
            End Get
            Set
                m_SearchOptions = value
            End Set
        End Property
        
        Public Property ItemsDataController() As String
            Get
                Return m_ItemsDataController
            End Get
            Set
                m_ItemsDataController = value
            End Set
        End Property
        
        Public Property ItemsDataView() As String
            Get
                Return m_ItemsDataView
            End Get
            Set
                m_ItemsDataView = value
            End Set
        End Property
        
        Public Property ItemsDataValueField() As String
            Get
                Return m_ItemsDataValueField
            End Get
            Set
                m_ItemsDataValueField = value
            End Set
        End Property
        
        Public Property ItemsDataTextField() As String
            Get
                Return m_ItemsDataTextField
            End Get
            Set
                m_ItemsDataTextField = value
            End Set
        End Property
        
        Public Property ItemsStyle() As String
            Get
                Return m_ItemsStyle
            End Get
            Set
                m_ItemsStyle = value
            End Set
        End Property
        
        Public Property ItemsPageSize() As Integer
            Get
                Return m_ItemsPageSize
            End Get
            Set
                m_ItemsPageSize = value
            End Set
        End Property
        
        Public Property ItemsNewDataView() As String
            Get
                Return m_ItemsNewDataView
            End Get
            Set
                m_ItemsNewDataView = value
            End Set
        End Property
        
        Public Property ItemsTargetController() As String
            Get
                Return m_ItemsTargetController
            End Get
            Set
                m_ItemsTargetController = value
            End Set
        End Property
        
        Public Property ItemsLetters() As Boolean
            Get
                Return m_ItemsLetters
            End Get
            Set
                m_ItemsLetters = value
            End Set
        End Property
        
        Public ReadOnly Property Items() As List(Of Object())
            Get
                Return m_Items
            End Get
        End Property
        
        Public Property Aggregate() As DataFieldAggregate
            Get
                Return m_Aggregate
            End Get
            Set
                m_Aggregate = value
            End Set
        End Property
        
        Public Property OnDemandHandler() As String
            Get
                Return m_OnDemandHandler
            End Get
            Set
                m_OnDemandHandler = value
            End Set
        End Property
        
        Public Property OnDemandStyle() As OnDemandDisplayStyle
            Get
                Return m_OnDemandStyle
            End Get
            Set
                m_OnDemandStyle = value
            End Set
        End Property
        
        Public Property TextMode() As TextInputMode
            Get
                Return m_TextMode
            End Get
            Set
                m_TextMode = value
            End Set
        End Property
        
        Public Property MaskType() As DataFieldMaskType
            Get
                Return m_MaskType
            End Get
            Set
                m_MaskType = value
            End Set
        End Property
        
        Public Property Mask() As String
            Get
                Return m_Mask
            End Get
            Set
                m_Mask = value
            End Set
        End Property
        
        Public ReadOnly Property ContextFields() As String
            Get
                Return m_ContextFields
            End Get
        End Property
        
        Public Property Formula() As String
            Get
                Return m_Formula
            End Get
            Set
                m_Formula = value
            End Set
        End Property
        
        Public Property ShowInSummary() As Boolean
            Get
                Return m_ShowInSummary
            End Get
            Set
                m_ShowInSummary = value
            End Set
        End Property
        
        Public ReadOnly Property IsMirror() As Boolean
            Get
                Return m_IsMirror
            End Get
        End Property
        
        Public Property HtmlEncode() As Boolean
            Get
                Return m_HtmlEncode
            End Get
            Set
                m_HtmlEncode = value
            End Set
        End Property
        
        Public Property AutoCompletePrefixLength() As Integer
            Get
                Return m_AutoCompletePrefixLength
            End Get
            Set
                m_AutoCompletePrefixLength = value
            End Set
        End Property
        
        Public Property Calculated() As Boolean
            Get
                Return m_Calculated
            End Get
            Set
                m_Calculated = value
            End Set
        End Property
        
        Public Property CausesCalculate() As Boolean
            Get
                Return m_CausesCalculate
            End Get
            Set
                m_CausesCalculate = value
            End Set
        End Property
        
        Public Property IsVirtual() As Boolean
            Get
                Return m_IsVirtual
            End Get
            Set
                m_IsVirtual = value
            End Set
        End Property
        
        Public Property Configuration() As String
            Get
                Return m_Configuration
            End Get
            Set
                m_Configuration = value
            End Set
        End Property
        
        Public Property Editor() As String
            Get
                Return m_Editor
            End Get
            Set
                m_Editor = value
            End Set
        End Property
        
        Public Property AutoSelect() As Boolean
            Get
                Return m_AutoSelect
            End Get
            Set
                m_AutoSelect = value
            End Set
        End Property
        
        Public Property SearchOnStart() As Boolean
            Get
                Return m_SearchOnStart
            End Get
            Set
                m_SearchOnStart = value
            End Set
        End Property
        
        Public Property ItemsDescription() As String
            Get
                Return m_ItemsDescription
            End Get
            Set
                m_ItemsDescription = value
            End Set
        End Property
        
        Public Property DataViewController() As String
            Get
                Return m_DataViewController
            End Get
            Set
                m_DataViewController = value
            End Set
        End Property
        
        Public Property DataViewId() As String
            Get
                Return m_DataViewId
            End Get
            Set
                m_DataViewId = value
            End Set
        End Property
        
        Public Property DataViewFilterSource() As String
            Get
                Return m_DataViewFilterSource
            End Get
            Set
                m_DataViewFilterSource = value
            End Set
        End Property
        
        Public Property DataViewFilterFields() As String
            Get
                Return m_DataViewFilterFields
            End Get
            Set
                m_DataViewFilterFields = value
            End Set
        End Property
        
        Public Property DataViewShowInSummary() As Boolean
            Get
                Return m_DataViewShowInSummary
            End Get
            Set
                m_DataViewShowInSummary = value
            End Set
        End Property
        
        Public Property DataViewShowActionBar() As Boolean
            Get
                Return m_DataViewShowActionBar
            End Get
            Set
                m_DataViewShowActionBar = value
            End Set
        End Property
        
        Public Property DataViewShowActionButtons() As String
            Get
                Return m_DataViewShowActionButtons
            End Get
            Set
                m_DataViewShowActionButtons = value
            End Set
        End Property
        
        Public Property DataViewShowDescription() As Boolean
            Get
                Return m_DataViewShowDescription
            End Get
            Set
                m_DataViewShowDescription = value
            End Set
        End Property
        
        Public Property DataViewShowViewSelector() As Boolean
            Get
                Return m_DataViewShowViewSelector
            End Get
            Set
                m_DataViewShowViewSelector = value
            End Set
        End Property
        
        Public Property DataViewShowModalForms() As Boolean
            Get
                Return m_DataViewShowModalForms
            End Get
            Set
                m_DataViewShowModalForms = value
            End Set
        End Property
        
        Public Property DataViewSearchByFirstLetter() As Boolean
            Get
                Return m_DataViewSearchByFirstLetter
            End Get
            Set
                m_DataViewSearchByFirstLetter = value
            End Set
        End Property
        
        Public Property DataViewSearchOnStart() As Boolean
            Get
                Return m_DataViewSearchOnStart
            End Get
            Set
                m_DataViewSearchOnStart = value
            End Set
        End Property
        
        Public Property DataViewPageSize() As Integer
            Get
                Return m_DataViewPageSize
            End Get
            Set
                m_DataViewPageSize = value
            End Set
        End Property
        
        Public Property DataViewMultiSelect() As Boolean
            Get
                Return m_DataViewMultiSelect
            End Get
            Set
                m_DataViewMultiSelect = value
            End Set
        End Property
        
        Public Property DataViewShowPager() As String
            Get
                Return m_DataViewShowPager
            End Get
            Set
                m_DataViewShowPager = value
            End Set
        End Property
        
        Public Property DataViewShowPageSize() As Boolean
            Get
                Return m_DataViewShowPageSize
            End Get
            Set
                m_DataViewShowPageSize = value
            End Set
        End Property
        
        Public Property DataViewShowSearchBar() As Boolean
            Get
                Return m_DataViewShowSearchBar
            End Get
            Set
                m_DataViewShowSearchBar = value
            End Set
        End Property
        
        Public Property DataViewShowQuickFind() As Boolean
            Get
                Return m_DataViewShowQuickFind
            End Get
            Set
                m_DataViewShowQuickFind = value
            End Set
        End Property
        
        Public Property DataViewShowRowNumber() As Boolean
            Get
                Return m_DataViewShowRowNumber
            End Get
            Set
                m_DataViewShowRowNumber = value
            End Set
        End Property
        
        Public Property DataViewAutoSelectFirstRow() As Boolean
            Get
                Return m_DataViewAutoSelectFirstRow
            End Get
            Set
                m_DataViewAutoSelectFirstRow = value
            End Set
        End Property
        
        Public Property DataViewAutoHighlightFirstRow() As Boolean
            Get
                Return m_DataViewAutoHighlightFirstRow
            End Get
            Set
                m_DataViewAutoHighlightFirstRow = value
            End Set
        End Property
        
        Public Function SelectExpression() As String
            Return m_SelectExpression
        End Function
        
        Public Sub NormalizeDataFormatString()
            If Not (String.IsNullOrEmpty(m_DataFormatString)) Then
                Dim fmt = m_DataFormatString
                If Not (fmt.Contains("{")) Then
                    m_DataFormatString = String.Format("{{0:{0}}}", fmt)
                End If
            Else
                If (m_Type = "DateTime") Then
                    m_DataFormatString = "{0:d}"
                End If
            End If
        End Sub
        
        Public Function ExpressionName() As String
            If IsMirror Then
                Return Name.Substring(0, (Name.Length - "_Mirror".Length))
            End If
            Return Name
        End Function
        
        Public Function SupportsStaticItems() As Boolean
            Return (Not (String.IsNullOrEmpty(ItemsDataController)) AndAlso Not (((ItemsStyle = "AutoComplete") OrElse (ItemsStyle = "Lookup"))))
        End Function
        
        Public Function IsMatchedByName(ByVal sample As String) As Boolean
            Dim headerText = Me.HeaderText
            If String.IsNullOrEmpty(headerText) Then
                headerText = Me.Label
            End If
            If String.IsNullOrEmpty(headerText) Then
                headerText = Me.Name
            End If
            headerText = headerText.Replace(" ", String.Empty)
            Return headerText.StartsWith(sample.Replace(" ", String.Empty), StringComparison.CurrentCultureIgnoreCase)
        End Function
        
        Public Overrides Function ToString() As String
            If Not (String.IsNullOrEmpty(Formula)) Then
                Return String.Format("{0} as {1}; SQL: {2}", Name, Type, Formula)
            Else
                Return String.Format("{0} as {1}", Name, Type)
            End If
        End Function
        
        Public Function IsTagged(ByVal tag As String) As Boolean
            If String.IsNullOrEmpty(Me.Tag) Then
                Return false
            End If
            Return Me.Tag.Contains(tag)
        End Function
    End Class
End Namespace
