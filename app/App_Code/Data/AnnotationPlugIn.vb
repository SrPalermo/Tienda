Imports Norwindt.Handlers
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
Imports System.Web.Configuration
Imports System.Web.Security
Imports System.Xml
Imports System.Xml.XPath

Namespace Norwindt.Data
    
    Public Class AnnotationPlugIn
        Inherits Object
        Implements IPlugIn
        
        Private m_Config As ControllerConfiguration
        
        Private m_Annotations As List(Of FieldValue)
        
        Private m_RetrieveAnnotations As Boolean
        
        Private m_RequireProcessing As Boolean
        
        Private m_Fields As XPathNavigator
        
        Shared Sub New()
            BlobFactory.Handlers.Add("AnnotationPlugIn", New AnnotationBlobHandler())
        End Sub
        
        Public Shared ReadOnly Property AnnotationsPath() As String
            Get
                Dim p = WebConfigurationManager.AppSettings("AnnotationsPath")
                If String.IsNullOrEmpty(p) Then
                    p = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "App_Data")
                End If
                Return p
            End Get
        End Property
        
        Property IPlugIn_Config() As ControllerConfiguration Implements IPlugIn.Config
            Get
                Return m_Config
            End Get
            Set
                m_Config = value
            End Set
        End Property
        
        Protected ReadOnly Property Fields() As XPathNavigator
            Get
                If (m_Fields Is Nothing) Then
                    m_Fields = m_Config.SelectSingleNode("/c:dataController/c:fields")
                End If
                Return m_Fields
            End Get
        End Property
        
        ReadOnly Property KeyFields() As String
            Get
                Dim kf = String.Empty
                Dim iterator = Fields.Select("c:field[@isPrimaryKey='true']/@name", m_Config.Resolver)
                Do While iterator.MoveNext()
                    If (kf.Length > 0) Then
                        kf = (kf + ",")
                    End If
                    kf = (kf + iterator.Current.Value)
                Loop
                Return kf
            End Get
        End Property
        
        Public Shared ReadOnly Property UserEmail() As String
            Get
                If TypeOf HttpContext.Current.User.Identity Is System.Security.Principal.WindowsIdentity Then
                    Return String.Empty
                Else
                    Dim user = Membership.GetUser()
                    If (user Is Nothing) Then
                        Return String.Empty
                    End If
                    Return user.Email
                End If
            End Get
        End Property
        
        Public Overloads Shared Function GenerateDataRecordPath() As String
            Return GenerateDataRecordPath(Nothing, Nothing, Nothing, 0)
        End Function
        
        Public Overloads Shared Function GenerateDataRecordPath(ByVal controller As String, ByVal page As ViewPage, ByVal values() As FieldValue, ByVal rowIndex As Integer) As String
            'Sample path:
            '[Documents]\Code OnTime\Projects\Web Site Factory\Annotations\App_Data\OrderDetails\10248,11
            'Sample URL parameter:
            'u|OrderDetails,_Annotation_AttachmentNew|10248|11
            Dim p = AnnotationPlugIn.AnnotationsPath
            If String.IsNullOrEmpty(controller) Then
                Dim handlerInfo = HttpContext.Current.Request("AnnotationPlugIn")
                Dim m = Regex.Match(handlerInfo, "^((t|o|u)\|){0,1}\w+\|(\w+).+?\|(.+)?$")
                If m.Success Then
                    p = Path.Combine(p, m.Groups(3).Value)
                    p = Path.Combine(p, m.Groups(4).Value.Replace("|", ","))
                End If
            Else
                p = Path.Combine(p, controller)
                Dim keys = String.Empty
                For Each field in page.Fields
                    If field.IsPrimaryKey Then
                        Dim keyValue As String = Nothing
                        If (values Is Nothing) Then
                            keyValue = Convert.ToString(page.Rows(rowIndex)(page.Fields.IndexOf(field)))
                        Else
                            For Each v in values
                                If (v.Name = field.Name) Then
                                    keyValue = Convert.ToString(v.Value)
                                    Exit For
                                End If
                            Next
                        End If
                        If (keys.Length > 0) Then
                            keys = (keys + ",")
                        End If
                        keys = (keys + keyValue.Trim())
                    End If
                Next
                p = Path.Combine(p, keys)
            End If
            Return p
        End Function
        
        Function IPlugIn_Create(ByVal config As ControllerConfiguration) As ControllerConfiguration Implements IPlugIn.Create
            If config.Navigator.CanEdit Then
                Return config
            End If
            Dim document = New XmlDocument()
            document.LoadXml(config.Navigator.OuterXml)
            Return New ControllerConfiguration(document.CreateNavigator())
        End Function
        
        Sub IPlugIn_PreProcessPageRequest(ByVal request As PageRequest, ByVal page As ViewPage) Implements IPlugIn.PreProcessPageRequest
            Dim view = m_Config.SelectSingleNode("//c:view[@id='{0}' and @type='Form']/c:categories", request.View)
            If ((Not (view) Is Nothing) AndAlso ((request.PageSize > 0) AndAlso (Not (request.Inserting) AndAlso (m_Config.SelectSingleNode("/c:dataController/c:fields/c:field[@name='_Annotation_NoteNew']") Is Nothing)))) Then
                m_RequireProcessing = true
                Dim ns = ControllerConfiguration.Namespace
                Dim expressions = New List(Of DynamicExpression)(m_Config.Expressions)
                'create NewXXX fields under "fields" node
                Dim sb = New StringBuilder()
                Dim settings = New XmlWriterSettings()
                settings.ConformanceLevel = ConformanceLevel.Fragment
                Dim writer = XmlWriter.Create(sb, settings)
                'NoteNew field
                writer.WriteStartElement("field", ns)
                writer.WriteAttributeString("name", "_Annotation_NoteNew")
                writer.WriteAttributeString("type", "String")
                writer.WriteAttributeString("allowSorting", "false")
                writer.WriteAttributeString("allowQBE", "false")
                writer.WriteAttributeString("label", Localizer.Replace("AnnotationNoteNewFieldLabel", "Notes"))
                writer.WriteAttributeString("computed", "true")
                writer.WriteElementString("formula", ns, "null")
                writer.WriteEndElement()
                Dim de = New DynamicExpression()
                de.Target = "_Annotation_NoteNew"
                de.Scope = DynamicExpressionScope.DataFieldVisibility
                de.Type = DynamicExpressionType.ClientScript
                de.Test = "this.get_isEditing()"
                de.ViewId = request.View
                expressions.Add(de)
                'AttachmentNew field
                writer.WriteStartElement("field", ns)
                writer.WriteAttributeString("name", "_Annotation_AttachmentNew")
                writer.WriteAttributeString("type", "Byte[]")
                writer.WriteAttributeString("onDemand", "true")
                writer.WriteAttributeString("sourceFields", Me.KeyFields)
                writer.WriteAttributeString("onDemandHandler", "AnnotationPlugIn")
                writer.WriteAttributeString("allowQBE", "false")
                writer.WriteAttributeString("allowSorting", "false")
                writer.WriteAttributeString("label", Localizer.Replace("AnnotationAttachmentNewFieldLabel", "Attachment"))
                writer.WriteAttributeString("computed", "true")
                writer.WriteElementString("formula", ns, "null")
                writer.WriteEndElement()
                writer.Close()
                Me.Fields.AppendChild(sb.ToString())
                Dim ade = New DynamicExpression()
                ade.Target = "_Annotation_AttachmentNew"
                ade.Scope = DynamicExpressionScope.DataFieldVisibility
                ade.Type = DynamicExpressionType.ClientScript
                ade.Test = "this.get_isEditing()"
                ade.ViewId = request.View
                expressions.Add(ade)
                'create NewXXX data fields under "view/dataFields" node
                sb = New StringBuilder()
                writer = XmlWriter.Create(sb)
                writer.WriteStartElement("category", ns)
                writer.WriteAttributeString("id", "Annotations")
                writer.WriteAttributeString("headerText", Localizer.Replace("AnnotationCategoryHeaderText", "Notes and Attachments"))
                writer.WriteElementString("description", ns, Localizer.Replace("AnnotationCategoryDescription", "Enter optional notes and attach files."))
                writer.WriteStartElement("dataFields", ns)
                '_Annotation_NoteNew dataField
                writer.WriteStartElement("dataField", ns)
                writer.WriteAttributeString("fieldName", "_Annotation_NoteNew")
                writer.WriteAttributeString("columns", "50")
                writer.WriteAttributeString("rows", "7")
                writer.WriteEndElement()
                '_Annotation_AttachmentNew
                writer.WriteStartElement("dataField", ns)
                writer.WriteAttributeString("fieldName", "_Annotation_AttachmentNew")
                writer.WriteEndElement()
                writer.WriteEndElement()
                writer.WriteEndElement()
                writer.Close()
                view.AppendChild(sb.ToString())
                m_RetrieveAnnotations = Not (request.Inserting)
                m_Config.Expressions = expressions.ToArray()
            End If
        End Sub
        
        Sub IPlugIn_ProcessPageRequest(ByVal request As PageRequest, ByVal page As ViewPage) Implements IPlugIn.ProcessPageRequest
            If (page.Rows.Count = 0) Then
                page.Icons = New String(-1) {}
                Return
            End If
            If Not (m_RequireProcessing) Then
                Dim icons = New List(Of String)()
                Dim i = 0
                Do While (i < page.Rows.Count)
                    Dim rowDir = AnnotationPlugIn.GenerateDataRecordPath(request.Controller, page, Nothing, i)
                    If Directory.Exists(rowDir) Then
                        icons.Add("Attachment")
                    Else
                        icons.Add(Nothing)
                    End If
                    i = (i + 1)
                Loop
                page.Icons = icons.ToArray()
                Return
            End If
            Dim expressions = New List(Of DynamicExpression)(page.Expressions)
            Dim de = New DynamicExpression()
            de.Target = "Annotations"
            de.Scope = DynamicExpressionScope.CategoryVisibility
            de.Type = DynamicExpressionType.ClientScript
            de.Test = "!this.get_isInserting()"
            de.ViewId = page.View
            expressions.Add(de)
            page.Expressions = expressions.ToArray()
            If Not (m_RetrieveAnnotations) Then
                Return
            End If
            Dim field = page.FindField("_Annotation_AttachmentNew")
            If (Not (field) Is Nothing) Then
                Dim fieldIndex = page.Fields.IndexOf(field)
                Dim newValue = String.Format("{0},{1}|{2}", request.Controller, field.Name, Regex.Replace(CType(page.Rows(0)(fieldIndex),String), "^\w+\|(.+)$", "$1"))
                If (field.Name = "_Annotation_AttachmentNew") Then
                    newValue = ("null|" + newValue)
                End If
                page.Rows(0)(fieldIndex) = newValue
            End If
            Dim p = AnnotationPlugIn.GenerateDataRecordPath(request.Controller, page, Nothing, 0)
            If Directory.Exists(p) Then
                Dim files = Directory.GetFiles(p, "*.xml")
                Dim values = New List(Of Object)(page.Rows(0))
                Dim i = (files.Length - 1)
                Do While (i >= 0)
                    Dim filename = files(i)
                    Dim doc = New XPathDocument(filename)
                    Dim nav = doc.CreateNavigator().SelectSingleNode("/*")
                    Dim f As DataField = Nothing
                    If (nav.Name = "note") Then
                        f = New DataField()
                        f.Name = "_Annotation_Note"
                        f.Type = "String"
                        f.HeaderText = String.Format(Localizer.Replace("AnnotationNoteDynamicFieldHeaderText", "{0} written at {1}"), ReadNameAndEmail(nav), Convert.ToDateTime(nav.GetAttribute("timestamp", String.Empty)))
                        f.Columns = 50
                        f.Rows = 7
                        f.TextMode = TextInputMode.Note
                        values.Add(nav.Value)
                    Else
                        If (nav.Name = "attachment") Then
                            f = New DataField()
                            f.Name = "_Annotation_Attachment"
                            f.Type = "Byte[]"
                            f.HeaderText = String.Format(Localizer.Replace("AnnotationAttachmentDynamicFieldHeaderText", "{0} attached <b>{1}</b> at {2}"), ReadNameAndEmail(nav), nav.GetAttribute("fileName", String.Empty), Convert.ToDateTime(nav.GetAttribute("timestamp", String.Empty)))
                            f.OnDemand = true
                            f.OnDemandHandler = "AnnotationPlugIn"
                            f.OnDemandStyle = OnDemandDisplayStyle.Link
                            If nav.GetAttribute("contentType", String.Empty).StartsWith("image/") Then
                                f.OnDemandStyle = OnDemandDisplayStyle.Thumbnail
                            End If
                            f.CategoryIndex = (page.Categories.Count - 1)
                            values.Add(nav.GetAttribute("value", String.Empty))
                        End If
                    End If
                    If (Not (f) Is Nothing) Then
                        f.Name = (f.Name + Path.GetFileNameWithoutExtension(filename))
                        f.AllowNulls = true
                        f.CategoryIndex = (page.Categories.Count - 1)
                        If Not (Controller.UserIsInRole("Administrators")) Then
                            f.ReadOnly = true
                        End If
                        page.Fields.Add(f)
                    End If
                    i = (i - 1)
                Loop
                page.Rows(0) = values.ToArray()
                If (files.Length > 0) Then
                    page.Categories((page.Categories.Count - 1)).Tab = Localizer.Replace("AnnotationTab", "Notes & Attachments")
                    expressions.RemoveAt((expressions.Count - 1))
                    page.Expressions = expressions.ToArray()
                End If
            Else
                de.Test = "this.get_isEditing() && this.get_view()._displayAnnotations"
                Dim g = New ActionGroup()
                page.ActionGroups.Add(g)
                g.Scope = "ActionBar"
                g.Flat = true
                Dim a = New Action()
                g.Actions.Add(a)
                a.WhenLastCommandName = "Edit"
                a.WhenView = page.View
                a.CommandName = "ClientScript"
                a.CommandArgument = "this.get_view()._displayAnnotations=true;this._focusedFieldName = '_Annotation_No"& _ 
                    "teNew';this._raiseSelectedDelayed=false;"
                a.HeaderText = Localizer.Replace("AnnotationActionHeaderText", "Annotate")
                a.CssClass = "AttachIcon"
                a.WhenClientScript = "this.get_view()._displayAnnotations!=true;"
            End If
        End Sub
        
        Private Function ReadNameAndEmail(ByVal nav As XPathNavigator) As String
            Dim userName = nav.GetAttribute("username", String.Empty)
            Dim email = nav.GetAttribute("email", String.Empty)
            If String.IsNullOrEmpty(email) Then
                Return userName
            End If
            Return String.Format("<a href=""mailto:{0}"" title=""{0}"" target=""_blank"">{1}</a>", email, userName)
        End Function
        
        Sub IPlugIn_PreProcessArguments(ByVal args As ActionArgs, ByVal result As ActionResult, ByVal page As ViewPage) Implements IPlugIn.PreProcessArguments
            m_Annotations = New List(Of FieldValue)()
            If (Not (args.Values) Is Nothing) Then
                For Each v in args.Values
                    If (v.Name.StartsWith("_Annotation_") AndAlso v.Modified) Then
                        m_Annotations.Add(v)
                        v.Modified = false
                    End If
                Next
            End If
        End Sub
        
        Sub IPlugIn_ProcessArguments(ByVal args As ActionArgs, ByVal result As ActionResult, ByVal page As ViewPage) Implements IPlugIn.ProcessArguments
            If (m_Annotations.Count = 0) Then
                Return
            End If
            Dim p = AnnotationPlugIn.GenerateDataRecordPath(args.Controller, page, args.Values, 0)
            If Not (Directory.Exists(p)) Then
                Directory.CreateDirectory(p)
            End If
            For Each v in m_Annotations
                Dim m = Regex.Match(v.Name, "^_Annotation_(Note)(New|\w+)$")
                If m.Success Then
                    If (m.Groups(1).Value = "Note") Then
                        Dim fileName = m.Groups(2).Value
                        If (fileName = "New") Then
                            fileName = DateTime.Now.ToString("u")
                            fileName = Regex.Replace(fileName, "[\W]", String.Empty)
                        End If
                        fileName = Path.Combine(p, (fileName + ".xml"))
                        If Not (String.IsNullOrEmpty(Convert.ToString(v.NewValue))) Then
                            Dim settings = New XmlWriterSettings()
                            settings.CloseOutput = true
                            Dim writer = XmlWriter.Create(New FileStream(fileName, FileMode.Create), settings)
                            Try 
                                writer.WriteStartElement("note")
                                writer.WriteAttributeString("timestamp", DateTime.Now.ToString("o"))
                                writer.WriteAttributeString("username", HttpContext.Current.User.Identity.Name)
                                writer.WriteAttributeString("email", AnnotationPlugIn.UserEmail)
                                writer.WriteString(Convert.ToString(v.NewValue))
                                writer.WriteEndElement()
                            Finally
                                writer.Close()
                            End Try
                        Else
                            File.Delete(fileName)
                            If (Directory.GetFiles(p).Length = 0) Then
                                Directory.Delete(p)
                            End If
                        End If
                    End If
                End If
            Next
        End Sub
    End Class
    
    Public Class AnnotationBlobHandler
        Inherits BlobHandlerInfo
        
        Public Sub New()
            MyBase.New
            Me.Key = "AnnotationPlugIn"
        End Sub
        
        Public Overrides Property Text() As String
            Get
                Return "Attachment"
            End Get
            Set
                MyBase.Text = value
            End Set
        End Property
        
        Public Overrides Function SaveFile(ByVal context As HttpContext, ByVal ba As BlobAdapter, ByVal keyValue As String) As Boolean
            If Not ((context.Request.Files.Count = 1)) Then
                Return false
            End If
            Dim file = context.Request.Files(0)
            Dim p = AnnotationPlugIn.GenerateDataRecordPath()
            If Not (Directory.Exists(p)) Then
                Directory.CreateDirectory(p)
            End If
            'u|OrderDetails,_Annotation_AttachmentNew|10248|11
            Dim m = Regex.Match(Me.Value, "_Annotation_Attachment(\w+)\|")
            If m.Success Then
                Dim fileName = m.Groups(1).Value
                If (fileName = "New") Then
                    fileName = DateTime.Now.ToString("u")
                    fileName = Regex.Replace(fileName, "[\W]", String.Empty)
                    If System.IO.File.Exists(Path.Combine(p, (fileName + ".xml"))) Then
                        fileName = (fileName + "_")
                    End If
                End If
                fileName = Path.Combine(p, (fileName + ".xml"))
                If (file.ContentLength = 0) Then
                    For Each f in Directory.GetFiles(p, (Path.GetFileNameWithoutExtension(fileName) + "*.*"))
                        System.IO.File.Delete(f)
                    Next
                Else
                    Dim settings = New XmlWriterSettings()
                    settings.CloseOutput = true
                    Dim writer = XmlWriter.Create(New FileStream(fileName, FileMode.Create), settings)
                    Try 
                        writer.WriteStartElement("attachment")
                        writer.WriteAttributeString("timestamp", DateTime.Now.ToString("o"))
                        writer.WriteAttributeString("username", HttpContext.Current.User.Identity.Name)
                        writer.WriteAttributeString("email", AnnotationPlugIn.UserEmail)
                        writer.WriteAttributeString("fileName", Path.GetFileName(file.FileName))
                        writer.WriteAttributeString("contentType", file.ContentType)
                        writer.WriteAttributeString("contentLength", file.ContentLength.ToString())
                        writer.WriteAttributeString("value", Regex.Replace(Me.Value, "^.+?\|([\w,]+?)_Annotation_Attachment(New|\w+)(.+)$", String.Format("1|$1_Annotation_Attachment{0}$3", Path.GetFileNameWithoutExtension(fileName))))
                        writer.WriteEndElement()
                        fileName = ((Path.GetFileNameWithoutExtension(fileName) + "_")  _
                                    + Path.GetExtension(file.FileName))
                        file.SaveAs(Path.Combine(p, fileName))
                    Finally
                        writer.Close()
                    End Try
                End If
            End If
            Return true
        End Function
        
        Public Overrides Sub LoadFile(ByVal stream As Stream)
            Dim p = AnnotationPlugIn.GenerateDataRecordPath()
            't|1|OrderDetails,_Annotation_Attachment20091219164153Z|10248|11
            Dim m = Regex.Match(Me.Value, "_Annotation_Attachment(\w+)\|")
            Dim fileName = Path.Combine(p, (m.Groups(1).Value + ".xml"))
            Dim nav = New XPathDocument(fileName).CreateNavigator().SelectSingleNode("/*")
            fileName = Path.Combine(p, ((Path.GetFileNameWithoutExtension(fileName) + "_")  _
                            + Path.GetExtension(nav.GetAttribute("fileName", String.Empty))))
            If Not (Me.Value.StartsWith("t|")) Then
                Me.ContentType = nav.GetAttribute("contentLength", String.Empty)
                HttpContext.Current.Response.ContentType = Me.ContentType
            End If
            Me.FileName = nav.GetAttribute("fileName", String.Empty)
            Dim input = File.OpenRead(fileName)
            Try 
                Dim buffer(((1024 * 64)) - 1) As Byte
                Dim offset = 0
                Dim bytesRead = input.Read(buffer, 0, buffer.Length)
                Do While (bytesRead > 0)
                    stream.Write(buffer, 0, Convert.ToInt32(bytesRead))
                    offset = (offset + bytesRead)
                    bytesRead = input.Read(buffer, 0, buffer.Length)
                Loop
            Finally
                input.Close()
            End Try
        End Sub
    End Class
End Namespace
