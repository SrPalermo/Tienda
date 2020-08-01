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
    
    Public Class ActionArgs
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Sequence As Nullable(Of Integer)
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Date As Nullable(Of DateTime)
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_IgnoreBusinessRules As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Tag As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CommandName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CommandArgument As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_LastCommandName As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ContextKey As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Path As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Controller As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_View As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_LastView As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Values() As FieldValue
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Filter() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SortExpression As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_GroupExpression As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SelectedValues() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ExternalFilter() As FieldValue
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SaveLEVs As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Trigger As String
        
        Public Sub New()
            MyBase.New
            If ((Not (HttpContext.Current) Is Nothing) AndAlso (Current Is Nothing)) Then
                HttpContext.Current.Items("ActionArgs_Current") = Me
            End If
        End Sub
        
        Public Property Sequence() As Nullable(Of Integer)
            Get
                Return m_Sequence
            End Get
            Set
                m_Sequence = value
            End Set
        End Property
        
        Public Property [Date]() As Nullable(Of DateTime)
            Get
                Return m_Date
            End Get
            Set
                m_Date = value
            End Set
        End Property
        
        Public Property IgnoreBusinessRules() As Boolean
            Get
                Return m_IgnoreBusinessRules
            End Get
            Set
                m_IgnoreBusinessRules = value
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
        
        Public Property CommandName() As String
            Get
                Return m_CommandName
            End Get
            Set
                m_CommandName = ControllerUtilities.ValidateName(value)
            End Set
        End Property
        
        Public Property CommandArgument() As String
            Get
                Return m_CommandArgument
            End Get
            Set
                m_CommandArgument = ControllerUtilities.ValidateName(value)
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
        
        Public Property ContextKey() As String
            Get
                Return m_ContextKey
            End Get
            Set
                m_ContextKey = value
            End Set
        End Property
        
        Public Property Path() As String
            Get
                Return m_Path
            End Get
            Set
                m_Path = value
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
        
        Public Property Values() As FieldValue()
            Get
                Return m_Values
            End Get
            Set
                m_Values = value
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
        
        Public ReadOnly Property SqlCommandType() As CommandConfigurationType
            Get
                Dim commandType = CommandConfigurationType.None
                If CommandName.Equals("update", StringComparison.OrdinalIgnoreCase) Then
                    commandType = CommandConfigurationType.Update
                Else
                    If CommandName.Equals("insert", StringComparison.OrdinalIgnoreCase) Then
                        commandType = CommandConfigurationType.Insert
                    Else
                        If CommandName.Equals("delete", StringComparison.OrdinalIgnoreCase) Then
                            commandType = CommandConfigurationType.Delete
                        End If
                    End If
                End If
                Return commandType
            End Get
        End Property
        
        Public ReadOnly Property IsBatchEditOrDelete() As Boolean
            Get
                Return (((LastCommandName = "BatchEdit") AndAlso (CommandName = "Update")) OrElse ((CommandName = "Delete") AndAlso (SelectedValues.Length > 1)))
            End Get
        End Property
        
        Public Property SelectedValues() As String()
            Get
                If (m_SelectedValues Is Nothing) Then
                    m_SelectedValues = New String((0) - 1) {}
                End If
                Return m_SelectedValues
            End Get
            Set
                m_SelectedValues = value
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
        
        Public Property SaveLEVs() As Boolean
            Get
                Return m_SaveLEVs
            End Get
            Set
                m_SaveLEVs = value
            End Set
        End Property
        
        Public Shared ReadOnly Property Current() As ActionArgs
            Get
                Return CType(HttpContext.Current.Items("ActionArgs_Current"),ActionArgs)
            End Get
        End Property
        
        ''' <summary>
        ''' The name of the field that has triggered the 'Calculate' action.
        ''' </summary>
        Public Property Trigger() As String
            Get
                Return m_Trigger
            End Get
            Set
                m_Trigger = value
            End Set
        End Property
        
        Public Default ReadOnly Property Item(ByVal name As String) As FieldValue
            Get
                Return SelectFieldValueObject(name)
            End Get
        End Property
        
        Public Shared Sub Forget()
            HttpContext.Current.Items("ActionArgs_Current") = Nothing
        End Sub
        
        Public Function SelectFieldValueObject(ByVal name As String) As FieldValue
            If (Not (Values) Is Nothing) Then
                For Each v in Values
                    If v.Name.Equals(name, StringComparison.OrdinalIgnoreCase) Then
                        Return v
                    End If
                Next
            End If
            Return Nothing
        End Function
        
        Public Function ToObject(Of T)() As T
            Dim objectType = GetType(T)
            Dim theObject = CType(objectType.Assembly.CreateInstance(objectType.FullName),T)
            For Each v in Values
                v.AssignTo(theObject)
            Next
            Return theObject
        End Function
        
        Public Sub AddValue(ByVal ParamArray values() as FieldValue)
            Dim newValues = New List(Of FieldValue)(Values)
            For Each fvo in values
                newValues.Add(fvo)
            Next
            Values = newValues.ToArray()
        End Sub
    End Class
End Namespace
