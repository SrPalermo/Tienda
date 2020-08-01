Imports Norwindt.Data
Imports Norwindt.Services
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Net.Mail
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web
Imports System.Web.Security

Namespace Norwindt.Rules
    
    Public Class MyProfileBusinessRulesBase
        Inherits BusinessRules
        
        Public Shared CreateErrors As SortedDictionary(Of MembershipCreateStatus, String) = New SortedDictionary(Of MembershipCreateStatus, String)()
        
        Private m_OauthProviders As SiteContentFileList
        
        Shared Sub New()
            CreateErrors.Add(MembershipCreateStatus.DuplicateEmail, "Duplicate email address.")
            CreateErrors.Add(MembershipCreateStatus.DuplicateProviderUserKey, "Duplicate provider key.")
            CreateErrors.Add(MembershipCreateStatus.DuplicateUserName, "Duplicate user name.")
            CreateErrors.Add(MembershipCreateStatus.InvalidAnswer, "Invalid password recovery answer.")
            CreateErrors.Add(MembershipCreateStatus.InvalidEmail, "Invalid email address.")
            CreateErrors.Add(MembershipCreateStatus.InvalidPassword, "Invalid password.")
            CreateErrors.Add(MembershipCreateStatus.InvalidProviderUserKey, "Invalid provider user key.")
            CreateErrors.Add(MembershipCreateStatus.InvalidQuestion, "Invalid password recovery question.")
            CreateErrors.Add(MembershipCreateStatus.InvalidUserName, "Invalid user name.")
            CreateErrors.Add(MembershipCreateStatus.ProviderError, "Provider error.")
            CreateErrors.Add(MembershipCreateStatus.UserRejected, "User has been rejected.")
        End Sub
        
        Protected Overridable ReadOnly Property OAuthProviders() As SiteContentFileList
            Get
                If (m_OauthProviders Is Nothing) Then
                    If ApplicationServices.IsSiteContentEnabled Then
                        m_OauthProviders = ApplicationServices.Current.ReadSiteContent("sys/saas", "%")
                    Else
                        m_OauthProviders = New SiteContentFileList()
                    End If
                End If
                Return m_OauthProviders
            End Get
        End Property
        
        Protected Overridable Sub InsertUser(ByVal username As String, ByVal password As String, ByVal confirmPassword As String, ByVal email As String, ByVal passwordQuestion As String, ByVal passwordAnswer As String, ByVal isApproved As Boolean, ByVal comment As String, ByVal roles As String)
            PreventDefault()
            If Not ((password = confirmPassword)) Then
                Throw New Exception(Localize("PasswordAndConfirmationDoNotMatch", "Password and confirmation do not match."))
            End If
            'create a user
            Dim status As MembershipCreateStatus
            Membership.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, status)
            If Not ((status = MembershipCreateStatus.Success)) Then
                Throw New Exception(Localize(status.ToString(), CreateErrors(status)))
            End If
            'retrieve the primary key of the new user account
            Dim newUser = Membership.GetUser(username)
            'update a comment
            If Not (String.IsNullOrEmpty(comment)) Then
                newUser.Comment = comment
                Membership.UpdateUser(newUser)
            End If
            If Not (String.IsNullOrEmpty(roles)) Then
                For Each role in roles.Split(Global.Microsoft.VisualBasic.ChrW(44))
                    System.Web.Security.Roles.AddUserToRole(username, role)
                Next
            End If
        End Sub
        
        <ControllerAction("MyProfile", "signUpForm", "Insert", ActionPhase.Before)>  _
        Protected Overridable Sub SignUpUser(ByVal username As String, ByVal password As String, ByVal confirmPassword As String, ByVal email As String, ByVal passwordQuestion As String, ByVal passwordAnswer As String)
            InsertUser(username, password, confirmPassword, email, passwordQuestion, passwordAnswer, true, Localize("SelfRegisteredUser", "Self-registered user."), "Users")
        End Sub
        
        <RowBuilder("MyProfile", "passwordRequestForm", RowKind.New)>  _
        Protected Overridable Sub NewPasswordRequestRow()
            UpdateFieldValue("UserName", Context.Session("IdentityConfirmation"))
        End Sub
        
        <RowBuilder("MyProfile", "loginForm", RowKind.New)>  _
        Protected Overridable Sub NewLoginFormRow()
            Dim urlReferrer = Context.Request.UrlReferrer
            If (Not (urlReferrer) Is Nothing) Then
                Dim url = urlReferrer.ToString()
                If url.Contains("/_invoke/getidentity") Then
                    UpdateFieldValue("DisplayRememberMe", false)
                End If
            End If
            If (OAuthProviders.Count > 0) Then
                UpdateFieldValue("OAuthEnabled", true)
            End If
        End Sub
        
        <ControllerAction("MyProfile", "passwordRequestForm", "Custom", "RequestPassword", ActionPhase.Execute)>  _
        Protected Overridable Sub PasswordRequest(ByVal userName As String)
            PreventDefault()
            Dim user = Membership.GetUser(userName)
            If ((user Is Nothing) OrElse (Not (String.IsNullOrEmpty(user.Comment)) AndAlso Regex.IsMatch(user.Comment, "Source:\s+\w+"))) Then
                Result.ShowAlert(Localize("UserNameDoesNotExist", "User name does not exist."), "UserName")
            Else
                Context.Session("IdentityConfirmation") = userName
                If Not (ApplicationServices.IsTouchClient) Then
                    Result.HideModal()
                End If
                Result.ShowModal("MyProfile", "identityConfirmationForm", "Edit", "identityConfirmationForm")
            End If
        End Sub
        
        <RowBuilder("MyProfile", "identityConfirmationForm", RowKind.Existing)>  _
        Protected Overridable Sub PrepareIdentityConfirmationRow()
            Dim userName = CType(Context.Session("IdentityConfirmation"),String)
            UpdateFieldValue("UserName", userName)
            UpdateFieldValue("PasswordAnswer", Nothing)
            UpdateFieldValue("PasswordQuestion", Membership.GetUser(userName).PasswordQuestion)
        End Sub
        
        <ControllerAction("MyProfile", "identityConfirmationForm", "Custom", "ConfirmIdentity", ActionPhase.Execute)>  _
        Protected Overridable Sub IdentityConfirmation(ByVal userName As String, ByVal passwordAnswer As String)
            PreventDefault()
            Dim user = Membership.GetUser(userName)
            If (Not (user) Is Nothing) Then
                Dim newPassword = user.ResetPassword(passwordAnswer)
                'create an email and send it to the user
                Dim message = New MailMessage()
                message.To.Add(user.Email)
                message.Subject = String.Format(Localize("NewPasswordSubject", "New password for '{0}'."), userName)
                message.Body = newPassword
                Try 
                    Dim client = New SmtpClient()
                    client.Send(message)
                    'hide modal popup and display a confirmation
                    Result.ExecuteOnClient("$app.alert('{0}', function () {{ window.history.go(-2); }})", Localize("NewPasswordAlert", "A new password has been emailed to the address on file."))
                Catch [error] As Exception
                    Result.ShowAlert([error].Message)
                End Try
            End If
        End Sub
        
        <RowBuilder("MyProfile", "myAccountForm", RowKind.Existing)>  _
        Protected Overridable Sub PrepareCurrentUserRow()
            UpdateFieldValue("UserName", UserName)
            UpdateFieldValue("Email", UserEmail)
            UpdateFieldValue("PasswordQuestion", Membership.GetUser().PasswordQuestion)
        End Sub
        
        <ControllerAction("MyProfile", "identityConfirmationForm", "Custom", "BackToRequestPassword", ActionPhase.Execute)>  _
        Protected Overridable Sub BackToRequestPassword()
            PreventDefault()
            Result.HideModal()
            If Not (ApplicationServices.IsTouchClient) Then
                Result.ShowModal("MyProfile", "passwordRequestForm", "New", "passwordRequestForm")
            End If
        End Sub
        
        <ControllerAction("MyProfile", "myAccountForm", "Update", ActionPhase.Before)>  _
        Protected Overridable Sub UpdateMyAccount(ByVal userName As String, ByVal oldPassword As String, ByVal password As String, ByVal confirmPassword As String, ByVal email As String, ByVal passwordQuestion As String, ByVal passwordAnswer As String)
            PreventDefault()
            Dim user = Membership.GetUser(userName)
            If (Not (user) Is Nothing) Then
                If String.IsNullOrEmpty(oldPassword) Then
                    Result.ShowAlert(Localize("EnterCurrentPassword", "Please enter your current password."), "OldPassword")
                    Return
                End If
                If Not (Membership.ValidateUser(userName, oldPassword)) Then
                    Result.ShowAlert(Localize("PasswordDoesNotMatchRecords", "Your password does not match our records."), "OldPassword")
                    Return
                End If
                If (Not (String.IsNullOrEmpty(password)) OrElse Not (String.IsNullOrEmpty(confirmPassword))) Then
                    If Not ((password = confirmPassword)) Then
                        Result.ShowAlert(Localize("NewPasswordAndConfirmatinDoNotMatch", "New password and confirmation do not match."), "Password")
                        Return
                    End If
                    If Not (user.ChangePassword(oldPassword, password)) Then
                        Result.ShowAlert(Localize("NewPasswordInvalid", "Your new password is invalid."), "Password")
                        Return
                    End If
                End If
                If Not ((email = user.Email)) Then
                    user.Email = email
                    Membership.UpdateUser(user)
                End If
                If (Not ((user.PasswordQuestion = passwordQuestion)) AndAlso String.IsNullOrEmpty(passwordAnswer)) Then
                    Result.ShowAlert(Localize("EnterPasswordAnswer", "Please enter a password answer."), "PasswordAnswer")
                    Return
                End If
                If Not (String.IsNullOrEmpty(passwordAnswer)) Then
                    user.ChangePasswordQuestionAndAnswer(oldPassword, passwordQuestion, passwordAnswer)
                    Membership.UpdateUser(user)
                End If
                Result.HideModal()
            Else
                Result.ShowAlert(Localize("UserNotFound", "User not found."))
            End If
        End Sub
        
        <ControllerAction("MyProfile", "Select", ActionPhase.Before)>  _
        Public Overridable Sub AccessControlValidation()
            If Context.User.Identity.IsAuthenticated Then
                Return
            End If
            If Not ((((Request.View = "signUpForm") OrElse (Request.View = "passwordRequestForm")) OrElse ((Request.View = "identityConfirmationForm") OrElse (Request.View = "loginForm")))) Then
                Throw New Exception("Not authorized")
            End If
        End Sub
        
        Public Overrides Function SupportsVirtualization(ByVal controllerName As String) As Boolean
            Return true
        End Function
        
        Protected Overrides Sub VirtualizeController(ByVal controllerName As String)
            MyBase.VirtualizeController(controllerName)
            NodeSet().SelectViews().SetTag("odp-enabled-none")
            If (OAuthProviders.Count > 0) Then
                'customize login form when OAuth providers are detected in Site Content
                NodeSet().SelectCustomAction("SignUp").WhenClientScript("$row.OAuthProvider== 'other'").SelectCustomAction("ForgotPassword").WhenClientScript("$row.OAuthProvider== 'other'").SelectViews("loginForm").SelectDataFields("UserName", "Password", "RememberMe").VisibleWhen("$row.OAuthProvider=='other'").SelectDataField("UserName").SetTag("focus-auto").SelectDataFields("OAuthProvider").SetHidden(false).VisibleWhen("$row.OAuthEnabled")
                'customize OAuth provider items
                Dim items = NodeSet().SelectField("OAuthProvider").SelectItems().Nodes
                If (items.Count > 0) Then
                    Dim supportedProviders = New List(Of String)()
                    For Each file in OAuthProviders
                        supportedProviders.Add(file.Name)
                    Next
                    For Each item in items
                        Dim provider = item.GetAttribute("value", String.Empty)
                        If (provider = "other") Then
                            Dim otherItemText = item.SelectSingleNode("@text")
                            If ((Not (otherItemText) Is Nothing) AndAlso (otherItemText.Value = "Other")) Then
                                otherItemText.SetValue(ApplicationServicesBase.Current.DisplayName)
                            End If
                        Else
                            If Not (supportedProviders.Contains(provider)) Then
                                item.DeleteSelf()
                            End If
                        End If
                    Next
                End If
            End If
        End Sub
    End Class
End Namespace
