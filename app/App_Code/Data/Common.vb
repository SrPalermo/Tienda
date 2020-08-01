Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data.Common
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions

Namespace Norwindt.Data
    
    Public Class SelectClauseDictionary
        Inherits SortedDictionary(Of String, String)
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_TrackAliases As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ReferencedAliases As List(Of String)
        
        Public Property TrackAliases() As Boolean
            Get
                Return m_TrackAliases
            End Get
            Set
                m_TrackAliases = value
            End Set
        End Property
        
        Public Property ReferencedAliases() As List(Of String)
            Get
                Return m_ReferencedAliases
            End Get
            Set
                m_ReferencedAliases = value
            End Set
        End Property
        
        Public Shadows Default Property Item(ByVal name As String) As String
            Get
                Dim expression As String = Nothing
                If Not (TryGetValue(name.ToLower(), expression)) Then
                    expression = "null"
                Else
                    If TrackAliases Then
                        Dim m = Regex.Match(expression, "^('|""|\[|`)(?'Alias'.+?)('|""|\]|`)")
                        If m.Success Then
                            If (ReferencedAliases Is Nothing) Then
                                ReferencedAliases = New List(Of String)()
                            End If
                            Dim aliasName = m.Groups("Alias").Value
                            If (m.Success AndAlso Not (ReferencedAliases.Contains(aliasName))) Then
                                ReferencedAliases.Add(aliasName)
                            End If
                        End If
                    End If
                End If
                Return expression
            End Get
            Set
                MyBase.Item(name.ToLower()) = value
            End Set
        End Property
        
        Public Shadows Function ContainsKey(ByVal name As String) As Boolean
            Return MyBase.ContainsKey(name.ToLower())
        End Function
        
        Public Shadows Sub Add(ByVal key As String, ByVal value As String)
            MyBase.Add(key.ToLower(), value)
        End Sub
        
        Public Shadows Function TryGetValue(ByVal key As String, ByRef value As String) As Boolean
            Return MyBase.TryGetValue(key.ToLower(), value)
        End Function
    End Class
    
    Public Interface IDataController
        
        Function GetPage(ByVal controller As String, ByVal view As String, ByVal request As PageRequest) As ViewPage
        
        Function GetListOfValues(ByVal controller As String, ByVal view As String, ByVal request As DistinctValueRequest) As Object()
        
        Function Execute(ByVal controller As String, ByVal view As String, ByVal args As ActionArgs) As ActionResult
    End Interface
    
    Public Interface IAutoCompleteManager
        
        Function GetCompletionList(ByVal prefixText As String, ByVal count As Integer, ByVal contextKey As String) As String()
    End Interface
    
    Public Interface IActionHandler
        
        Sub BeforeSqlAction(ByVal args As ActionArgs, ByVal result As ActionResult)
        
        Sub AfterSqlAction(ByVal args As ActionArgs, ByVal result As ActionResult)
        
        Sub ExecuteAction(ByVal args As ActionArgs, ByVal result As ActionResult)
    End Interface
    
    Public Interface IRowHandler
        
        Function SupportsNewRow(ByVal requet As PageRequest) As Boolean
        
        Sub NewRow(ByVal request As PageRequest, ByVal page As ViewPage, ByVal row() As Object)
        
        Function SupportsPrepareRow(ByVal request As PageRequest) As Boolean
        
        Sub PrepareRow(ByVal request As PageRequest, ByVal page As ViewPage, ByVal row() As Object)
    End Interface
    
    Public Interface IDataFilter
        
        Sub Filter(ByVal filter As SortedDictionary(Of String, Object))
    End Interface
    
    Public Interface IDataFilter2
        
        Sub Filter(ByVal controller As String, ByVal view As String, ByVal filter As SortedDictionary(Of String, Object))
        
        Sub AssignContext(ByVal controller As String, ByVal view As String, ByVal lookupContextController As String, ByVal lookupContextView As String, ByVal lookupContextFieldName As String)
    End Interface
    
    Public Interface IDataEngine
        
        Function ExecuteReader(ByVal request As PageRequest) As DbDataReader
    End Interface
    
    Public Interface IPlugIn
        
        Property Config() As ControllerConfiguration
        
        Function Create(ByVal config As ControllerConfiguration) As ControllerConfiguration
        
        Sub PreProcessPageRequest(ByVal request As PageRequest, ByVal page As ViewPage)
        
        Sub ProcessPageRequest(ByVal request As PageRequest, ByVal page As ViewPage)
        
        Sub PreProcessArguments(ByVal args As ActionArgs, ByVal result As ActionResult, ByVal page As ViewPage)
        
        Sub ProcessArguments(ByVal args As ActionArgs, ByVal result As ActionResult, ByVal page As ViewPage)
    End Interface
    
    Public Class BusinessObjectParameters
        Inherits SortedDictionary(Of String, Object)
        
        Private m_ParameterMarker As String = Nothing
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal ParamArray values() as System.[Object])
            MyBase.New
            Assign(values)
        End Sub
        
        Public Shared Function Create(ByVal parameterMarker As String, ByVal ParamArray values() as System.[Object]) As BusinessObjectParameters
            Dim paramList = New BusinessObjectParameters()
            paramList.m_ParameterMarker = parameterMarker
            paramList.Assign(values)
            Return paramList
        End Function
        
        Public Sub Assign(ByVal ParamArray values() as System.[Object])
            Dim parameterMarker = m_ParameterMarker
            Dim i = 0
            Do While (i < values.Length)
                Dim v = values(i)
                If TypeOf v Is FieldValue Then
                    Dim fv = CType(v,FieldValue)
                    Add(fv.Name, fv.Value)
                Else
                    If TypeOf v Is BusinessObjectParameters Then
                        Dim paramList = CType(v,BusinessObjectParameters)
                        For Each name in paramList.Keys
                            Add(name, paramList(name))
                        Next
                    Else
                        If String.IsNullOrEmpty(parameterMarker) Then
                            parameterMarker = SqlStatement.GetParameterMarker(String.Empty)
                        End If
                        If ((Not (v) Is Nothing) AndAlso (v.GetType().Namespace Is Nothing)) Then
                            For Each pi in v.GetType().GetProperties()
                                Add((parameterMarker + pi.Name), pi.GetValue(v))
                            Next
                        Else
                            Add((parameterMarker  _
                                            + ("p" + i.ToString())), v)
                        End If
                    End If
                End If
                i = (i + 1)
            Loop
        End Sub
        
        Public Function ToWhere() As String
            Dim filterExpression = New StringBuilder()
            For Each paramName in Keys
                If (filterExpression.Length > 0) Then
                    filterExpression.Append("and")
                End If
                Dim v = Me(paramName)
                If (DBNull.Value.Equals(v) OrElse (v Is Nothing)) Then
                    filterExpression.AppendFormat("({0} is null)", paramName.Substring(1))
                Else
                    filterExpression.AppendFormat("({0}={1})", paramName.Substring(1), paramName)
                End If
            Next
            Return filterExpression.ToString()
        End Function
    End Class
    
    Public Interface IBusinessObject
        
        Sub AssignFilter(ByVal filter As String, ByVal parameters As BusinessObjectParameters)
    End Interface
    
    Public Enum CommandConfigurationType
        
        [Select]
        
        Update
        
        Insert
        
        Delete
        
        SelectCount
        
        SelectDistinct
        
        SelectAggregates
        
        SelectFirstLetters
        
        SelectExisting
        
        Sync
        
        None
    End Enum
End Namespace
