Imports Norwindt.Data
Imports Norwindt.Services
Imports System
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Security
Imports System.Xml.XPath

Namespace Norwindt.Security
    
    Partial Public Class EventTracker
        Inherits EventTrackerBase
    End Class
    
    Public Class EventTrackerBase
        
        Private Shared m_ModifiedByUserNameRegex As Regex = Nothing
        
        Private Shared m_ModifiedByUserIdRegex As Regex = Nothing
        
        Private Shared m_ModifiedOnRegex As Regex = Nothing
        
        Private Shared m_CreatedByUserNameRegex As Regex = Nothing
        
        Private Shared m_CreatedByUserIdRegex As Regex = Nothing
        
        Private Shared m_CreatedOnRegex As Regex = Nothing
        
        Private m_Email As String
        
        Private m_UserId As Object
        
        Public Sub New()
            MyBase.New
        End Sub
        
        Public Overridable ReadOnly Property Email() As String
            Get
                EnsureMembershipUserProperties()
                Return m_Email
            End Get
        End Property
        
        Public Overridable ReadOnly Property UserId() As Object
            Get
                EnsureMembershipUserProperties()
                Return m_UserId
            End Get
        End Property
        
        Public Overridable ReadOnly Property UserName() As String
            Get
                Return HttpContext.Current.User.Identity.Name
            End Get
        End Property
        
        Public Overridable ReadOnly Property DateTimeFormatString() As String
            Get
                Return "{0:g}"
            End Get
        End Property
        
        Public Overridable Function IsModifiedByUserNamePattern(ByVal fieldName As String) As Boolean
            Return ((Not (m_ModifiedByUserNameRegex) Is Nothing) AndAlso m_ModifiedByUserNameRegex.IsMatch(fieldName))
        End Function
        
        Public Overridable Function IsModifiedByUserIdPattern(ByVal fieldName As String) As Boolean
            Return ((Not (m_ModifiedByUserIdRegex) Is Nothing) AndAlso m_ModifiedByUserIdRegex.IsMatch(fieldName))
        End Function
        
        Public Overridable Function IsModifiedOnPattern(ByVal fieldName As String) As Boolean
            Return ((Not (m_ModifiedOnRegex) Is Nothing) AndAlso m_ModifiedOnRegex.IsMatch(fieldName))
        End Function
        
        Public Overridable Function IsCreatedByUserNamePattern(ByVal fieldName As String) As Boolean
            Return ((Not (m_CreatedByUserNameRegex) Is Nothing) AndAlso m_CreatedByUserNameRegex.IsMatch(fieldName))
        End Function
        
        Public Overridable Function IsCreatedByUserIdPattern(ByVal fieldName As String) As Boolean
            Return ((Not (m_CreatedByUserIdRegex) Is Nothing) AndAlso m_CreatedByUserIdRegex.IsMatch(fieldName))
        End Function
        
        Public Overridable Function IsCreatedOnPattern(ByVal fieldName As String) As Boolean
            Return ((Not (m_CreatedOnRegex) Is Nothing) AndAlso m_CreatedOnRegex.IsMatch(fieldName))
        End Function
        
        Public Overloads Shared Sub Process(ByVal page As ViewPage, ByVal request As PageRequest)
            Dim tracker = New EventTracker()
            tracker.InternalProcess(page, request)
        End Sub
        
        Protected Overridable Function IsNewRow(ByVal page As ViewPage, ByVal request As PageRequest) As Boolean
            If (request.Inserting AndAlso (page.NewRow Is Nothing)) Then
                page.NewRow = New Object((page.Fields.Count) - 1) {}
            End If
            Return request.Inserting
        End Function
        
        Protected Overloads Overridable Sub InternalProcess(ByVal page As ViewPage, ByVal request As PageRequest)
            Dim index = 0
            For Each field in page.Fields
                If Not (field.ReadOnly) Then
                    If (IsCreatedByUserIdPattern(field.Name) OrElse IsModifiedByUserIdPattern(field.Name)) Then
                        field.TextMode = TextInputMode.Static
                        If (String.IsNullOrEmpty(field.ItemsDataController) OrElse request.Inserting) Then
                            field.Hidden = true
                        Else
                            If Not (ApplicationServicesBase.IsSuperUser) Then
                                field.Tag = (field.Tag + " lookup-details-hidden")
                            End If
                        End If
                        If (IsNewRow(page, request) AndAlso (page.NewRow(index) Is Nothing)) Then
                            page.NewRow(index) = UserId
                        End If
                    Else
                        If (IsCreatedByUserNamePattern(field.Name) OrElse IsModifiedByUserNamePattern(field.Name)) Then
                            field.TextMode = TextInputMode.Static
                            If (IsNewRow(page, request) AndAlso (page.NewRow(index) Is Nothing)) Then
                                page.NewRow(index) = UserName
                            End If
                        Else
                            If (IsCreatedOnPattern(field.Name) OrElse IsModifiedOnPattern(field.Name)) Then
                                field.TextMode = TextInputMode.Static
                                field.DataFormatString = DateTimeFormatString
                                If request.Inserting Then
                                    field.Hidden = true
                                End If
                                If (IsNewRow(page, request) AndAlso (page.NewRow(index) Is Nothing)) Then
                                    page.NewRow(index) = DateTime.Now
                                End If
                            End If
                        End If
                    End If
                End If
                index = (index + 1)
            Next
        End Sub
        
        Public Overloads Shared Sub Process(ByVal args As ActionArgs, ByVal config As ControllerConfiguration)
            If ((args.CommandName = "Update") OrElse (args.CommandName = "Insert")) Then
                Dim tracker = New EventTracker()
                tracker.InternalProcess(args, config)
            End If
        End Sub
        
        Protected Overloads Overridable Sub InternalProcess(ByVal args As ActionArgs, ByVal config As ControllerConfiguration)
            Dim updating = (args.CommandName = "Update")
            Dim hasCreatedByUserId = updating
            Dim hasCreatedByUserName = updating
            Dim hasCreatedOn = updating
            Dim hasModifiedByUserId = false
            Dim hasModifiedByUserName = false
            Dim hasModifiedOn = false
            'assign tracking values to field values passed from the client
            For Each v in args.Values
                If Not (v.ReadOnly) Then
                    If (Not (hasCreatedByUserId) AndAlso IsCreatedByUserIdPattern(v.Name)) Then
                        hasCreatedByUserId = true
                        If (v.Value Is Nothing) Then
                            v.NewValue = UserId
                            v.Modified = true
                        End If
                    Else
                        If (Not (hasCreatedByUserName) AndAlso IsCreatedByUserNamePattern(v.Name)) Then
                            hasCreatedByUserName = true
                            If (v.Value Is Nothing) Then
                                v.NewValue = UserName
                                v.Modified = true
                            End If
                        Else
                            If (Not (hasCreatedOn) AndAlso IsCreatedOnPattern(v.Name)) Then
                                hasCreatedOn = true
                                If (v.Value Is Nothing) Then
                                    v.NewValue = DateTime.Now
                                    v.Modified = true
                                End If
                            Else
                                If (Not (hasModifiedByUserId) AndAlso IsModifiedByUserIdPattern(v.Name)) Then
                                    hasModifiedByUserId = true
                                    v.NewValue = UserId
                                    v.Modified = true
                                Else
                                    If (Not (hasModifiedByUserName) AndAlso IsModifiedByUserNamePattern(v.Name)) Then
                                        hasModifiedByUserName = true
                                        v.NewValue = UserName
                                        v.Modified = true
                                    Else
                                        If (Not (hasModifiedOn) AndAlso IsModifiedOnPattern(v.Name)) Then
                                            hasModifiedOn = true
                                            v.NewValue = DateTime.Now
                                            v.Modified = true
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            Next
            'assign missing tracking values
            Dim values = New List(Of FieldValue)(args.Values)
            Dim fieldIterator = config.Select("/c:dataController/c:fields/c:field[not(@readOnly='true')]")
            Do While fieldIterator.MoveNext()
                Dim fieldName = fieldIterator.Current.GetAttribute("name", String.Empty)
                'ensure that missing "created" values are provided
                If (args.CommandName = "Insert") Then
                    If (Not (hasCreatedByUserId) AndAlso IsCreatedByUserIdPattern(fieldName)) Then
                        hasCreatedByUserId = true
                        Dim v = New FieldValue(fieldName, UserId)
                        values.Add(v)
                    Else
                        If (Not (hasCreatedByUserName) AndAlso IsCreatedByUserNamePattern(fieldName)) Then
                            hasCreatedByUserName = true
                            Dim v = New FieldValue(fieldName, UserName)
                            values.Add(v)
                        Else
                            If (Not (hasCreatedOn) AndAlso IsCreatedOnPattern(fieldName)) Then
                                hasCreatedOn = true
                                Dim v = New FieldValue(fieldName, DateTime.Now)
                                values.Add(v)
                            End If
                        End If
                    End If
                End If
                'ensure that missing "modified" values are provided
                If (Not (hasModifiedByUserId) AndAlso IsModifiedByUserIdPattern(fieldName)) Then
                    hasModifiedByUserId = true
                    Dim v = New FieldValue(fieldName, UserId)
                    values.Add(v)
                Else
                    If (Not (hasModifiedByUserName) AndAlso IsModifiedByUserNamePattern(fieldName)) Then
                        hasModifiedByUserName = true
                        Dim v = New FieldValue(fieldName, UserName)
                        values.Add(v)
                    Else
                        If (Not (hasModifiedOn) AndAlso IsModifiedOnPattern(fieldName)) Then
                            hasModifiedOn = true
                            Dim v = New FieldValue(fieldName, DateTime.Now)
                            values.Add(v)
                        End If
                    End If
                End If
            Loop
            args.Values = values.ToArray()
        End Sub
        
        Public Shared Sub EnsureTrackingFields(ByVal page As ViewPage, ByVal config As ControllerConfiguration)
            Dim tracker = New EventTracker()
            tracker.InternalEnsureTrackingFields(page, config)
        End Sub
        
        Protected Overridable Sub InternalEnsureTrackingFields(ByVal page As ViewPage, ByVal config As ControllerConfiguration)
            Dim hasCreatedByUserId = false
            Dim hasCreatedByUserName = false
            Dim hasCreatedOn = false
            Dim hasModifiedByUserId = false
            Dim hasModifiedByUserName = false
            Dim hasModifiedOn = false
            'detect missing tracking fields
            For Each field in page.Fields
                If Not (field.ReadOnly) Then
                    If IsCreatedByUserIdPattern(field.Name) Then
                        hasCreatedByUserId = true
                    End If
                    If IsCreatedByUserNamePattern(field.Name) Then
                        hasCreatedByUserName = true
                    End If
                    If IsCreatedOnPattern(field.Name) Then
                        hasCreatedOn = true
                    End If
                    If IsModifiedByUserIdPattern(field.Name) Then
                        hasModifiedByUserId = true
                    End If
                    If IsModifiedByUserNamePattern(field.Name) Then
                        hasModifiedByUserName = true
                    End If
                    If IsModifiedOnPattern(field.Name) Then
                        hasModifiedOn = true
                    End If
                End If
            Next
            'Create DataField instances for missing tracking fields
            Dim fieldIterator = config.Select("/c:dataController/c:fields/c:field[not(@readOnly='true')]")
            Do While fieldIterator.MoveNext()
                Dim fieldName = fieldIterator.Current.GetAttribute("name", String.Empty)
                'ensure that missing "created" data fields are declared
                If (Not (hasCreatedByUserId) AndAlso IsCreatedByUserIdPattern(fieldName)) Then
                    page.Fields.Add(New DataField(fieldIterator.Current, config.Resolver))
                    hasCreatedByUserId = true
                End If
                If (Not (hasCreatedByUserName) AndAlso IsCreatedByUserNamePattern(fieldName)) Then
                    page.Fields.Add(New DataField(fieldIterator.Current, config.Resolver))
                    hasCreatedByUserName = true
                End If
                If (Not (hasCreatedOn) AndAlso IsCreatedOnPattern(fieldName)) Then
                    page.Fields.Add(New DataField(fieldIterator.Current, config.Resolver))
                    hasCreatedOn = true
                End If
                'ensure that missing "modified" data fields are declared
                If (Not (hasModifiedByUserId) AndAlso IsModifiedByUserIdPattern(fieldName)) Then
                    page.Fields.Add(New DataField(fieldIterator.Current, config.Resolver))
                    hasModifiedByUserId = true
                End If
                If (Not (hasModifiedByUserName) AndAlso IsModifiedByUserNamePattern(fieldName)) Then
                    page.Fields.Add(New DataField(fieldIterator.Current, config.Resolver))
                    hasModifiedByUserName = true
                End If
                If (Not (hasModifiedOn) AndAlso IsModifiedOnPattern(fieldName)) Then
                    page.Fields.Add(New DataField(fieldIterator.Current, config.Resolver))
                    hasModifiedOn = true
                End If
            Loop
        End Sub
        
        Protected Sub EnsureMembershipUserProperties()
            If (m_UserId Is Nothing) Then
                m_UserId = Guid.Empty
                If (HttpContext.Current.User.Identity.IsAuthenticated AndAlso Not (HttpContext.Current.User.Identity.GetType().Equals(GetType(System.Security.Principal.WindowsIdentity)))) Then
                    Dim user = Membership.GetUser()
                    m_UserId = Convert.ToString(user.ProviderUserKey)
                    m_Email = user.Email
                End If
            End If
        End Sub
    End Class
End Namespace
