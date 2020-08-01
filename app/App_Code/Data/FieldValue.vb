Imports System
Imports System.Collections
Imports System.Collections.Generic

Namespace Norwindt.Data
    
    <Serializable()>  _
    Public Class FieldValue
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_NewValueIsSet As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ModifiedIsSet As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Name As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_OldValue As Object
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_NewValue As Object
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Modified As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ReadOnly As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Error As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Scope As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_EnableConversion As Boolean = true
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal fieldName As String)
            MyBase.New
            m_Name = fieldName
        End Sub
        
        Public Sub New(ByVal fieldName As String, ByVal newValue As Object)
            Me.New(fieldName, Nothing, newValue)
        End Sub
        
        Public Sub New(ByVal fieldName As String, ByVal oldValue As Object, ByVal newValue As Object)
            MyBase.New
            m_Name = fieldName
            m_OldValue = oldValue
            m_NewValue = newValue
            If ((Not (newValue) Is Nothing) AndAlso (Not (oldValue) Is Nothing)) Then
                m_NewValueIsSet = Not (newValue.Equals(oldValue))
            Else
                m_NewValueIsSet = ((((Not (newValue) Is Nothing) AndAlso (oldValue Is Nothing)) OrElse ((Not (oldValue) Is Nothing) AndAlso (newValue Is Nothing))) OrElse Not ((newValue = oldValue)))
            End If
        End Sub
        
        Public Sub New(ByVal fieldName As String, ByVal oldValue As Object, ByVal newValue As Object, ByVal [readOnly] As Boolean)
            Me.New(fieldName, oldValue, newValue)
            m_ReadOnly = [readOnly]
        End Sub
        
        Public Property Name() As String
            Get
                Return m_Name
            End Get
            Set
                m_Name = value
            End Set
        End Property
        
        Public Property OldValue() As Object
            Get
                Return m_OldValue
            End Get
            Set
                If (m_EnableConversion AndAlso TypeOf value Is String) Then
                    m_OldValue = DataControllerBase.StringToValue(CType(value,String))
                Else
                    m_OldValue = value
                End If
            End Set
        End Property
        
        Public Property NewValue() As Object
            Get
                Return m_NewValue
            End Get
            Set
                If (m_EnableConversion AndAlso TypeOf value Is String) Then
                    m_NewValue = DataControllerBase.StringToValue(CType(value,String))
                Else
                    m_NewValue = value
                End If
            End Set
        End Property
        
        Public Property Modified() As Boolean
            Get
                If (m_ModifiedIsSet AndAlso Not ([ReadOnly])) Then
                    Return m_Modified
                End If
                Return (m_NewValueIsSet AndAlso Not ([ReadOnly]))
            End Get
            Set
                m_Modified = value
                m_ModifiedIsSet = true
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
        
        Public Property Value() As Object
            Get
                If [ReadOnly] Then
                    If m_NewValueIsSet Then
                        Return NewValue
                    Else
                        If (m_ModifiedIsSet AndAlso m_Modified) Then
                            Return NewValue
                        Else
                            Return OldValue
                        End If
                    End If
                End If
                If Modified Then
                    Return NewValue
                Else
                    Return OldValue
                End If
            End Get
            Set
                OldValue = value
                Modified = false
            End Set
        End Property
        
        Public Property [Error]() As String
            Get
                Return m_Error
            End Get
            Set
                m_Error = value
            End Set
        End Property
        
        Public Property Scope() As String
            Get
                Return m_Scope
            End Get
            Set
                m_Scope = value
            End Set
        End Property
        
        Public Overrides Function ToString() As String
            Dim oldValueInfo = String.Empty
            Dim v = Value
            If Modified Then
                Dim ov = OldValue
                If (ov Is Nothing) Then
                    ov = "null"
                End If
                oldValueInfo = String.Format(" (old value = {0})", ov)
            End If
            Dim isReadOnly = String.Empty
            If [ReadOnly] Then
                isReadOnly = " (read-only)"
            End If
            If (v Is Nothing) Then
                v = "null"
            End If
            Dim err = String.Empty
            If Not (String.IsNullOrEmpty([Error])) Then
                err = String.Format("; Input Error: {0}", [Error])
            End If
            Return String.Format(String.Format("{0} = {1}{2}{3}{4}", Name, v, oldValueInfo, isReadOnly, err))
        End Function
        
        Public Sub AssignTo(ByVal instance As Object)
            Dim t = instance.GetType()
            Dim propInfo = t.GetProperty(Name)
            Dim v = Value
            If (Not (v) Is Nothing) Then
                If propInfo.PropertyType.IsGenericType Then
                    If propInfo.PropertyType.GetProperty("Value").PropertyType.Equals(GetType(Guid)) Then
                        v = New Guid(Convert.ToString(v))
                    Else
                        v = Convert.ChangeType(v, propInfo.PropertyType.GetProperty("Value").PropertyType)
                    End If
                Else
                    v = Convert.ChangeType(v, propInfo.PropertyType)
                End If
            End If
            t.InvokeMember(Name, System.Reflection.BindingFlags.SetProperty, Nothing, instance, New Object() {v})
        End Sub
        
        Public Sub EnableConversion()
            m_EnableConversion = true
        End Sub
        
        Public Sub DisableConversion()
            m_EnableConversion = false
        End Sub
    End Class
    
    Public Class FieldValueDictionary
        Inherits SortedDictionary(Of String, FieldValue)
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal args As ActionArgs)
            MyBase.New
            If (Not (args.Values) Is Nothing) Then
                AddRange(args.Values)
            End If
        End Sub
        
        Public Sub New(ByVal values As List(Of FieldValue))
            MyBase.New
            If (Not (values) Is Nothing) Then
                AddRange(values.ToArray())
            End If
        End Sub
        
        Public Sub New(ByVal values() As FieldValue)
            MyBase.New
            If (Not (values) Is Nothing) Then
                AddRange(values)
            End If
        End Sub
        
        Public Sub AddRange(ByVal values() As FieldValue)
            For Each fvo in values
                Me(fvo.Name) = fvo
            Next
        End Sub
        
        Public Sub Assign(ByVal values As IDictionary, ByVal assignToNewValues As Boolean)
            For Each fieldName As String in values.Keys
                If Not (ContainsKey(fieldName)) Then
                    Add(fieldName, New FieldValue(fieldName))
                End If
                Dim v = Me(fieldName)
                If assignToNewValues Then
                    v.NewValue = values(fieldName)
                Else
                    v.OldValue = values(fieldName)
                End If
            Next
        End Sub
    End Class
End Namespace
