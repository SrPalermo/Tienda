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
    
    Public Class ActionGroup
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Scope As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_HeaderText As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Flat As Boolean
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Id As String
        
        Private m_Actions As List(Of Action)
        
        Public Sub New()
            MyBase.New
            Me.m_Actions = New List(Of Action)()
        End Sub
        
        Public Sub New(ByVal actionGroup As XPathNavigator, ByVal resolver As IXmlNamespaceResolver)
            Me.New()
            Me.m_Scope = actionGroup.GetAttribute("scope", String.Empty)
            Me.m_HeaderText = actionGroup.GetAttribute("headerText", String.Empty)
            Me.m_Id = actionGroup.GetAttribute("id", String.Empty)
            m_Flat = (actionGroup.GetAttribute("flat", String.Empty) = "true")
            Dim actionIterator = actionGroup.Select("c:action", resolver)
            Do While actionIterator.MoveNext()
                If Controller.UserIsInRole(actionIterator.Current.GetAttribute("roles", String.Empty)) Then
                    Me.Actions.Add(New Action(actionIterator.Current, resolver))
                End If
            Loop
        End Sub
        
        Public Property Scope() As String
            Get
                Return m_Scope
            End Get
            Set
                m_Scope = value
            End Set
        End Property
        
        Public Property HeaderText() As String
            Get
                Return m_HeaderText
            End Get
            Set
                m_HeaderText = value
            End Set
        End Property
        
        Public Property Flat() As Boolean
            Get
                Return m_Flat
            End Get
            Set
                m_Flat = value
            End Set
        End Property
        
        Public Property Id() As String
            Get
                Return m_Id
            End Get
            Set
                m_Id = value
            End Set
        End Property
        
        Public ReadOnly Property Actions() As List(Of Action)
            Get
                Return m_Actions
            End Get
        End Property
    End Class
End Namespace
