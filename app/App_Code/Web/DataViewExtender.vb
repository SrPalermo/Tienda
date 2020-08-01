Imports Norwindt.Data
Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Text
Imports System.Web
Imports System.Web.Security
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls
Imports System.Web.UI.WebControls.WebParts

Namespace Norwindt.Web
    
    Public Enum DataViewSelectionMode
        
        [Single]
        
        Multiple
    End Enum
    
    Public Enum ActionButtonLocation
        
        [Auto]
        
        None
        
        Top
        
        Bottom
        
        TopAndBottom
    End Enum
    
    Public Enum PagerLocation
        
        None
        
        Top
        
        Bottom
        
        TopAndBottom
    End Enum
    
    Public Enum AutoHideMode
        
        [Nothing]
        
        Self
        
        Container
    End Enum
    
    Public Class FieldFilter
        
        Private m_FieldName As String
        
        Private m_Operation As RowFilterOperation
        
        Private m_Value As Object
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal fieldName As String, ByVal operation As RowFilterOperation)
            Me.New(fieldName, operation, Nothing)
        End Sub
        
        Public Sub New(ByVal fieldName As String, ByVal operation As RowFilterOperation, ByVal value As Object)
            MyBase.New
            Me.FieldName = fieldName
            Me.Operation = operation
            Me.Value = value
        End Sub
        
        Public Property FieldName() As String
            Get
                Return m_FieldName
            End Get
            Set
                m_FieldName = value
            End Set
        End Property
        
        Public Property Operation() As RowFilterOperation
            Get
                Return m_Operation
            End Get
            Set
                m_Operation = value
            End Set
        End Property
        
        Public Property Value() As Object
            Get
                Return m_Value
            End Get
            Set
                If ((((Not (value) Is Nothing) AndAlso TypeOf value Is [String]) AndAlso (Operation = RowFilterOperation.Like)) AndAlso Not (CType(value,String).Contains("%"))) Then
                    m_Value = String.Format("%{0}%", value)
                Else
                    m_Value = value
                End If
            End Set
        End Property
    End Class
    
    Partial Public Class DataViewExtender
        Inherits DataViewExtenderBase
    End Class
    
    <TargetControlType(GetType(Panel)),  _
     TargetControlType(GetType(HtmlContainerControl))>  _
    Public Class DataViewExtenderBase
        Inherits AquariumExtenderBase
        
        Private m_AutoHide As AutoHideMode
        
        Private m_Controller As String
        
        Private m_View As String
        
        Private m_PageSize As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ShowActionBar As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ShowActionButtons As ActionButtonLocation
        
        Private m_ShowSearchBar As Boolean
        
        Private m_ShowModalForms As Boolean
        
        Private m_FilterSource As String
        
        Private m_FilterFields As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_VisibleWhen As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Roles As String
        
        Private m_StartCommandName As String
        
        Private m_StartCommandArgument As String
        
        Private m_SelectedValue As String
        
        Private m_SelectionMode As DataViewSelectionMode
        
        Private m_ShowInSummary As Boolean
        
        Private m_ShowDescription As Boolean
        
        Private m_ShowViewSelector As Boolean
        
        Private m_SearchOnStart As Boolean
        
        Private m_SummaryFieldCount As Integer
        
        Private m_Tag As String
        
        Private m_ShowDetailsInListMode As Boolean
        
        Private m_ShowPager As PagerLocation
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ShowPageSize As Boolean
        
        Private m_Enabled As Boolean = true
        
        Private m_TabIndex As Integer
        
        Private m_LookupMode As Boolean
        
        Private m_LookupValue As String
        
        Private m_LookupText As String
        
        Private m_LookupPostBackExpression As String
        
        Private m_AllowCreateLookupItems As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ShowRowNumber As Boolean
        
        Private m_ShowQuickFind As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SearchByFirstLetter As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AutoSelectFirstRow As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AutoHighlightFirstRow As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RefreshInterval As Integer
        
        Public Sub New()
            MyBase.New("Web.DataView")
            Me.m_PageSize = 10
            Me.m_ShowActionBar = true
            Me.m_ShowActionButtons = ActionButtonLocation.TopAndBottom
            Me.m_ShowDetailsInListMode = true
            Me.m_ShowPager = PagerLocation.Bottom
            Me.m_ShowPageSize = true
            Me.m_ShowSearchBar = true
            m_ShowDescription = true
            m_ShowViewSelector = true
            m_SummaryFieldCount = 5
            m_ShowQuickFind = true
        End Sub
        
        <System.ComponentModel.Description("Specifies user interface element that will be hidden if data view can be automati"& _ 
            "cally hidden."),  _
         System.ComponentModel.DefaultValue(AutoHideMode.Nothing)>  _
        Public Property AutoHide() As AutoHideMode
            Get
                Return m_AutoHide
            End Get
            Set
                m_AutoHide = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The name of the data controller. Controllers are stored in the ""~/Controllers"" fo"& _ 
            "lder of your project. Do not include the file extension.")>  _
        Public Property Controller() As String
            Get
                Return m_Controller
            End Get
            Set
                m_Controller = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The name of the startup view in the data controller. The first view is displayed "& _ 
            "if the property is left blank.")>  _
        Public Property View() As String
            Get
                Return m_View
            End Get
            Set
                m_View = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The number of rows displayed by grid views of the data controller."),  _
         System.ComponentModel.DefaultValue(10)>  _
        Public Property PageSize() As Integer
            Get
                Return m_PageSize
            End Get
            Set
                m_PageSize = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specifies if the action bar is displayed above the views of the data controller."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowActionBar() As Boolean
            Get
                Return m_ShowActionBar
            End Get
            Set
                m_ShowActionBar = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specifies if the action buttons are displayed in the form views of the data contr"& _ 
            "oller."),  _
         System.ComponentModel.DefaultValue(ActionButtonLocation.TopAndBottom)>  _
        Public Property ShowActionButtons() As ActionButtonLocation
            Get
                Return m_ShowActionButtons
            End Get
            Set
                m_ShowActionButtons = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specifies if the search bar is enabled in the views of the data controller."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowSearchBar() As Boolean
            Get
                Return m_ShowSearchBar
            End Get
            Set
                m_ShowSearchBar = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specifies that form views are displayed as modal popups. The default form renderi"& _ 
            "ng mode is in-place."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowModalForms() As Boolean
            Get
                Return m_ShowModalForms
            End Get
            Set
                m_ShowModalForms = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Defines the external source of filtering values. This may be the name of URL para"& _ 
            "meter or DHTML element in the page. Data view extender will automatically recogn"& _ 
            "ize if the DHTML element is also extended and will interface with the client-sid"& _ 
            "e extender object.")>  _
        Public Property FilterSource() As String
            Get
                Return m_FilterSource
            End Get
            Set
                m_FilterSource = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specify the field(s) of the data controller that shall be filtered with the value"& _ 
            "s from the source defined by the FilterSource property.")>  _
        Public Property FilterFields() As String
            Get
                Return m_FilterFields
            End Get
            Set
                m_FilterFields = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The JavaScript expression that must evaluate as true if the data view is visible."& _ 
            "")>  _
        Public Property VisibleWhen() As String
            Get
                Return m_VisibleWhen
            End Get
            Set
                m_VisibleWhen = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The comma-separated list of roles allowed to see the data view on the page.")>  _
        Public Property Roles() As String
            Get
                Return m_Roles
            End Get
            Set
                m_Roles = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specify a command that must be executed when the data view is instantiated.")>  _
        Public Property StartCommandName() As String
            Get
                Return m_StartCommandName
            End Get
            Set
                m_StartCommandName = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specify an argument of a command that must be executed when the data view is inst"& _ 
            "antiated.")>  _
        Public Property StartCommandArgument() As String
            Get
                Return m_StartCommandArgument
            End Get
            Set
                m_StartCommandArgument = value
            End Set
        End Property
        
        <System.ComponentModel.Browsable(false)>  _
        Public Property SelectedValue() As String
            Get
                If (m_SelectedValue Is Nothing) Then
                    m_SelectedValue = Page.Request.Params(String.Format("{0}_{1}_SelectedValue", ClientID, Controller))
                End If
                Return m_SelectedValue
            End Get
            Set
                m_SelectedValue = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The selection mode for the data view."),  _
         System.ComponentModel.DefaultValue(DataViewSelectionMode.Single)>  _
        Public Property SelectionMode() As DataViewSelectionMode
            Get
                Return m_SelectionMode
            End Get
            Set
                m_SelectionMode = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The data view is presented in the page summary."),  _
         System.ComponentModel.DefaultValue(false)>  _
        Public Property ShowInSummary() As Boolean
            Get
                Return m_ShowInSummary
            End Get
            Set
                m_ShowInSummary = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The view descripition is displayed at the top the data extender target."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowDescription() As Boolean
            Get
                Return m_ShowDescription
            End Get
            Set
                m_ShowDescription = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The view selector is displayed in the action bar."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowViewSelector() As Boolean
            Get
                Return m_ShowViewSelector
            End Get
            Set
                m_ShowViewSelector = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Display grid view in search mode and do not retreive the data when view is displa"& _ 
            "yed for the first time."),  _
         System.ComponentModel.DefaultValue(false)>  _
        Public Property SearchOnStart() As Boolean
            Get
                Return m_SearchOnStart
            End Get
            Set
                m_SearchOnStart = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The maximum number of fields that can be contributed to the summary."),  _
         System.ComponentModel.DefaultValue(5)>  _
        Public Property SummaryFieldCount() As Integer
            Get
                Return m_SummaryFieldCount
            End Get
            Set
                m_SummaryFieldCount = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The identifying string passed from the client to server. Use it to filter actions"& _ 
            " and to program business rules.")>  _
        Public Property Tag() As String
            Get
                Return m_Tag
            End Get
            Set
                m_Tag = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The identifying string passed from the client to server. Use it to filter actions"& _ 
            " and to program business rules. Separate multiple tags with comma or space.")>  _
        Public Property Tags() As String
            Get
                Return m_Tag
            End Get
            Set
                m_Tag = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The child data views are hidden if the active view is displaying a list of record"& _ 
            "s."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowDetailsInListMode() As Boolean
            Get
                Return m_ShowDetailsInListMode
            End Get
            Set
                m_ShowDetailsInListMode = value
            End Set
        End Property
        
        <System.ComponentModel.Description("Specifies if the pager is displayed at the top or/and bottom of the views."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowPager() As PagerLocation
            Get
                Return m_ShowPager
            End Get
            Set
                m_ShowPager = value
            End Set
        End Property
        
        <System.ComponentModel.Description("The page size information is displayed in the pager area of data views."),  _
         System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowPageSize() As Boolean
            Get
                Return m_ShowPageSize
            End Get
            Set
                m_ShowPageSize = value
            End Set
        End Property
        
        <System.ComponentModel.Browsable(false)>  _
        Public Property Enabled() As Boolean
            Get
                Return m_Enabled
            End Get
            Set
                m_Enabled = value
            End Set
        End Property
        
        <System.ComponentModel.Browsable(false)>  _
        Public Property TabIndex() As Integer
            Get
                Return m_TabIndex
            End Get
            Set
                m_TabIndex = value
            End Set
        End Property
        
        <System.ComponentModel.Browsable(false)>  _
        Public Property LookupValue() As String
            Get
                Return m_LookupValue
            End Get
            Set
                m_LookupValue = value
                m_LookupMode = true
            End Set
        End Property
        
        <System.ComponentModel.Browsable(false)>  _
        Public Property LookupText() As String
            Get
                Return m_LookupText
            End Get
            Set
                m_LookupText = value
            End Set
        End Property
        
        <System.ComponentModel.Browsable(false)>  _
        Public Property LookupPostBackExpression() As String
            Get
                Return m_LookupPostBackExpression
            End Get
            Set
                m_LookupPostBackExpression = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(true),  _
         System.ComponentModel.Browsable(false)>  _
        Public Property AllowCreateLookupItems() As Boolean
            Get
                Return m_AllowCreateLookupItems
            End Get
            Set
                m_AllowCreateLookupItems = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(false)>  _
        Public Property ShowRowNumber() As Boolean
            Get
                Return m_ShowRowNumber
            End Get
            Set
                m_ShowRowNumber = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(true)>  _
        Public Property ShowQuickFind() As Boolean
            Get
                Return m_ShowQuickFind
            End Get
            Set
                m_ShowQuickFind = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(false)>  _
        Public Property SearchByFirstLetter() As Boolean
            Get
                Return m_SearchByFirstLetter
            End Get
            Set
                m_SearchByFirstLetter = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(false)>  _
        Public Property AutoSelectFirstRow() As Boolean
            Get
                Return m_AutoSelectFirstRow
            End Get
            Set
                m_AutoSelectFirstRow = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(false)>  _
        Public Property AutoHighlightFirstRow() As Boolean
            Get
                Return m_AutoHighlightFirstRow
            End Get
            Set
                m_AutoHighlightFirstRow = value
            End Set
        End Property
        
        <System.ComponentModel.DefaultValue(0)>  _
        Public Property RefreshInterval() As Integer
            Get
                Return m_RefreshInterval
            End Get
            Set
                m_RefreshInterval = value
            End Set
        End Property
        
        Protected Overrides Sub ConfigureDescriptor(ByVal descriptor As ScriptBehaviorDescriptor)
            Page.ClientScript.RegisterHiddenField(String.Format("{0}_{1}_SelectedValue", ClientID, Controller), SelectedValue)
            descriptor.AddProperty("appId", Me.TargetControlID)
            descriptor.AddProperty("controller", Me.Controller)
            descriptor.AddProperty("viewId", Me.View)
            descriptor.AddProperty("pageSize", Me.PageSize)
            If Not (ShowActionBar) Then
                descriptor.AddProperty("showActionBar", false)
            End If
            If Not ((ShowActionButtons = ActionButtonLocation.TopAndBottom)) Then
                descriptor.AddProperty("showActionButtons", ShowActionButtons.ToString())
            End If
            If Not ((ShowPager = PagerLocation.Bottom)) Then
                descriptor.AddProperty("showPager", ShowPager.ToString())
            End If
            If Not (ShowPageSize) Then
                descriptor.AddProperty("showPageSize", false)
            End If
            If Not (ShowDetailsInListMode) Then
                descriptor.AddProperty("showDetailsInListMode", false)
            End If
            If ShowSearchBar Then
                descriptor.AddProperty("showSearchBar", true)
            End If
            If ShowModalForms Then
                descriptor.AddProperty("showModalForms", true)
            End If
            If SearchOnStart Then
                descriptor.AddProperty("searchOnStart", true)
            End If
            If m_LookupMode Then
                descriptor.AddProperty("mode", "Lookup")
                descriptor.AddProperty("lookupValue", LookupValue)
                descriptor.AddProperty("lookupText", LookupText)
                If Not (String.IsNullOrEmpty(LookupPostBackExpression)) Then
                    descriptor.AddProperty("lookupPostBackExpression", LookupPostBackExpression)
                End If
                If AllowCreateLookupItems Then
                    descriptor.AddProperty("newViewId", Norwindt.Data.Controller.LookupActionArgument(Controller, "New"))
                End If
            End If
            If Not (String.IsNullOrEmpty(FilterSource)) Then
                Dim source = NamingContainer.FindControl(FilterSource)
                If (Not (source) Is Nothing) Then
                    descriptor.AddProperty("filterSource", source.ClientID)
                    If TypeOf source Is DataViewExtender Then
                        descriptor.AddProperty("appFilterSource", CType(source,DataViewExtender).TargetControlID)
                    End If
                Else
                    descriptor.AddProperty("filterSource", Me.FilterSource)
                End If
            End If
            If Not (String.IsNullOrEmpty(FilterFields)) Then
                descriptor.AddProperty("filterFields", Me.FilterFields)
            End If
            descriptor.AddProperty("cookie", Guid.NewGuid().ToString())
            If Not (String.IsNullOrEmpty(StartCommandName)) Then
                descriptor.AddProperty("startCommandName", StartCommandName)
            End If
            If Not (String.IsNullOrEmpty(StartCommandArgument)) Then
                descriptor.AddProperty("startCommandArgument", StartCommandArgument)
            End If
            If (SelectionMode = DataViewSelectionMode.Multiple) Then
                descriptor.AddProperty("selectionMode", "Multiple")
            End If
            If Not (Enabled) Then
                descriptor.AddProperty("enabled", false)
            End If
            If (TabIndex > 0) Then
                descriptor.AddProperty("tabIndex", TabIndex)
            End If
            If ShowInSummary Then
                descriptor.AddProperty("showInSummary", "true")
            End If
            If Not (ShowDescription) Then
                descriptor.AddProperty("showDescription", false)
            End If
            If Not (ShowViewSelector) Then
                descriptor.AddProperty("showViewSelector", false)
            End If
            If Not (String.IsNullOrEmpty(Tag)) Then
                descriptor.AddProperty("tag", Tag)
            End If
            If Not ((SummaryFieldCount = 5)) Then
                descriptor.AddProperty("summaryFieldCount", SummaryFieldCount)
            End If
            If SearchByFirstLetter Then
                descriptor.AddProperty("showFirstLetters", true)
            End If
            If AutoSelectFirstRow Then
                descriptor.AddProperty("autoSelectFirstRow", true)
            End If
            If AutoHighlightFirstRow Then
                descriptor.AddProperty("autoHighlightFirstRow", true)
            End If
            If (RefreshInterval > 0) Then
                descriptor.AddProperty("refreshInterval", RefreshInterval)
            End If
            If Not (ShowQuickFind) Then
                descriptor.AddProperty("showQuickFind", false)
            End If
            If ShowRowNumber Then
                descriptor.AddProperty("showRowNumber", true)
            End If
            If Not ((AutoHide = AutoHideMode.Nothing)) Then
                descriptor.AddProperty("autoHide", Convert.ToInt16(AutoHide))
            End If
            If Properties.ContainsKey("StartupFilter") Then
                descriptor.AddProperty("startupFilter", Properties("StartupFilter"))
            End If
            Dim visibleWhenExpression = VisibleWhen
            If (Not (String.IsNullOrEmpty(Roles)) AndAlso Not (DataControllerBase.UserIsInRole(Roles))) Then
                If String.IsNullOrEmpty(visibleWhenExpression) Then
                    visibleWhenExpression = "false"
                Else
                    visibleWhenExpression = String.Format("({0}) && false", visibleWhenExpression)
                End If
            End If
            If Not (String.IsNullOrEmpty(visibleWhenExpression)) Then
                descriptor.AddProperty("visibleWhen", visibleWhenExpression)
            End If
        End Sub
        
        Protected Overrides Sub ConfigureScripts(ByVal scripts As List(Of ScriptReference))
        End Sub
        
        Public Overloads Sub AssignFilter(ByVal filter As List(Of FieldFilter))
            Me.AssignFilter(filter.ToArray())
        End Sub
        
        Public Overloads Sub AssignStartupFilter(ByVal filter As List(Of FieldFilter))
            Me.AssignStartupFilter(filter.ToArray())
        End Sub
        
        Private Function CreateFilterExpressions(ByVal filter() As FieldFilter) As SortedDictionary(Of String, String)
            'prepare a list of filter expressions
            Dim list = New SortedDictionary(Of String, String)()
            For Each ff in filter
                Dim filterExpression As String = Nothing
                If Not (list.TryGetValue(ff.FieldName, filterExpression)) Then
                    filterExpression = String.Empty
                Else
                    filterExpression = (filterExpression + "\0")
                End If
                If TypeOf ff.Value Is Array Then
                    Dim values = CType(ff.Value,Object())
                    If (ff.Operation = RowFilterOperation.Between) Then
                        ff.Value = String.Format("{0}$and${1}", DataControllerBase.ValueToString(values(0)), DataControllerBase.ValueToString(values(1)))
                    Else
                        If ((ff.Operation = RowFilterOperation.Includes) OrElse (ff.Operation = RowFilterOperation.DoesNotInclude)) Then
                            Dim svb = New StringBuilder()
                            For Each o in values
                                If (svb.Length > 0) Then
                                    svb.Append("$or$")
                                End If
                                svb.Append(DataControllerBase.ValueToString(o))
                            Next
                            ff.Value = svb.ToString()
                        End If
                    End If
                Else
                    ff.Value = DataControllerBase.ValueToString(ff.Value)
                End If
                If (ff.Operation = RowFilterOperation.None) Then
                    filterExpression = Nothing
                Else
                    filterExpression = (filterExpression  _
                                + (RowFilterAttribute.ComparisonOperations(Convert.ToInt32(ff.Operation)) + Convert.ToString(ff.Value).Replace("'", "\'")))
                End If
                list(ff.FieldName) = filterExpression
            Next
            Return list
        End Function
        
        Public Overloads Sub AssignFilter(ByVal filter() As FieldFilter)
            Dim list = CreateFilterExpressions(filter)
            'create a filter
            Dim sb = New StringBuilder()
            sb.AppendFormat("var dv = Web.DataView.find('{0}');dv.beginFilter();var f;", Me.ID)
            For Each fieldName in list.Keys
                If String.IsNullOrEmpty(list(fieldName)) Then
                    sb.AppendFormat("f=dv.findField('{0}');if(f)dv.removeFromFilter(f);", fieldName)
                Else
                    sb.AppendFormat("f=dv.findField('{0}');if(f)dv.applyFilter(f,':', '{1}');", fieldName, list(fieldName))
                End If
            Next
            sb.Append("dv.endFilter();")
            ScriptManager.RegisterClientScriptBlock(Page, GetType(DataViewExtender), ("AsyncPostBackScript" + Me.ID), sb.ToString(), true)
        End Sub
        
        Public Overloads Sub AssignStartupFilter(ByVal filter() As FieldFilter)
            Dim list = CreateFilterExpressions(filter)
            Dim dataViewFilter = New List(Of String)()
            For Each fieldName in list.Keys
                dataViewFilter.Add(String.Format("{0}:{1}", fieldName, list(fieldName)))
            Next
            Properties("StartupFilter") = dataViewFilter
        End Sub
    End Class
End Namespace
