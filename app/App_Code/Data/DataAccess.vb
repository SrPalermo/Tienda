Imports System
Imports System.Configuration
Imports System.Data
Imports System.Data.Common
Imports System.Diagnostics
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Configuration

Namespace Norwindt.Data
    
    Public Class ConnectionStringSettingsFactoryBase
        
        Protected Overridable Function CreateSettings(ByVal connectionStringName As String) As ConnectionStringSettings
            If String.IsNullOrEmpty(connectionStringName) Then
                connectionStringName = "Norwindt"
            End If
            Return WebConfigurationManager.ConnectionStrings(connectionStringName)
        End Function
    End Class
    
    Partial Public Class ConnectionStringSettingsFactory
        Inherits ConnectionStringSettingsFactoryBase
        
        Public Shared Function Create(ByVal connectionStringName As String) As ConnectionStringSettings
            Dim settingsFactory = New ConnectionStringSettingsFactory()
            Return settingsFactory.CreateSettings(connectionStringName)
        End Function
    End Class
    
    Public Class SqlParam
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Name As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Value As Object
        
        Public Sub New(ByVal name As String, ByVal value As Object)
            MyBase.New
            Me.Name = name
            Me.Value = value
        End Sub
        
        Public Overridable Property Name() As String
            Get
                Return m_Name
            End Get
            Set
                m_Name = value
            End Set
        End Property
        
        Public Overridable Property Value() As Object
            Get
                Return m_Value
            End Get
            Set
                m_Value = value
            End Set
        End Property
    End Class
    
    Public Class SqlStatementBase
        
        Public Overridable Function ParseSql(ByVal sql As String) As String
            Return sql
        End Function
        
        Public Overridable Sub Configure(ByVal command As DbCommand)
        End Sub
    End Class
    
    Partial Public Class SqlStatement
        Inherits SqlStatementBase
        Implements IDisposable
        
        Private m_Disposed As Boolean
        
        Private m_Scalar As Object
        
        Private m_Connection As DbConnection
        
        Private m_Command As DbCommand
        
        Private m_Reader As DbDataReader
        
        Private m_CanCloseConnection As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Name As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_WriteExceptionsToEventLog As Boolean
        
        Private Shared m_SqlClientPatternEscape As Regex = New Regex("(%|_|\[)")
        
        Private m_ParameterMarker As String
        
        Private m_LeftQuote As String
        
        Private m_RightQuote As String
        
        Public Shared MinSqlServerDate As Date = New DateTime(1753, 1, 1)
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal commandType As CommandType, ByVal commandText As String, ByVal connectionStringName As String)
            MyBase.New
            If String.IsNullOrEmpty(connectionStringName) Then
                connectionStringName = "Norwindt"
            End If
            Dim key = ("DataConnection_" + connectionStringName)
            Dim context = HttpContext.Current
            If (Not (context) Is Nothing) Then
                m_Connection = CType(context.Items((key + "_connection")),DbConnection)
            End If
            If (m_Connection Is Nothing) Then
                m_CanCloseConnection = true
                m_Connection = CreateConnection(connectionStringName, false, m_ParameterMarker, m_LeftQuote, m_RightQuote)
            End If
            m_Command = SqlStatement.CreateCommand(m_Connection)
            m_Command.CommandType = commandType
            m_Command.CommandText = ParseSql(commandText)
            If Not (m_CanCloseConnection) Then
                m_Command.Transaction = CType(HttpContext.Current.Items((key + "_transaction")),DbTransaction)
                m_ParameterMarker = CType(HttpContext.Current.Items((key + "_parameterMarker")),String)
                m_LeftQuote = CType(HttpContext.Current.Items((key + "_leftQuote")),String)
                m_RightQuote = CType(HttpContext.Current.Items((key + "_rightQuote")),String)
            End If
        End Sub
        
        Public Property Name() As String
            Get
                Return m_Name
            End Get
            Set
                m_Name = value
            End Set
        End Property
        
        Public Property WriteExceptionsToEventLog() As Boolean
            Get
                Return m_WriteExceptionsToEventLog
            End Get
            Set
                m_WriteExceptionsToEventLog = value
            End Set
        End Property
        
        Public ReadOnly Property Reader() As DbDataReader
            Get
                Return m_Reader
            End Get
        End Property
        
        Public ReadOnly Property Command() As DbCommand
            Get
                Return m_Command
            End Get
        End Property
        
        Public ReadOnly Property Scalar() As Object
            Get
                Return m_Scalar
            End Get
        End Property
        
        Public ReadOnly Property Parameters() As DbParameterCollection
            Get
                Return m_Command.Parameters
            End Get
        End Property
        
        Public Overloads Default ReadOnly Property Item(ByVal name As String) As Object
            Get
                Return m_Reader(name)
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
        
        Public Overloads Default ReadOnly Property Item(ByVal index As Integer) As Object
            Get
                Return m_Reader(index)
            End Get
        End Property
        
        Public Overloads Shared Function CreateConnection(ByVal connectionStringName As String) As DbConnection
            Dim parameterMarker As String = Nothing
            Dim leftQuote As String = Nothing
            Dim rightQuote As String = Nothing
            Return CreateConnection(connectionStringName, true, parameterMarker, leftQuote, rightQuote)
        End Function
        
        Public Overloads Shared Function CreateConnection(ByVal connectionStringName As String, ByVal open As Boolean, ByRef parameterMarker As String, ByRef leftQuote As String, ByRef rightQuote As String) As DbConnection
            Dim settings = ConnectionStringSettingsFactory.Create(connectionStringName)
            If (settings Is Nothing) Then
                Throw New Exception(String.Format(connectionStringName))
            End If
            If (settings.ProviderName = "CodeOnTime.CustomDataProvider") Then
                open = false
                settings = New ConnectionStringSettings("CustomDataProvider", String.Empty, "System.Data.SqlClient")
            End If
            Dim providerName = settings.ProviderName
            Dim factory = DbProviderFactories.GetFactory(providerName)
            Dim connection = factory.CreateConnection()
            Dim connectionString = settings.ConnectionString
            If providerName.Contains("MySql") Then
                connectionString = (connectionString + "Allow User Variables=True")
            End If
            connection.ConnectionString = connectionString
            If open Then
                connection.Open()
            End If
            parameterMarker = ConvertTypeToParameterMarker(providerName)
            leftQuote = ConvertTypeToLeftQuote(providerName)
            rightQuote = ConvertTypeToRightQuote(providerName)
            Return connection
        End Function
        
        Public Shared Function EscapePattern(ByVal command As DbCommand, ByVal s As String) As String
            If String.IsNullOrEmpty(s) Then
                Return s
            End If
            If (command.GetType().FullName = "System.Data.SqlClient.SqlCommand") Then
                Return m_SqlClientPatternEscape.Replace(s, "[$1]")
            End If
            Return s
        End Function
        
        Public Shared Function GetParameterMarker(ByVal connectionStringName As String) As String
            Dim settings = ConnectionStringSettingsFactory.Create(connectionStringName)
            Return ConvertTypeToParameterMarker(settings.ProviderName)
        End Function
        
        Public Overloads Shared Function ConvertTypeToParameterMarker(ByVal t As Type) As String
            Return ConvertTypeToParameterMarker(t.FullName)
        End Function
        
        Public Overloads Shared Function ConvertTypeToParameterMarker(ByVal typeName As String) As String
            If (typeName.Contains("Oracle") OrElse typeName.Contains("SQLAnywhere")) Then
                Return ":"
            End If
            Return "@"
        End Function
        
        Public Shared Function ConvertTypeToLeftQuote(ByVal typeName As String) As String
            If typeName.Contains("OleDb") Then
                Return "["
            End If
            If typeName.Contains("MySql") Then
                Return "`"
            End If
            Return """"
        End Function
        
        Public Shared Function ConvertTypeToRightQuote(ByVal typeName As String) As String
            Dim quote = ConvertTypeToLeftQuote(typeName)
            If (quote = "[") Then
                Return "]"
            End If
            Return quote
        End Function
        
        Public Overridable Function StringToValue(ByVal s As String) As Object
            Dim v As Object = s
            Dim guidValue As Guid
            If Guid.TryParse(s, guidValue) Then
                v = guidValue
                If Command.GetType().FullName.Contains("Oracle") Then
                    v = guidValue.ToByteArray()
                End If
            End If
            Return v
        End Function
        
        Public Sub Close()
            If ((Not (m_Reader) Is Nothing) AndAlso Not (m_Reader.IsClosed)) Then
                m_Reader.Close()
            End If
            If (((Not (m_Command) Is Nothing) AndAlso (m_Command.Connection.State = ConnectionState.Open)) AndAlso m_CanCloseConnection) Then
                m_Command.Connection.Close()
            End If
        End Sub
        
        Sub IDisposable_Dispose() Implements IDisposable.Dispose
            Dispose(true)
        End Sub
        
        Public Sub Dispose(ByVal disposing As Boolean)
            Close()
            If Not (m_Disposed) Then
                If (Not (m_Reader) Is Nothing) Then
                    m_Reader.Dispose()
                End If
                If (Not (m_Command) Is Nothing) Then
                    m_Command.Dispose()
                End If
                If ((Not (m_Connection) Is Nothing) AndAlso m_CanCloseConnection) Then
                    m_Connection.Dispose()
                End If
                m_Disposed = true
            End If
            If disposing Then
                GC.SuppressFinalize(Me)
            End If
        End Sub
        
        Private Sub EnsureOpenConnection()
            If Not ((m_Connection.State = ConnectionState.Open)) Then
                m_Connection.Open()
            End If
        End Sub
        
        Public Overloads Function ExecuteReader() As DbDataReader
            Try 
                EnsureOpenConnection()
                m_Reader = m_Command.ExecuteReader()
                Return m_Reader
            Catch e As Exception
                Log(e)
                Throw
            End Try
        End Function
        
        Public Overloads Function ExecuteReader(ByVal parameters As Object) As DbDataReader
            AddParameters(parameters)
            Return ExecuteReader()
        End Function
        
        Public Overloads Function ExecuteScalar() As Object
            Try 
                EnsureOpenConnection()
                m_Scalar = m_Command.ExecuteScalar()
                Return m_Scalar
            Catch e As Exception
                Log(e)
                Throw
            End Try
        End Function
        
        Public Overloads Function ExecuteScalar(ByVal parameters As Object) As Object
            AddParameters(parameters)
            Return ExecuteScalar()
        End Function
        
        Public Overloads Function ExecuteNonQuery() As Integer
            Try 
                EnsureOpenConnection()
                Return m_Command.ExecuteNonQuery()
            Catch e As Exception
                Log(e)
                Throw
            End Try
        End Function
        
        Public Overloads Function ExecuteNonQuery(ByVal parameters As Object) As Integer
            AddParameters(parameters)
            Return ExecuteNonQuery()
        End Function
        
        Public Overloads Function Read() As Boolean
            Try 
                If (m_Reader Is Nothing) Then
                    ExecuteReader()
                End If
                Return m_Reader.Read()
            Catch e As Exception
                Log(e)
                Throw
            End Try
        End Function
        
        Public Overloads Function Read(ByVal parameters As Object) As Boolean
            AddParameters(parameters)
            Return Read()
        End Function
        
        Protected Overridable Sub Log(ByVal ex As Exception)
            If WriteExceptionsToEventLog Then
                Dim log = New EventLog("Application")
                log.Source = [GetType]().FullName
                Dim action As String = Nothing
                If Not (String.IsNullOrEmpty(Name)) Then
                    Dim parts = Name.Split(Global.Microsoft.VisualBasic.ChrW(44))
                    log.Source = parts(0)
                    If (parts.Length > 1) Then
                        action = parts(1)
                    End If
                End If
                Dim message = "An exception has occurred. Please check the Event Log."&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)
                If Not (String.IsNullOrEmpty(action)) Then
                    message = String.Format("{0}Action: {1}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10), message, action)
                End If
                message = String.Format("{0}Exception: {1}", message, message.ToString())
                log.WriteEntry(message)
            Else
                Throw ex
            End If
        End Sub
        
        Private Function AddParameterWithoutValue(ByVal parameterName As String) As DbParameter
            Dim p = m_Command.CreateParameter()
            p.ParameterName = parameterName
            p.Value = DBNull.Value
            m_Command.Parameters.Add(p)
            Return p
        End Function
        
        Private Function AddParameterWithValue(ByVal parameterName As String, ByVal value As Object) As DbParameter
            Dim p = m_Command.CreateParameter()
            p.ParameterName = parameterName
            If (((Not (value) Is Nothing) AndAlso TypeOf value Is Guid) AndAlso p.GetType().FullName.Contains("Oracle")) Then
                value = CType(value,Guid).ToByteArray()
            End If
            p.Value = value
            m_Command.Parameters.Add(p)
            Return p
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As SByte) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of SByte)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Byte) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Byte)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Short) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Short)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As UShort) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of UShort)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Integer) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Integer)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As UInteger) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of UInteger)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Long) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Long)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As ULong) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of ULong)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Single) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Single)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Decimal) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Decimal)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Double) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Double)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Char) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Char)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Boolean) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Boolean)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Date) As DbParameter
            Return AddParameterWithValue(parameterName, value)
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Nullable(Of Date)) As DbParameter
            If value.HasValue Then
                Return AddParameterWithValue(parameterName, value)
            Else
                Return AddParameterWithoutValue(parameterName)
            End If
        End Function
        
        Public Overloads Function AddParameter(ByVal parameterName As String, ByVal value As Object) As DbParameter
            If ((value Is Nothing) OrElse DBNull.Value.Equals(value)) Then
                Return AddParameterWithoutValue(parameterName)
            Else
                Return AddParameterWithValue(parameterName, value)
            End If
        End Function
        
        Public Sub AddParameters(ByVal parameters As Object)
            Dim paramList = BusinessObjectParameters.Create(ParameterMarker, parameters)
            For Each paramName in paramList.Keys
                AddParameter(paramName, paramList(paramName))
            Next
        End Sub
        
        Public Shared Function CreateCommand(ByVal connection As DbConnection) As DbCommand
            Dim command = connection.CreateCommand()
            Dim t = command.GetType()
            Dim typeName = t.FullName
            If (typeName.Contains("Oracle") AndAlso typeName.Contains("DataAccess")) Then
                t.GetProperty("BindByName").SetValue(command, true, Nothing)
            End If
            Using configurator = New SqlStatement()
                configurator.Configure(command)
            End Using
            Return command
        End Function
        
        Public Shared Function TryParseDate(ByVal t As Type, ByVal s As String, ByRef result As Date) As Boolean
            Dim success = Date.TryParse(s, result)
            If success Then
                If (t.FullName.Contains(".SqlClient.") AndAlso (result < MinSqlServerDate)) Then
                    Return false
                End If
                Return true
            End If
            Return false
        End Function
        
        ''' <summary>
        ''' The method will automatically locate and change an existing parameter for a given name with or without a parameter marker.
        ''' If the parameter is not found then a new parameter is created. Otherwise the value of an existing parameter is changed.
        ''' </summary>
        ''' <param name="name">The name of a parameter.</param>
        ''' <param name="value">The new value of a parameter.</param>
        Public Overridable Sub AssignParameter(ByVal name As String, ByVal value As Object)
            If ((Not (value) Is Nothing) AndAlso TypeOf value Is Boolean) Then
                If CType(value,Boolean) Then
                    value = 1
                Else
                    value = 0
                End If
            End If
            If Not (name.StartsWith(ParameterMarker)) Then
                name = (ParameterMarker + name)
            End If
            For Each p As DbParameter in Command.Parameters
                If (p.ParameterName = name) Then
                    p.Value = value
                    Return
                End If
            Next
            AddParameter(name, value)
        End Sub
    End Class
    
    Public Class SqlProcedure
        Inherits SqlStatement
        
        Public Sub New(ByVal procedureName As String)
            Me.New(procedureName, Nothing)
        End Sub
        
        Public Sub New(ByVal procedureName As String, ByVal connectionStringName As String)
            MyBase.New(CommandType.StoredProcedure, procedureName, connectionStringName)
        End Sub
    End Class
    
    Public Class SqlText
        Inherits SqlStatement
        
        Public Sub New(ByVal text As String)
            Me.New(text, Nothing)
        End Sub
        
        Public Sub New(ByVal text As String, ByVal connectionStringName As String)
            MyBase.New(CommandType.Text, text, connectionStringName)
        End Sub
        
        Public Shared Function Create(ByVal text As String, ByVal connectionStringName As String, ByVal ParamArray args() as System.[Object]) As SqlText
            Dim sel = New SqlText(text, connectionStringName)
            If ((args.Length = 1) AndAlso (args(0).GetType().Namespace Is Nothing)) Then
                Dim paramList = BusinessObjectParameters.Create(sel.ParameterMarker, args)
                For Each paramName in paramList.Keys
                    sel.AddParameter(paramName, paramList(paramName))
                Next
            Else
                Dim m = Regex.Match(text, String.Format("({0}\w+)", sel.ParameterMarker))
                Dim parameterIndex = 0
                Do While m.Success
                    sel.AddParameter(m.Value, args(parameterIndex))
                    parameterIndex = (parameterIndex + 1)
                    m = m.NextMatch()
                Loop
            End If
            Return sel
        End Function
        
        Public Overloads Shared Function ExecuteScalar(ByVal text As String, ByVal ParamArray args() as System.[Object]) As Object
            Return ExecuteScalar(text, String.Empty, args)
        End Function
        
        Public Overloads Shared Function ExecuteScalar(ByVal text As String, ByVal connectionStringName As String, ByVal ParamArray args() as System.[Object]) As Object
            Using sel = Create(text, connectionStringName, args)
                Return sel.ExecuteScalar()
            End Using
        End Function
        
        Public Overloads Shared Function ExecuteNonQuery(ByVal text As String, ByVal ParamArray args() as System.[Object]) As Integer
            Return ExecuteNonQuery(text, String.Empty, args)
        End Function
        
        Public Overloads Shared Function ExecuteNonQuery(ByVal text As String, ByVal connectionStringName As String, ByVal ParamArray args() as System.[Object]) As Integer
            Using sel = Create(text, connectionStringName, args)
                Return sel.ExecuteNonQuery()
            End Using
        End Function
        
        Public Overloads Shared Function Execute(ByVal text As String, ByVal ParamArray args() as System.[Object]) As Object()
            Return Execute(text, String.Empty, args)
        End Function
        
        Public Overloads Shared Function Execute(ByVal text As String, ByVal connectionStringName As String, ByVal ParamArray args() as System.[Object]) As Object()
            Using sel = Create(text, connectionStringName, args)
                If sel.Read() Then
                    Dim result((sel.Reader.FieldCount) - 1) As Object
                    sel.Reader.GetValues(result)
                    Return result
                Else
                    Return Nothing
                End If
            End Using
        End Function
        
        Public Shared Function NextSequenceValue(ByVal sequence As String) As Integer
            Try 
                Return Convert.ToInt32(SqlText.ExecuteScalar(String.Format("select {0}.nextval from dual", sequence)))
            Catch __exception As Exception
                Return 0
            End Try
        End Function
        
        Public Shared Function NextGeneratorValue(ByVal generator As String) As Integer
            Try 
                Return Convert.ToInt32(SqlText.ExecuteScalar(String.Format("SELECT NEXT VALUE FOR {0} FROM RDB$DATABASE", generator)))
            Catch __exception As Exception
                Return 0
            End Try
        End Function
    End Class
End Namespace
