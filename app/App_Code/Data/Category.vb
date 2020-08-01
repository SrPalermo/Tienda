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
    
    Public Class Category
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Id As String
        
        Private m_Index As Integer
        
        Private m_HeaderText As String
        
        Private m_Description As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Flow As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Wrap As Nullable(Of Boolean)
        
        Private m_Tab As String
        
        Private m_Wizard As String
        
        Private m_Template As String
        
        Private m_Floating As Boolean
        
        Private m_Collapsed As Boolean
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal category As XPathNavigator, ByVal resolver As IXmlNamespaceResolver)
            MyBase.New
            Me.Id = category.GetAttribute("id", String.Empty)
            Me.m_Index = Convert.ToInt32(category.Evaluate("count(preceding-sibling::c:category)", resolver))
            Me.m_HeaderText = CType(category.GetAttribute("headerText", String.Empty),String)
            Dim descriptionNav = category.SelectSingleNode("c:description", resolver)
            If (Not (descriptionNav) Is Nothing) Then
                Me.m_Description = descriptionNav.Value
            End If
            m_Tab = category.GetAttribute("tab", String.Empty)
            m_Wizard = category.GetAttribute("wizard", String.Empty)
            m_Flow = category.GetAttribute("flow", String.Empty)
            Dim doWrap = category.GetAttribute("wrap", String.Empty)
            If Not (String.IsNullOrEmpty(doWrap)) Then
                m_Wrap = (doWrap = "true")
            End If
            Dim templateNav = category.SelectSingleNode("c:template", resolver)
            If (Not (templateNav) Is Nothing) Then
                Me.m_Template = templateNav.Value
            End If
            m_Floating = (category.GetAttribute("floating", String.Empty) = "true")
            m_Collapsed = (category.GetAttribute("collapsed", String.Empty) = "true")
        End Sub
        
        Public Property Id() As String
            Get
                Return m_Id
            End Get
            Set
                m_Id = value
            End Set
        End Property
        
        Public ReadOnly Property Index() As Integer
            Get
                Return m_Index
            End Get
        End Property
        
        Public ReadOnly Property HeaderText() As String
            Get
                Return m_HeaderText
            End Get
        End Property
        
        Public ReadOnly Property Description() As String
            Get
                Return m_Description
            End Get
        End Property
        
        Public Property Flow() As String
            Get
                Return m_Flow
            End Get
            Set
                m_Flow = value
            End Set
        End Property
        
        Public Property Wrap() As Nullable(Of Boolean)
            Get
                Return m_Wrap
            End Get
            Set
                m_Wrap = value
            End Set
        End Property
        
        Public Property Tab() As String
            Get
                Return m_Tab
            End Get
            Set
                m_Tab = value
            End Set
        End Property
        
        Public Property Wizard() As String
            Get
                Return m_Wizard
            End Get
            Set
                m_Wizard = value
            End Set
        End Property
        
        Public Property Template() As String
            Get
                Return m_Template
            End Get
            Set
                m_Template = value
            End Set
        End Property
        
        Public Property Floating() As Boolean
            Get
                Return m_Floating
            End Get
            Set
                m_Floating = value
            End Set
        End Property
        
        Public Property Collapsed() As Boolean
            Get
                Return m_Collapsed
            End Get
            Set
                m_Collapsed = value
            End Set
        End Property
    End Class
End Namespace
