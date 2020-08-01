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
    
    Public Enum DynamicExpressionScope
        
        Field
        
        ViewRowStyle
        
        CategoryVisibility
        
        DataFieldVisibility
        
        DefaultValues
        
        [ReadOnly]
        
        Rule
    End Enum
    
    Public Enum DynamicExpressionType
        
        RegularExpression
        
        ClientScript
    End Enum
    
    Public Class DynamicExpression
        
        Private m_Scope As DynamicExpressionScope
        
        Private m_Target As String
        
        Private m_Type As DynamicExpressionType
        
        Private m_Test As String
        
        Private m_Result As String
        
        Private m_ViewId As String
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Sub New(ByVal expression As XPathNavigator, ByVal nm As XmlNamespaceManager)
            MyBase.New
            Dim scope = expression.SelectSingleNode("parent::c:*", nm)
            Dim target = expression.SelectSingleNode("parent::c:*/parent::c:*", nm)
            If (scope.LocalName = "validate") Then
                m_Scope = DynamicExpressionScope.Field
                m_Target = target.GetAttribute("name", String.Empty)
            Else
                If (scope.LocalName = "styles") Then
                    m_Scope = DynamicExpressionScope.ViewRowStyle
                    m_Target = target.GetAttribute("id", String.Empty)
                Else
                    If (scope.LocalName = "visibility") Then
                        'determine the scope and target of visibility
                        If (target.LocalName = "field") Then
                            m_Scope = DynamicExpressionScope.DataFieldVisibility
                            m_Target = target.GetAttribute("name", String.Empty)
                        Else
                            If (target.LocalName = "dataField") Then
                                m_Scope = DynamicExpressionScope.DataFieldVisibility
                                m_Target = target.GetAttribute("fieldName", String.Empty)
                            Else
                                If (target.LocalName = "category") Then
                                    m_Scope = DynamicExpressionScope.CategoryVisibility
                                    m_Target = target.GetAttribute("id", String.Empty)
                                End If
                            End If
                        End If
                    Else
                        If (scope.LocalName = "defaultValues") Then
                            'determine the scope and target of default values
                            If (target.LocalName = "field") Then
                                m_Scope = DynamicExpressionScope.DataFieldVisibility
                                m_Target = target.GetAttribute("name", String.Empty)
                            Else
                                If (target.LocalName = "dataField") Then
                                    m_Scope = DynamicExpressionScope.DataFieldVisibility
                                    m_Target = target.GetAttribute("fieldName", String.Empty)
                                End If
                            End If
                        Else
                            If (scope.LocalName = "readOnly") Then
                                'determine the scope and target of read-only expression
                                If (target.LocalName = "field") Then
                                    m_Scope = DynamicExpressionScope.ReadOnly
                                    m_Target = target.GetAttribute("name", String.Empty)
                                Else
                                    If (target.LocalName = "dataField") Then
                                        m_Scope = DynamicExpressionScope.ReadOnly
                                        m_Target = target.GetAttribute("fieldName", String.Empty)
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If
            Dim expressionType = expression.GetAttribute("type", String.Empty)
            If String.IsNullOrEmpty(expressionType) Then
                expressionType = "ClientScript"
            End If
            m_Type = CType(TypeDescriptor.GetConverter(GetType(DynamicExpressionType)).ConvertFromString(expressionType),DynamicExpressionType)
            m_Test = expression.GetAttribute("test", String.Empty)
            m_Result = expression.GetAttribute("result", String.Empty)
            If (m_Result = String.Empty) Then
                m_Result = Nothing
            End If
            m_ViewId = CType(expression.Evaluate("string(ancestor::c:view/@id)", nm),String)
        End Sub
        
        Public Property Scope() As DynamicExpressionScope
            Get
                Return m_Scope
            End Get
            Set
                m_Scope = value
            End Set
        End Property
        
        Public Property Target() As String
            Get
                Return m_Target
            End Get
            Set
                m_Target = value
            End Set
        End Property
        
        Public Property Type() As DynamicExpressionType
            Get
                Return m_Type
            End Get
            Set
                m_Type = value
            End Set
        End Property
        
        Public Property Test() As String
            Get
                Return m_Test
            End Get
            Set
                m_Test = value
            End Set
        End Property
        
        Public Property Result() As String
            Get
                Return m_Result
            End Get
            Set
                m_Result = value
            End Set
        End Property
        
        Public Property ViewId() As String
            Get
                Return m_ViewId
            End Get
            Set
                m_ViewId = value
            End Set
        End Property
        
        Public Function AllowedInView(ByVal view As String) As Boolean
            Return (String.IsNullOrEmpty(m_ViewId) OrElse (m_ViewId = view))
        End Function
    End Class
End Namespace
