Imports Norwindt.Data
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Data
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.UI.Design
Imports System.Web.UI.Design.WebControls
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Web.Design
    
    Public Class ControllerDataSourceDesigner
        Inherits DataSourceDesigner
        
        Private m_Control As ControllerDataSource
        
        Private m_View As ControllerDataSourceDesignView
        
        Public Overrides ReadOnly Property CanRefreshSchema() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Property DataController() As String
            Get
                Return m_Control.DataController
            End Get
            Set
                If Not ((String.Compare(m_Control.DataController, value, false) = 0)) Then
                    m_Control.DataController = value
                    RefreshSchema(false)
                End If
            End Set
        End Property
        
        Public Property DataView() As String
            Get
                Return m_Control.DataView
            End Get
            Set
                If Not ((String.Compare(m_Control.DataView, value, false) = 0)) Then
                    m_Control.DataView = value
                    RefreshSchema(false)
                End If
            End Set
        End Property
        
        Public Overrides Sub Initialize(ByVal component As IComponent)
            MyBase.Initialize(component)
            m_Control = CType(component,ControllerDataSource)
        End Sub
        
        Public Overrides Function GetView(ByVal viewName As String) As DesignerDataSourceView
            If Not (viewName.Equals(ControllerDataSourceView.DefaultViewName)) Then
                Return Nothing
            End If
            Dim webApp = CType(Me.Component.Site.GetService(GetType(IWebApplication)),IWebApplication)
            If (webApp Is Nothing) Then
                Return Nothing
            End If
            Dim item = webApp.GetProjectItemFromUrl("~/Controllers")
            If (m_View Is Nothing) Then
                m_View = New ControllerDataSourceDesignView(Me, ControllerDataSourceView.DefaultViewName)
            End If
            m_View.DataController = m_Control.DataController
            m_View.DataView = m_Control.DataView
            If (Not (item) Is Nothing) Then
                m_View.BasePath = item.PhysicalPath
            End If
            Return m_View
        End Function
        
        Public Overrides Function GetViewNames() As String()
            Return New String() {ControllerDataSourceView.DefaultViewName}
        End Function
        
        Public Overrides Sub RefreshSchema(ByVal preferSilent As Boolean)
            OnSchemaRefreshed(EventArgs.Empty)
        End Sub
        
        Protected Overrides Sub PreFilterProperties(ByVal properties As IDictionary)
            MyBase.PreFilterProperties(properties)
            Dim typeNameProp = CType(properties("DataController"),PropertyDescriptor)
            properties("DataController") = TypeDescriptor.CreateProperty([GetType](), typeNameProp, New Attribute((0) - 1) {})
            typeNameProp = CType(properties("DataView"),PropertyDescriptor)
            properties("DataView") = TypeDescriptor.CreateProperty([GetType](), typeNameProp, New Attribute((0) - 1) {})
        End Sub
    End Class
    
    Public Class ControllerDataSourceDesignView
        Inherits DesignerDataSourceView
        
        Private m_BasePath As String
        
        Private m_DataController As String
        
        Private m_DataView As String
        
        Public Sub New(ByVal owner As ControllerDataSourceDesigner, ByVal viewName As String)
            MyBase.New(owner, viewName)
        End Sub
        
        Public Property BasePath() As String
            Get
                Return m_BasePath
            End Get
            Set
                m_BasePath = value
            End Set
        End Property
        
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
        
        Public Overrides ReadOnly Property CanPage() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanSort() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanRetrieveTotalRowCount() As Boolean
            Get
                Return true
            End Get
        End Property
        
        Public Overrides ReadOnly Property CanDelete() As Boolean
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
        
        Public Overrides ReadOnly Property Schema() As IDataSourceViewSchema
            Get
                Dim document As XPathDocument = Nothing
                Dim res = [GetType]().Assembly.GetManifestResourceStream(String.Format("Norwindt.Controllers.{0}.xml", DataController))
                If (res Is Nothing) Then
                    res = [GetType]().Assembly.GetManifestResourceStream(String.Format("Norwindt.{0}.xml", DataController))
                End If
                If (Not (res) Is Nothing) Then
                    document = New XPathDocument(res)
                Else
                    Dim dataControllerPath = Path.Combine(BasePath, (DataController + ".xml"))
                    document = New XPathDocument(dataControllerPath)
                End If
                Return New DataViewDesignSchema(document, DataView)
            End Get
        End Property
        
        Public Overrides Function GetDesignTimeData(ByVal minimumRows As Integer, ByRef isSampleData As Boolean) As IEnumerable
            Dim fields = Schema.GetFields()
            Dim dt = New DataTable(DataView)
            For Each field in fields
                dt.Columns.Add(field.Name, field.DataType)
            Next
            Dim i = 0
            Do While (i < minimumRows)
                Dim row = dt.NewRow()
                For Each field in fields
                    Dim typeName = field.DataType.Name
                    Dim v As Object = i
                    If (typeName = "String") Then
                        v = "abc"
                    Else
                        If (typeName = "DateTime") Then
                            v = DateTime.Now
                        Else
                            If (typeName = "Boolean") Then
                                v = ((i Mod 2) = 1)
                            Else
                                If (typeName = "Guid") Then
                                    v = Guid.NewGuid()
                                Else
                                    If Not (typeName.Contains("Int")) Then
                                        v = (Convert.ToDouble(i) / 10)
                                    End If
                                End If
                            End If
                        End If
                    End If
                    row(field.Name) = v
                Next
                dt.Rows.Add(row)
                i = (i + 1)
            Loop
            dt.AcceptChanges()
            isSampleData = true
            Return dt.DefaultView
        End Function
    End Class
    
    Public Class DataViewDesignSchema
        Inherits Object
        Implements IDataSourceViewSchema
        
        Private m_Name As String
        
        Private m_Nm As XmlNamespaceManager
        
        Private m_View As XPathNavigator
        
        Public Sub New(ByVal document As XPathDocument, ByVal dataView As String)
            MyBase.New
            'initialize the schema
            Dim navigator = document.CreateNavigator()
            m_Nm = New XmlNamespaceManager(navigator.NameTable)
            m_Nm.AddNamespace("a", "urn:schemas-codeontime-com:data-aquarium")
            m_Name = CType(navigator.Evaluate("string(/a:dataController/@name)", m_Nm),String)
            'find the data view metadata
            If String.IsNullOrEmpty(dataView) Then
                m_View = navigator.SelectSingleNode("//a:view", m_Nm)
            Else
                m_View = navigator.SelectSingleNode(String.Format("//a:view[@id='{0}']", dataView), m_Nm)
            End If
        End Sub
        
        ReadOnly Property IDataSourceViewSchema_Name() As String Implements IDataSourceViewSchema.Name
            Get
                Return m_Name
            End Get
        End Property
        
        Function IDataSourceViewSchema_GetChildren() As IDataSourceViewSchema() Implements IDataSourceViewSchema.GetChildren
            Return Nothing
        End Function
        
        Function IDataSourceViewSchema_GetFields() As IDataSourceFieldSchema() Implements IDataSourceViewSchema.GetFields
            Dim fields = New List(Of IDataSourceFieldSchema)()
            If (Not (m_View) Is Nothing) Then
                Dim dataFieldIterator = m_View.Select(".//a:dataField", m_Nm)
                Do While dataFieldIterator.MoveNext()
                    fields.Add(New DataViewFieldSchema(dataFieldIterator.Current, m_Nm))
                Loop
                Dim systemFieldIterator = m_View.Select(String.Format("//a:field[not(@name=//a:view[@id='{0}']//a:dataField/@fieldName) and @isPrimaryKe"& _ 
                            "y='true']", m_View.GetAttribute("id", String.Empty)), m_Nm)
                Do While systemFieldIterator.MoveNext()
                    fields.Add(New DataViewFieldSchema(systemFieldIterator.Current, m_Nm))
                Loop
            End If
            Return fields.ToArray()
        End Function
    End Class
    
    Public Class DataViewFieldSchema
        Inherits Object
        Implements IDataSourceFieldSchema
        
        Private m_Name As String
        
        Private m_Type As Type
        
        Private m_Identity As Boolean
        
        Private m_ReadOnly As Boolean
        
        Private m_Unique As Boolean
        
        Private m_Length As Integer
        
        Private m_Nullable As Boolean
        
        Private m_PrimaryKey As Boolean
        
        Public Sub New(ByVal fieldInfo As XPathNavigator, ByVal nm As XmlNamespaceManager)
            MyBase.New
            Dim field = fieldInfo
            If (fieldInfo.LocalName = "dataField") Then
                m_Name = fieldInfo.GetAttribute("fieldName", String.Empty)
                Dim aliasFieldName = fieldInfo.GetAttribute("aliasFieldName", String.Empty)
                If Not (String.IsNullOrEmpty(aliasFieldName)) Then
                    m_Name = aliasFieldName
                End If
                field = fieldInfo.SelectSingleNode(String.Format("/a:dataController/a:fields/a:field[@name='{0}']", m_Name), nm)
            Else
                m_Name = fieldInfo.GetAttribute("name", String.Empty)
            End If
            m_Type = GetType(String)
            If (Not (field) Is Nothing) Then
                m_Type = Controller.TypeMap(field.GetAttribute("type", String.Empty))
                If Not (String.IsNullOrEmpty(field.GetAttribute("length", String.Empty))) Then
                    m_Length = Convert.ToInt32(field.GetAttribute("length", String.Empty))
                End If
                m_Identity = CType(field.Evaluate("@isPrimaryKey='true' and @readOnly='true'"),Boolean)
                m_ReadOnly = CType(field.Evaluate("@readOnly='true'"),Boolean)
                m_Unique = false
                m_Nullable = CType(field.Evaluate("not(@allowNulls='false')"),Boolean)
                m_PrimaryKey = CType(field.Evaluate("@isPrimaryKey='true'"),Boolean)
            End If
        End Sub
        
        ReadOnly Property IDataSourceFieldSchema_DataType() As Type Implements IDataSourceFieldSchema.DataType
            Get
                Return m_Type
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_Identity() As Boolean Implements IDataSourceFieldSchema.Identity
            Get
                Return m_Identity
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_IsReadOnly() As Boolean Implements IDataSourceFieldSchema.IsReadOnly
            Get
                Return m_ReadOnly
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_IsUnique() As Boolean Implements IDataSourceFieldSchema.IsUnique
            Get
                Return m_Unique
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_Length() As Integer Implements IDataSourceFieldSchema.Length
            Get
                Return m_Length
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_Name() As String Implements IDataSourceFieldSchema.Name
            Get
                Return m_Name
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_Nullable() As Boolean Implements IDataSourceFieldSchema.Nullable
            Get
                Return m_Nullable
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_Precision() As Integer Implements IDataSourceFieldSchema.Precision
            Get
                Return 0
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_PrimaryKey() As Boolean Implements IDataSourceFieldSchema.PrimaryKey
            Get
                Return m_PrimaryKey
            End Get
        End Property
        
        ReadOnly Property IDataSourceFieldSchema_Scale() As Integer Implements IDataSourceFieldSchema.Scale
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace
