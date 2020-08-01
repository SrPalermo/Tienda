Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.Common
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Xml.XPath

Namespace Norwindt.Data
    
    ''' <summary>
    ''' Links a data controller business rule to a method with its implementation.
    ''' </summary>
    <AttributeUsage(AttributeTargets.Method, AllowMultiple:=true, Inherited:=true)>  _
    Public Class RuleAttribute
        Inherits Attribute
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Id As String
        
        ''' <summary>
        ''' Links the method to the business rule by its Id.
        ''' </summary>
        ''' <param name="id">The Id of the data controller business rule.</param>
        Public Sub New(ByVal id As String)
            MyBase.New
            Me.Id = id
        End Sub
        
        ''' <summary>
        ''' The Id of the data controller business rule.
        ''' </summary>
        Public Property Id() As String
            Get
                Return m_Id
            End Get
            Set
                m_Id = value
            End Set
        End Property
    End Class
    
    Public Class ActionHandlerBase
        Inherits Object
        Implements Norwindt.Data.IActionHandler
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Arguments As ActionArgs
        
        Private m_Result As ActionResult
        
        Private m_Whitelist As List(Of String)
        
        Private m_Blacklist As List(Of String)
        
        Public Property Arguments() As ActionArgs
            Get
                Return m_Arguments
            End Get
            Set
                m_Arguments = value
            End Set
        End Property
        
        Public Property Result() As ActionResult
            Get
                If (m_Result Is Nothing) Then
                    m_Result = New ActionResult()
                End If
                Return m_Result
            End Get
            Set
                m_Result = value
            End Set
        End Property
        
        Public Property Whitelist() As String
            Get
                If (m_Whitelist Is Nothing) Then
                    Return String.Empty
                End If
                Return String.Join(",", m_Whitelist.ToArray())
            End Get
            Set
                m_Whitelist = New List(Of String)(value.Split(Global.Microsoft.VisualBasic.ChrW(44), Global.Microsoft.VisualBasic.ChrW(59)))
            End Set
        End Property
        
        Public Property Blacklist() As String
            Get
                If (m_Blacklist Is Nothing) Then
                    Return String.Empty
                End If
                Return String.Join(",", m_Blacklist.ToArray())
            End Get
            Set
                m_Blacklist = New List(Of String)(value.Split(Global.Microsoft.VisualBasic.ChrW(44), Global.Microsoft.VisualBasic.ChrW(59)))
            End Set
        End Property
        
        Public Overloads Sub PreventDefault()
            PreventDefault(false)
        End Sub
        
        Public Overloads Sub PreventDefault(ByVal cancelSelectedValues As Boolean)
            If (Not (m_Result) Is Nothing) Then
                m_Result.Canceled = true
                If cancelSelectedValues Then
                    m_Result.CanceledSelectedValues = true
                End If
            End If
        End Sub
        
        Public Sub ClearBlackAndWhiteLists()
            If (Not (m_Whitelist) Is Nothing) Then
                m_Whitelist.Clear()
            End If
            If (Not (m_Blacklist) Is Nothing) Then
                m_Blacklist.Clear()
            End If
        End Sub
        
        Protected Sub AddToWhitelist(ByVal rule As String)
            If (m_Whitelist Is Nothing) Then
                m_Whitelist = New List(Of String)()
            End If
            If Not (m_Whitelist.Contains(rule)) Then
                m_Whitelist.Add(rule)
            End If
        End Sub
        
        Protected Sub RemoveFromWhitelist(ByVal rule As String)
            If (Not (m_Whitelist) Is Nothing) Then
                m_Whitelist.Remove(rule)
            End If
        End Sub
        
        Public Function RuleInWhitelist(ByVal rule As String) As Boolean
            Return ((m_Whitelist Is Nothing) OrElse ((m_Whitelist.Count = 0) OrElse m_Whitelist.Contains(rule)))
        End Function
        
        Protected Sub AddToBlacklist(ByVal rule As String)
            If (m_Blacklist Is Nothing) Then
                m_Blacklist = New List(Of String)()
            End If
            If Not (m_Blacklist.Contains(rule)) Then
                m_Blacklist.Add(rule)
            End If
        End Sub
        
        Protected Sub RemoveFromBlacklist(ByVal rule As String)
            If (Not (m_Blacklist) Is Nothing) Then
                m_Blacklist.Remove(rule)
            End If
        End Sub
        
        Public Function RuleInBlacklist(ByVal rule As String) As Boolean
            Return ((Not (m_Blacklist) Is Nothing) AndAlso m_Blacklist.Contains(rule))
        End Function
        
        <System.Diagnostics.DebuggerStepThrough()>  _
        Protected Overridable Sub ExecuteMethod(ByVal args As ActionArgs, ByVal result As ActionResult, ByVal phase As ActionPhase)
            Dim match = InternalExecuteMethod(args, result, phase, true, true)
            If Not (match) Then
                match = InternalExecuteMethod(args, result, phase, true, false)
            End If
            If Not (match) Then
                match = InternalExecuteMethod(args, result, phase, false, true)
            End If
            If Not (match) Then
                InternalExecuteMethod(args, result, phase, false, false)
            End If
        End Sub
        
        Private Function InternalExecuteMethod(ByVal args As ActionArgs, ByVal result As ActionResult, ByVal phase As ActionPhase, ByVal viewMatch As Boolean, ByVal argumentMatch As Boolean) As Boolean
            m_Arguments = args
            m_Result = result
            Dim success = false
            Dim methods = [GetType]().GetMethods((BindingFlags.Public Or (BindingFlags.NonPublic Or BindingFlags.Instance)))
            For Each method in methods
                Dim filters = method.GetCustomAttributes(GetType(ControllerActionAttribute), true)
                For Each action As ControllerActionAttribute in filters
                    If (((action.Controller = args.Controller) OrElse (Not (String.IsNullOrEmpty(args.Controller)) AndAlso Regex.IsMatch(args.Controller, action.Controller))) AndAlso ((Not (viewMatch) AndAlso String.IsNullOrEmpty(action.View)) OrElse (action.View = args.View))) Then
                        If ((action.CommandName = args.CommandName) AndAlso ((Not (argumentMatch) AndAlso String.IsNullOrEmpty(action.CommandArgument)) OrElse (action.CommandArgument = args.CommandArgument))) Then
                            If (action.Phase = phase) Then
                                Dim parameters = method.GetParameters()
                                If ((parameters.Length = 2) AndAlso ((parameters(0).ParameterType Is GetType(ActionArgs)) AndAlso (parameters(1).ParameterType Is GetType(ActionResult)))) Then
                                    method.Invoke(Me, New Object() {args, result})
                                Else
                                    Dim arguments((parameters.Length) - 1) As Object
                                    Dim i = 0
                                    Do While (i < parameters.Length)
                                        Dim p = parameters(i)
                                        Dim v = SelectFieldValueObject(p.Name)
                                        If (Not (v) Is Nothing) Then
                                            If p.ParameterType.Equals(GetType(FieldValue)) Then
                                                arguments(i) = v
                                            Else
                                                Try 
                                                    arguments(i) = DataControllerBase.ConvertToType(p.ParameterType, v.Value)
                                                Catch __exception As Exception
                                                End Try
                                            End If
                                        End If
                                        i = (i + 1)
                                    Loop
                                    method.Invoke(Me, arguments)
                                    success = true
                                End If
                            End If
                        End If
                    End If
                Next
            Next
            Return success
        End Function
        
        Protected Overridable Sub BeforeSqlAction(ByVal args As ActionArgs, ByVal result As ActionResult)
        End Sub
        
        Protected Overridable Sub AfterSqlAction(ByVal args As ActionArgs, ByVal result As ActionResult)
        End Sub
        
        Protected Overridable Sub ExecuteAction(ByVal args As ActionArgs, ByVal result As ActionResult)
        End Sub
        
        Sub IActionHandler_BeforeSqlAction(ByVal args As ActionArgs, ByVal result As ActionResult) Implements IActionHandler.BeforeSqlAction
            ExecuteMethod(args, result, ActionPhase.Before)
            BeforeSqlAction(args, result)
        End Sub
        
        Sub IActionHandler_AfterSqlAction(ByVal args As ActionArgs, ByVal result As ActionResult) Implements IActionHandler.AfterSqlAction
            ExecuteMethod(args, result, ActionPhase.After)
            AfterSqlAction(args, result)
        End Sub
        
        Sub IActionHandler_ExecuteAction(ByVal args As ActionArgs, ByVal result As ActionResult) Implements IActionHandler.ExecuteAction
            ExecuteMethod(args, result, ActionPhase.Execute)
            ExecuteAction(args, result)
        End Sub
        
        Public Overridable Function SelectFieldValueObject(ByVal name As String) As FieldValue
            Return Nothing
        End Function
        
        Protected Overridable Function BuildingDataRows() As Boolean
            Return false
        End Function
        
        Protected Overloads Overridable Sub ExecuteRule(ByVal rule As XPathNavigator)
            ExecuteRule(rule.GetAttribute("id", String.Empty))
        End Sub
        
        Protected Overridable Sub BlockRule(ByVal id As String)
            If Not (BuildingDataRows()) Then
                AddToBlacklist(id)
            End If
        End Sub
        
        Protected Overloads Overridable Sub ExecuteRule(ByVal ruleId As String)
            Dim methods = [GetType]().GetMethods((BindingFlags.Public Or (BindingFlags.NonPublic Or BindingFlags.Instance)))
            For Each method in methods
                Dim ruleBindings = method.GetCustomAttributes(GetType(RuleAttribute), true)
                For Each ra As RuleAttribute in ruleBindings
                    If (ra.Id = ruleId) Then
                        BlockRule(ruleId)
                        Dim parameters = method.GetParameters()
                        Dim arguments((parameters.Length) - 1) As Object
                        Dim i = 0
                        Do While (i < parameters.Length)
                            Dim p = parameters(i)
                            If p.ParameterType.IsSubclassOf(GetType(BusinessRulesObjectModel)) Then
                                Dim self = p.ParameterType.Assembly.CreateInstance(p.ParameterType.FullName, true, BindingFlags.CreateInstance, Nothing, New Object() {Me}, System.Globalization.CultureInfo.CurrentCulture, Nothing)
                                Dim fields = self.GetType().GetFields((BindingFlags.Instance Or BindingFlags.NonPublic))
                                For Each fi in fields
                                    Dim index = (fi.Name.IndexOf("_") + 1)
                                    Dim fieldName = fi.Name.Substring(index)
                                    If (fieldName.Length = 1) Then
                                        fieldName = fieldName.ToUpper()
                                    Else
                                        fieldName = (Char.ToUpperInvariant(fieldName(0)) + fieldName.Substring(1))
                                    End If
                                    Dim v = SelectFieldValueObject(fieldName)
                                    If (Not (v) Is Nothing) Then
                                        Try 
                                            self.GetType().InvokeMember(fi.Name, (BindingFlags.SetField Or (BindingFlags.Instance Or BindingFlags.NonPublic)), Nothing, self, New Object() {DataControllerBase.ConvertToType(fi.FieldType, v.Value)})
                                        Finally
                                            'release resources here
                                        End Try
                                    End If
                                Next
                                arguments(i) = self
                            Else
                                Dim v = SelectFieldValueObject(p.Name)
                                If (Not (v) Is Nothing) Then
                                    If p.ParameterType.Equals(GetType(FieldValue)) Then
                                        arguments(i) = v
                                    Else
                                        Try 
                                            arguments(i) = DataControllerBase.ConvertToType(p.ParameterType, v.Value)
                                        Catch __exception As Exception
                                        End Try
                                    End If
                                End If
                            End If
                            i = (i + 1)
                        Loop
                        method.Invoke(Me, arguments)
                    End If
                Next
            Next
        End Sub
        
        ''' <summary>
        ''' Returns True if there are no field values with errors.
        ''' </summary>
        ''' <returns>True if all field values have a blank 'Error' property.</returns>
        Protected Function ValidateInput() As Boolean
            If (Not (Arguments) Is Nothing) Then
                For Each v in Arguments.Values
                    If Not (String.IsNullOrEmpty(v.Error)) Then
                        Return false
                    End If
                Next
            End If
            Return true
        End Function
    End Class
    
    Public Class BusinessRulesObjectModel
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Rules As BusinessRules
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal rules As BusinessRules)
            MyBase.New
            Me.m_Rules = rules
        End Sub
        
        Public Default ReadOnly Property Item(ByVal name As String) As FieldValue
            Get
                If (m_Rules Is Nothing) Then
                    Return Nothing
                End If
                Dim fv = m_Rules.SelectFieldValueObject(name)
                If (fv Is Nothing) Then
                    fv = New FieldValue(name, Nothing, Nothing, true)
                    fv.Error = "This field is missing in the arguments."
                End If
                Return fv
            End Get
        End Property
        
        Protected Overridable Sub UpdateFieldValue(ByVal fieldName As String, ByVal value As Object)
            If (Not (Me.m_Rules) Is Nothing) Then
                Me.m_Rules.UpdateFieldValue(fieldName, value)
            End If
        End Sub
        
        Public Shared Function CreateInstance(Of T)(ByVal ParamArray values() as System.[Object]) As T
            Dim parameters = New BusinessObjectParameters(values)
            Dim instance = CType(Activator.CreateInstance(GetType(T)),T)
            Dim instanceType = instance.GetType()
            For Each name in parameters.Keys
                Dim pi = instanceType.GetProperty(name.Substring(1), (BindingFlags.IgnoreCase Or (BindingFlags.Public Or BindingFlags.Instance)))
                If (Not (pi) Is Nothing) Then
                    Try 
                        Dim v = parameters(name)
                        If ((Not (v) Is Nothing) AndAlso pi.PropertyType.IsGenericType) Then
                            v = Convert.ChangeType(v, Nullable.GetUnderlyingType(pi.PropertyType))
                        End If
                        pi.SetValue(instance, v)
                    Catch ex As Exception
                        Throw New Exception(String.Format("{0}.{1}: {2}", instanceType.Name, pi.Name, ex.Message))
                    End Try
                End If
            Next
            Return instance
        End Function
    End Class
End Namespace
