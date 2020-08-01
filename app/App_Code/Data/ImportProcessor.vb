Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.Common
Imports System.Data.OleDb
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Net.Mail
Imports System.Runtime.Serialization
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Web
Imports System.Web.Caching
Imports System.Web.Configuration

Namespace Norwindt.Data
    
    Public Class ImportMapDictionary
        Inherits SortedDictionary(Of Integer, DataField)
    End Class
    
    Public Class ImportLookupDictionary
        Inherits SortedDictionary(Of String, DataField)
    End Class
    
    Partial Public Class ImportProcessor
        Inherits ImportProcessorBase
    End Class
    
    Partial Public Class CsvImportProcessor
        Inherits ImportProcessorBase
        
        Public Overrides Function OpenRead(ByVal fileName As String, ByVal selectClause As String) As IDataReader
            Return New CsvReader(New StreamReader(fileName, true), true)
        End Function
        
        Public Overrides Function CountRecords(ByVal fileName As String) As Integer
            Dim count = 0
            Using reader = New CsvReader(New StreamReader(fileName), true)
                Do While reader.ReadNextRecord()
                    count = (count + 1)
                Loop
                Return count
            End Using
        End Function
    End Class
    
    Partial Public Class ImportProcessorFactory
        Inherits ImportProcessorFactoryBase
    End Class
    
    Public Class ImportProcessorFactoryBase
        
        Public Overridable Function CreateProcessor(ByVal fileName As String) As ImportProcessorBase
            Dim extension = Path.GetExtension(fileName).ToLower()
            If (extension.Contains(".xls") OrElse extension.Contains(".xlsx")) Then
                Return New ImportProcessor()
            End If
            If (extension.Contains(".csv") OrElse extension.Contains(".txt")) Then
                Return New CsvImportProcessor()
            End If
            Throw New Exception(String.Format("The format of file <b>{0}</b> is not supported.", Path.GetFileName(fileName)))
        End Function
        
        Public Shared Function Create(ByVal fileName As String) As ImportProcessorBase
            Dim factory = New ImportProcessorFactory()
            Return factory.CreateProcessor(fileName)
        End Function
    End Class
    
    Public Class ImportProcessorBase
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Shared ReadOnly Property SharedTempPath() As String
            Get
                Dim p = WebConfigurationManager.AppSettings("SharedTempPath")
                If String.IsNullOrEmpty(p) Then
                    p = Path.GetTempPath()
                End If
                If Not (Path.IsPathRooted(p)) Then
                    p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p)
                End If
                Return p
            End Get
        End Property
        
        Public Shared Sub Execute(ByVal args As ActionArgs)
            Process(args)
        End Sub
        
        Private Overloads Shared Sub Process(ByVal args As Object)
            Dim arguments = New List(Of String)(CType(args,ActionArgs).CommandArgument.Split(Global.Microsoft.VisualBasic.ChrW(59)))
            Dim fileName = Path.Combine(ImportProcessor.SharedTempPath, arguments(0))
            arguments.RemoveAt(0)
            Dim controller = arguments(0)
            arguments.RemoveAt(0)
            Dim view = arguments(0)
            arguments.RemoveAt(0)
            Dim notify = arguments(0)
            arguments.RemoveAt(0)
            Dim ip = ImportProcessorFactory.Create(fileName)
            Try 
                ip.Process(fileName, controller, view, notify, arguments)
            Finally
                If File.Exists(fileName) Then
                    Try 
                        File.Delete(fileName)
                    Catch __exception As Exception
                    End Try
                End If
            End Try
        End Sub
        
        Public Overridable Function OpenRead(ByVal fileName As String, ByVal selectClause As String) As IDataReader
            Dim extension = Path.GetExtension(fileName).ToLower()
            Dim tableName As String = Nothing
            Dim connectionString = New OleDbConnectionStringBuilder()
            connectionString.Provider = "Microsoft.ACE.OLEDB.12.0"
            If (extension = ".csv") Then
                connectionString("Extended Properties") = "text;HDR=Yes;FMT=Delimited"
                connectionString.DataSource = Path.GetDirectoryName(fileName)
                tableName = Path.GetFileName(fileName)
            Else
                If (extension = ".xls") Then
                    connectionString("Extended Properties") = "Excel 8.0;HDR=Yes;IMEX=1"
                    connectionString.DataSource = fileName
                Else
                    If (extension = ".xlsx") Then
                        connectionString("Extended Properties") = "Excel 12.0 Xml;HDR=YES"
                        connectionString.DataSource = fileName
                    End If
                End If
            End If
            Dim connection = New OleDbConnection(connectionString.ToString())
            connection.Open()
            If String.IsNullOrEmpty(tableName) Then
                Dim tables = connection.GetSchema("Tables")
                tableName = Convert.ToString(tables.Rows(0)("TABLE_NAME"))
            End If
            Try 
                Dim command = connection.CreateCommand()
                command.CommandText = String.Format("select {0} from [{1}]", selectClause, tableName)
                Return command.ExecuteReader(CommandBehavior.CloseConnection)
            Catch __exception As Exception
                connection.Close()
                Throw
            End Try
        End Function
        
        Private Sub EnumerateFields(ByVal reader As IDataReader, ByVal page As ViewPage, ByVal map As ImportMapDictionary, ByVal lookups As ImportLookupDictionary, ByVal userMapping As List(Of String))
            Dim mappedFields = New List(Of String)()
            Dim i = 0
            Do While (i < reader.FieldCount)
                Dim fieldName = reader.GetName(i)
                Dim field As DataField = Nothing
                Dim autoDetect = true
                If (Not (userMapping) Is Nothing) Then
                    Dim mappedFieldName = userMapping(i)
                    autoDetect = String.IsNullOrEmpty(mappedFieldName)
                    If Not (autoDetect) Then
                        fieldName = mappedFieldName
                    End If
                End If
                If autoDetect Then
                    For Each f in page.Fields
                        If (fieldName.Equals(f.HeaderText, StringComparison.CurrentCultureIgnoreCase) OrElse fieldName.Equals(f.Label, StringComparison.CurrentCultureIgnoreCase)) Then
                            field = f
                            Exit For
                        End If
                    Next
                End If
                If (field Is Nothing) Then
                    field = page.FindField(fieldName)
                End If
                If (Not (field) Is Nothing) Then
                    If Not (String.IsNullOrEmpty(field.AliasName)) Then
                        field = page.FindField(field.AliasName)
                    End If
                    If Not (field.ReadOnly) Then
                        If Not (mappedFields.Contains(field.Name)) Then
                            map.Add(i, field)
                            mappedFields.Add(field.Name)
                        End If
                    Else
                        For Each f in page.Fields
                            If (f.AliasName = field.Name) Then
                                map.Add(i, field)
                                lookups.Add(field.Name, f)
                                Exit For
                            End If
                        Next
                    End If
                End If
                i = (i + 1)
            Loop
        End Sub
        
        Public Sub ResolveLookups(ByVal lookups As ImportLookupDictionary)
            For Each fieldName in lookups.Keys
                Dim lookupField = lookups(fieldName)
                If ((lookupField.Items.Count = 0) AndAlso (String.IsNullOrEmpty(lookupField.ItemsDataValueField) OrElse String.IsNullOrEmpty(lookupField.ItemsDataTextField))) Then
                    Dim lookupRequest = New PageRequest()
                    lookupRequest.Controller = lookupField.ItemsDataController
                    lookupRequest.View = lookupField.ItemsDataView
                    lookupRequest.RequiresMetaData = true
                    Dim lp = ControllerFactory.CreateDataController().GetPage(lookupRequest.Controller, lookupRequest.View, lookupRequest)
                    If String.IsNullOrEmpty(lookupField.ItemsDataValueField) Then
                        For Each f in lp.Fields
                            If f.IsPrimaryKey Then
                                lookupField.ItemsDataValueField = f.Name
                                Exit For
                            End If
                        Next
                    End If
                    If String.IsNullOrEmpty(lookupField.ItemsDataTextField) Then
                        For Each f in lp.Fields
                            If ((Not (f.IsPrimaryKey) AndAlso Not (f.Hidden)) AndAlso (Not (f.AllowNulls) OrElse (f.Type = "String"))) Then
                                lookupField.ItemsDataTextField = f.Name
                                Exit For
                            End If
                        Next
                    End If
                End If
            Next
        End Sub
        
        Protected Overridable Sub BeforeProcess(ByVal fileName As String, ByVal controller As String, ByVal view As String, ByVal notify As String, ByVal userMapping As List(Of String))
        End Sub
        
        Protected Overridable Sub AfterProcess(ByVal fileName As String, ByVal controller As String, ByVal view As String, ByVal notify As String, ByVal userMapping As List(Of String))
        End Sub
        
        Public Overloads Overridable Sub Process(ByVal fileName As String, ByVal controller As String, ByVal view As String, ByVal notify As String, ByVal userMapping As List(Of String))
            BeforeProcess(fileName, controller, view, notify, userMapping)
            Dim logFileName = Path.GetTempFileName()
            Dim log = File.CreateText(logFileName)
            log.WriteLine("{0:s} Import process started.", DateTime.Now)
            'retrieve metadata
            Dim request = New PageRequest()
            request.Controller = controller
            request.View = view
            request.RequiresMetaData = true
            Dim page = ControllerFactory.CreateDataController().GetPage(controller, view, request)
            'open data reader and enumerate fields
            Dim reader = OpenRead(fileName, "*")
            Dim map = New ImportMapDictionary()
            Dim lookups = New ImportLookupDictionary()
            EnumerateFields(reader, page, map, lookups, userMapping)
            'resolve lookup data value field and data text fields
            ResolveLookups(lookups)
            'insert records from the file
            Dim recordCount = 0
            Dim errorCount = 0
            Dim nfi = CultureInfo.CurrentCulture.NumberFormat
            Dim numberCleanupRegex = New Regex(String.Format("[^\d\{0}\{1}\{2}]", nfi.CurrencyDecimalSeparator, nfi.NegativeSign, nfi.NumberDecimalSeparator))
            Dim externalFilterValues = New SortedDictionary(Of String, Object)()
            If (Not (ActionArgs.Current.ExternalFilter) Is Nothing) Then
                For Each fvo in ActionArgs.Current.ExternalFilter
                    externalFilterValues(fvo.Name) = fvo.Value
                Next
            End If
            'prepare default values
            Dim newRequest = New PageRequest()
            newRequest.RequiresMetaData = true
            newRequest.Inserting = true
            newRequest.LastCommandName = "New"
            newRequest.LastCommandArgument = view
            newRequest.Controller = controller
            newRequest.View = view
            Dim newPage = ControllerFactory.CreateDataController().GetPage(controller, view, newRequest)
            Dim defaultValues = New SortedDictionary(Of String, Object)()
            Dim i = 0
            Do While (i < newPage.Fields.Count)
                defaultValues(newPage.Fields(i).Name) = newPage.NewRow(i)
                i = (i + 1)
            Loop
            'process data rows
            Do While reader.Read()
                Dim args = New ActionArgs()
                args.Controller = controller
                args.View = view
                args.LastCommandName = "New"
                args.CommandName = "Insert"
                Dim values = New List(Of FieldValue)()
                Dim valueDictionary = New SortedDictionary(Of String, String)()
                For Each index in map.Keys
                    Dim field = map(index)
                    Dim v = reader(index)
                    If String.Empty.Equals(v) Then
                        v = DBNull.Value
                    End If
                    If DBNull.Value.Equals(v) Then
                        v = defaultValues(field.Name)
                    Else
                        If (Not ((field.Type = "String")) AndAlso TypeOf v Is String) Then
                            Dim s = CType(v,String)
                            If (field.Type = "Boolean") Then
                                v = s.ToLower()
                            Else
                                If (Not (field.Type.StartsWith("Date")) AndAlso Not ((field.Type = "Time"))) Then
                                    v = numberCleanupRegex.Replace(s, String.Empty)
                                End If
                            End If
                        End If
                    End If
                    If (Not (v) Is Nothing) Then
                        Dim lookupField As DataField = Nothing
                        If lookups.TryGetValue(field.Name, lookupField) Then
                            If (lookupField.Items.Count > 0) Then
                                'copy static values
                                For Each item in lookupField.Items
                                    If Convert.ToString(item(1)).Equals(Convert.ToString(v), StringComparison.CurrentCultureIgnoreCase) Then
                                        values.Add(New FieldValue(lookupField.Name, item(0)))
                                    End If
                                Next
                            Else
                                Dim lookupRequest = New PageRequest()
                                lookupRequest.Controller = lookupField.ItemsDataController
                                lookupRequest.View = lookupField.ItemsDataView
                                lookupRequest.RequiresMetaData = true
                                lookupRequest.PageSize = 1
                                lookupRequest.Filter = New String() {String.Format("{0}:={1}{2}", lookupField.ItemsDataTextField, v, Convert.ToChar(0))}
                                Dim vp = ControllerFactory.CreateDataController().GetPage(lookupRequest.Controller, lookupRequest.View, lookupRequest)
                                If (vp.Rows.Count > 0) Then
                                    values.Add(New FieldValue(lookupField.Name, vp.Rows(0)(vp.Fields.IndexOf(vp.FindField(lookupField.ItemsDataValueField)))))
                                End If
                            End If
                        Else
                            values.Add(New FieldValue(field.Name, v))
                        End If
                        If (values.Count > 0) Then
                            Dim lastValue = values((values.Count - 1))
                            valueDictionary(lastValue.Name) = String.Empty
                        End If
                    End If
                Next
                recordCount = (recordCount + 1)
                If (values.Count > 0) Then
                    For Each field in page.Fields
                        If Not (valueDictionary.ContainsKey(field.Name)) Then
                            Dim missingField = New FieldValue(field.Name)
                            Dim missingValue As Object = Nothing
                            If externalFilterValues.TryGetValue(missingField.Name, missingValue) Then
                                missingField.NewValue = missingValue
                                missingField.Modified = true
                            End If
                            values.Add(missingField)
                        End If
                    Next
                    args.Values = values.ToArray()
                    Dim r = ControllerFactory.CreateDataController().Execute(controller, view, args)
                    If (r.Errors.Count > 0) Then
                        If Not (HandleError(r, args)) Then
                            log.WriteLine("{0:s} Error importing record #{1}.", DateTime.Now, recordCount)
                            log.WriteLine()
                            For Each s in r.Errors
                                log.WriteLine(s)
                            Next
                            For Each v in values
                                If v.Modified Then
                                    log.WriteLine("{0}={1};", v.Name, v.Value)
                                End If
                            Next
                            log.WriteLine()
                            errorCount = (errorCount + 1)
                        End If
                    End If
                Else
                    log.WriteLine("{0:s} Record #1 has been ignored.", DateTime.Now, recordCount)
                    errorCount = (errorCount + 1)
                End If
            Loop
            reader.Close()
            log.WriteLine("{0:s} Processed {1} records. Detected {2} errors.", DateTime.Now, recordCount, errorCount)
            log.Close()
            If Not (String.IsNullOrEmpty(notify)) Then
                ReportErrors(controller, notify, logFileName)
            End If
            File.Delete(logFileName)
            AfterProcess(fileName, controller, view, notify, userMapping)
        End Sub
        
        Protected Overridable Sub ReportErrors(ByVal controller As String, ByVal recipients As String, ByVal logFileName As String)
            Dim recipientsList = recipients.Split(Global.Microsoft.VisualBasic.ChrW(44))
            Dim client = New SmtpClient()
            For Each s in recipientsList
                Dim address = s.Trim()
                If Not (String.IsNullOrEmpty(address)) Then
                    Dim message = New MailMessage()
                    Try 
                        message.To.Add(New MailAddress(address))
                        message.Subject = String.Format("Import of {0} has been completed", controller)
                        message.Body = File.ReadAllText(logFileName)
                        client.Send(message)
                    Catch __exception As Exception
                    End Try
                End If
            Next
        End Sub
        
        Protected Overridable Function HandleError(ByVal r As ActionResult, ByVal args As ActionArgs) As Boolean
            Return false
        End Function
        
        Public Overridable Function CountRecords(ByVal fileName As String) As Integer
            Dim reader = OpenRead(fileName, "count(*)")
            Try 
                reader.Read()
                Return Convert.ToInt32(reader(0))
            Finally
                reader.Close()
            End Try
        End Function
        
        Public Overridable Function MapFieldName(ByVal field As DataField) As String
            Dim s = field.HeaderText
            If String.IsNullOrEmpty(s) Then
                s = field.Label
            End If
            If String.IsNullOrEmpty(s) Then
                s = field.Name
            End If
            Return s
        End Function
        
        Public Function CreateListOfAvailableFields(ByVal controller As String, ByVal view As String) As String
            Dim request = New PageRequest()
            request.Controller = controller
            request.View = view
            request.RequiresMetaData = true
            Dim page = ControllerFactory.CreateDataController().GetPage(controller, view, request)
            Dim sb = New StringBuilder()
            For Each f in page.Fields
                If (Not (f.Hidden) AndAlso Not (f.ReadOnly)) Then
                    sb.AppendFormat("{0}=", f.Name)
                    Dim field = f
                    If Not (String.IsNullOrEmpty(f.AliasName)) Then
                        field = page.FindField(f.AliasName)
                    End If
                    sb.AppendLine(MapFieldName(field))
                End If
            Next
            Return sb.ToString()
        End Function
        
        Public Function CreateInitialFieldMap(ByVal fileName As String, ByVal controller As String, ByVal view As String) As String
            'retreive metadata
            Dim request = New PageRequest()
            request.Controller = controller
            request.View = view
            request.RequiresMetaData = true
            Dim page = ControllerFactory.CreateDataController().GetPage(controller, view, request)
            'create initial map
            Dim sb = New StringBuilder()
            Dim reader = OpenRead(fileName, "*")
            Try 
                Dim map = New ImportMapDictionary()
                Dim lookups = New ImportLookupDictionary()
                EnumerateFields(reader, page, map, lookups, Nothing)
                Dim i = 0
                Do While (i < reader.FieldCount)
                    sb.AppendFormat("{0}=", reader.GetName(i))
                    Dim field As DataField = Nothing
                    If map.TryGetValue(i, field) Then
                        Dim fieldName = field.Name
                        For Each lookupField in lookups.Values
                            If (lookupField.AliasName = field.Name) Then
                                fieldName = lookupField.Name
                                Exit For
                            End If
                        Next
                        sb.Append(fieldName)
                    End If
                    sb.AppendLine()
                    i = (i + 1)
                Loop
            Finally
                reader.Close()
            End Try
            Return sb.ToString()
        End Function
    End Class
    
    ''' Copyright (c) 2005 Sébastien Lorion
    ''' MIT license (http://en.wikipedia.org/wiki/MIT_License)
    ''' Permission is hereby granted, free of charge, to any person obtaining a copy
    ''' of this software and associated documentation files (the "Software"), to deal
    ''' in the Software without restriction, including without limitation the rights
    ''' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
    ''' of the Software, and to permit persons to whom the Software is furnished to do so,
    ''' subject to the following conditions:
    ''' The above copyright notice and this permission notice shall be included in all
    ''' copies or substantial portions of the Software.
    ''' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    ''' INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
    ''' PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
    ''' FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
    ''' ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    Partial Public Class CsvReader
        Inherits Object
        Implements IDataReader, IDisposable
        
        Public Const DefaultBufferSize As Integer = 4096
        
        Public Const DefaultDelimiter As Char = Global.Microsoft.VisualBasic.ChrW(44)
        
        Public Const DefaultQuote As Char = Global.Microsoft.VisualBasic.ChrW(34)
        
        Public Const DefaultEscape As Char = Global.Microsoft.VisualBasic.ChrW(34)
        
        Public Const DefaultComment As Char = Global.Microsoft.VisualBasic.ChrW(35)
        
        Private m_FieldHeaderComparer As StringComparer = StringComparer.CurrentCultureIgnoreCase
        
        Private m_Reader As TextReader
        
        Private m_Comment As Char
        
        Private m_Escape As Char
        
        Private m_Delimiter As Char
        
        Private m_Quote As Char
        
        Private m_HasHeaders As Boolean
        
        Private m_TrimmingOptions As ValueTrimmingOptions
        
        Private m_BufferSize As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DefaultParseErrorAction As ParseErrorAction
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_MissingFieldAction As MissingFieldAction
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SupportsMultiline As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SkipEmptyLines As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_DefaultHeaderName As String
        
        Private m_FieldCount As Integer
        
        Private m_Eof As Boolean
        
        Private m_FieldHeaders() As String
        
        Private m_FieldHeaderIndexes As Dictionary(Of String, Integer)
        
        Private m_CurrentRecordIndex As Long
        
        Private m_MissingFieldFlag As Boolean
        
        Private m_ParseErrorFlag As Boolean
        
        Private m_Initialized As Boolean
        
        Private m_Buffer() As Char
        
        Private m_BufferLength As Integer
        
        Private m_Fields() As String
        
        Private m_NextFieldStart As Integer
        
        Private m_NextFieldIndex As Integer
        
        Private m_Eol As Boolean
        
        Private m_FirstRecordInCache As Boolean
        
        Private m_IsDisposed As Boolean = false
        
        Public Sub New(ByVal reader As TextReader, ByVal hasHeaders As Boolean)
            Me.New(reader, hasHeaders, DefaultDelimiter, DefaultQuote, DefaultEscape, DefaultComment, ValueTrimmingOptions.UnquotedOnly, DefaultBufferSize)
        End Sub
        
        Public Sub New(ByVal reader As TextReader, ByVal hasHeaders As Boolean, ByVal bufferSize As Integer)
            Me.New(reader, hasHeaders, DefaultDelimiter, DefaultQuote, DefaultEscape, DefaultComment, ValueTrimmingOptions.UnquotedOnly, bufferSize)
        End Sub
        
        Public Sub New(ByVal reader As TextReader, ByVal hasHeaders As Boolean, ByVal delimiter As Char)
            Me.New(reader, hasHeaders, delimiter, DefaultQuote, DefaultEscape, DefaultComment, ValueTrimmingOptions.UnquotedOnly, DefaultBufferSize)
        End Sub
        
        Public Sub New(ByVal reader As TextReader, ByVal hasHeaders As Boolean, ByVal delimiter As Char, ByVal bufferSize As Integer)
            Me.New(reader, hasHeaders, delimiter, DefaultQuote, DefaultEscape, DefaultComment, ValueTrimmingOptions.UnquotedOnly, bufferSize)
        End Sub
        
        Public Sub New(ByVal reader As TextReader, ByVal hasHeaders As Boolean, ByVal delimiter As Char, ByVal quote As Char, ByVal escape As Char, ByVal comment As Char, ByVal trimmingOptions As ValueTrimmingOptions)
            Me.New(reader, hasHeaders, delimiter, quote, escape, comment, trimmingOptions, DefaultBufferSize)
        End Sub
        
        Public Sub New(ByVal reader As TextReader, ByVal hasHeaders As Boolean, ByVal delimiter As Char, ByVal quote As Char, ByVal escape As Char, ByVal comment As Char, ByVal trimmingOptions As ValueTrimmingOptions, ByVal bufferSize As Integer)
            MyBase.New
            If (reader Is Nothing) Then
                Throw New ArgumentNullException("reader")
            End If
            If (bufferSize <= 0) Then
                Throw New ArgumentOutOfRangeException("bufferSize", bufferSize, ExceptionMessage.BufferSizeTooSmall)
            End If
            m_BufferSize = bufferSize
            If TypeOf reader Is StreamReader Then
                Dim stream = CType(reader,StreamReader).BaseStream
                If stream.CanSeek Then
                    If (stream.Length > 0) Then
                        m_BufferSize = CType(Math.Min(bufferSize, stream.Length),Integer)
                    End If
                End If
            End If
            m_Reader = reader
            m_Delimiter = delimiter
            m_Quote = quote
            m_Escape = escape
            m_Comment = comment
            m_HasHeaders = hasHeaders
            m_TrimmingOptions = trimmingOptions
            m_SupportsMultiline = true
            m_SkipEmptyLines = true
            Me.DefaultHeaderName = "Column"
            m_CurrentRecordIndex = -1
            m_DefaultParseErrorAction = ParseErrorAction.RaiseEvent
        End Sub
        
        Public ReadOnly Property Comment() As Char
            Get
                Return m_Comment
            End Get
        End Property
        
        Public ReadOnly Property Escape() As Char
            Get
                Return m_Escape
            End Get
        End Property
        
        Public ReadOnly Property Delimiter() As Char
            Get
                Return m_Delimiter
            End Get
        End Property
        
        Public ReadOnly Property Quote() As Char
            Get
                Return m_Quote
            End Get
        End Property
        
        Public ReadOnly Property HasHeaders() As Boolean
            Get
                Return m_HasHeaders
            End Get
        End Property
        
        Public ReadOnly Property TrimmingOptions() As ValueTrimmingOptions
            Get
                Return m_TrimmingOptions
            End Get
        End Property
        
        Public ReadOnly Property BufferSize() As Integer
            Get
                Return m_BufferSize
            End Get
        End Property
        
        Public Property DefaultParseErrorAction() As ParseErrorAction
            Get
                Return m_DefaultParseErrorAction
            End Get
            Set
                m_DefaultParseErrorAction = value
            End Set
        End Property
        
        Public Property MissingFieldAction() As MissingFieldAction
            Get
                Return m_MissingFieldAction
            End Get
            Set
                m_MissingFieldAction = value
            End Set
        End Property
        
        Public Property SupportsMultiline() As Boolean
            Get
                Return m_SupportsMultiline
            End Get
            Set
                m_SupportsMultiline = value
            End Set
        End Property
        
        Public Property SkipEmptyLines() As Boolean
            Get
                Return m_SkipEmptyLines
            End Get
            Set
                m_SkipEmptyLines = value
            End Set
        End Property
        
        Public Property DefaultHeaderName() As String
            Get
                Return m_DefaultHeaderName
            End Get
            Set
                m_DefaultHeaderName = value
            End Set
        End Property
        
        ReadOnly Property IDataRecord_FieldCount() As Integer Implements IDataRecord.FieldCount
            Get
                EnsureInitialize()
                Return m_FieldCount
            End Get
        End Property
        
        Public Overridable ReadOnly Property EndOfStream() As Boolean
            Get
                Return m_Eof
            End Get
        End Property
        
        Public ReadOnly Property CurrentRecordIndex() As Long
            Get
                Return m_CurrentRecordIndex
            End Get
        End Property
        
        Public ReadOnly Property MissingFieldFlag() As Boolean
            Get
                Return m_MissingFieldFlag
            End Get
        End Property
        
        Public ReadOnly Property ParseErrorFlag() As Boolean
            Get
                Return m_ParseErrorFlag
            End Get
        End Property
        
        Public Overloads Default ReadOnly Property Item(ByVal record As Integer, ByVal field As String) As String
            Get
                If Not (MoveTo(record)) Then
                    Throw New InvalidOperationException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.CannotReadRecordAtIndex, record))
                End If
                Return Me(field)
            End Get
        End Property
        
        Public Overloads Default ReadOnly Property Item(ByVal record As Integer, ByVal field As Integer) As String
            Get
                If Not (MoveTo(record)) Then
                    Throw New InvalidOperationException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.CannotReadRecordAtIndex, record))
                End If
                Return Me(field)
            End Get
        End Property
        
        Public Overloads Default ReadOnly Property Item(ByVal field As String) As String
            Get
                If String.IsNullOrEmpty(field) Then
                    Throw New ArgumentNullException("field")
                End If
                If Not (m_HasHeaders) Then
                    Throw New InvalidOperationException(ExceptionMessage.NoHeaders)
                End If
                Dim index = GetFieldIndex(field)
                If (index < 0) Then
                    Throw New ArgumentException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldHeaderNotFound, field), "field")
                End If
                Return Me(index)
            End Get
        End Property
        
        Public Overloads Overridable Default ReadOnly Property Item(ByVal field As Integer) As String
            Get
                Return ReadField(field, false, false)
            End Get
        End Property
        
        ReadOnly Property IDataReader_RecordsAffected() As Integer Implements IDataReader.RecordsAffected
            Get
                Return -1
            End Get
        End Property
        
        ReadOnly Property IDataReader_IsClosed() As Boolean Implements IDataReader.IsClosed
            Get
                Return m_Eof
            End Get
        End Property
        
        ReadOnly Property IDataReader_Depth() As Integer Implements IDataReader.Depth
            Get
                ValidateDataReader(DataReaderValidations.IsNotClosed)
                Return 0
            End Get
        End Property
        
        ReadOnly Property IDataRecord_Item(ByVal name As String) As Object Implements IDataRecord.Item
            Get
                ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
                Return Me(name)
            End Get
        End Property
        
        ReadOnly Property IDataRecord_Item(ByVal i As Integer) As Object Implements IDataRecord.Item
            Get
                ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
                Return Me(i)
            End Get
        End Property
        
        Public ReadOnly Property IsDisposed() As Boolean
            Get
                Return m_IsDisposed
            End Get
        End Property
        
        Public Event ParseError As EventHandler(Of ParseErrorEventArgs)
        
        Public Event Disposed As EventHandler
        
        Public Function GetFieldHeaders() As String()
            EnsureInitialize()
            Dim fieldHeaders((m_FieldHeaders.Length) - 1) As String
            Dim i = 0
            Do While (i < fieldHeaders.Length)
                fieldHeaders(i) = m_FieldHeaders(i)
                i = (i + 1)
            Loop
            Return fieldHeaders
        End Function
        
        Protected Overridable Sub OnParseError(ByVal e As ParseErrorEventArgs)
            Dim handler = Me.ParseErrorEvent
            If (Not (handler) Is Nothing) Then
                handler(Me, e)
            End If
        End Sub
        
        Private Sub EnsureInitialize()
            If Not (m_Initialized) Then
                Me.ReadNextRecord(true, false)
            End If
        End Sub
        
        Public Function GetFieldIndex(ByVal header As String) As Integer
            EnsureInitialize()
            Dim index As Integer
            If ((Not (m_FieldHeaderIndexes) Is Nothing) AndAlso m_FieldHeaderIndexes.TryGetValue(header, index)) Then
                Return index
            Else
                Return -1
            End If
        End Function
        
        Public Overloads Sub CopyCurrentRecordTo(ByVal array() As String)
            CopyCurrentRecordTo(array, 0)
        End Sub
        
        Public Overloads Sub CopyCurrentRecordTo(ByVal array() As String, ByVal index As Integer)
            If (array Is Nothing) Then
                Throw New ArgumentNullException("array")
            End If
            If ((index < 0) OrElse (index >= array.Length)) Then
                Throw New ArgumentOutOfRangeException("index", index, String.Empty)
            End If
            If ((m_CurrentRecordIndex < 0) OrElse Not (m_Initialized)) Then
                Throw New InvalidOperationException(ExceptionMessage.NoCurrentRecord)
            End If
            If ((array.Length - index) < m_FieldCount) Then
                Throw New ArgumentException(ExceptionMessage.NotEnoughSpaceInArray, "array")
            End If
            Dim i = 0
            Do While (i < m_FieldCount)
                If m_ParseErrorFlag Then
                    array((index + i)) = Nothing
                Else
                    array((index + i)) = Me(i)
                End If
                i = (i + 1)
            Loop
        End Sub
        
        Public Function GetCurrentRawData() As String
            If ((Not (m_Buffer) Is Nothing) And (m_BufferLength > 0)) Then
                Return New [String](m_Buffer, 0, m_BufferLength)
            Else
                Return String.Empty
            End If
        End Function
        
        Private Function IsWhiteSpace(ByVal c As Char) As Boolean
            If (c = m_Delimiter) Then
                Return false
            Else
                If (c <= Convert.ToChar(255)) Then
                    Return ((c = Global.Microsoft.VisualBasic.ChrW(32)) OrElse (c = Global.Microsoft.VisualBasic.ChrW(9)))
                Else
                    Return (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) = System.Globalization.UnicodeCategory.SpaceSeparator)
                End If
            End If
        End Function
        
        Public Overridable Function MoveTo(ByVal record As Long) As Boolean
            If (record < m_CurrentRecordIndex) Then
                Return false
            End If
            Dim offset = (record - m_CurrentRecordIndex)
            Do While (offset > 0)
                If Not (ReadNextRecord()) Then
                    Return false
                End If
                offset = (offset - 1)
            Loop
            Return true
        End Function
        
        Private Function ParseNewLine(ByRef pos As Integer) As Boolean
            If (pos = m_BufferLength) Then
                pos = 0
                If Not (ReadBuffer()) Then
                    Return false
                End If
            End If
            Dim c = m_Buffer(pos)
            If ((c = Global.Microsoft.VisualBasic.ChrW(13)) AndAlso Not ((m_Delimiter = Global.Microsoft.VisualBasic.ChrW(13)))) Then
                pos = (pos + 1)
                If (pos < m_BufferLength) Then
                    If (m_Buffer(pos) = Global.Microsoft.VisualBasic.ChrW(10)) Then
                        pos = (pos + 1)
                    End If
                Else
                    If ReadBuffer() Then
                        If (m_Buffer(0) = Global.Microsoft.VisualBasic.ChrW(10)) Then
                            pos = 1
                        Else
                            pos = 0
                        End If
                    End If
                End If
                If (pos >= m_BufferLength) Then
                    ReadBuffer()
                    pos = 0
                End If
                Return true
            Else
                If (c = Global.Microsoft.VisualBasic.ChrW(10)) Then
                    pos = (pos + 1)
                    If (pos >= m_BufferLength) Then
                        ReadBuffer()
                        pos = 0
                    End If
                    Return true
                End If
            End If
            Return false
        End Function
        
        Function IsNewLine(ByVal pos As Integer) As Boolean
            Dim c = m_Buffer(pos)
            If (c = Global.Microsoft.VisualBasic.ChrW(10)) Then
                Return true
            End If
            If ((c = Global.Microsoft.VisualBasic.ChrW(13)) AndAlso Not ((m_Delimiter = Global.Microsoft.VisualBasic.ChrW(13)))) Then
                Return true
            End If
            Return false
        End Function
        
        Private Function ReadBuffer() As Boolean
            If m_Eof Then
                Return false
            End If
            CheckDisposed()
            m_BufferLength = m_Reader.Read(m_Buffer, 0, m_BufferSize)
            If (m_BufferLength > 0) Then
                Return true
            Else
                m_Eof = true
                m_Buffer = Nothing
                Return false
            End If
        End Function
        
        Private Function ReadField(ByVal field As Integer, ByVal initializing As Boolean, ByVal discardValue As Boolean) As String
            If Not (initializing) Then
                If ((field < 0) OrElse (field >= m_FieldCount)) Then
                    Throw New ArgumentOutOfRangeException("field", field, String.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, field))
                End If
                If (m_CurrentRecordIndex < 0) Then
                    Throw New InvalidOperationException(ExceptionMessage.NoCurrentRecord)
                End If
                If (Not (m_Fields(field)) Is Nothing) Then
                    Return m_Fields(field)
                Else
                    If m_MissingFieldFlag Then
                        Return HandleMissingField(Nothing, field, m_NextFieldStart)
                    End If
                End If
            End If
            CheckDisposed()
            Dim index = m_NextFieldIndex
            Do While (index < (field + 1))
                If (m_NextFieldStart = m_BufferLength) Then
                    m_NextFieldStart = 0
                    ReadBuffer()
                End If
                Dim value As String = Nothing
                If m_MissingFieldFlag Then
                    value = HandleMissingField(value, index, m_NextFieldStart)
                Else
                    If (m_NextFieldStart = m_BufferLength) Then
                        If (index = field) Then
                            If Not (discardValue) Then
                                value = String.Empty
                                m_Fields(index) = value
                            End If
                            m_MissingFieldFlag = true
                        Else
                            value = HandleMissingField(value, index, m_NextFieldStart)
                        End If
                    Else
                        If ((m_TrimmingOptions And ValueTrimmingOptions.UnquotedOnly) <> 0) Then
                            SkipWhiteSpaces(m_NextFieldStart)
                        End If
                        If m_Eof Then
                            value = String.Empty
                            m_Fields(field) = value
                            If (field < m_FieldCount) Then
                                m_MissingFieldFlag = true
                            End If
                        Else
                            If Not ((m_Buffer(m_NextFieldStart) = m_Quote)) Then
                                Dim start = m_NextFieldStart
                                Dim pos = m_NextFieldStart
                                Do While true
                                    'read characters
                                    Do While (pos < m_BufferLength)
                                        Dim c = m_Buffer(pos)
                                        If (c = m_Delimiter) Then
                                            m_NextFieldStart = (pos + 1)
                                            Exit Do
                                        Else
                                            If ((c = Global.Microsoft.VisualBasic.ChrW(13)) OrElse (c = Global.Microsoft.VisualBasic.ChrW(10))) Then
                                                m_NextFieldStart = pos
                                                m_Eol = true
                                                Exit Do
                                            Else
                                                pos = (pos + 1)
                                            End If
                                        End If
                                    Loop
                                    If (pos < m_BufferLength) Then
                                        Exit Do
                                    Else
                                        If Not (discardValue) Then
                                            value = (value + New String(m_Buffer, start, (pos - start)))
                                        End If
                                        start = 0
                                        pos = 0
                                        m_NextFieldStart = 0
                                        If Not (ReadBuffer()) Then
                                            Exit Do
                                        End If
                                    End If
                                Loop
                                If Not (discardValue) Then
                                    If ((m_TrimmingOptions And ValueTrimmingOptions.UnquotedOnly) = 0) Then
                                        If (Not (m_Eof) AndAlso (pos > start)) Then
                                            value = (value + New String(m_Buffer, start, (pos - start)))
                                        End If
                                    Else
                                        If (Not (m_Eof) AndAlso (pos > start)) Then
                                            pos = (pos - 1)
                                            Do While ((pos > -1) AndAlso IsWhiteSpace(m_Buffer(pos)))
                                                pos = (pos - 1)
                                            Loop
                                            pos = (pos + 1)
                                            If (pos > 0) Then
                                                value = (value + New String(m_Buffer, start, (pos - start)))
                                            End If
                                        Else
                                            pos = -1
                                        End If
                                        If (pos <= 0) Then
                                            If (value Is Nothing) Then
                                                pos = -1
                                            Else
                                                pos = (value.Length - 1)
                                            End If
                                            Do While ((pos > -1) AndAlso IsWhiteSpace(value(pos)))
                                                pos = (pos - 1)
                                            Loop
                                            pos = (pos + 1)
                                            If ((pos > 0) AndAlso Not ((pos = value.Length))) Then
                                                value = value.Substring(0, pos)
                                            End If
                                        End If
                                    End If
                                    If (value Is Nothing) Then
                                        value = String.Empty
                                    End If
                                End If
                                If (m_Eol OrElse m_Eof) Then
                                    m_Eol = ParseNewLine(m_NextFieldStart)
                                    If (Not (initializing) AndAlso Not ((index = (m_FieldCount - 1)))) Then
                                        If ((Not (value) Is Nothing) AndAlso (value.Length = 0)) Then
                                            value = Nothing
                                        End If
                                        value = HandleMissingField(value, index, m_NextFieldStart)
                                    End If
                                End If
                                If Not (discardValue) Then
                                    m_Fields(index) = value
                                End If
                            Else
                                Dim start = (m_NextFieldStart + 1)
                                Dim pos = start
                                Dim quoted = true
                                Dim escaped = false
                                If Not (((m_TrimmingOptions And ValueTrimmingOptions.QuotedOnly) = 0)) Then
                                    SkipWhiteSpaces(start)
                                    pos = start
                                End If
                                Do While true
                                    'read value
                                    Do While (pos < m_BufferLength)
                                        Dim c = m_Buffer(pos)
                                        If escaped Then
                                            escaped = false
                                            start = pos
                                        Else
                                            If ((c = m_Escape) AndAlso (Not ((m_Escape = m_Quote)) OrElse ((((pos + 1) < m_BufferLength) AndAlso (m_Buffer((pos + 1)) = m_Quote)) OrElse (((pos + 1) = m_BufferLength) AndAlso m_Reader.Peek().Equals(m_Quote))))) Then
                                                If Not (discardValue) Then
                                                    value = (value + New String(m_Buffer, start, (pos - start)))
                                                End If
                                                escaped = true
                                            Else
                                                If (c = m_Quote) Then
                                                    quoted = false
                                                    Exit Do
                                                End If
                                            End If
                                        End If
                                        pos = (pos + 1)
                                    Loop
                                    If Not (quoted) Then
                                        Exit Do
                                    Else
                                        If (Not (discardValue) AndAlso Not (escaped)) Then
                                            value = (value + New String(m_Buffer, start, (pos - start)))
                                        End If
                                        start = 0
                                        pos = 0
                                        m_NextFieldStart = 0
                                        If Not (ReadBuffer()) Then
                                            HandleParseError(New MalformedCsvException(GetCurrentRawData(), m_NextFieldStart, Math.Max(0, m_CurrentRecordIndex), index), m_NextFieldStart)
                                            Return Nothing
                                        End If
                                    End If
                                Loop
                                If Not (m_Eof) Then
                                    If (Not (discardValue) AndAlso (pos > start)) Then
                                        value = (value + New String(m_Buffer, start, (pos - start)))
                                    End If
                                    If ((Not (discardValue) AndAlso (Not (value) Is Nothing)) AndAlso Not (((m_TrimmingOptions And ValueTrimmingOptions.QuotedOnly) = 0))) Then
                                        Dim newLength = value.Length
                                        Do While ((newLength > 0) AndAlso IsWhiteSpace(value((newLength - 1))))
                                            newLength = (newLength - 1)
                                        Loop
                                        If (newLength < value.Length) Then
                                            value = value.Substring(0, newLength)
                                        End If
                                    End If
                                    m_NextFieldStart = (pos + 1)
                                    SkipWhiteSpaces(m_NextFieldStart)
                                    Dim delimiterSkipped As Boolean
                                    If ((m_NextFieldStart < m_BufferLength) AndAlso (m_Buffer(m_NextFieldStart) = m_Delimiter)) Then
                                        delimiterSkipped = true
                                        m_NextFieldStart = (m_NextFieldStart + 1)
                                    Else
                                        delimiterSkipped = false
                                    End If
                                    If ((Not (m_Eof) AndAlso Not (delimiterSkipped)) AndAlso (initializing OrElse (index = (m_FieldCount - 1)))) Then
                                        m_Eol = ParseNewLine(m_NextFieldStart)
                                    End If
                                    If ((Not (delimiterSkipped) AndAlso Not (m_Eof)) AndAlso Not ((m_Eol OrElse IsNewLine(m_NextFieldStart)))) Then
                                        HandleParseError(New MalformedCsvException(GetCurrentRawData(), m_NextFieldStart, Math.Max(0, m_CurrentRecordIndex), index), m_NextFieldStart)
                                    End If
                                End If
                                If Not (discardValue) Then
                                    If (value Is Nothing) Then
                                        value = String.Empty
                                    End If
                                    m_Fields(index) = value
                                End If
                            End If
                        End If
                    End If
                End If
                If Not ((m_MissingFieldFlag OrElse (m_NextFieldStart = m_BufferLength))) Then
                End If
                m_NextFieldIndex = Math.Max((index + 1), m_NextFieldIndex)
                If (index = field) Then
                    If initializing Then
                        If (m_Eol OrElse m_Eof) Then
                            Return Nothing
                        Else
                            If String.IsNullOrEmpty(value) Then
                                Return String.Empty
                            Else
                                Return value
                            End If
                        End If
                    Else
                        Return value
                    End If
                End If
                index = (index + 1)
            Loop
            HandleParseError(New MalformedCsvException(GetCurrentRawData(), m_NextFieldStart, Math.Max(0, m_CurrentRecordIndex), index), m_NextFieldStart)
            Return Nothing
        End Function
        
        Public Overloads Function ReadNextRecord() As Boolean
            Return ReadNextRecord(false, false)
        End Function
        
        Protected Overloads Overridable Function ReadNextRecord(ByVal onlyReadHeaders As Boolean, ByVal skipToNextLine As Boolean) As Boolean
            If m_Eof Then
                If m_FirstRecordInCache Then
                    m_FirstRecordInCache = false
                    m_CurrentRecordIndex = (m_CurrentRecordIndex + 1)
                    Return true
                Else
                    Return false
                End If
            End If
            CheckDisposed()
            If Not (m_Initialized) Then
                m_Buffer = New Char((m_BufferSize) - 1) {}
                m_FieldHeaders = New String((0) - 1) {}
                If Not (ReadBuffer()) Then
                    Return false
                End If
                If Not (SkipEmptyAndCommentedLines(m_NextFieldStart)) Then
                    Return false
                End If
                m_FieldCount = 0
                m_Fields = New String((512) - 1) {}
                Do While (Not (ReadField(m_FieldCount, true, false)) Is Nothing)
                    If m_ParseErrorFlag Then
                        m_FieldCount = 0
                        System.Array.Clear(m_Fields, 0, m_Fields.Length)
                        m_ParseErrorFlag = false
                        m_NextFieldIndex = 0
                    Else
                        m_FieldCount = (m_FieldCount + 1)
                        If (m_FieldCount = m_Fields.Length) Then
                            System.Array.Resize(Of String)(m_Fields, ((m_FieldCount + 1)  _
                                            * 2))
                        End If
                    End If
                Loop
                m_FieldCount = (m_FieldCount + 1)
                If Not ((m_Fields.Length = m_FieldCount)) Then
                    System.Array.Resize(Of String)(m_Fields, m_FieldCount)
                End If
                m_Initialized = true
                If m_HasHeaders Then
                    m_CurrentRecordIndex = -1
                    m_FirstRecordInCache = false
                    m_FieldHeaders = New String((m_FieldCount) - 1) {}
                    m_FieldHeaderIndexes = New Dictionary(Of String, Integer)(m_FieldCount, m_FieldHeaderComparer)
                    Dim i = 0
                    Do While (i < m_Fields.Length)
                        Dim headerName = m_Fields(i)
                        If (String.IsNullOrEmpty(headerName) OrElse (headerName.Trim().Length = 0)) Then
                            headerName = (Me.DefaultHeaderName + i.ToString())
                        End If
                        m_FieldHeaders(i) = headerName
                        m_FieldHeaderIndexes.Add(headerName, i)
                        i = (i + 1)
                    Loop
                    If Not (onlyReadHeaders) Then
                        If Not (SkipEmptyAndCommentedLines(m_NextFieldStart)) Then
                            Return false
                        End If
                        Array.Clear(m_Fields, 0, m_Fields.Length)
                        m_NextFieldIndex = 0
                        m_Eol = false
                        m_CurrentRecordIndex = (m_CurrentRecordIndex + 1)
                        Return true
                    End If
                Else
                    If onlyReadHeaders Then
                        m_FirstRecordInCache = true
                        m_CurrentRecordIndex = -1
                    Else
                        m_FirstRecordInCache = false
                        m_CurrentRecordIndex = 0
                    End If
                End If
            Else
                If skipToNextLine Then
                    Me.SkipToNextLine(m_NextFieldStart)
                Else
                    If ((m_CurrentRecordIndex > -1) AndAlso Not (m_MissingFieldFlag)) Then
                        If (Not (m_Eol) AndAlso Not (m_Eof)) Then
                            If Not (m_SupportsMultiline) Then
                                Me.SkipToNextLine(m_NextFieldStart)
                            Else
                                Do While (Not (ReadField(m_NextFieldIndex, true, true)) Is Nothing)
                                Loop
                            End If
                        End If
                    End If
                End If
                If (Not (m_FirstRecordInCache) AndAlso Not (SkipEmptyAndCommentedLines(m_NextFieldStart))) Then
                    Return false
                End If
                If (m_HasHeaders OrElse Not (m_FirstRecordInCache)) Then
                    m_Eol = false
                End If
                If m_FirstRecordInCache Then
                    m_FirstRecordInCache = false
                Else
                    Array.Clear(m_Fields, 0, m_Fields.Length)
                    m_NextFieldIndex = 0
                End If
                m_MissingFieldFlag = false
                m_ParseErrorFlag = false
                m_CurrentRecordIndex = (m_CurrentRecordIndex + 1)
            End If
            Return true
        End Function
        
        Private Function SkipEmptyAndCommentedLines(ByRef pos As Integer) As Boolean
            If (pos < m_BufferLength) Then
                DoSkipEmptyAndCommentedLines(pos)
            End If
            Do While ((pos >= m_BufferLength) AndAlso Not (m_Eof))
                If ReadBuffer() Then
                    pos = 0
                    DoSkipEmptyAndCommentedLines(pos)
                Else
                    Return false
                End If
            Loop
            Return Not (m_Eof)
        End Function
        
        Private Sub DoSkipEmptyAndCommentedLines(ByRef pos As Integer)
            Do While (pos < m_BufferLength)
                If (m_Buffer(pos) = m_Comment) Then
                    pos = (pos + 1)
                    SkipToNextLine(pos)
                Else
                    If Not ((m_SkipEmptyLines AndAlso ParseNewLine(pos))) Then
                        Exit Do
                    End If
                End If
            Loop
        End Sub
        
        Private Function SkipWhiteSpaces(ByRef pos As Integer) As Boolean
            Do While true
                'skip spaces
                Do While ((pos < m_BufferLength) AndAlso IsWhiteSpace(m_Buffer(pos)))
                    pos = (pos + 1)
                Loop
                If (pos < m_BufferLength) Then
                    Exit Do
                Else
                    pos = 0
                    If Not (ReadBuffer()) Then
                        Return false
                    End If
                End If
            Loop
            Return true
        End Function
        
        Private Function SkipToNextLine(ByRef pos As Integer) As Boolean
            'It should be ((pos = 0) == 0), double-check to ensure it works
            pos = 0
            Do While (((pos < m_BufferLength) OrElse ReadBuffer()) AndAlso Not (ParseNewLine(pos)))
                pos = 0
                pos = (pos + 1)
            Loop
            Return Not (m_Eof)
        End Function
        
        Private Sub HandleParseError(ByVal [error] As MalformedCsvException, ByRef pos As Integer)
            'check this one as well, uses switches
            If ([error] Is Nothing) Then
                Throw New ArgumentNullException("error")
            End If
            m_ParseErrorFlag = true
            If (m_DefaultParseErrorAction = ParseErrorAction.ThrowException) Then
                Throw [error]
            End If
            If (m_DefaultParseErrorAction = ParseErrorAction.RaiseEvent) Then
                Dim e = New ParseErrorEventArgs([error], ParseErrorAction.ThrowException)
                OnParseError(e)
                If (e.Action = ParseErrorAction.ThrowException) Then
                    Throw e.Error
                End If
                If (e.Action = ParseErrorAction.RaiseEvent) Then
                    Throw New InvalidOperationException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.ParseErrorActionInvalidInsideParseErrorEvent, e.Action), e.Error)
                End If
                If (e.Action = ParseErrorAction.AdvanceToNextLine) Then
                    If (Not (m_MissingFieldFlag) AndAlso (pos >= 0)) Then
                        SkipToNextLine(pos)
                    End If
                Else
                    Throw New NotSupportedException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.ParseErrorActionNotSupported, e.Action), e.Error)
                End If
            End If
            If (m_DefaultParseErrorAction = ParseErrorAction.AdvanceToNextLine) Then
                If (Not (m_MissingFieldFlag) AndAlso (pos >= 0)) Then
                    SkipToNextLine(pos)
                End If
            Else
                Throw New NotSupportedException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.ParseErrorActionNotSupported, m_DefaultParseErrorAction), [error])
            End If
        End Sub
        
        Private Function HandleMissingField(ByVal value As String, ByVal fieldIndex As Integer, ByRef currentPosition As Integer) As String
            If ((fieldIndex < 0) OrElse (fieldIndex >= m_FieldCount)) Then
                Throw New ArgumentOutOfRangeException("fieldIndex", fieldIndex, String.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, fieldIndex))
            End If
            m_MissingFieldFlag = true
            Dim i = (fieldIndex + 1)
            Do While (i < m_FieldCount)
                m_Fields(i) = Nothing
                i = (i + 1)
            Loop
            If (Not (value) Is Nothing) Then
                Return value
            Else
                If (m_MissingFieldAction = MissingFieldAction.ParseError) Then
                    HandleParseError(New MissingFieldCsvException(GetCurrentRawData(), currentPosition, Math.Max(0, m_CurrentRecordIndex), fieldIndex), currentPosition)
                    Return value
                End If
                If (m_MissingFieldAction = MissingFieldAction.ReplaceByEmpty) Then
                    Return String.Empty
                End If
                If (m_MissingFieldAction = MissingFieldAction.ReplaceByNull) Then
                    Return Nothing
                End If
                Throw New NotSupportedException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.MissingFieldActionNotSupported, m_MissingFieldAction))
            End If
        End Function
        
        Private Sub ValidateDataReader(ByVal validations As DataReaderValidations)
            If (Not (((validations And DataReaderValidations.IsInitialized) = 0)) AndAlso Not (m_Initialized)) Then
                Throw New InvalidOperationException(ExceptionMessage.NoCurrentRecord)
            End If
            If (Not (((validations And DataReaderValidations.IsNotClosed) = 0)) AndAlso m_IsDisposed) Then
                Throw New InvalidOperationException(ExceptionMessage.ReaderClosed)
            End If
        End Sub
        
        Private Function CopyFieldToArray(ByVal field As Integer, ByVal fieldOffset As Long, ByVal destinationArray As Array, ByVal destinationOffset As Integer, ByVal length As Integer) As Long
            EnsureInitialize()
            If ((field < 0) OrElse (field >= m_FieldCount)) Then
                Throw New ArgumentOutOfRangeException("field", field, String.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, field))
            End If
            If ((fieldOffset < 0) OrElse (fieldOffset >= Integer.MaxValue)) Then
                Throw New ArgumentOutOfRangeException("fieldOffset")
            End If
            If (length = 0) Then
                Return 0
            End If
            Dim value = Me(field)
            If (value Is Nothing) Then
                value = String.Empty
            End If
            If (destinationArray.GetType() Is GetType(Char())) Then
                Array.Copy(value.ToCharArray(CType(fieldOffset,Integer), length), 0, destinationArray, destinationOffset, length)
            Else
                Dim chars = value.ToCharArray(CType(fieldOffset,Integer), length)
                Dim source((chars.Length) - 1) As Byte
                Dim i = 0
                Do While (i < chars.Length)
                    source(i) = Convert.ToByte(chars(i))
                    i = (i + 1)
                Loop
                Array.Copy(source, 0, destinationArray, destinationOffset, length)
            End If
            Return length
        End Function
        
        Function IDataReader_NextResult() As Boolean Implements IDataReader.NextResult
            ValidateDataReader(DataReaderValidations.IsNotClosed)
            Return false
        End Function
        
        Sub IDataReader_Close() Implements IDataReader.Close
            CType(Me,IDataReader).Dispose()
        End Sub
        
        Function IDataReader_Read() As Boolean Implements IDataReader.Read
            ValidateDataReader(DataReaderValidations.IsNotClosed)
            Return ReadNextRecord()
        End Function
        
        Function IDataReader_GetSchemaTable() As DataTable Implements IDataReader.GetSchemaTable
            EnsureInitialize()
            ValidateDataReader(DataReaderValidations.IsNotClosed)
            Dim schema = New DataTable("SchemaTable")
            schema.Locale = CultureInfo.InvariantCulture
            schema.MinimumCapacity = m_FieldCount
            schema.Columns.Add(SchemaTableColumn.AllowDBNull, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.BaseColumnName, GetType(String)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.BaseSchemaName, GetType(String)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.BaseTableName, GetType(String)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.ColumnName, GetType(String)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.ColumnOrdinal, GetType(Integer)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.ColumnSize, GetType(Integer)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.DataType, GetType(Object)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.IsAliased, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.IsExpression, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.IsKey, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.IsLong, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.IsUnique, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.NumericPrecision, GetType(Short)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.NumericScale, GetType(Short)).ReadOnly = true
            schema.Columns.Add(SchemaTableColumn.ProviderType, GetType(Integer)).ReadOnly = true
            schema.Columns.Add(SchemaTableOptionalColumn.BaseCatalogName, GetType(String)).ReadOnly = true
            schema.Columns.Add(SchemaTableOptionalColumn.BaseServerName, GetType(String)).ReadOnly = true
            schema.Columns.Add(SchemaTableOptionalColumn.IsAutoIncrement, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableOptionalColumn.IsHidden, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableOptionalColumn.IsReadOnly, GetType(Boolean)).ReadOnly = true
            schema.Columns.Add(SchemaTableOptionalColumn.IsRowVersion, GetType(Boolean)).ReadOnly = true
            Dim columnNames() As String
            If m_HasHeaders Then
                columnNames = m_FieldHeaders
            Else
                columnNames = New String((m_FieldCount) - 1) {}
                Dim i = 0
                Do While (i < m_FieldCount)
                    columnNames(i) = ("Column" + i.ToString(CultureInfo.InvariantCulture))
                    i = (i + 1)
                Loop
            End If
            Dim schemaRow = New Object() {true, Nothing, String.Empty, String.Empty, Nothing, Nothing, Integer.MaxValue, GetType(String), false, false, false, false, false, DBNull.Value, DBNull.Value, CType(DbType.String,Integer), String.Empty, String.Empty, false, false, true, false}
            Dim j = 0
            Do While (j < columnNames.Length)
                schemaRow(1) = columnNames(j)
                schemaRow(4) = columnNames(j)
                schemaRow(5) = j
                schema.Rows.Add(schemaRow)
                j = (j + 1)
            Loop
            Return schema
        End Function
        
        Function IDataRecord_GetInt32(ByVal i As Integer) As Integer Implements IDataRecord.GetInt32
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Dim value = Me(i)
            If (value Is Nothing) Then
                Return Integer.Parse(String.Empty)
            Else
                Return Integer.Parse(value, CultureInfo.InvariantCulture)
            End If
        End Function
        
        Function IDataRecord_GetValue(ByVal i As Integer) As Object Implements IDataRecord.GetValue
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Dim isNull = CType(Me,IDataRecord).IsDBNull(i)
            If isNull Then
                Return DBNull.Value
            Else
                Return Me(i)
            End If
        End Function
        
        Function IDataRecord_IsDBNull(ByVal i As Integer) As Boolean Implements IDataRecord.IsDBNull
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return String.IsNullOrEmpty(Me(i))
        End Function
        
        Function IDataRecord_GetBytes(ByVal i As Integer, ByVal fieldOffset As Long, ByVal buffer() As Byte, ByVal bufferoffset As Integer, ByVal length As Integer) As Long Implements IDataRecord.GetBytes
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return CopyFieldToArray(i, fieldOffset, buffer, bufferoffset, length)
        End Function
        
        Function IDataRecord_GetByte(ByVal i As Integer) As Byte Implements IDataRecord.GetByte
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Byte.Parse(Me(i), CultureInfo.CurrentCulture)
        End Function
        
        Function IDataRecord_GetFieldType(ByVal i As Integer) As Type Implements IDataRecord.GetFieldType
            EnsureInitialize()
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            If ((i < 0) OrElse (i >= m_FieldCount)) Then
                Throw New ArgumentOutOfRangeException("i", i, String.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, i))
            End If
            Return GetType(String)
        End Function
        
        Function IDataRecord_GetDecimal(ByVal i As Integer) As Decimal Implements IDataRecord.GetDecimal
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Decimal.Parse(Me(i), CultureInfo.CurrentCulture)
        End Function
        
        Function IDataRecord_GetValues(ByVal values() As Object) As Integer Implements IDataRecord.GetValues
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Dim record = CType(Me,IDataRecord)
            Dim i = 0
            Do While (i < m_FieldCount)
                values(i) = record.GetValue(i)
                i = (i + 1)
            Loop
            Return m_FieldCount
        End Function
        
        Function IDataRecord_GetName(ByVal i As Integer) As String Implements IDataRecord.GetName
            ValidateDataReader(DataReaderValidations.IsNotClosed)
            If ((i < 0) OrElse (i >= m_FieldCount)) Then
                Throw New ArgumentOutOfRangeException("i", i, String.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldIndexOutOfRange, i))
            End If
            If m_HasHeaders Then
                Return m_FieldHeaders(i)
            Else
                Return ("Column" + i.ToString(CultureInfo.InvariantCulture))
            End If
        End Function
        
        Function IDataRecord_GetInt64(ByVal i As Integer) As Long Implements IDataRecord.GetInt64
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Long.Parse(Me(i), CultureInfo.CurrentCulture)
        End Function
        
        Function IDataRecord_GetDouble(ByVal i As Integer) As Double Implements IDataRecord.GetDouble
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Double.Parse(Me(i), CultureInfo.CurrentCulture)
        End Function
        
        Function IDataRecord_GetBoolean(ByVal i As Integer) As Boolean Implements IDataRecord.GetBoolean
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Dim value = Me(i)
            Dim result As Integer
            If Integer.TryParse(value, result) Then
                Return Not ((result = 0))
            Else
                Return Boolean.Parse(value)
            End If
        End Function
        
        Function IDataRecord_GetGuid(ByVal i As Integer) As Guid Implements IDataRecord.GetGuid
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return New Guid(Me(i))
        End Function
        
        Function IDataRecord_GetDateTime(ByVal i As Integer) As DateTime Implements IDataRecord.GetDateTime
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return DateTime.Parse(Me(i), CultureInfo.CurrentCulture)
        End Function
        
        Function IDataRecord_GetOrdinal(ByVal name As String) As Integer Implements IDataRecord.GetOrdinal
            EnsureInitialize()
            ValidateDataReader(DataReaderValidations.IsNotClosed)
            Dim index As Integer
            If Not (m_FieldHeaderIndexes.TryGetValue(name, index)) Then
                Throw New ArgumentException(String.Format(CultureInfo.InvariantCulture, ExceptionMessage.FieldHeaderNotFound, name), "name")
            End If
            Return index
        End Function
        
        Function IDataRecord_GetDataTypeName(ByVal i As Integer) As String Implements IDataRecord.GetDataTypeName
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return GetType(String).FullName
        End Function
        
        Function IDataRecord_GetFloat(ByVal i As Integer) As Single Implements IDataRecord.GetFloat
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Single.Parse(Me(i), CultureInfo.CurrentCulture)
        End Function
        
        Function IDataRecord_GetData(ByVal i As Integer) As IDataReader Implements IDataRecord.GetData
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            If (i = 0) Then
                Return Me
            Else
                Return Nothing
            End If
        End Function
        
        Function IDataRecord_GetChars(ByVal i As Integer, ByVal fieldoffset As Long, ByVal buffer() As Char, ByVal bufferoffset As Integer, ByVal length As Integer) As Long Implements IDataRecord.GetChars
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return CopyFieldToArray(i, fieldoffset, buffer, bufferoffset, length)
        End Function
        
        Function IDataRecord_GetString(ByVal i As Integer) As String Implements IDataRecord.GetString
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Me(i)
        End Function
        
        Function IDataRecord_GetChar(ByVal i As Integer) As Char Implements IDataRecord.GetChar
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Char.Parse(Me(i))
        End Function
        
        Function IDataRecord_GetInt16(ByVal i As Integer) As Short Implements IDataRecord.GetInt16
            ValidateDataReader((DataReaderValidations.IsInitialized Or DataReaderValidations.IsNotClosed))
            Return Short.Parse(Me(i), CultureInfo.CurrentCulture)
        End Function
        
        Public Function GetEnumerator() As CsvReaderRecordEnumerator
            Return New CsvReaderRecordEnumerator(Me)
        End Function
        
        Protected Overridable Sub OnDisposed(ByVal e As EventArgs)
            Dim handler = Me.DisposedEvent
            If (Not (handler) Is Nothing) Then
                handler(Me, e)
            End If
        End Sub
        
        Protected Sub CheckDisposed()
            If m_IsDisposed Then
                Throw New ObjectDisposedException(Me.GetType().FullName)
            End If
        End Sub
        
        Sub IDisposable_Dispose() Implements IDisposable.Dispose
            If Not (m_IsDisposed) Then
                Dispose(true)
                GC.SuppressFinalize(Me)
            End If
        End Sub
        
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not (m_IsDisposed) Then
                Try 
                    If disposing Then
                        If (Not (m_Reader) Is Nothing) Then
                            If (Not (m_Reader) Is Nothing) Then
                                m_Reader.Dispose()
                                m_Reader = Nothing
                                m_Buffer = Nothing
                                m_Eof = true
                            End If
                        End If
                    End If
                Finally
                    m_IsDisposed = true
                    Try 
                        OnDisposed(EventArgs.Empty)
                    Catch __exception As Exception
                    End Try
                End Try
            End If
        End Sub
    End Class
    
    Friend Enum DataReaderValidations
        
        None = 0
        
        IsInitialized = 1
        
        IsNotClosed = 2
    End Enum
    
    Public Class CsvReaderRecordEnumerator
        Inherits Object
        Implements IEnumerator, IDisposable
        
        Private m_Current() As String
        
        Private m_CurrentRecordIndex As Long
        
        Private m_Reader As CsvReader
        
        Public Sub New(ByVal reader As CsvReader)
            MyBase.New
            If (reader Is Nothing) Then
                Throw New ArgumentNullException("reader")
            End If
            m_Reader = reader
            m_Current = Nothing
            m_CurrentRecordIndex = reader.CurrentRecordIndex
        End Sub
        
        Public ReadOnly Property Current() As String()
            Get
                Return m_Current
            End Get
        End Property
        
        ReadOnly Property IEnumerator_Current() As Object Implements IEnumerator.Current
            Get
                If Not ((m_Reader.CurrentRecordIndex = m_CurrentRecordIndex)) Then
                    Throw New InvalidOperationException(ExceptionMessage.EnumerationVersionCheckFailed)
                End If
                Return Me.Current
            End Get
        End Property
        
        Function IEnumerator_MoveNext() As Boolean Implements IEnumerator.MoveNext
            If Not ((m_Reader.CurrentRecordIndex = m_CurrentRecordIndex)) Then
                Throw New InvalidOperationException(ExceptionMessage.EnumerationVersionCheckFailed)
            End If
            If m_Reader.ReadNextRecord() Then
                m_Current = New String((CType(m_Reader,IDataRecord).FieldCount) - 1) {}
                m_Reader.CopyCurrentRecordTo(m_Current)
                m_CurrentRecordIndex = m_Reader.CurrentRecordIndex
                Return true
            Else
                m_Current = Nothing
                m_CurrentRecordIndex = m_Reader.CurrentRecordIndex
                Return false
            End If
        End Function
        
        Sub IEnumerator_Reset() Implements IEnumerator.Reset
            If Not ((m_Reader.CurrentRecordIndex = m_CurrentRecordIndex)) Then
                Throw New InvalidOperationException(ExceptionMessage.EnumerationVersionCheckFailed)
            End If
            m_Reader.MoveTo(-1)
            m_Current = Nothing
            m_CurrentRecordIndex = m_Reader.CurrentRecordIndex
        End Sub
        
        Sub IDisposable_Dispose() Implements IDisposable.Dispose
            m_Reader = Nothing
            m_Current = Nothing
        End Sub
    End Class
    
    Public Enum MissingFieldAction
        
        ParseError = 0
        
        ReplaceByEmpty = 1
        
        ReplaceByNull = 2
    End Enum
    
    Public Enum ParseErrorAction
        
        [RaiseEvent] = 0
        
        AdvanceToNextLine = 1
        
        ThrowException = 2
    End Enum
    
    <Flags()>  _
    Public Enum ValueTrimmingOptions
        
        None = 0
        
        UnquotedOnly = 1
        
        QuotedOnly = 2
        
        All = (UnquotedOnly Or QuotedOnly)
    End Enum
    
    Public Class ParseErrorEventArgs
        Inherits EventArgs
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Error As MalformedCsvException
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Action As ParseErrorAction
        
        Public Sub New(ByVal [error] As MalformedCsvException, ByVal defaultAction As ParseErrorAction)
            MyBase.New()
        End Sub
        
        Public Property [Error]() As MalformedCsvException
            Get
                Return m_Error
            End Get
            Set
                m_Error = value
            End Set
        End Property
        
        Public Property Action() As ParseErrorAction
            Get
                Return m_Action
            End Get
            Set
                m_Action = value
            End Set
        End Property
    End Class
    
    Public Class MalformedCsvException
        Inherits Exception
        
        Private m_Message As String
        
        Private m_RawData As String
        
        Private m_CurrentFieldIndex As Integer
        
        Private m_CurrentRecordIndex As Long
        
        Private m_CurrentPosition As Integer
        
        Public Sub New()
            Me.New(CType(Nothing,String), Nothing)
        End Sub
        
        Public Sub New(ByVal message As String)
            Me.New(message, Nothing)
        End Sub
        
        Public Sub New(ByVal message As String, ByVal innerException As Exception)
            MyBase.New(String.Empty, innerException)
            If (message Is Nothing) Then
                m_Message = String.Empty
            Else
                m_Message = message
            End If
            m_RawData = String.Empty
            m_CurrentPosition = -1
            m_CurrentRecordIndex = -1
            m_CurrentFieldIndex = -1
        End Sub
        
        Public Sub New(ByVal rawData As String, ByVal currentPosition As Integer, ByVal currentRecordIndex As Long, ByVal currentFieldIndex As Integer)
            Me.New(rawData, currentPosition, currentRecordIndex, currentFieldIndex, Nothing)
        End Sub
        
        Public Sub New(ByVal rawData As String, ByVal currentPosition As Integer, ByVal currentRecordIndex As Long, ByVal currentFieldIndex As Integer, ByVal innerException As Exception)
            MyBase.New(String.Empty, innerException)
            If (rawData Is Nothing) Then
                m_RawData = String.Empty
            Else
                m_RawData = rawData
            End If
            m_CurrentPosition = currentPosition
            m_CurrentRecordIndex = currentRecordIndex
            m_CurrentFieldIndex = currentFieldIndex
        End Sub
        
        Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
            MyBase.New(info, context)
            m_Message = info.GetString("MyMessage")
            m_RawData = info.GetString("RawData")
            m_CurrentPosition = info.GetInt32("CurrentPosition")
            m_CurrentRecordIndex = info.GetInt64("currentRecordIndex")
            m_CurrentFieldIndex = info.GetInt32("currentFieldIndex")
        End Sub
        
        Public ReadOnly Property RawData() As String
            Get
                Return m_RawData
            End Get
        End Property
        
        Public ReadOnly Property CurrentPosition() As Integer
            Get
                Return m_CurrentPosition
            End Get
        End Property
        
        Public ReadOnly Property CurrentRecordIndex() As Long
            Get
                Return m_CurrentRecordIndex
            End Get
        End Property
        
        Public ReadOnly Property CurrentFieldIndex() As Integer
            Get
                Return m_CurrentFieldIndex
            End Get
        End Property
        
        Public Overrides ReadOnly Property Message() As String
            Get
                Return m_Message
            End Get
        End Property
        
        Public Overrides Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.GetObjectData(info, context)
            info.AddValue("MyMessage", m_Message)
            info.AddValue("RawData", m_RawData)
            info.AddValue("CurrentPosition", m_CurrentPosition)
            info.AddValue("CurrentRecordIndex", m_CurrentRecordIndex)
            info.AddValue("CurrentFieldIndex", m_CurrentFieldIndex)
        End Sub
    End Class
    
    <Serializable()>  _
    Public Class MissingFieldCsvException
        Inherits MalformedCsvException
        
        Public Sub New()
            MyBase.New()
        End Sub
        
        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub
        
        Public Sub New(ByVal message As String, ByVal innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
        
        Public Sub New(ByVal rawData As String, ByVal currentPosition As Integer, ByVal currentRecordIndex As Long, ByVal currentFieldIndex As Integer)
            MyBase.New(rawData, currentPosition, currentRecordIndex, currentFieldIndex)
        End Sub
        
        Public Sub New(ByVal rawData As String, ByVal currentPosition As Integer, ByVal currentRecordIndex As Long, ByVal currentFieldIndex As Integer, ByVal innerException As Exception)
            MyBase.New(rawData, currentPosition, currentRecordIndex, currentFieldIndex, innerException)
        End Sub
        
        Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class
    
    <System.Diagnostics.DebuggerNonUserCodeAttribute()>  _
    Friend Class ExceptionMessage
        
        Friend Sub New()
            MyBase.New
        End Sub
        
        Friend Shared ReadOnly Property BufferSizeTooSmall() As String
            Get
                Return "Buffer size is too small."
            End Get
        End Property
        
        Friend Shared ReadOnly Property CannotMovePreviousRecordInForwardOnly() As String
            Get
                Return "Cannot move previous record in forward only."
            End Get
        End Property
        
        Friend Shared ReadOnly Property CannotReadRecordAtIndex() As String
            Get
                Return "Cannot read record at index."
            End Get
        End Property
        
        Friend Shared ReadOnly Property EnumerationFinishedOrNotStarted() As String
            Get
                Return "Enumeration finished or not started."
            End Get
        End Property
        
        Friend Shared ReadOnly Property EnumerationVersionCheckFailed() As String
            Get
                Return "Enumeration version check failed."
            End Get
        End Property
        
        Friend Shared ReadOnly Property FieldHeaderNotFound() As String
            Get
                Return "Field header not found."
            End Get
        End Property
        
        Friend Shared ReadOnly Property FieldIndexOutOfRange() As String
            Get
                Return "Field index out of range."
            End Get
        End Property
        
        Friend Shared ReadOnly Property MalformedCsvException() As String
            Get
                Return "Malformed CSV exception."
            End Get
        End Property
        
        Friend Shared ReadOnly Property MissingFieldActionNotSupported() As String
            Get
                Return "Missing field action not supported."
            End Get
        End Property
        
        Friend Shared ReadOnly Property NoCurrentRecord() As String
            Get
                Return "No current record."
            End Get
        End Property
        
        Friend Shared ReadOnly Property NoHeaders() As String
            Get
                Return "No headers."
            End Get
        End Property
        
        Friend Shared ReadOnly Property NotEnoughSpaceInArray() As String
            Get
                Return "Not enough space in array."
            End Get
        End Property
        
        Friend Shared ReadOnly Property ParseErrorActionInvalidInsideParseErrorEvent() As String
            Get
                Return "Parse error action invalid inside parse error event."
            End Get
        End Property
        
        Friend Shared ReadOnly Property ParseErrorActionNotSupported() As String
            Get
                Return "Parse error action not supported."
            End Get
        End Property
        
        Friend Shared ReadOnly Property ReaderClosed() As String
            Get
                Return "Reader closed."
            End Get
        End Property
        
        Friend Shared ReadOnly Property RecordIndexLessThanZero() As String
            Get
                Return "Record index less than zero."
            End Get
        End Property
    End Class
End Namespace
