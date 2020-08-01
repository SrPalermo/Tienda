Imports Newtonsoft.Json
Imports Norwindt.Data
Imports Norwindt.Web.Design
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Configuration
Imports System.Data
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.HtmlControls
Imports System.Web.UI.WebControls
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Web
    
    <Designer(GetType(ControllerDataSourceDesigner)),  _
     ToolboxData("<{0}:ControllerDataSource runat=""server""></{0}:ControllerDataSource>"),  _
     PersistChildren(false),  _
     DefaultProperty("DataController"),  _
     ParseChildren(true)>  _
    Public Class ControllerDataSource
        Inherits DataSourceControl
        
        Private m_View As ControllerDataSourceView
        
        Private m_PageRequestParameterName As String
        
        Public Sub New()
            MyBase.New()
        End Sub
        
        Public Property DataController() As String
            Get
                Return GetView().DataController
            End Get
            Set
                GetView().DataController = value
            End Set
        End Property
        
        Public Property DataView() As String
            Get
                Return GetView().DataView
            End Get
            Set
                GetView().DataView = value
            End Set
        End Property
        
        Public Property PageRequestParameterName() As String
            Get
                Return m_PageRequestParameterName
            End Get
            Set
                m_PageRequestParameterName = value
            End Set
        End Property
        
        <MergableProperty(false),  _
         DefaultValue(""),  _
         Editor("System.Web.UI.Design.WebControls.ParameterCollectionEditor, System.Design, Versio"& _ 
            "n=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", GetType(System.Drawing.Design.UITypeEditor)),  _
         PersistenceMode(PersistenceMode.InnerProperty)>  _
        Public ReadOnly Property FilterParameters() As ParameterCollection
            Get
                Return GetView().FilterParameters
            End Get
        End Property
        
        Protected Overloads Function GetView() As ControllerDataSourceView
            Return CType(GetView(String.Empty),ControllerDataSourceView)
        End Function
        
        Protected Overrides Sub OnInit(ByVal e As EventArgs)
            MyBase.OnInit(e)
            AddHandler Me.Page.LoadComplete, AddressOf Me.PageLoadComplete
        End Sub
        
        Private Sub PageLoadComplete(ByVal sender As Object, ByVal e As EventArgs)
            FilterParameters.UpdateValues(Me.Context, Me)
        End Sub
        
        Protected Overrides Function GetViewNames() As ICollection
            Return New String() {ControllerDataSourceView.DefaultViewName}
        End Function
        
        Protected Overloads Overrides Function GetView(ByVal viewName As String) As DataSourceView
            If (m_View Is Nothing) Then
                m_View = New ControllerDataSourceView(Me, String.Empty)
                If IsTrackingViewState Then
                    CType(m_View,IStateManager).TrackViewState()
                End If
            End If
            Return m_View
        End Function
        
        Protected Overrides Sub LoadViewState(ByVal savedState As Object)
            Dim pair = CType(savedState,Pair)
            If (savedState Is Nothing) Then
                MyBase.LoadViewState(Nothing)
            Else
                MyBase.LoadViewState(pair.First)
                If (Not (pair.Second) Is Nothing) Then
                    CType(GetView(),IStateManager).LoadViewState(pair.Second)
                End If
            End If
        End Sub
        
        Protected Overrides Sub TrackViewState()
            MyBase.TrackViewState()
            If (Not (m_View) Is Nothing) Then
                CType(m_View,IStateManager).TrackViewState()
            End If
        End Sub
        
        Protected Overrides Function SaveViewState() As Object
            Dim pair = New Pair()
            pair.First = MyBase.SaveViewState()
            If (Not (m_View) Is Nothing) Then
                pair.Second = CType(m_View,IStateManager).SaveViewState()
            End If
            If ((pair.First Is Nothing) AndAlso (pair.Second Is Nothing)) Then
                Return Nothing
            End If
            Return pair
        End Function
    End Class
    
    Public Class ControllerDataSourceView
        Inherits DataSourceView
        Implements IStateManager
        
        Public Shared DefaultViewName As String = "DataControllerView"
        
        Private m_DataController As String
        
        Private m_DataView As String
        
        Private m_Tracking As Boolean
        
        Private m_Owner As ControllerDataSource
        
        Private m_FilterParameters As ParameterCollection
        
        Public Sub New(ByVal owner As IDataSource, ByVal viewName As String)
            MyBase.New(owner, viewName)
            m_Owner = CType(owner,ControllerDataSource)
        End Sub
        
        Public Property DataController() As String
            Get
                Return m_DataController
            End Get
            Set
                m_DataController = value
            End Set
        End Property
        
        Public Property DataView() As String
            Get
                Return m_DataView
            End Get
            Set
                m_DataView = value
            End Set
        End Property
        
        Public ReadOnly Property FilterParameters() As ParameterCollection
            Get
                If (m_FilterParameters Is Nothing) Then
                    m_FilterParameters = New ParameterCollection()
                    AddHandler m_FilterParameters.ParametersChanged, AddressOf Me._filterParametersParametersChanged
                End If
                Return m_FilterParameters
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanRetrieveTotalRowCount() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanSort() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanPage() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanInsert() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanUpdate() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanDelete() As Boolean
            Get
                Return true
            End Get
        End Property
        
        ReadOnly Property IStateManager_IsTrackingViewState() As Boolean Implements IStateManager.IsTrackingViewState
            Get
                Return m_Tracking
            End Get
        End Property
        
        Private Sub _filterParametersParametersChanged(ByVal sender As Object, ByVal e As EventArgs)
            OnDataSourceViewChanged(EventArgs.Empty)
        End Sub
        
        Protected Overrides Function ExecuteSelect(ByVal arguments As DataSourceSelectArguments) As IEnumerable
            Dim pageSize = Int32.MaxValue
            If (arguments.MaximumRows > 0) Then
                pageSize = arguments.MaximumRows
            End If
            Dim pageIndex = (arguments.StartRowIndex / pageSize)
            Dim request As PageRequest = Nothing
            If Not (String.IsNullOrEmpty(m_Owner.PageRequestParameterName)) Then
                Dim r = HttpContext.Current.Request.Params(m_Owner.PageRequestParameterName)
                If Not (String.IsNullOrEmpty(r)) Then
                    request = JsonConvert.DeserializeObject(Of PageRequest)(r)
                    request.PageIndex = pageIndex
                    request.PageSize = pageSize
                    request.View = m_Owner.DataView
                End If
            End If
            If (request Is Nothing) Then
                request = New PageRequest(pageIndex, pageSize, arguments.SortExpression, Nothing)
                Dim filter = New List(Of String)()
                Dim filterValues = FilterParameters.GetValues(HttpContext.Current, m_Owner)
                For Each p As Parameter in FilterParameters
                    Dim v = filterValues(p.Name)
                    If (Not (v) Is Nothing) Then
                        Dim query = (p.Name + ":")
                        If ((p.DbType = DbType.Object) OrElse (p.DbType = DbType.String)) Then
                            For Each s in Convert.ToString(v).Split(Global.Microsoft.VisualBasic.ChrW(44), Global.Microsoft.VisualBasic.ChrW(59))
                                Dim q = Controller.ConvertSampleToQuery(s)
                                If Not (String.IsNullOrEmpty(q)) Then
                                    query = (query + q)
                                End If
                            Next
                        Else
                            query = String.Format("{0}={1}", query, v)
                        End If
                        filter.Add(query)
                    End If
                Next
                request.Filter = filter.ToArray()
            End If
            request.RequiresMetaData = true
            request.RequiresRowCount = arguments.RetrieveTotalRowCount
            Dim page = ControllerFactory.CreateDataController().GetPage(m_DataController, m_DataView, request)
            If arguments.RetrieveTotalRowCount Then
                arguments.TotalRowCount = page.TotalRowCount
            End If
            Return page.ToDataTable().DefaultView
        End Function
        
        Protected Overrides Function ExecuteUpdate(ByVal keys As IDictionary, ByVal values As IDictionary, ByVal oldValues As IDictionary) As Integer
            Dim fieldValues = New FieldValueDictionary()
            fieldValues.Assign(oldValues, false)
            fieldValues.Assign(keys, false)
            fieldValues.Assign(keys, true)
            fieldValues.Assign(values, true)
            Return ExecuteAction("Edit", "Update", fieldValues)
        End Function
        
        Protected Overrides Function ExecuteDelete(ByVal keys As IDictionary, ByVal oldValues As IDictionary) As Integer
            Dim fieldValues = New FieldValueDictionary()
            fieldValues.Assign(keys, false)
            fieldValues.Assign(keys, true)
            fieldValues.Assign(oldValues, true)
            Return ExecuteAction("Select", "Delete", fieldValues)
        End Function
        
        Protected Overrides Function ExecuteInsert(ByVal values As IDictionary) As Integer
            Dim fieldValues = New FieldValueDictionary()
            fieldValues.Assign(values, true)
            Return ExecuteAction("New", "Insert", fieldValues)
        End Function
        
        Protected Function ExecuteAction(ByVal lastCommandName As String, ByVal commandName As String, ByVal fieldValues As FieldValueDictionary) As Integer
            Dim args = New ActionArgs()
            args.Controller = DataController
            args.View = DataView
            args.LastCommandName = lastCommandName
            args.CommandName = commandName
            args.Values = fieldValues.Values.ToArray()
            Dim result = ControllerFactory.CreateDataController().Execute(DataController, DataView, args)
            result.RaiseExceptionIfErrors()
            Return result.RowsAffected
        End Function
        
        Protected Overridable Sub LoadViewState(ByVal savedState As Object)
            If (Not (savedState) Is Nothing) Then
                Dim pair = CType(savedState,Pair)
                If (Not (pair.Second) Is Nothing) Then
                    CType(FilterParameters,IStateManager).LoadViewState(pair.Second)
                End If
            End If
        End Sub
        
        Protected Overridable Function SaveViewState() As Object
            Dim pair = New Pair()
            If (Not (m_FilterParameters) Is Nothing) Then
                pair.Second = CType(m_FilterParameters,IStateManager).SaveViewState()
            End If
            If ((pair.First Is Nothing) AndAlso (pair.Second Is Nothing)) Then
                Return Nothing
            End If
            Return pair
        End Function
        
        Protected Overridable Sub TrackViewState()
            m_Tracking = true
            If (Not (m_FilterParameters) Is Nothing) Then
                CType(m_FilterParameters,IStateManager).TrackViewState()
            End If
        End Sub
        
        Sub IStateManager_LoadViewState(ByVal state As Object) Implements IStateManager.LoadViewState
            LoadViewState(state)
        End Sub
        
        Function IStateManager_SaveViewState() As Object Implements IStateManager.SaveViewState
            Return SaveViewState()
        End Function
        
        Sub IStateManager_TrackViewState() Implements IStateManager.TrackViewState
            TrackViewState()
        End Sub
    End Class
End Namespace
