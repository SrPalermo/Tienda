Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Data.Common
Imports System.IO
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
    
    Public Class View
        
        Private m_Id As String
        
        Private m_Label As String
        
        Private m_HeaderText As String
        
        Private m_Type As String
        
        Private m_Group As String
        
        Private m_ShowInSelector As Boolean
        
        Private m_Tags As String
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal view As XPathNavigator, ByVal mainView As XPathNavigator, ByVal resolver As IXmlNamespaceResolver)
            MyBase.New
            Me.m_Id = view.GetAttribute("id", String.Empty)
            Me.m_Type = view.GetAttribute("type", String.Empty)
            If (Me.m_Id = mainView.GetAttribute("virtualViewId", String.Empty)) Then
                view = mainView
            End If
            Me.m_Label = view.GetAttribute("label", String.Empty)
            Dim headerTextNav = view.SelectSingleNode("c:headerText", resolver)
            If (Not (headerTextNav) Is Nothing) Then
                Me.m_HeaderText = headerTextNav.Value
            End If
            m_Group = view.GetAttribute("group", String.Empty)
            m_Tags = view.GetAttribute("tags", String.Empty)
            m_ShowInSelector = Not ((view.GetAttribute("showInSelector", String.Empty) = "false"))
        End Sub
        
        Public Property Id() As String
            Get
                Return m_Id
            End Get
            Set
                m_Id = value
            End Set
        End Property
        
        Public ReadOnly Property Label() As String
            Get
                Return m_Label
            End Get
        End Property
        
        Public ReadOnly Property Type() As String
            Get
                Return m_Type
            End Get
        End Property
        
        Public ReadOnly Property Group() As String
            Get
                Return m_Group
            End Get
        End Property
        
        Public ReadOnly Property ShowInSelector() As Boolean
            Get
                Return m_ShowInSelector
            End Get
        End Property
        
        Public ReadOnly Property Tags() As String
            Get
                Return m_Tags
            End Get
        End Property
        
        Public Function HeaderText() As String
            Return m_HeaderText
        End Function
    End Class
End Namespace
