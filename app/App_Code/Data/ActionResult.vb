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
    
    Public Class ActionResult
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Tag As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Errors As List(Of String)
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Values As List(Of FieldValue)
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_CanceledSelectedValues As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Canceled As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_NavigateUrl As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ClientScript As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_RowsAffected As Integer
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_KeepSelection As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_ClearSelection As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Filter() As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SortExpression As String
        
        Private m_RowNotFound As Boolean
        
        Public Sub New()
            MyBase.New
            Me.m_Errors = New List(Of String)()
            Me.m_Values = New List(Of FieldValue)()
        End Sub
        
        Public Property Tag() As String
            Get
                Return m_Tag
            End Get
            Set
                m_Tag = value
            End Set
        End Property
        
        Public ReadOnly Property Errors() As List(Of String)
            Get
                Return m_Errors
            End Get
        End Property
        
        Public ReadOnly Property Values() As List(Of FieldValue)
            Get
                Return m_Values
            End Get
        End Property
        
        Public Property CanceledSelectedValues() As Boolean
            Get
                Return m_CanceledSelectedValues
            End Get
            Set
                m_CanceledSelectedValues = value
            End Set
        End Property
        
        Public Property Canceled() As Boolean
            Get
                Return m_Canceled
            End Get
            Set
                m_Canceled = value
            End Set
        End Property
        
        Public Property NavigateUrl() As String
            Get
                Return m_NavigateUrl
            End Get
            Set
                m_NavigateUrl = value
            End Set
        End Property
        
        Public Property ClientScript() As String
            Get
                Return m_ClientScript
            End Get
            Set
                m_ClientScript = value
            End Set
        End Property
        
        Public Property RowsAffected() As Integer
            Get
                Return m_RowsAffected
            End Get
            Set
                m_RowsAffected = value
            End Set
        End Property
        
        Public Property KeepSelection() As Boolean
            Get
                Return m_KeepSelection
            End Get
            Set
                m_KeepSelection = value
            End Set
        End Property
        
        Public Property ClearSelection() As Boolean
            Get
                Return m_ClearSelection
            End Get
            Set
                m_ClearSelection = value
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
        
        Public Property RowNotFound() As Boolean
            Get
                Return m_RowNotFound
            End Get
            Set
                m_RowNotFound = value
            End Set
        End Property
        
        Public Sub RaiseExceptionIfErrors()
            If (Errors.Count > 0) Then
                Dim sb = New StringBuilder()
                For Each er in Errors
                    sb.AppendLine(er)
                    Throw New Exception(sb.ToString())
                Next
            End If
        End Sub
        
        Public Function ToObject(Of T)() As T
            Dim objectType = GetType(T)
            Dim theObject = CType(objectType.Assembly.CreateInstance(objectType.FullName),T)
            AssignTo(theObject)
            Return theObject
        End Function
        
        Public Sub AssignTo(ByVal instance As Object)
            For Each v in Values
                v.AssignTo(instance)
            Next
        End Sub
        
        Public Overloads Sub ShowMessage(ByVal format As String, ByVal ParamArray args() as System.[Object])
            ShowMessage(String.Format(format, args))
        End Sub
        
        Public Overloads Sub ShowMessage(ByVal message As String)
            ExecuteOnClient("Web.DataView.showMessage('{0}');", BusinessRules.JavaScriptString(message))
        End Sub
        
        Public Overloads Sub ShowViewMessage(ByVal format As String, ByVal ParamArray args() as System.[Object])
            ShowViewMessage(String.Format(format, args))
        End Sub
        
        Public Overloads Sub ShowViewMessage(ByVal message As String)
            ExecuteOnClient("this.showViewMessage('{0}');", BusinessRules.JavaScriptString(message))
        End Sub
        
        Public Overloads Sub Focus(ByVal fieldName As String, ByVal fmt As String, ByVal ParamArray args() as System.[Object])
            ExecuteOnClient("this._serverFocus('{0}','{1}');", fieldName, BusinessRules.JavaScriptString(String.Format(fmt, args)))
        End Sub
        
        Public Overloads Sub Focus(ByVal fieldName As String)
            Focus(fieldName, String.Empty)
        End Sub
        
        Public Overloads Sub ExecuteOnClient(ByVal javaScriptFormatString As String, ByVal ParamArray args() as System.[Object])
            ExecuteOnClient(String.Format(javaScriptFormatString, args))
        End Sub
        
        Public Overloads Sub ExecuteOnClient(ByVal javaScript As String)
            If (Not (String.IsNullOrEmpty(ClientScript)) AndAlso Not (ClientScript.EndsWith(";"))) Then
                ClientScript = (ClientScript + ";")
            End If
            If Not (String.IsNullOrEmpty(javaScript)) Then
                ClientScript = (ClientScript + javaScript)
            End If
        End Sub
        
        Public Sub ShowLastView()
            ExecuteOnClient("this.goToView(this._lastViewId);")
        End Sub
        
        Public Sub ShowView(ByVal viewId As String)
            ExecuteOnClient("this.goToView('{0}');", viewId)
        End Sub
        
        Public Overloads Sub ShowAlert(ByVal message As String)
            ExecuteOnClient("$app.alert('{0}');", BusinessRules.JavaScriptString(message))
        End Sub
        
        Public Overloads Sub ShowAlert(ByVal fmt As String, ByVal ParamArray args() as System.[Object])
            ShowAlert(String.Format(fmt, args))
        End Sub
        
        Public Sub HideModal()
            ExecuteOnClient("this.endModalState('Cancel');")
        End Sub
        
        Public Sub ShowModal(ByVal controller As String, ByVal view As String, ByVal startCommandName As String, ByVal startCommandArgument As String)
            ExecuteOnClient("if(this._container&&this.get_controller()=='{0}'){{this._savePosition();this._sho"& _ 
                    "wModal({{commandName:'{2}',commandArgument:'{3}'}});}}else Web.DataView.showModa"& _ 
                    "l(null, '{0}', '{1}', '{2}', '{3}');", controller, view, startCommandName, startCommandArgument)
        End Sub
        
        Public Sub SelectFirstRow()
            ExecuteOnClient("this.set_autoSelectFirstRow(true);this._autoSelect();")
        End Sub
        
        Public Sub HighlightFirstRow()
            ExecuteOnClient("this.set_autoHighlightFirstRow(true);this._autoSelect();")
        End Sub
        
        ''' <summary>
        ''' Refreshes the data view that has caused execution of business rules. Fresh data will be fetched from the server.
        ''' </summary>
        Public Overloads Sub Refresh()
            Refresh(true)
        End Sub
        
        ''' <summary>
        ''' Refreshes the data view that has caused execution of business rules.
        ''' </summary>
        ''' <param name="fetch">Indicates that the fresh data must be fetched from the server.</param>
        Public Overloads Sub Refresh(ByVal fetch As Boolean)
            Dim noFetch = Not (fetch)
            ExecuteOnClient("this.refresh({0});", noFetch.ToString().ToLower())
        End Sub
        
        ''' <summary>
        ''' Refreshes the children of the data view that has caused execution of business rules.
        ''' </summary>
        Public Sub RefreshChildren()
            ExecuteOnClient("this.refreshChildren();")
        End Sub
        
        ''' <summary>
        ''' Ensures that the action state machine will execute an iteration when the server response is returned to the client library.
        ''' </summary>
        Public Sub [Continue]()
            Dim script = "this._continueAfterScript=true;"
            If (String.IsNullOrEmpty(ClientScript) OrElse Not (ClientScript.Contains(script))) Then
                ExecuteOnClient(script)
            End If
        End Sub
        
        Public Overloads Sub Merge(ByVal page As ViewPage)
            page.ClientScript = ClientScript
        End Sub
        
        Public Overloads Sub Merge(ByVal result As ActionResult)
            For Each er in result.Errors
                Errors.Add(er)
            Next
            ClientScript = (ClientScript + result.ClientScript)
            For Each v in result.Values
                Values.Add(v)
            Next
        End Sub
        
        Public Sub EnsureJsonCompatibility()
            If (Not (Values) Is Nothing) Then
                For Each v in Values
                    If v.Modified Then
                        v.DisableConversion()
                        v.NewValue = DataControllerBase.EnsureJsonCompatibility(v.NewValue)
                        v.EnableConversion()
                    End If
                Next
            End If
        End Sub
    End Class
End Namespace
