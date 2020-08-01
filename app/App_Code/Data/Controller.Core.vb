Imports Newtonsoft.Json
Imports Norwindt.Handlers
Imports Norwindt.Services
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Configuration
Imports System.Data
Imports System.Data.Common
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Transactions
Imports System.Web
Imports System.Web.Caching
Imports System.Web.Configuration
Imports System.Web.Security
Imports System.Xml
Imports System.Xml.XPath
Imports System.Xml.Xsl

Namespace Norwindt.Data
    
    Partial Public Class Controller
        Inherits DataControllerBase
        
        Public Shared Function GrantFullAccess(ByVal ParamArray controllers() as System.[String]) As String()
            FullAccess(true, controllers)
            Return controllers
        End Function
        
        Public Shared Sub RevokeFullAccess(ByVal ParamArray controllers() as System.[String])
            FullAccess(false, controllers)
        End Sub
    End Class
    
    Partial Public Class DataControllerBase
        Inherits Object
        Implements IDataController, IAutoCompleteManager, IDataEngine, IBusinessObject
        
        Public Const MaximumDistinctValues As Integer = 200
        
        Public Shared SpecialConversionTypes() As Type = New Type() {GetType(System.Guid), GetType(System.DateTimeOffset), GetType(System.TimeSpan)}
        
        Public Shared SpecialConverters() As SpecialConversionFunction
        
        Public Shared ISO8601DateStringMatcher As Regex = New Regex("^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")
        
        Public Shared SpecialTypes() As String = New String() {"System.DateTimeOffset", "System.TimeSpan", "Microsoft.SqlServer.Types.SqlGeography", "Microsoft.SqlServer.Types.SqlHierarchyId"}
        
        Private m_ServerRules As BusinessRules
        
        Private m_OriginalFieldValues() As FieldValue
        
        Private m_JunctionTableMap As SortedDictionary(Of String, List(Of String))
        
        Private m_JunctionTableFieldName As String
        
        Public Shared DefaultDataControllerStream As Stream = New MemoryStream()
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AllowPublicAccess As Boolean
        
        Private m_ResultSetParameters As DbParameterCollection
        
        Shared Sub New()
            'initialize type map
            m_TypeMap = New SortedDictionary(Of String, Type)()
            m_TypeMap.Add("AnsiString", GetType(String))
            m_TypeMap.Add("Binary", GetType(Byte()))
            m_TypeMap.Add("Byte[]", GetType(Byte()))
            m_TypeMap.Add("Byte", GetType(Byte))
            m_TypeMap.Add("Boolean", GetType(Boolean))
            m_TypeMap.Add("Currency", GetType(Decimal))
            m_TypeMap.Add("Date", GetType(DateTime))
            m_TypeMap.Add("DateTime", GetType(DateTime))
            m_TypeMap.Add("Decimal", GetType(Decimal))
            m_TypeMap.Add("Double", GetType(Double))
            m_TypeMap.Add("Guid", GetType(Guid))
            m_TypeMap.Add("Int16", GetType(Short))
            m_TypeMap.Add("Int32", GetType(Integer))
            m_TypeMap.Add("Int64", GetType(Long))
            m_TypeMap.Add("Object", GetType(Object))
            m_TypeMap.Add("SByte", GetType(SByte))
            m_TypeMap.Add("Single", GetType(Single))
            m_TypeMap.Add("String", GetType(String))
            m_TypeMap.Add("Time", GetType(TimeSpan))
            m_TypeMap.Add("TimeSpan", GetType(DateTime))
            m_TypeMap.Add("UInt16", GetType(UShort))
            m_TypeMap.Add("UInt32", GetType(UInteger))
            m_TypeMap.Add("UInt64", GetType(ULong))
            m_TypeMap.Add("VarNumeric", GetType(Object))
            m_TypeMap.Add("AnsiStringFixedLength", GetType(String))
            m_TypeMap.Add("StringFixedLength", GetType(String))
            m_TypeMap.Add("Xml", GetType(String))
            m_TypeMap.Add("DateTime2", GetType(DateTime))
            m_TypeMap.Add("DateTimeOffset", GetType(DateTimeOffset))
            'initialize rowset type map
            m_RowsetTypeMap = New SortedDictionary(Of String, String)()
            m_RowsetTypeMap.Add("AnsiString", "string")
            m_RowsetTypeMap.Add("Binary", "bin.base64")
            m_RowsetTypeMap.Add("Byte", "u1")
            m_RowsetTypeMap.Add("Boolean", "boolean")
            m_RowsetTypeMap.Add("Currency", "float")
            m_RowsetTypeMap.Add("Date", "date")
            m_RowsetTypeMap.Add("DateTime", "dateTime")
            m_RowsetTypeMap.Add("Decimal", "float")
            m_RowsetTypeMap.Add("Double", "float")
            m_RowsetTypeMap.Add("Guid", "uuid")
            m_RowsetTypeMap.Add("Int16", "i2")
            m_RowsetTypeMap.Add("Int32", "i4")
            m_RowsetTypeMap.Add("Int64", "i8")
            m_RowsetTypeMap.Add("Object", "string")
            m_RowsetTypeMap.Add("SByte", "i1")
            m_RowsetTypeMap.Add("Single", "float")
            m_RowsetTypeMap.Add("String", "string")
            m_RowsetTypeMap.Add("Time", "time")
            m_RowsetTypeMap.Add("UInt16", "u2")
            m_RowsetTypeMap.Add("UInt32", "u4")
            m_RowsetTypeMap.Add("UIn64", "u8")
            m_RowsetTypeMap.Add("VarNumeric", "float")
            m_RowsetTypeMap.Add("AnsiStringFixedLength", "string")
            m_RowsetTypeMap.Add("StringFixedLength", "string")
            m_RowsetTypeMap.Add("Xml", "string")
            m_RowsetTypeMap.Add("DateTime2", "dateTime")
            m_RowsetTypeMap.Add("DateTimeOffset", "dateTime.tz")
            m_RowsetTypeMap.Add("TimeSpan", "time")
            'initialize the special converters
            SpecialConverters = New SpecialConversionFunction((SpecialConversionTypes.Length) - 1) {}
            SpecialConverters(0) = AddressOf ConvertToGuid
            SpecialConverters(1) = AddressOf ConvertToDateTimeOffset
            SpecialConverters(2) = AddressOf ConvertToTimeSpan
        End Sub
        
        Public Sub New()
            MyBase.New
            Initialize()
        End Sub
        
        Protected Overridable ReadOnly Property OriginalFieldValues() As FieldValue()
            Get
                Return m_OriginalFieldValues
            End Get
        End Property
        
        Protected Overridable ReadOnly Property HierarchyOrganizationFieldName() As String
            Get
                Return "HierarchyOrganization__"
            End Get
        End Property
        
        Public Overridable Property AllowPublicAccess() As Boolean
            Get
                Return m_AllowPublicAccess
            End Get
            Set
                m_AllowPublicAccess = value
            End Set
        End Property
        
        Protected Overridable Sub Initialize()
            CultureManager.Initialize()
        End Sub
        
        Public Shared Function StringIsNull(ByVal s As String) As Boolean
            Return ((s = "null") OrElse (s = "%js%null"))
        End Function
        
        Public Shared Function ConvertToGuid(ByVal o As Object) As Object
            Return New Guid(Convert.ToString(o))
        End Function
        
        Public Shared Function ConvertToDateTimeOffset(ByVal o As Object) As Object
            Return System.DateTimeOffset.Parse(Convert.ToString(o))
        End Function
        
        Public Shared Function ConvertToTimeSpan(ByVal o As Object) As Object
            Return System.TimeSpan.Parse(Convert.ToString(o))
        End Function
        
        Public Shared Function ConvertToType(ByVal targetType As Type, ByVal o As Object) As Object
            If targetType.IsGenericType Then
                targetType = targetType.GetProperty("Value").PropertyType
            End If
            If ((o Is Nothing) OrElse o.GetType().Equals(targetType)) Then
                Return o
            End If
            Dim i = 0
            Do While (i < SpecialConversionTypes.Length)
                Dim t = SpecialConversionTypes(i)
                If (t Is targetType) Then
                    Return SpecialConverters(i)(o)
                End If
                i = (i + 1)
            Loop
            If TypeOf o Is IConvertible Then
                o = Convert.ChangeType(o, targetType)
            Else
                If (targetType.Equals(GetType(String)) AndAlso (Not (o) Is Nothing)) Then
                    o = o.ToString()
                End If
            End If
            Return o
        End Function
        
        Public Shared Function ValueToString(ByVal o As Object) As String
            If ((Not (o) Is Nothing) AndAlso TypeOf o Is Date) Then
                o = CType(o,DateTime).ToString("yyyy-MM-ddTHH\:mm\:ss.fff")
            End If
            Return ("%js%" + JsonConvert.SerializeObject(o))
        End Function
        
        Public Overloads Shared Function StringToValue(ByVal s As String) As Object
            Return StringToValue(Nothing, s)
        End Function
        
        Public Overloads Shared Function StringToValue(ByVal field As DataField, ByVal s As String) As Object
            If (Not (String.IsNullOrEmpty(s)) AndAlso s.StartsWith("%js%")) Then
                Dim v = JsonConvert.DeserializeObject(s.Substring(4))
                If (TypeOf v Is String AndAlso ISO8601DateStringMatcher.IsMatch(CType(v,String))) Then
                    Return Date.Parse(CType(v,String))
                End If
                If (Not (TypeOf v Is String) OrElse ((field Is Nothing) OrElse (field.Type = "String"))) Then
                    Return v
                End If
                s = CType(v,String)
            Else
                If ISO8601DateStringMatcher.IsMatch(s) Then
                    Return Date.Parse(s)
                End If
            End If
            If (Not (field) Is Nothing) Then
                Return TypeDescriptor.GetConverter(Controller.TypeMap(field.Type)).ConvertFromString(s)
            End If
            Return s
        End Function
        
        Public Shared Function ConvertObjectToValue(ByVal o As Object) As Object
            If SpecialTypes.Contains(o.GetType().FullName) Then
                Return o.ToString()
            End If
            Return o
        End Function
        
        Public Shared Function EnsureJsonCompatibility(ByVal o As Object) As Object
            If (Not (o) Is Nothing) Then
                If TypeOf o Is List(Of Object()) Then
                    For Each values in CType(o,List(Of Object()))
                        EnsureJsonCompatibility(values)
                    Next
                Else
                    If (TypeOf o Is Array AndAlso (o.GetType().GetElementType() Is GetType(Object))) Then
                        Dim row = CType(o,Object())
                        Dim i = 0
                        Do While (i < row.Length)
                            row(i) = EnsureJsonCompatibility(row(i))
                            i = (i + 1)
                        Loop
                    Else
                        If TypeOf o Is DateTime Then
                            Dim d = CType(o,DateTime)
                            Return String.Format("{0:d4}-{1:d2}-{2:d2}T{3:d2}:{4:d2}:{5:d2}.{6:d3}", d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second, d.Millisecond)
                        End If
                    End If
                End If
            End If
            Return o
        End Function
        
        Protected Function CreateBusinessRules() As BusinessRules
            Return BusinessRules.Create(m_Config)
        End Function
        
        Private Sub ApplyFieldFilter(ByVal page As ViewPage)
            If ((Not (page.FieldFilter) Is Nothing) AndAlso (page.FieldFilter.Length > 0)) Then
                Dim newFields = New List(Of DataField)()
                For Each f in page.Fields
                    If (f.IsPrimaryKey OrElse page.IncludeField(f.Name)) Then
                        newFields.Add(f)
                    End If
                Next
                page.Fields.Clear()
                page.Fields.AddRange(newFields)
                page.FieldFilter = Nothing
            End If
        End Sub
        
        Protected Overridable Function InitBusinessRules(ByVal request As PageRequest, ByVal page As ViewPage) As BusinessRules
            Dim rules = m_Config.CreateBusinessRules()
            m_ServerRules = rules
            If (m_ServerRules Is Nothing) Then
                m_ServerRules = CreateBusinessRules()
            End If
            m_ServerRules.Page = page
            m_ServerRules.RequiresRowCount = (page.RequiresRowCount AndAlso Not ((request.Inserting OrElse request.DoesNotRequireData)))
            If (Not (rules) Is Nothing) Then
                rules.BeforeSelect(request)
            Else
                m_ServerRules.ExecuteServerRules(request, ActionPhase.Before)
            End If
            Return rules
        End Function
        
        Public Overridable Function GetPageList(ByVal requests() As PageRequest) As ViewPage()
            Dim result = New List(Of ViewPage)()
            For Each r in requests
                result.Add(ControllerFactory.CreateDataController().GetPage(r.Controller, r.View, r))
            Next
            Return result.ToArray()
        End Function
        
        Public Overridable Function ExecuteList(ByVal requests() As ActionArgs) As ActionResult()
            Dim result = New List(Of ActionResult)()
            For Each r in requests
                result.Add(ControllerFactory.CreateDataController().Execute(r.Controller, r.View, r))
            Next
            Return result.ToArray()
        End Function
        
        Function IDataController_GetPage(ByVal controller As String, ByVal view As String, ByVal request As PageRequest) As ViewPage Implements IDataController.GetPage
            SelectView(controller, view)
            request.AssignContext(controller, Me.m_ViewId, m_Config)
            Dim page = New ViewPage(request)
            If (((Not (page.FieldFilter) Is Nothing) AndAlso Not (page.Distinct)) AndAlso ((page.FieldFilter.Length > 0) AndAlso (Not (Config.SelectSingleNode("/c:dataController/c:businessRules/c:rule[@commandName='Select']")) Is Nothing))) Then
                page.FieldFilter = Nothing
            End If
            If (Not (m_Config.PlugIn) Is Nothing) Then
                m_Config.PlugIn.PreProcessPageRequest(request, page)
            End If
            m_Config.AssignDynamicExpressions(page)
            page.ApplyDataFilter(m_Config.CreateDataFilter(), request.Controller, request.View, request.LookupContextController, request.LookupContextView, request.LookupContextFieldName)
            Dim rules = InitBusinessRules(request, page)
            Using connection = CreateConnection(Me)
                Dim selectCommand = CreateCommand(connection)
                If ((selectCommand Is Nothing) AndAlso m_ServerRules.EnableResultSet) Then
                    PopulatePageFields(page)
                    EnsurePageFields(page, Nothing)
                End If
                If (page.RequiresMetaData AndAlso page.IncludeMetadata("categories")) Then
                    PopulatePageCategories(page)
                End If
                SyncRequestedPage(request, page, connection)
                ConfigureCommand(selectCommand, page, CommandConfigurationType.Select, Nothing)
                If ((page.PageSize > 0) AndAlso Not ((request.Inserting OrElse request.DoesNotRequireData))) Then
                    EnsureSystemPageFields(request, page, selectCommand)
                    Dim reader = ExecuteResultSetReader(page)
                    If (reader Is Nothing) Then
                        If (selectCommand Is Nothing) Then
                            reader = ExecuteVirtualReader(request, page)
                        Else
                            reader = selectCommand.ExecuteReader()
                        End If
                    End If
                    Do While page.SkipNext()
                        reader.Read()
                    Loop
                    Dim fieldMap As List(Of Integer) = Nothing
                    Dim typedFieldMap As List(Of Integer) = Nothing
                    Do While (page.ReadNext() AndAlso reader.Read())
                        If (fieldMap Is Nothing) Then
                            fieldMap = New List(Of Integer)()
                            typedFieldMap = New List(Of Integer)()
                            Dim availableColumns = New SortedDictionary(Of String, Integer)()
                            Dim j = 0
                            Do While (j < reader.FieldCount)
                                availableColumns(reader.GetName(j).ToLower()) = j
                                j = (j + 1)
                            Loop
                            Dim k = 0
                            Do While (k < page.Fields.Count)
                                Dim columnIndex = 0
                                If Not (availableColumns.TryGetValue(page.Fields(k).Name.ToLower(), columnIndex)) Then
                                    columnIndex = -1
                                End If
                                fieldMap.Add(columnIndex)
                                Dim columnType = reader.GetFieldType(columnIndex)
                                If (columnType Is Nothing) Then
                                    typedFieldMap.Add(-1)
                                Else
                                    typedFieldMap.Add(columnIndex)
                                End If
                                k = (k + 1)
                            Loop
                        End If
                        Dim values((page.Fields.Count) - 1) As Object
                        Dim i = 0
                        Do While (i < values.Length)
                            Dim columnIndex = fieldMap(i)
                            If Not ((columnIndex = -1)) Then
                                Dim field = page.Fields(i)
                                Dim v As Object
                                If (typedFieldMap(i) = -1) Then
                                    Using stream = New MemoryStream()
                                        'use GetBytes instead of GetStream for compatiblity with .NET 4 and below
                                        Dim dataBuffer = New Byte((4096) - 1) {}
                                        Dim bytesRead As Long
                                        Try 
                                            bytesRead = reader.GetBytes(columnIndex, 0, dataBuffer, 0, dataBuffer.Length)
                                        Catch __exception As Exception
                                            bytesRead = 0
                                        End Try
                                        Do While (bytesRead > 0)
                                            stream.Write(dataBuffer, 0, Convert.ToInt32(bytesRead))
                                            bytesRead = reader.GetBytes(columnIndex, stream.Length, dataBuffer, 0, dataBuffer.Length)
                                        Loop
                                        If (stream.Length = 0) Then
                                            v = DBNull.Value
                                        Else
                                            stream.Position = 0
                                            dataBuffer = New Byte((stream.Length) - 1) {}
                                            stream.Read(dataBuffer, 0, Convert.ToInt32(stream.Length))
                                            v = ("0x" + BitConverter.ToString(dataBuffer).Replace("-", String.Empty))
                                        End If
                                    End Using
                                Else
                                    v = reader(columnIndex)
                                End If
                                If Not (DBNull.Value.Equals(v)) Then
                                    If field.IsMirror Then
                                        v = String.Format(field.DataFormatString, v)
                                    Else
                                        If ((field.Type = "Guid") AndAlso (v.GetType() Is GetType(Byte()))) Then
                                            v = New Guid(CType(v,Byte()))
                                        Else
                                            v = ConvertObjectToValue(v)
                                        End If
                                    End If
                                    values(i) = v
                                End If
                                If Not (String.IsNullOrEmpty(field.SourceFields)) Then
                                    values(i) = CreateValueFromSourceFields(field, reader)
                                End If
                            End If
                            i = (i + 1)
                        Loop
                        If page.RequiresPivot Then
                            page.AddPivotValues(values)
                        Else
                            page.Rows.Add(values)
                        End If
                    Loop
                    reader.Close()
                End If
                If m_ServerRules.RequiresRowCount Then
                    If m_ServerRules.EnableResultSet Then
                        page.TotalRowCount = m_ServerRules.ResultSetSize
                    Else
                        Dim countCommand = CreateCommand(connection)
                        page.FieldFilter = request.FieldFilter
                        ConfigureCommand(countCommand, page, CommandConfigurationType.SelectCount, Nothing)
                        page.FieldFilter = Nothing
                        If YieldsSingleRow(countCommand) Then
                            page.TotalRowCount = 1
                        Else
                            If ((page.Rows.Count < page.PageSize) AndAlso (page.PageIndex <= 0)) Then
                                page.TotalRowCount = page.Rows.Count
                            Else
                                page.TotalRowCount = Convert.ToInt32(countCommand.ExecuteScalar())
                            End If
                        End If
                    End If
                    If (Not (request.DoesNotRequireAggregates) AndAlso page.RequiresAggregates) Then
                        Dim aggregates((page.Fields.Count) - 1) As Object
                        If m_ServerRules.EnableResultSet Then
                            Dim dt = ExecuteResultSetTable(page)
                            Dim j = 0
                            Do While (j < aggregates.Length)
                                Dim field = page.Fields(j)
                                If Not ((field.Aggregate = DataFieldAggregate.None)) Then
                                    Dim func = field.Aggregate.ToString()
                                    If (func = "Count") Then
                                        Dim uniqueValues = New SortedDictionary(Of String, String)()
                                        For Each r As DataRow in dt.Rows
                                            Dim v = r(field.Name)
                                            If Not (DBNull.Value.Equals(v)) Then
                                                uniqueValues(v.ToString()) = Nothing
                                            End If
                                        Next
                                        aggregates(j) = uniqueValues.Keys.Count
                                    Else
                                        If (func = "Average") Then
                                            func = "avg"
                                        End If
                                        aggregates(j) = dt.Compute(String.Format("{0}([{1}])", func, field.Name), Nothing)
                                    End If
                                End If
                                j = (j + 1)
                            Loop
                        Else
                            Dim aggregateCommand = CreateCommand(connection)
                            ConfigureCommand(aggregateCommand, page, CommandConfigurationType.SelectAggregates, Nothing)
                            Dim reader = aggregateCommand.ExecuteReader()
                            If reader.Read() Then
                                Dim j = 0
                                Do While (j < aggregates.Length)
                                    Dim field = page.Fields(j)
                                    If Not ((field.Aggregate = DataFieldAggregate.None)) Then
                                        aggregates(j) = reader(field.Name)
                                    End If
                                    j = (j + 1)
                                Loop
                            End If
                            reader.Close()
                        End If
                        Dim i = 0
                        Do While (i < aggregates.Length)
                            Dim field = page.Fields(i)
                            If Not ((field.Aggregate = DataFieldAggregate.None)) Then
                                Dim v = aggregates(i)
                                If (Not (DBNull.Value.Equals(v)) AndAlso (Not (v) Is Nothing)) Then
                                    If (Not (field.FormatOnClient) AndAlso Not (String.IsNullOrEmpty(field.DataFormatString))) Then
                                        v = String.Format(field.DataFormatString, v)
                                    End If
                                    aggregates(i) = v
                                End If
                            End If
                            i = (i + 1)
                        Loop
                        page.Aggregates = aggregates
                    End If
                End If
                If (request.RequiresFirstLetters AndAlso Not ((Me.m_ViewType = "Form"))) Then
                    If Not (page.RequiresRowCount) Then
                        page.FirstLetters = String.Empty
                    Else
                        Dim firstLettersCommand = CreateCommand(connection)
                        Dim oldFilter = page.Filter
                        ConfigureCommand(firstLettersCommand, page, CommandConfigurationType.SelectFirstLetters, Nothing)
                        page.Filter = oldFilter
                        If Not (String.IsNullOrEmpty(page.FirstLetters)) Then
                            Dim reader = firstLettersCommand.ExecuteReader()
                            Dim firstLetters = New StringBuilder(page.FirstLetters)
                            Do While reader.Read()
                                firstLetters.Append(",")
                                Dim letter = Convert.ToString(reader(0))
                                If Not (String.IsNullOrEmpty(letter)) Then
                                    firstLetters.Append(letter)
                                End If
                            Loop
                            reader.Close()
                            page.FirstLetters = firstLetters.ToString()
                        End If
                    End If
                End If
            End Using
            If (Not (m_Config.PlugIn) Is Nothing) Then
                m_Config.PlugIn.ProcessPageRequest(request, page)
            End If
            If request.Inserting Then
                page.NewRow = New Object((page.Fields.Count) - 1) {}
            End If
            If request.Inserting Then
                If m_ServerRules.SupportsCommand("Sql|Code", "New") Then
                    m_ServerRules.ExecuteServerRules(request, ActionPhase.Execute, "New", page.NewRow)
                End If
            Else
                If (m_ServerRules.SupportsCommand("Sql|Code", "Select") AndAlso Not (page.Distinct)) Then
                    For Each row in page.Rows
                        m_ServerRules.ExecuteServerRules(request, ActionPhase.Execute, "Select", row)
                    Next
                End If
            End If
            If Not (request.Inserting) Then
                PopulateManyToManyFields(page)
            End If
            If (Not (rules) Is Nothing) Then
                Dim rowHandler As IRowHandler = rules
                If request.Inserting Then
                    If rowHandler.SupportsNewRow(request) Then
                        rowHandler.NewRow(request, page, page.NewRow)
                    End If
                Else
                    If rowHandler.SupportsPrepareRow(request) Then
                        For Each row in page.Rows
                            rowHandler.PrepareRow(request, page, row)
                        Next
                    End If
                End If
                rules.ProcessPageRequest(request, page)
                If rules.CompleteConfiguration() Then
                    ResetViewPage(page)
                End If
            End If
            If (Not (rules) Is Nothing) Then
                rules.AfterSelect(request)
            Else
                m_ServerRules.ExecuteServerRules(request, ActionPhase.After)
            End If
            m_ServerRules.Result.Merge(page)
            Return page.ToResult(m_Config, m_View)
        End Function
        
        Public Overridable Sub ResetViewPage(ByVal page As ViewPage)
            page.RequiresMetaData = true
            Dim fieldIndexes = New SortedDictionary(Of String, Integer)()
            Dim i = 0
            Do While (i < page.Fields.Count)
                fieldIndexes(page.Fields(i).Name) = i
                i = (i + 1)
            Loop
            page.Fields.Clear()
            page.Categories.Clear()
            PopulatePageFields(page)
            EnsurePageFields(page, m_Expressions)
            page.FieldFilter = page.RequestedFieldFilter()
            ApplyFieldFilter(page)
            PopulatePageCategories(page)
            If (Not (page.NewRow) Is Nothing) Then
                page.NewRow = ReorderRowValues(page, fieldIndexes, page.NewRow)
            End If
            If (Not (page.Rows) Is Nothing) Then
                Dim j = 0
                Do While (j < page.Rows.Count)
                    page.Rows(j) = ReorderRowValues(page, fieldIndexes, page.Rows(j))
                    j = (j + 1)
                Loop
            End If
        End Sub
        
        Private Function ReorderRowValues(ByVal page As ViewPage, ByVal indexes As SortedDictionary(Of String, Integer), ByVal row() As Object) As Object()
            Dim newRow((row.Length) - 1) As Object
            Dim i = 0
            Do While (i < page.Fields.Count)
                Dim field = page.Fields(i)
                newRow(i) = row(indexes(field.Name))
                i = (i + 1)
            Loop
            Return newRow
        End Function
        
        Function IDataController_GetListOfValues(ByVal controller As String, ByVal view As String, ByVal request As DistinctValueRequest) As Object() Implements IDataController.GetListOfValues
            SelectView(controller, view)
            Dim page = New ViewPage(request)
            page.ApplyDataFilter(m_Config.CreateDataFilter(), controller, view, request.LookupContextController, request.LookupContextView, request.LookupContextFieldName)
            Dim distinctValues = New List(Of Object)()
            Dim rules = m_Config.CreateBusinessRules()
            m_ServerRules = rules
            If (m_ServerRules Is Nothing) Then
                m_ServerRules = CreateBusinessRules()
            End If
            m_ServerRules.Page = page
            If (Not (rules) Is Nothing) Then
                rules.BeforeSelect(request)
            Else
                m_ServerRules.ExecuteServerRules(request, ActionPhase.Before)
            End If
            If m_ServerRules.EnableResultSet Then
                Dim reader = ExecuteResultSetReader(page)
                Dim uniqueValues = New SortedDictionary(Of Object, Object)()
                Dim hasNull = false
                Do While reader.Read()
                    Dim v = reader(request.FieldName)
                    If DBNull.Value.Equals(v) Then
                        hasNull = true
                    Else
                        uniqueValues(v) = v
                    End If
                Loop
                If hasNull Then
                    distinctValues.Add(Nothing)
                End If
                For Each v in uniqueValues.Keys
                    If (distinctValues.Count < page.PageSize) Then
                        distinctValues.Add(ConvertObjectToValue(v))
                    Else
                        Exit For
                    End If
                Next
            Else
                Using connection = CreateConnection(Me)
                    Dim command = CreateCommand(connection)
                    ConfigureCommand(command, page, CommandConfigurationType.SelectDistinct, Nothing)
                    Dim reader = command.ExecuteReader()
                    Do While (reader.Read() AndAlso (distinctValues.Count < page.PageSize))
                        Dim v = reader.GetValue(0)
                        If Not (DBNull.Value.Equals(v)) Then
                            v = ConvertObjectToValue(v)
                        End If
                        distinctValues.Add(v)
                    Loop
                    reader.Close()
                End Using
            End If
            If (Not (rules) Is Nothing) Then
                rules.AfterSelect(request)
            Else
                m_ServerRules.ExecuteServerRules(request, ActionPhase.After)
            End If
            Dim result = distinctValues.ToArray()
            EnsureJsonCompatibility(result)
            Return result
        End Function
        
        Function IDataController_Execute(ByVal controller As String, ByVal view As String, ByVal args As ActionArgs) As ActionResult Implements IDataController.Execute
            Dim result = New ActionResult()
            SelectView(controller, view)
            Try 
                m_ServerRules = m_Config.CreateBusinessRules()
                If (m_ServerRules Is Nothing) Then
                    m_ServerRules = CreateBusinessRules()
                End If
                Dim handler = CType(m_ServerRules,IActionHandler)
                If (Not (m_Config.PlugIn) Is Nothing) Then
                    m_Config.PlugIn.PreProcessArguments(args, result, CreateViewPage())
                End If
                EnsureFieldValues(args)
                If Not ((args.SqlCommandType = CommandConfigurationType.None)) Then
                    If args.IsBatchEditOrDelete Then
                        Dim page = CreateViewPage()
                        PopulatePageFields(page)
                        For Each sv in args.SelectedValues
                            result.Canceled = false
                            m_ServerRules.ClearBlackAndWhiteLists()
                            Dim key = sv.Split(Global.Microsoft.VisualBasic.ChrW(44))
                            Dim keyIndex = 0
                            For Each v in OriginalFieldValues
                                Dim field = page.FindField(v.Name)
                                If (Not (field) Is Nothing) Then
                                    If Not (field.IsPrimaryKey) Then
                                        v.Modified = true
                                    Else
                                        If (v.Name = field.Name) Then
                                            v.OldValue = key(keyIndex)
                                            v.Modified = false
                                            keyIndex = (keyIndex + 1)
                                        End If
                                    End If
                                End If
                            Next
                            Using connection = CreateConnection(Me, true)
                                Try 
                                    Dim command = CreateCommand(connection, args)
                                    ExecutePreActionCommands(args, result, connection)
                                    If (Not (handler) Is Nothing) Then
                                        handler.BeforeSqlAction(args, result)
                                    Else
                                        m_ServerRules.ExecuteServerRules(args, result, ActionPhase.Before)
                                    End If
                                    If ((result.Errors.Count = 0) AndAlso Not (result.Canceled)) Then
                                        If Not ((args.CommandName = "Delete")) Then
                                            ProcessOneToOneFields(args)
                                        End If
                                        If (args.CommandName = "Delete") Then
                                            ProcessManyToManyFields(args)
                                        End If
                                        Dim rowsAffected = 1
                                        If ConfigureCommand(command, Nothing, args.SqlCommandType, args.Values) Then
                                            rowsAffected = command.ExecuteNonQuery()
                                        End If
                                        result.RowsAffected = (result.RowsAffected + rowsAffected)
                                        If (args.CommandName = "Update") Then
                                            ProcessManyToManyFields(args)
                                        End If
                                        If (args.CommandName = "Delete") Then
                                            ProcessOneToOneFields(args)
                                        End If
                                        If (Not (handler) Is Nothing) Then
                                            handler.AfterSqlAction(args, result)
                                        Else
                                            m_ServerRules.ExecuteServerRules(args, result, ActionPhase.After)
                                        End If
                                        command.Parameters.Clear()
                                        If (Not (m_Config.PlugIn) Is Nothing) Then
                                            m_Config.PlugIn.ProcessArguments(args, result, page)
                                        End If
                                    End If
                                Catch ex As Exception
                                    If (connection.CanClose AndAlso TypeOf connection Is DataTransaction) Then
                                        CType(connection,DataTransaction).Rollback()
                                    End If
                                    Throw ex
                                End Try
                            End Using
                            If result.CanceledSelectedValues Then
                                Exit For
                            End If
                        Next
                    Else
                        Using connection = CreateConnection(Me, true)
                            Try 
                                Dim command = CreateCommand(connection, args)
                                ExecutePreActionCommands(args, result, connection)
                                If (Not (handler) Is Nothing) Then
                                    handler.BeforeSqlAction(args, result)
                                Else
                                    m_ServerRules.ExecuteServerRules(args, result, ActionPhase.Before)
                                End If
                                If ((result.Errors.Count = 0) AndAlso Not (result.Canceled)) Then
                                    If Not ((args.CommandName = "Delete")) Then
                                        ProcessOneToOneFields(args)
                                    End If
                                    If (args.CommandName = "Delete") Then
                                        ProcessManyToManyFields(args)
                                    End If
                                    If ConfigureCommand(command, Nothing, args.SqlCommandType, args.Values) Then
                                        result.RowsAffected = command.ExecuteNonQuery()
                                        If (result.RowsAffected = 0) Then
                                            result.RowNotFound = true
                                            result.Errors.Add(Localizer.Replace("RecordChangedByAnotherUser", "The record has been changed by another user."))
                                        Else
                                            ExecutePostActionCommands(args, result, connection)
                                        End If
                                    End If
                                    If ((args.CommandName = "Insert") OrElse (args.CommandName = "Update")) Then
                                        ProcessManyToManyFields(args)
                                    End If
                                    If (args.CommandName = "Delete") Then
                                        ProcessOneToOneFields(args)
                                    End If
                                    If (Not (handler) Is Nothing) Then
                                        handler.AfterSqlAction(args, result)
                                    Else
                                        m_ServerRules.ExecuteServerRules(args, result, ActionPhase.After)
                                    End If
                                    If (Not (m_Config.PlugIn) Is Nothing) Then
                                        m_Config.PlugIn.ProcessArguments(args, result, CreateViewPage())
                                    End If
                                End If
                            Catch ex As Exception
                                If (connection.CanClose AndAlso TypeOf connection Is DataTransaction) Then
                                    CType(connection,DataTransaction).Rollback()
                                End If
                                Throw ex
                            End Try
                        End Using
                    End If
                Else
                    If args.CommandName.StartsWith("Export") Then
                        ExecuteDataExport(args, result)
                    Else
                        If args.CommandName.Equals("PopulateDynamicLookups") Then
                            PopulateDynamicLookups(args, result)
                        Else
                            If args.CommandName.Equals("ProcessImportFile") Then
                                ImportProcessor.Execute(args)
                            Else
                                If args.CommandName.Equals("Execute") Then
                                    Using connection = CreateConnection(Me)
                                        Dim command = CreateCommand(connection, args)
                                        command.ExecuteNonQuery()
                                    End Using
                                Else
                                    m_ServerRules.ProcessSpecialActions(args, result)
                                End If
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                If TypeOf ex Is System.Reflection.TargetInvocationException Then
                    ex = ex.InnerException
                End If
                HandleException(ex, args, result)
            End Try
            result.EnsureJsonCompatibility()
            Return result
        End Function
        
        Private Sub EnsureFieldValues(ByVal args As ActionArgs)
            m_OriginalFieldValues = args.Values
            Dim page = CreateViewPage()
            Dim fieldValues = New FieldValueDictionary()
            If (args.Values Is Nothing) Then
                args.Values = New FieldValue(-1) {}
            End If
            If (args.Values.Length > 0) Then
                fieldValues.AddRange(args.Values)
            End If
            Dim missingValues = New List(Of FieldValue)()
            For Each f in page.Fields
                If Not (fieldValues.ContainsKey(f.Name)) Then
                    missingValues.Add(New FieldValue(f.Name))
                End If
            Next
            If (missingValues.Count > 0) Then
                Dim newValues = New List(Of FieldValue)(args.Values)
                newValues.AddRange(missingValues)
                args.Values = newValues.ToArray()
            End If
        End Sub
        
        Private Function SupportsLimitInSelect(ByVal command As Object) As Boolean
            Return command.ToString().Contains("MySql")
        End Function
        
        Private Function SupportsSkipInSelect(ByVal command As Object) As Boolean
            Return command.ToString().Contains("Firebird")
        End Function
        
        Protected Overridable Sub SyncRequestedPage(ByVal request As PageRequest, ByVal page As ViewPage, ByVal connection As DataConnection)
            If (((request.SyncKey Is Nothing) OrElse (request.SyncKey.Length = 0)) OrElse (page.PageSize < 0)) Then
                Return
            End If
            Dim syncCommand = CreateCommand(connection)
            ConfigureCommand(syncCommand, page, CommandConfigurationType.Sync, Nothing)
            Dim keyFields = page.EnumerateSyncFields()
            If ((keyFields.Count > 0) AndAlso (keyFields.Count = request.SyncKey.Length)) Then
                Dim useSkip = (m_ServerRules.EnableResultSet OrElse SupportsSkipInSelect(syncCommand))
                If Not (useSkip) Then
                    Dim i = 0
                    Do While (i < keyFields.Count)
                        Dim field = keyFields(i)
                        Dim p = syncCommand.CreateParameter()
                        p.ParameterName = String.Format("{0}PrimaryKey_{1}", m_ParameterMarker, field.Name)
                        Try 
                            AssignParameterValue(p, field, request.SyncKey(i))
                        Catch __exception As Exception
                            Return
                        End Try
                        syncCommand.Parameters.Add(p)
                        i = (i + 1)
                    Loop
                End If
                Dim reader As DbDataReader
                If m_ServerRules.EnableResultSet Then
                    reader = ExecuteResultSetReader(page)
                Else
                    reader = syncCommand.ExecuteReader()
                End If
                If Not (useSkip) Then
                    If reader.Read() Then
                        Dim rowIndex = Convert.ToInt64(reader(0))
                        page.PageIndex = Convert.ToInt32(Math.Floor((Convert.ToDouble((rowIndex - 1)) / Convert.ToDouble(page.PageSize))))
                        page.PageOffset = 0
                    End If
                Else
                    Dim rowIndex = 1
                    Dim keyFieldIndexes = New List(Of Integer)()
                    For Each pkField in keyFields
                        keyFieldIndexes.Add(reader.GetOrdinal(pkField.Name))
                    Next
                    Do While reader.Read()
                        Dim matchCount = 0
                        For Each primaryKeyFieldIndex in keyFieldIndexes
                            If (Convert.ToString(reader(primaryKeyFieldIndex)) = Convert.ToString(request.SyncKey(matchCount))) Then
                                matchCount = (matchCount + 1)
                            Else
                                Exit For
                            End If
                        Next
                        If (matchCount = keyFieldIndexes.Count) Then
                            page.PageIndex = Convert.ToInt32(Math.Floor((Convert.ToDouble((rowIndex - 1)) / Convert.ToDouble(page.PageSize))))
                            page.PageOffset = 0
                            page.ResetSkipCount(false)
                            Exit Do
                        Else
                            rowIndex = (rowIndex + 1)
                        End If
                    Loop
                End If
                reader.Close()
            End If
        End Sub
        
        Protected Overridable Sub HandleException(ByVal ex As Exception, ByVal args As ActionArgs, ByVal result As ActionResult)
            Do While (Not (ex) Is Nothing)
                result.Errors.Add(ex.Message)
                ex = ex.InnerException
            Loop
        End Sub
        
        Function IDataEngine_ExecuteReader(ByVal request As PageRequest) As DbDataReader Implements IDataEngine.ExecuteReader
            m_ViewPage = New ViewPage(request)
            If (m_Config Is Nothing) Then
                m_Config = CreateConfiguration(request.Controller)
                SelectView(request.Controller, request.View)
            End If
            m_ViewPage.ApplyDataFilter(m_Config.CreateDataFilter(), request.Controller, request.View, Nothing, Nothing, Nothing)
            InitBusinessRules(request, m_ViewPage)
            Dim connection = CreateConnection()
            Dim selectCommand = CreateCommand(connection)
            ConfigureCommand(selectCommand, m_ViewPage, CommandConfigurationType.Select, Nothing)
            Return selectCommand.ExecuteReader(CommandBehavior.CloseConnection)
        End Function
        
        Function IAutoCompleteManager_GetCompletionList(ByVal prefixText As String, ByVal count As Integer, ByVal contextKey As String) As String() Implements IAutoCompleteManager.GetCompletionList
            If (contextKey = Nothing) Then
                Return Nothing
            End If
            Dim arguments = contextKey.Split(Global.Microsoft.VisualBasic.ChrW(44))
            If Not ((arguments.Length = 3)) Then
                Return Nothing
            End If
            Dim request = New DistinctValueRequest()
            request.FieldName = arguments(2)
            Dim filter = (request.FieldName + ":")
            For Each s in prefixText.Split(Global.Microsoft.VisualBasic.ChrW(44), Global.Microsoft.VisualBasic.ChrW(59))
                Dim query = Controller.ConvertSampleToQuery(s)
                If Not (String.IsNullOrEmpty(query)) Then
                    filter = (filter + query)
                End If
            Next
            request.Filter = New String() {filter}
            request.AllowFieldInFilter = true
            request.MaximumValueCount = count
            request.Controller = arguments(0)
            request.View = arguments(1)
            Dim list = ControllerFactory.CreateDataController().GetListOfValues(arguments(0), arguments(1), request)
            Dim result = New List(Of String)()
            For Each o in list
                result.Add(Convert.ToString(o))
            Next
            Return result.ToArray()
        End Function
        
        Overridable Sub IBusinessObject_AssignFilter(ByVal filter As String, ByVal parameters As BusinessObjectParameters) Implements IBusinessObject.AssignFilter
            m_ViewFilter = filter
            m_Parameters = parameters
        End Sub
        
        Public Shared Function GetSelectView(ByVal controller As String) As String
            Dim c = New ControllerUtilities()
            Return c.GetActionView(controller, "editForm1", "Select")
        End Function
        
        Public Shared Function GetUpdateView(ByVal controller As String) As String
            Dim c = New ControllerUtilities()
            Return c.GetActionView(controller, "editForm1", "Update")
        End Function
        
        Public Shared Function GetInsertView(ByVal controller As String) As String
            Dim c = New ControllerUtilities()
            Return c.GetActionView(controller, "createForm1", "Insert")
        End Function
        
        Public Shared Function GetDeleteView(ByVal controller As String) As String
            Dim c = New ControllerUtilities()
            Return c.GetActionView(controller, "editForm1", "Delete")
        End Function
        
        Private Sub PopulateManyToManyFields(ByVal page As ViewPage)
            Dim primaryKeyField = String.Empty
            For Each field in page.Fields
                If Not (String.IsNullOrEmpty(field.ItemsTargetController)) Then
                    If String.IsNullOrEmpty(primaryKeyField) Then
                        For Each f in page.Fields
                            If f.IsPrimaryKey Then
                                primaryKeyField = f.Name
                                Exit For
                            End If
                        Next
                    End If
                    PopulateManyToManyField(page, field, primaryKeyField)
                End If
            Next
        End Sub
        
        Public Sub PopulateManyToManyField(ByVal page As ViewPage, ByVal field As DataField, ByVal primaryKeyField As String)
            If Not ((m_JunctionTableFieldName = field.Name)) Then
                m_JunctionTableFieldName = field.Name
                m_JunctionTableMap = Nothing
            End If
            If (m_JunctionTableMap Is Nothing) Then
                m_JunctionTableMap = New SortedDictionary(Of String, List(Of String))()
                If (page.Rows.Count > 0) Then
                    'read contents of junction table from the database for each row of the page
                    Dim foreignKeyIndex = page.IndexOfField(primaryKeyField)
                    Dim listOfForeignKeys = New StringBuilder()
                    For Each row in page.Rows
                        If (listOfForeignKeys.Length > 0) Then
                            listOfForeignKeys.Append("$or$")
                        End If
                        listOfForeignKeys.Append(DataControllerBase.ConvertObjectToValue(row(foreignKeyIndex)))
                    Next
                    Dim targetForeignKey1 As String = Nothing
                    Dim targetForeignKey2 As String = Nothing
                    ViewPage.InitializeManyToManyProperties(field, page.Controller, targetForeignKey1, targetForeignKey2)
                    Dim filter = String.Format("{0}:$in${1}", targetForeignKey1, listOfForeignKeys.ToString())
                    Dim request = New PageRequest(0, Int32.MaxValue, Nothing, New String() {filter})
                    request.RequiresMetaData = true
                    Dim manyToManyPage = ControllerFactory.CreateDataController().GetPage(field.ItemsTargetController, Nothing, request)
                    'enumerate values in junction table
                    Dim targetForeignKey1Index = manyToManyPage.IndexOfField(targetForeignKey1)
                    Dim targetForeignKey2Index = manyToManyPage.IndexOfField(targetForeignKey2)
                    'determine text field for items
                    Dim items = New SortedDictionary(Of Object, Object)()
                    Dim keyList = New List(Of Object)()
                    Dim targetTextIndex = -1
                    If Not (field.SupportsStaticItems()) Then
                        For Each f in manyToManyPage.Fields
                            If (f.Name = targetForeignKey2) Then
                                If Not (String.IsNullOrEmpty(f.AliasName)) Then
                                    targetTextIndex = manyToManyPage.IndexOfField(f.AliasName)
                                Else
                                    targetTextIndex = manyToManyPage.IndexOfField(f.Name)
                                End If
                                Exit For
                            End If
                        Next
                    End If
                    For Each row in manyToManyPage.Rows
                        Dim v1 = row(targetForeignKey1Index)
                        Dim v2 = row(targetForeignKey2Index)
                        If (Not (v1) Is Nothing) Then
                            Dim s1 = Convert.ToString(v1)
                            Dim values As List(Of String) = Nothing
                            If Not (m_JunctionTableMap.TryGetValue(s1, values)) Then
                                values = New List(Of String)()
                                m_JunctionTableMap(s1) = values
                            End If
                            values.Add(Convert.ToString(v2))
                            If Not ((targetTextIndex = -1)) Then
                                Dim text = row(targetTextIndex)
                                If Not (items.ContainsKey(v2)) Then
                                    items.Add(v2, text)
                                    keyList.Add(v2)
                                End If
                            End If
                        End If
                    Next
                    If Not ((items.Count = 0)) Then
                        For Each k in keyList
                            Dim v = items(k)
                            field.Items.Add(New Object() {k, v})
                        Next
                    End If
                End If
            End If
            For Each values in page.Rows
                Dim key = Convert.ToString(page.SelectFieldValue(primaryKeyField, values))
                Dim keyValues As List(Of String) = Nothing
                If m_JunctionTableMap.TryGetValue(key, keyValues) Then
                    page.UpdateFieldValue(field.Name, values, String.Join(",", keyValues.ToArray()))
                End If
            Next
        End Sub
        
        Protected Overridable Sub ProcessOneToOneFields(ByVal args As ActionArgs)
            Dim oneToOneFieldNav = Config.SelectSingleNode("/c:dataController/c:fields/c:field[c:items/@style='OneToOne']")
            If (Not (oneToOneFieldNav) Is Nothing) Then
                Dim targetValues = New List(Of FieldValue)()
                Dim itemsNav = oneToOneFieldNav.SelectSingleNode("c:items", Config.Resolver)
                Dim fieldMap = New SortedDictionary(Of String, String)()
                'configure the primary key field value
                Dim localKeyFieldName = oneToOneFieldNav.GetAttribute("name", String.Empty)
                Dim targetKeyFieldName = itemsNav.GetAttribute("dataValueField", String.Empty)
                Dim fvo = args(localKeyFieldName)
                Dim targetFvo = New FieldValue(targetKeyFieldName, fvo.OldValue, fvo.NewValue, fvo.ReadOnly)
                targetFvo.Modified = fvo.Modified
                targetValues.Add(targetFvo)
                fieldMap(targetKeyFieldName) = localKeyFieldName
                ' enumerate "copy" field values
                Dim copy = itemsNav.GetAttribute("copy", String.Empty)
                Dim m = Regex.Match(copy, "(\w+)\s*=\s*(\w+)")
                Do While m.Success
                    Dim localFieldName = m.Groups(1).Value
                    Dim targetFieldName = m.Groups(2).Value
                    If Not (fieldMap.ContainsKey(targetFieldName)) Then
                        fvo = args(localFieldName)
                        targetFvo = New FieldValue(targetFieldName, fvo.OldValue, fvo.NewValue, fvo.ReadOnly)
                        targetFvo.Modified = fvo.Modified
                        targetValues.Add(targetFvo)
                        fieldMap(targetFieldName) = localFieldName
                    End If
                    m = m.NextMatch()
                Loop
                'create a request
                Dim targetArgs = New ActionArgs()
                targetArgs.Controller = itemsNav.GetAttribute("dataController", String.Empty)
                targetArgs.View = args.View
                targetArgs.CommandName = args.CommandName
                targetArgs.LastCommandName = args.LastCommandName
                If (targetArgs.LastCommandName = "BatchEdit") Then
                    targetArgs.LastCommandName = "Edit"
                End If
                targetArgs.Values = targetValues.ToArray()
                Dim result = ControllerFactory.CreateDataController().Execute(targetArgs.Controller, targetArgs.View, targetArgs)
                result.RaiseExceptionIfErrors()
                'copy the new values back to the original source
                For Each tfvo in targetArgs.Values
                    Dim mappedFieldName As String = Nothing
                    If fieldMap.TryGetValue(tfvo.Name, mappedFieldName) Then
                        fvo = args(mappedFieldName)
                        If ((Not (tfvo) Is Nothing) AndAlso Not ((fvo.NewValue = tfvo.NewValue))) Then
                            fvo.NewValue = tfvo.NewValue
                            fvo.Modified = true
                        End If
                    End If
                Next
            End If
        End Sub
        
        Private Sub ProcessManyToManyFields(ByVal args As ActionArgs)
            Dim m2mFields = Config.Select("/c:dataController/c:fields/c:field[c:items/@targetController!='']")
            If (m2mFields.Count > 0) Then
                Dim primaryKeyNode = Config.SelectSingleNode("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
                Dim primaryKey = args.SelectFieldValueObject(primaryKeyNode.GetAttribute("name", String.Empty))
                Do While m2mFields.MoveNext()
                    Dim field = New DataField(m2mFields.Current, Config.Resolver)
                    Dim fv = args.SelectFieldValueObject(field.Name)
                    If (Not (fv) Is Nothing) Then
                        If (fv.Scope = "client") Then
                            fv.OldValue = fv.NewValue
                            fv.Modified = false
                        Else
                            If (args.CommandName = "Delete") Then
                                fv.Modified = true
                                fv.NewValue = Nothing
                            End If
                            ProcessManyToManyField(args.Controller, field, fv, primaryKey.Value)
                            fv.Modified = false
                        End If
                    End If
                Loop
            End If
        End Sub
        
        Public Sub ProcessManyToManyField(ByVal controllerName As String, ByVal field As DataField, ByVal fieldValue As FieldValue, ByVal primaryKey As Object)
            Dim originalOldValue = fieldValue.OldValue
            Dim restoreOldValue = false
            Dim keepBatch = false
            Dim args = ActionArgs.Current
            If ((Not (args) Is Nothing) AndAlso args.IsBatchEditOrDelete) Then
                If ((args.CommandName = "Update") AndAlso Not (fieldValue.Modified)) Then
                    Return
                End If
                Dim pkFilter = New List(Of String)()
                Dim pkIterator = m_Config.Select("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
                Do While pkIterator.MoveNext()
                    pkFilter.Add(String.Format("{0}:={1}", pkIterator.Current.GetAttribute("name", String.Empty), primaryKey))
                Loop
                Dim r = New PageRequest(0, 1, Nothing, pkFilter.ToArray())
                r.FieldFilter = New String() {field.Name}
                r.MetadataFilter = New String() {"fields"}
                r.RequiresMetaData = true
                Dim p = ControllerFactory.CreateDataController().GetPage(controllerName, m_ViewId, r)
                If (p.Rows.Count = 1) Then
                    originalOldValue = fieldValue.OldValue
                    restoreOldValue = true
                    fieldValue.OldValue = p.Rows(0)(p.IndexOfField(field.Name))
                End If
                Dim keepBatchFlag = args((fieldValue.Name + "_BatchKeep"))
                If (Not (keepBatchFlag) Is Nothing) Then
                    keepBatch = true.Equals(keepBatchFlag.Value)
                End If
            End If
            Dim oldValues = BusinessRulesBase.ValueToList(CType(fieldValue.OldValue,String))
            Dim newValues = BusinessRulesBase.ValueToList(CType(fieldValue.Value,String))
            If keepBatch Then
                For Each v in oldValues
                    If Not (newValues.Contains(v)) Then
                        newValues.Add(v)
                    End If
                Next
            End If
            If Not (BusinessRulesBase.ListsAreEqual(oldValues, newValues)) Then
                Dim targetForeignKey1 As String = Nothing
                Dim targetForeignKey2 As String = Nothing
                ViewPage.InitializeManyToManyProperties(field, controllerName, targetForeignKey1, targetForeignKey2)
                Dim controller = ControllerFactory.CreateDataController()
                For Each s in oldValues
                    If Not (newValues.Contains(s)) Then
                        Dim deleteArgs = New ActionArgs()
                        deleteArgs.Controller = field.ItemsTargetController
                        deleteArgs.CommandName = "Delete"
                        deleteArgs.LastCommandName = "Select"
                        deleteArgs.Values = New FieldValue() {New FieldValue(targetForeignKey1, primaryKey, primaryKey), New FieldValue(targetForeignKey2, s, s), New FieldValue("_SurrogatePK", New String() {targetForeignKey1, targetForeignKey2})}
                        Dim result = controller.Execute(field.ItemsTargetController, Nothing, deleteArgs)
                        result.RaiseExceptionIfErrors()
                    End If
                Next
                For Each s in newValues
                    If Not (oldValues.Contains(s)) Then
                        Dim updateArgs = New ActionArgs()
                        updateArgs.Controller = field.ItemsTargetController
                        updateArgs.CommandName = "Insert"
                        updateArgs.LastCommandName = "New"
                        updateArgs.Values = New FieldValue() {New FieldValue(targetForeignKey1, primaryKey), New FieldValue(targetForeignKey2, s)}
                        Dim result = controller.Execute(field.ItemsTargetController, Nothing, updateArgs)
                        result.RaiseExceptionIfErrors()
                    End If
                Next
            End If
            If restoreOldValue Then
                fieldValue.OldValue = originalOldValue
            End If
        End Sub
        
        Public Overridable Function GetDataControllerStream(ByVal controller As String) As Stream
            If (Not (ApplicationServices.IsSiteContentEnabled) OrElse (controller = ApplicationServices.SiteContentControllerName)) Then
                Return Nothing
            End If
            Dim context = HttpContext.Current
            Dim requiresUser = false
            If (context.User Is Nothing) Then
                requiresUser = true
                'Establish user identity for REST requests
                Dim authorization = context.Request.Headers("Authorization")
                If String.IsNullOrEmpty(authorization) Then
                    context.User = New RolePrincipal(New AnonymousUserIdentity())
                Else
                    If authorization.StartsWith("Basic", StringComparison.OrdinalIgnoreCase) Then
                        Dim login = Encoding.Default.GetString(Convert.FromBase64String(authorization.Substring(6))).Split(New Char() {Global.Microsoft.VisualBasic.ChrW(58)}, 2)
                        If Membership.ValidateUser(login(0), login(1)) Then
                            context.User = New RolePrincipal(New FormsIdentity(New FormsAuthenticationTicket(login(0), false, 10)))
                        End If
                    End If
                    If (context.User Is Nothing) Then
                        context.Response.StatusCode = 401
                        context.Response.End()
                    End If
                End If
            End If
            Dim sysControllersPath = ("sys/controllers%/"  _
                        + (controller + ".xml"))
            Dim data = CType(HttpContext.Current.Cache(sysControllersPath),Byte())
            If (data Is Nothing) Then
                If ApplicationServicesBase.Create().Supports(ApplicationFeature.DynamicControllerCustomization) Then
                    data = ApplicationServices.Current.ReadSiteContentBytes(sysControllersPath)
                End If
                If (data Is Nothing) Then
                    data = New Byte(-1) {}
                End If
                HttpContext.Current.Cache.Add(sysControllersPath, data, Nothing, DateTime.Now.AddMinutes(10), Cache.NoSlidingExpiration, CacheItemPriority.Normal, Nothing)
            End If
            If requiresUser Then
                context.User = Nothing
            End If
            If ((data Is Nothing) OrElse (data.Length = 0)) Then
                Return Nothing
            Else
                Return New MemoryStream(data)
            End If
        End Function
        
        Public Overridable Function GetSurvey(ByVal surveyName As String) As String
            Dim root = Path.Combine(HttpRuntime.AppDomainAppPath, "js", "surveys")
            Dim survey = ControllerConfigurationUtility.GetFileText(Path.Combine(root, (surveyName + ".min.js")), Path.Combine(root, (surveyName + ".js")))
            Dim layout = ControllerConfigurationUtility.GetFileText(Path.Combine(root, (surveyName + ".html")), Path.Combine(root, (surveyName + ".htm")))
            Dim rules = ControllerConfigurationUtility.GetFileText(Path.Combine(root, (surveyName + ".rules.min.js")), Path.Combine(root, (surveyName + ".rules.js")))
            If String.IsNullOrEmpty(survey) Then
                survey = ControllerConfigurationUtility.GetResourceText(String.Format("Norwindt.Surveys.{0}.min.js", surveyName), String.Format("Norwindt.{0}.min.js", surveyName), String.Format("Norwindt.Surveys.{0}.js", surveyName), String.Format("Norwindt.{0}.js", surveyName))
            End If
            If String.IsNullOrEmpty(survey) Then
                Throw New HttpException(404, "Not found.")
            End If
            If String.IsNullOrEmpty(layout) Then
                layout = ControllerConfigurationUtility.GetResourceText(String.Format("Norwindt.Surveys.{0}.html", surveyName), String.Format("Norwindt.Surveys.{0}.htm", surveyName), String.Format("Norwindt.{0}.html", surveyName), String.Format("Norwindt.{0}.htm", surveyName))
            End If
            If String.IsNullOrEmpty(rules) Then
                rules = ControllerConfigurationUtility.GetResourceText(String.Format("Norwindt.Surveys.{0}.rules.min.js", surveyName), String.Format("Norwindt.{0}.rules.min.js", surveyName), String.Format("Norwindt.Surveys.{0}.rules.js", surveyName), String.Format("Norwindt.{0}.rules.js", surveyName))
            End If
            Dim sb = New StringBuilder()
            If Not (String.IsNullOrEmpty(rules)) Then
                sb.AppendLine("(function() {")
                sb.AppendFormat("$app.survey('register', '{0}', function () {{", surveyName)
                sb.AppendLine()
                sb.AppendLine(rules)
                sb.AppendLine("});")
                sb.AppendLine("})();")
            End If
            If Not (String.IsNullOrEmpty(layout)) Then
                survey = Regex.Replace(survey, "}\s*\)\s*;?\s*$", String.Format(", layout: '{0}' }});", HttpUtility.JavaScriptStringEncode(layout)))
            End If
            sb.Append(survey)
            Return sb.ToString()
        End Function
        
        Protected Overridable Function ExecuteVirtualReader(ByVal request As PageRequest, ByVal page As ViewPage) As DbDataReader
            Dim table = New DataTable()
            For Each field in page.Fields
                table.Columns.Add(field.Name, GetType(Integer))
            Next
            Dim r = table.NewRow()
            If page.ContainsField("PrimaryKey") Then
                r("PrimaryKey") = 1
            End If
            table.Rows.Add(r)
            Return New DataTableReader(table)
        End Function
        
        Protected Overridable Function GetRequestedViewType(ByVal page As ViewPage) As String
            Dim viewType = page.ViewType
            If String.IsNullOrEmpty(viewType) Then
                viewType = m_View.GetAttribute("type", String.Empty)
            End If
            Return viewType
        End Function
        
        Protected Overridable Sub EnsureSystemPageFields(ByVal request As PageRequest, ByVal page As ViewPage, ByVal command As DbCommand)
            If page.Distinct Then
                Dim i = 0
                Do While (i < page.Fields.Count)
                    If page.Fields(i).IsPrimaryKey Then
                        page.Fields.RemoveAt(i)
                    Else
                        i = (i + 1)
                    End If
                Loop
                Dim field = New DataField()
                field.Name = "group_count_"
                field.Type = "Double"
                page.Fields.Add(field)
            End If
            If Not (RequiresHierarchy(page)) Then
                Return
            End If
            Dim requiresHierarchyOrganization = false
            For Each field in page.Fields
                If field.IsTagged("hierarchy-parent") Then
                    requiresHierarchyOrganization = true
                Else
                    If field.IsTagged("hierarchy-organization") Then
                        requiresHierarchyOrganization = false
                        Exit For
                    End If
                End If
            Next
            If requiresHierarchyOrganization Then
                Dim field = New DataField()
                field.Name = HierarchyOrganizationFieldName
                field.Type = "String"
                field.Tag = "hierarchy-organization"
                field.Len = 255
                field.Columns = 20
                field.Hidden = true
                field.ReadOnly = true
                page.Fields.Add(field)
            End If
        End Sub
        
        Protected Overridable Function RequiresHierarchy(ByVal page As ViewPage) As Boolean
            If Not ((GetRequestedViewType(page) = "DataSheet")) Then
                Return false
            End If
            For Each field in page.Fields
                If field.IsTagged("hierarchy-parent") Then
                    If ((Not (page.Filter) Is Nothing) AndAlso (page.Filter.Length > 0)) Then
                        Return false
                    End If
                    Return true
                End If
            Next
            Return false
        End Function
        
        Protected Overloads Overridable Function DatabaseEngineIs(ByVal command As DbCommand, ByVal ParamArray flavors() as System.[String]) As Boolean
            Return DatabaseEngineIs(command.GetType().FullName, flavors)
        End Function
        
        Protected Overloads Overridable Function DatabaseEngineIs(ByVal typeName As String, ByVal ParamArray flavors() as System.[String]) As Boolean
            For Each s in flavors
                If typeName.Contains(s) Then
                    Return true
                End If
            Next
            Return false
        End Function
        
        Protected Shared Sub FullAccess(ByVal grant As Boolean, ByVal ParamArray controllers() as System.[String])
            Dim access = CType(HttpContext.Current.Items("Controller_AccessGranted"),SortedDictionary(Of String, Integer))
            If (access Is Nothing) Then
                access = New SortedDictionary(Of String, Integer)()
                HttpContext.Current.Items("Controller_AccessGranted") = access
            End If
            For Each c in controllers
                Dim count = 0
                access.TryGetValue(c, count)
                If grant Then
                    count = (count + 1)
                Else
                    count = (count - 1)
                End If
                access(c) = count
            Next
        End Sub
        
        Public Shared Function FullAccessGranted(ByVal controller As String) As Boolean
            Dim access = CType(HttpContext.Current.Items("Controller_AccessGranted"),SortedDictionary(Of String, Integer))
            Dim count = 0
            If (Not (access) Is Nothing) Then
                access.TryGetValue(controller, count)
                If (count = 0) Then
                    access.TryGetValue("*", count)
                End If
            End If
            Return (count > 0)
        End Function
        
        Protected Overridable Function ValidateViewAccess(ByVal controller As String, ByVal view As String, ByVal access As String) As Boolean
            If Not (ApplicationServicesBase.AuthorizationIsSupported) Then
                Return true
            End If
            Dim context = HttpContext.Current
            If ((AllowPublicAccess OrElse FullAccessGranted(controller)) OrElse (context.Request.Params("_validationKey") = BlobAdapter.ValidationKey)) Then
                Return true
            End If
            If AccessControlList.Current.Enabled Then
                Return true
            End If
            Dim allow = true
            Return allow
        End Function
        
        Function ExecuteResultSetTable(ByVal page As ViewPage) As DataTable
            If (m_ServerRules.ResultSet Is Nothing) Then
                Return Nothing
            End If
            Dim expressions = New SelectClauseDictionary()
            For Each c As DataColumn in m_ServerRules.ResultSet.Columns
                expressions(c.ColumnName) = c.ColumnName
            Next
            If (page.Fields.Count = 0) Then
                PopulatePageFields(page)
                EnsurePageFields(page, Nothing)
            End If
            Dim resultView = New DataView(m_ServerRules.ResultSet)
            resultView.Sort = page.SortExpression
            Using connection = CreateConnection(false)
                Dim command = connection.CreateCommand()
                Dim sb = New StringBuilder()
                m_ResultSetParameters = command.Parameters
                expressions.Add("_DataView_RowFilter_", "true")
                AppendFilterExpressionsToWhere(sb, page, command, expressions, String.Empty)
                Dim filter = sb.ToString()
                If filter.StartsWith("where") Then
                    filter = filter.Substring(5)
                End If
                filter = Regex.Replace(filter, (Regex.Escape(m_ParameterMarker) + "\w+"), AddressOf DoReplaceResultSetParameter)
                resultView.RowFilter = filter
                If (page.PageSize > 0) Then
                    page.TotalRowCount = resultView.Count
                End If
            End Using
            If RequiresPreFetching(page) Then
                page.ResetSkipCount(true)
            End If
            Dim result = resultView.ToTable()
            Dim fieldFilter = page.RequestedFieldFilter()
            If ((Not (fieldFilter) Is Nothing) AndAlso (fieldFilter.Length > 0)) Then
                Dim fieldIndex = 0
                Do While (fieldIndex < page.Fields.Count)
                    Dim outputField = page.Fields(fieldIndex)
                    Dim fieldName = outputField.Name
                    If ((Array.IndexOf(fieldFilter, fieldName) = -1) AndAlso (Not ((fieldName = "group_count_")) AndAlso Not (outputField.IsPrimaryKey))) Then
                        page.Fields.RemoveAt(fieldIndex)
                    Else
                        fieldIndex = (fieldIndex + 1)
                    End If
                Loop
            End If
            If page.Distinct Then
                Dim groupedTable = result.DefaultView.ToTable(true, fieldFilter)
                groupedTable.Columns.Add(New DataColumn("group_count_", GetType(Integer)))
                For Each r As DataRow in groupedTable.Rows
                    Dim filterExpression = New StringBuilder()
                    For Each fieldName in fieldFilter
                        If (filterExpression.Length > 0) Then
                            filterExpression.Append("and")
                        End If
                        filterExpression.AppendFormat("({0}='{1}')", fieldName, r(fieldName).ToString().Replace("'", "\'\'"))
                    Next
                    result.DefaultView.RowFilter = filterExpression.ToString()
                    r("group_count_") = result.DefaultView.Count
                Next
                result = groupedTable
            End If
            m_ServerRules.ResultSetSize = result.Rows.Count
            Return result
        End Function
        
        Function ExecuteResultSetReader(ByVal page As ViewPage) As DbDataReader
            If (m_ServerRules.ResultSet Is Nothing) Then
                Return Nothing
            End If
            Return ExecuteResultSetTable(page).CreateDataReader()
        End Function
        
        Protected Overridable Function DoReplaceResultSetParameter(ByVal m As Match) As String
            Dim p = m_ResultSetParameters(m.Value)
            Return String.Format("'{0}'", p.Value.ToString().Replace("'", "''"))
        End Function
        
        Function RequiresPreFetching(ByVal page As ViewPage) As Boolean
            Dim viewType = page.ViewType
            If String.IsNullOrEmpty(viewType) Then
                viewType = m_View.GetAttribute("type", String.Empty)
            End If
            Return (Not ((page.PageSize = Int32.MaxValue)) AndAlso New ControllerUtilities().SupportsCaching(page, viewType))
        End Function
        
        Public Delegate Function SpecialConversionFunction(ByVal o As Object) As Object
    End Class
    
    Partial Public Class ControllerUtilities
        Inherits ControllerUtilitiesBase
    End Class
    
    Public Class ControllerUtilitiesBase
        
        Public Overridable ReadOnly Property SupportsScrollingInDataSheet() As Boolean
            Get
                Return false
            End Get
        End Property
        
        Public Overridable Function GetActionView(ByVal controller As String, ByVal view As String, ByVal action As String) As String
            Return view
        End Function
        
        Public Overridable Function UserIsInRole(ByVal ParamArray roles() as System.[String]) As Boolean
            Dim context = HttpContext.Current
            If (context Is Nothing) Then
                Return true
            End If
            Dim count = 0
            For Each r in roles
                If Not (String.IsNullOrEmpty(r)) Then
                    For Each role in r.Split(Global.Microsoft.VisualBasic.ChrW(44))
                        Dim testRole = role.Trim()
                        If Not (String.IsNullOrEmpty(testRole)) Then
                            If Not (context.User.Identity.IsAuthenticated) Then
                                Return false
                            End If
                            Dim roleKey = ("IsInRole_" + testRole)
                            Dim isInRole = context.Items(roleKey)
                            If (isInRole Is Nothing) Then
                                isInRole = context.User.IsInRole(testRole)
                                context.Items(roleKey) = isInRole
                            End If
                            If CType(isInRole,Boolean) Then
                                Return true
                            End If
                        End If
                        count = (count + 1)
                    Next
                End If
            Next
            Return (count = 0)
        End Function
        
        Public Overridable Function SupportsLastEnteredValues(ByVal controller As String) As Boolean
            Return false
        End Function
        
        Public Overridable Function SupportsCaching(ByVal page As ViewPage, ByVal viewType As String) As Boolean
            If (viewType = "DataSheet") Then
                If (Not (SupportsScrollingInDataSheet) AndAlso Not (ApplicationServices.IsTouchClient)) Then
                    page.SupportsCaching = false
                End If
            Else
                If (viewType = "Grid") Then
                    If Not (ApplicationServices.IsTouchClient) Then
                        page.SupportsCaching = false
                    End If
                Else
                    page.SupportsCaching = false
                End If
            End If
            Return page.SupportsCaching
        End Function
        
        Public Shared Function ValidateName(ByVal name As String) As String
            'Prevent injection of single quote in the name used in XPath queries.
            If Not (String.IsNullOrEmpty(name)) Then
                Return name.Replace("'", "_")
            End If
            Return name
        End Function
    End Class
    
    Public Class ControllerFactory
        
        Public Shared Function CreateDataController() As IDataController
            Return New Controller()
        End Function
        
        Public Shared Function CreateAutoCompleteManager() As IAutoCompleteManager
            Return New Controller()
        End Function
        
        Public Shared Function CreateDataEngine() As IDataEngine
            Return New Controller()
        End Function
        
        Public Shared Function GetDataControllerStream(ByVal controller As String) As Stream
            Return New Controller().GetDataControllerStream(controller)
        End Function
        
        Public Shared Function GetSurvey(ByVal survey As String) As String
            Return New Controller().GetSurvey(survey)
        End Function
    End Class
    
    Partial Public Class StringEncryptor
        Inherits StringEncryptorBase
        
        Public Overloads Shared Function ToString(ByVal o As Object) As String
            Dim enc = New StringEncryptor()
            Return enc.Encrypt(o.ToString())
        End Function
        
        Public Shared Function ToBase64String(ByVal o As Object) As String
            Return Convert.ToBase64String(Encoding.Default.GetBytes(ToString(o)))
        End Function
        
        Public Shared Function FromString(ByVal s As String) As String
            Dim enc = New StringEncryptor()
            Return enc.Decrypt(s)
        End Function
        
        Public Shared Function FromBase64String(ByVal s As String) As String
            Return FromString(Encoding.Default.GetString(Convert.FromBase64String(s)))
        End Function
    End Class
    
    Public Class StringEncryptorBase
        
        Public Overridable ReadOnly Property Key() As Byte()
            Get
                Return New Byte() {253, 124, 8, 201, 31, 27, 89, 189, 251, 47, 198, 241, 38, 78, 198, 193, 18, 179, 209, 220, 34, 84, 178, 99, 193, 84, 64, 15, 188, 98, 101, 153}
            End Get
        End Property
        
        Public Overridable ReadOnly Property IV() As Byte()
            Get
                Return New Byte() {87, 84, 163, 98, 205, 255, 139, 173, 16, 88, 88, 254, 133, 176, 55, 112}
            End Get
        End Property
        
        Public Overridable Function Encrypt(ByVal s As String) As String
            Dim plainText = Encoding.Default.GetBytes(String.Format("{0}$${1}", s, s.GetHashCode()))
            Dim cipherText() As Byte
            Using output = New MemoryStream()
                Using cOutput = New CryptoStream(output, Aes.Create().CreateEncryptor(Key, IV), CryptoStreamMode.Write)
                    cOutput.Write(plainText, 0, plainText.Length)
                End Using
                cipherText = output.ToArray()
            End Using
            Return Convert.ToBase64String(cipherText)
        End Function
        
        Public Overridable Function Decrypt(ByVal s As String) As String
            Dim cipherText = Convert.FromBase64String(s)
            Dim plainText() As Byte
            Using output = New MemoryStream()
                Using cOutput = New CryptoStream(output, Aes.Create().CreateDecryptor(Key, IV), CryptoStreamMode.Write)
                    cOutput.Write(cipherText, 0, cipherText.Length)
                End Using
                plainText = output.ToArray()
            End Using
            Dim plain = Encoding.Default.GetString(plainText)
            Dim parts = plain.Split(New String() {"$$"}, StringSplitOptions.None)
            If (Not ((parts.Length = 2)) OrElse Not ((parts(0).GetHashCode() = Convert.ToInt32(parts(1))))) Then
                Throw New Exception("Attempt to alter the hashed URL.")
            End If
            Return parts(0)
        End Function
    End Class
End Namespace
