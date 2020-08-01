Imports Newtonsoft.Json.Linq
Imports Norwindt.Services
Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.Common
Imports System.Text.RegularExpressions
Imports System.Web

Namespace Norwindt.Data
    
    Public Class DataTransaction
        Inherits DataConnection
        
        Public Sub New()
            Me.New("Norwindt")
        End Sub
        
        Public Sub New(ByVal connectionStringName As String)
            MyBase.New(connectionStringName, true)
        End Sub
    End Class
    
    Public Class DataConnection
        Inherits [Object]
        Implements IDisposable
        
        Private m_ConnectionStringName As String
        
        Private m_Disposed As Boolean
        
        Private m_KeepOpen As Boolean
        
        Private m_CanClose As Boolean
        
        Private m_Connection As DbConnection
        
        Private m_ParameterMarker As String
        
        Private m_LeftQuote As String
        
        Private m_RightQuote As String
        
        Private m_Transaction As DbTransaction
        
        Private m_TransactionsEnabled As Boolean
        
        Public Sub New(ByVal connectionStringName As String)
            Me.New(connectionStringName, false)
        End Sub
        
        Public Sub New(ByVal connectionStringName As String, ByVal keepOpen As Boolean)
            MyBase.New
            Me.m_ConnectionStringName = connectionStringName
            Me.m_KeepOpen = keepOpen
            Dim contextItems = HttpContext.Current.Items
            Me.m_Connection = CType(contextItems(ToContextKey("connection")),DbConnection)
            If (Me.m_Connection Is Nothing) Then
                Me.m_Connection = SqlStatement.CreateConnection(connectionStringName, true, m_ParameterMarker, m_LeftQuote, m_RightQuote)
                Me.m_CanClose = true
                If keepOpen Then
                    Dim transactionsEnabled = ApplicationServices.Settings("odp.transactions.enabled")
                    Me.m_TransactionsEnabled = ((transactionsEnabled Is Nothing) OrElse CType(transactionsEnabled,Boolean))
                    BeginTransaction()
                    contextItems(ToContextKey("connection")) = m_Connection
                    contextItems(ToContextKey("parameterMarker")) = m_ParameterMarker
                    contextItems(ToContextKey("leftQuote")) = m_LeftQuote
                    contextItems(ToContextKey("rightQuote")) = m_RightQuote
                End If
            Else
                m_Transaction = CType(contextItems(ToContextKey("transaction")),DbTransaction)
                m_ParameterMarker = CType(contextItems(ToContextKey("parameterMarker")),String)
                m_LeftQuote = CType(contextItems(ToContextKey("leftQuote")),String)
                m_RightQuote = CType(contextItems(ToContextKey("rightQuote")),String)
            End If
        End Sub
        
        Public ReadOnly Property Connection() As DbConnection
            Get
                Return m_Connection
            End Get
        End Property
        
        Public ReadOnly Property Transaction() As DbTransaction
            Get
                Return m_Transaction
            End Get
        End Property
        
        Public ReadOnly Property KeepOpen() As Boolean
            Get
                Return m_KeepOpen
            End Get
        End Property
        
        Public ReadOnly Property CanClose() As Boolean
            Get
                Return m_CanClose
            End Get
        End Property
        
        Public ReadOnly Property ConnectionStringName() As String
            Get
                Return m_ConnectionStringName
            End Get
        End Property
        
        Public ReadOnly Property ParameterMarker() As String
            Get
                Return m_ParameterMarker
            End Get
        End Property
        
        Public ReadOnly Property LeftQuote() As String
            Get
                Return m_LeftQuote
            End Get
        End Property
        
        Public ReadOnly Property RightQuote() As String
            Get
                Return m_RightQuote
            End Get
        End Property
        
        Sub IDisposable_Dispose() Implements IDisposable.Dispose
            Dispose(true)
        End Sub
        
        Public Sub Dispose(ByVal disposing As Boolean)
            Close()
            If Not (m_Disposed) Then
                If ((Not (m_Connection) Is Nothing) AndAlso m_CanClose) Then
                    m_Connection.Dispose()
                End If
                m_Disposed = true
            End If
            If disposing Then
                GC.SuppressFinalize(Me)
            End If
        End Sub
        
        Public Sub Close()
            If ((Not (m_Connection) Is Nothing) AndAlso (m_Connection.State = ConnectionState.Open)) Then
                If m_CanClose Then
                    Commit()
                    m_Connection.Close()
                    If m_KeepOpen Then
                        Dim contextItems = HttpContext.Current.Items
                        contextItems.Remove(ToContextKey("connection"))
                        contextItems.Remove(ToContextKey("transaction"))
                        contextItems.Remove(ToContextKey("parameterMarker"))
                        contextItems.Remove(ToContextKey("leftQuote"))
                        contextItems.Remove(ToContextKey("rightQuote"))
                    End If
                End If
            End If
        End Sub
        
        Protected Function ToContextKey(ByVal name As String) As String
            Return String.Format("DataConnection_{0}_{1}", m_ConnectionStringName, name)
        End Function
        
        Public Sub BeginTransaction()
            If m_TransactionsEnabled Then
                If (Not (Me.m_Transaction) Is Nothing) Then
                    Me.m_Transaction.Dispose()
                End If
                Me.m_Transaction = Me.m_Connection.BeginTransaction()
                HttpContext.Current.Items(ToContextKey("transaction")) = Me.m_Transaction
            End If
        End Sub
        
        Public Sub Commit()
            If (Not (Me.m_Transaction) Is Nothing) Then
                Me.m_Transaction.Commit()
                HttpContext.Current.Items(ToContextKey("transaction")) = Nothing
                Me.m_Transaction.Dispose()
                Me.m_Transaction = Nothing
            End If
        End Sub
        
        Public Sub Rollback()
            If (Not (Me.m_Transaction) Is Nothing) Then
                Me.m_Transaction.Rollback()
                HttpContext.Current.Items(ToContextKey("transaction")) = Nothing
                Me.m_Transaction.Dispose()
                Me.m_Transaction = Nothing
            End If
        End Sub
    End Class
    
    Public Class ControllerFieldValue
        Inherits FieldValue
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Controller As String
        
        Public Sub New()
            MyBase.New()
        End Sub
        
        Public Sub New(ByVal controller As String, ByVal fieldName As String, ByVal oldValue As Object, ByVal newValue As Object)
            MyBase.New(fieldName, oldValue, newValue)
            Me.Controller = controller
        End Sub
        
        Public Property Controller() As String
            Get
                Return m_Controller
            End Get
            Set
                m_Controller = value
            End Set
        End Property
    End Class
    
    Public Class CommitResult
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Date As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Sequence As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Index As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Errors() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Values As List(Of ControllerFieldValue)
        
        Public Sub New()
            MyBase.New
            m_Sequence = -1
            m_Index = -1
            m_Date = DateTime.Now.ToString("s")
            m_Values = New List(Of ControllerFieldValue)()
        End Sub
        
        ''' <summary>The timestamp indicating the start of ActionArgs log processing on the server.</summary>
        Public Property [Date]() As String
            Get
                Return m_Date
            End Get
            Set
                m_Date = value
            End Set
        End Property
        
        ''' <summary>The last committed sequence in the ActionArgs log. Equals -1 if no entries in the log were committed to the database.</summary>
        Public Property Sequence() As Integer
            Get
                Return m_Sequence
            End Get
            Set
                m_Sequence = value
            End Set
        End Property
        
        ''' <summary>The index of the ActionArgs entry in the log that has caused an error. Equals -1 if no errors were detected.</summary>
        Public Property Index() As Integer
            Get
                Return m_Index
            End Get
            Set
                m_Index = value
            End Set
        End Property
        
        ''' <summary>The array of errors reported when an entry in the log has failed to executed.</summary>
        Public Property Errors() As String()
            Get
                Return m_Errors
            End Get
            Set
                m_Errors = value
            End Set
        End Property
        
        ''' <summary>The list of values that includes resolved primary key values.</summary>
        Public Property Values() As List(Of ControllerFieldValue)
            Get
                Return m_Values
            End Get
            Set
                m_Values = value
            End Set
        End Property
        
        ''' <summary>Indicates that the log has been committed sucessfully. Returns false if property Index has any value other than -1.</summary>
        Public ReadOnly Property Success() As Boolean
            Get
                Return (Index = -1)
            End Get
        End Property
    End Class
    
    ''' <summary>Provides a mechism to execute an array of ActionArgs instances in the context of a transaction.
    ''' Transactions are enabled by default. The default "scope" is "all". The default "upload" is "all".
    ''' </summary>
    ''' <remarks>
    ''' Use the following definition in touch-settings.json file to control Offline Data Processor (ODP):
    ''' {
    ''' "odp": {
    ''' "enabled": true,
    ''' "transactions": {
    ''' "enabled": true,
    ''' "scope": "sequence",
    ''' "upload": "all"
    ''' }
    ''' }
    ''' }
    ''' </remarks>
    Public Class TransactionManager
        Inherits TransactionManagerBase
    End Class
    
    Public Class TransactionManagerBase
        
        Private m_Controllers As SortedDictionary(Of String, ControllerConfiguration)
        
        Private m_ResolvedKeys As SortedDictionary(Of String, Object)
        
        Private m_Pk As FieldValue
        
        Private m_CommitResult As CommitResult
        
        Public Sub New()
            MyBase.New
            m_Controllers = New SortedDictionary(Of String, ControllerConfiguration)()
            m_ResolvedKeys = New SortedDictionary(Of String, Object)()
        End Sub
        
        Protected Overridable Function LoadConfig(ByVal controllerName As String) As ControllerConfiguration
            Dim config As ControllerConfiguration = Nothing
            If Not (m_Controllers.TryGetValue(controllerName, config)) Then
                config = DataControllerBase.CreateConfigurationInstance([GetType](), controllerName)
                m_Controllers(controllerName) = config
            End If
            Return config
        End Function
        
        Protected Overridable Sub ResolvePrimaryKey(ByVal controllerName As String, ByVal fieldName As String, ByVal oldValue As Object, ByVal newValue As Object)
            m_ResolvedKeys(String.Format("{0}${1}", controllerName, oldValue)) = newValue
            m_CommitResult.Values.Add(New ControllerFieldValue(controllerName, fieldName, oldValue, newValue))
        End Sub
        
        Protected Overridable Sub ProcessArguments(ByVal config As ControllerConfiguration, ByVal args As ActionArgs)
            If (args.Values Is Nothing) Then
                Return
            End If
            Dim values = New FieldValueDictionary(args)
            m_Pk = Nothing
            'detect negative primary keys
            Dim pkNav = config.SelectSingleNode("/c:dataController/c:fields/c:field[@isPrimaryKey='true']")
            If (Not (pkNav) Is Nothing) Then
                Dim fvo As FieldValue = Nothing
                If values.TryGetValue(pkNav.GetAttribute("name", String.Empty), fvo) Then
                    Dim value = 0
                    If ((Not (fvo.Value) Is Nothing) AndAlso Integer.TryParse(Convert.ToString(fvo.Value), value)) Then
                        If (value < 0) Then
                            If (args.CommandName = "Insert") Then
                                'request a new row from business rules
                                Dim newRowRequest = New PageRequest()
                                newRowRequest.Controller = args.Controller
                                newRowRequest.View = args.View
                                newRowRequest.Inserting = true
                                newRowRequest.RequiresMetaData = true
                                newRowRequest.MetadataFilter = New String() {"fields"}
                                Dim page = ControllerFactory.CreateDataController().GetPage(newRowRequest.Controller, newRowRequest.View, newRowRequest)
                                If (Not (page.NewRow) Is Nothing) Then
                                    Dim i = 0
                                    Do While (i < page.NewRow.Length)
                                        Dim newValue = page.NewRow(i)
                                        If (Not (newValue) Is Nothing) Then
                                            Dim field = page.Fields(i)
                                            If field.IsPrimaryKey Then
                                                'resolve the value of the primary key
                                                ResolvePrimaryKey(args.Controller, fvo.Name, value, newValue)
                                                value = 0
                                                fvo.NewValue = newValue
                                            Else
                                                'inject a missing default value in the arguments
                                                Dim newFieldValue As FieldValue = Nothing
                                                If values.TryGetValue(field.Name, newFieldValue) Then
                                                    If Not (newFieldValue.Modified) Then
                                                        newFieldValue.NewValue = newValue
                                                        newFieldValue.Modified = true
                                                    End If
                                                Else
                                                    Dim newValues = New List(Of FieldValue)(args.Values)
                                                    newFieldValue = New FieldValue(field.Name, newValue)
                                                    newValues.Add(newFieldValue)
                                                    args.Values = newValues.ToArray()
                                                    values(field.Name) = newFieldValue
                                                End If
                                            End If
                                        End If
                                        i = (i + 1)
                                    Loop
                                End If
                            End If
                            'resolve the primary key after the command execution
                            If (value < 0) Then
                                If (args.CommandName = "Insert") Then
                                    If (pkNav.SelectSingleNode("c:items/@dataController", config.Resolver) Is Nothing) Then
                                        m_Pk = New FieldValue(fvo.Name, value)
                                        fvo.NewValue = Nothing
                                        fvo.Modified = false
                                    End If
                                Else
                                    'otherwise try to resolve the primary key
                                    Dim resolvedKey As Object = Nothing
                                    If m_ResolvedKeys.TryGetValue(String.Format("{0}${1}", args.Controller, fvo.Value), resolvedKey) Then
                                        If fvo.Modified Then
                                            fvo.NewValue = resolvedKey
                                        Else
                                            fvo.OldValue = resolvedKey
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If
            'resolve negative foreign keys
            If (m_ResolvedKeys.Count > 0) Then
                Dim fkIterator = config.Select("/c:dataController/c:fields/c:field[c:items/@dataController]")
                Do While fkIterator.MoveNext()
                    Dim fvo As FieldValue = Nothing
                    If values.TryGetValue(fkIterator.Current.GetAttribute("name", String.Empty), fvo) Then
                        Dim itemsDataControllerNav = fkIterator.Current.SelectSingleNode("c:items/@dataController", config.Resolver)
                        Dim resolvedKey As Object = Nothing
                        If m_ResolvedKeys.TryGetValue(String.Format("{0}${1}", itemsDataControllerNav.Value, fvo.Value), resolvedKey) Then
                            If fvo.Modified Then
                                fvo.NewValue = resolvedKey
                            Else
                                fvo.OldValue = resolvedKey
                            End If
                        End If
                    End If
                Loop
            End If
            'scan resolved primary keys and look for the one that are matching the keys referenced in SelectedValues, ExternalFilter, or Filter of the action
            For Each resolvedKeyInfo in m_ResolvedKeys.Keys
                Dim separatorIndex = resolvedKeyInfo.IndexOf("$")
                Dim resolvedController = resolvedKeyInfo.Substring(0, separatorIndex)
                Dim unresolvedKeyValue = resolvedKeyInfo.Substring((separatorIndex + 1))
                Dim resolvedKeyValue As Object = Nothing
                If ((args.Controller = resolvedController) AndAlso m_ResolvedKeys.TryGetValue(resolvedKeyInfo, resolvedKeyValue)) Then
                    Dim resolvedKeyValueAsString = resolvedKeyValue.ToString()
                    'resolve primary key references in SelectedValues
                    If (Not (args.SelectedValues) Is Nothing) Then
                        Dim selectedValueIndex = 0
                        Do While (selectedValueIndex < args.SelectedValues.Length)
                            Dim selectedKey = Regex.Split(args.SelectedValues(selectedValueIndex), ",")
                            Dim keyValueIndex = 0
                            Do While (keyValueIndex < selectedKey.Length)
                                If (selectedKey(keyValueIndex) = unresolvedKeyValue) Then
                                    selectedKey(keyValueIndex) = resolvedKeyValueAsString
                                    args.SelectedValues(selectedValueIndex) = String.Join(",", selectedKey)
                                    selectedKey = Nothing
                                    Exit Do
                                End If
                                keyValueIndex = (keyValueIndex + 1)
                            Loop
                            If (selectedKey Is Nothing) Then
                                Exit Do
                            End If
                            selectedValueIndex = (selectedValueIndex + 1)
                        Loop
                    End If
                End If
            Next
        End Sub
        
        Protected Overridable Sub ProcessResult(ByVal config As ControllerConfiguration, ByVal result As ActionResult)
            If (m_Pk Is Nothing) Then
                For Each fvo in result.Values
                    m_CommitResult.Values.Add(New ControllerFieldValue(config.ControllerName, fvo.Name, fvo.OldValue, fvo.NewValue))
                Next
            Else
                For Each fvo in result.Values
                    If (fvo.Name = m_Pk.Name) Then
                        ResolvePrimaryKey(config.ControllerName, fvo.Name, m_Pk.Value, fvo.Value)
                        Exit For
                    End If
                Next
            End If
        End Sub
        
        Public Overridable Function Commit(ByVal log As JArray) As CommitResult
            m_CommitResult = New CommitResult()
            Try 
                If (log.Count > 0) Then
                    Using tx = New DataTransaction(LoadConfig(CType(log(0)("controller"),String)).ConnectionStringName)
                        Dim index = -1
                        Dim sequence = -1
                        Dim lastSequence = sequence
                        Dim commitedValueCount = m_CommitResult.Values.Count
                        Dim transactionScope = CType(ApplicationServices.Settings("odp.transactions.scope"),String)
                        Dim i = 0
                        Do While (i < log.Count)
                            Dim entry = log(i)
                            Dim controller = CType(entry("controller"),String)
                            Dim view = CType(entry("view"),String)
                            ActionArgs.Forget()
                            Dim executeArgs = entry("args").ToObject(Of ActionArgs)()
                            If executeArgs.Sequence.HasValue Then
                                sequence = executeArgs.Sequence.Value
                                If (Not ((transactionScope = "all")) AndAlso (Not ((sequence = lastSequence)) AndAlso (i > 0))) Then
                                    tx.Commit()
                                    m_CommitResult.Sequence = lastSequence
                                    commitedValueCount = m_CommitResult.Values.Count
                                    tx.BeginTransaction()
                                End If
                                lastSequence = sequence
                            End If
                            Dim config = LoadConfig(executeArgs.Controller)
                            ProcessArguments(config, executeArgs)
                            Dim executeResult = ControllerFactory.CreateDataController().Execute(controller, view, executeArgs)
                            If (executeResult.Errors.Count > 0) Then
                                index = i
                                m_CommitResult.Index = index
                                m_CommitResult.Errors = executeResult.Errors.ToArray()
                                Exit Do
                            Else
                                ProcessResult(config, executeResult)
                            End If
                            i = (i + 1)
                        Loop
                        If (index = -1) Then
                            tx.Commit()
                            m_CommitResult.Sequence = sequence
                            commitedValueCount = m_CommitResult.Values.Count
                        Else
                            tx.Rollback()
                            m_CommitResult.Index = index
                            m_CommitResult.Values.RemoveRange(commitedValueCount, (m_CommitResult.Values.Count - commitedValueCount))
                        End If
                    End Using
                End If
            Catch ex As Exception
                m_CommitResult.Errors = New String() {ex.Message}
                m_CommitResult.Index = 0
            End Try
            Return m_CommitResult
        End Function
    End Class
End Namespace
