Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Configuration
Imports System.Data
Imports System.Data.Common
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Transactions
Imports System.Web
Imports System.Web.Caching
Imports System.Web.Configuration
Imports System.Web.Security
Imports System.Xml
Imports System.Xml.XPath
Imports System.Xml.Xsl

Namespace Norwindt.Data
    
    Partial Public Class DataControllerBase
        
        Public Const MaximumRssItems As Integer = 200
        
        Private Shared m_RowsetTypeMap As SortedDictionary(Of String, String)
        
        Public Shared ReadOnly Property RowsetTypeMap() As SortedDictionary(Of String, String)
            Get
                Return m_RowsetTypeMap
            End Get
        End Property
        
        Private Sub ExecuteDataExport(ByVal args As ActionArgs, ByVal result As ActionResult)
            If Not (String.IsNullOrEmpty(args.CommandArgument)) Then
                Dim arguments = args.CommandArgument.Split(Global.Microsoft.VisualBasic.ChrW(44))
                If (arguments.Length > 0) Then
                    Dim sameController = (args.Controller = arguments(0))
                    args.Controller = arguments(0)
                    If (arguments.Length = 1) Then
                        args.View = "grid1"
                    Else
                        args.View = arguments(1)
                    End If
                    If sameController Then
                        args.SortExpression = Nothing
                    End If
                    SelectView(args.Controller, args.View)
                End If
            End If
            Dim request = New PageRequest(-1, -1, Nothing, Nothing)
            request.SortExpression = args.SortExpression
            request.Filter = args.Filter
            request.ContextKey = Nothing
            request.PageIndex = 0
            request.PageSize = Int32.MaxValue
            request.View = args.View
            If args.CommandName.EndsWith("Template") Then
                request.PageSize = 0
                args.CommandName = "ExportCsv"
            End If
            'store export data to a temporary file
            Dim fileName = Path.GetTempFileName()
            Dim writer = File.CreateText(fileName)
            Try 
                Dim page = New ViewPage(request)
                page.ApplyDataFilter(m_Config.CreateDataFilter(), args.Controller, args.View, Nothing, Nothing, Nothing)
                If (m_ServerRules Is Nothing) Then
                    m_ServerRules = m_Config.CreateBusinessRules()
                    If (m_ServerRules Is Nothing) Then
                        m_ServerRules = CreateBusinessRules()
                    End If
                End If
                m_ServerRules.Page = page
                m_ServerRules.ExecuteServerRules(request, ActionPhase.Before)
                Using connection = CreateConnection(Me)
                    Dim selectCommand = CreateCommand(connection)
                    If ((selectCommand Is Nothing) AndAlso m_ServerRules.EnableResultSet) Then
                        PopulatePageFields(page)
                        EnsurePageFields(page, Nothing)
                    End If
                    ConfigureCommand(selectCommand, page, CommandConfigurationType.Select, Nothing)
                    Dim reader = ExecuteResultSetReader(page)
                    If (reader Is Nothing) Then
                        reader = selectCommand.ExecuteReader()
                    End If
                    If args.CommandName.EndsWith("Csv") Then
                        ExportDataAsCsv(page, reader, writer)
                    End If
                    If args.CommandName.EndsWith("Rss") Then
                        ExportDataAsRss(page, reader, writer)
                    End If
                    If args.CommandName.EndsWith("Rowset") Then
                        ExportDataAsRowset(page, reader, writer)
                    End If
                    reader.Close()
                End Using
                m_ServerRules.ExecuteServerRules(request, ActionPhase.After)
            Finally
                writer.Close()
            End Try
            result.Values.Add(New FieldValue("FileName", Nothing, fileName))
        End Sub
        
        Private Sub ExportDataAsRowset(ByVal page As ViewPage, ByVal reader As DbDataReader, ByVal writer As StreamWriter)
            Dim s = "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882"
            Dim dt = "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882"
            Dim rs = "urn:schemas-microsoft-com:rowset"
            Dim z = "#RowsetSchema"
            Dim settings = New XmlWriterSettings()
            settings.CloseOutput = false
            Dim output = XmlWriter.Create(writer, settings)
            output.WriteStartDocument()
            output.WriteStartElement("xml")
            output.WriteAttributeString("xmlns", "s", Nothing, s)
            output.WriteAttributeString("xmlns", "dt", Nothing, dt)
            output.WriteAttributeString("xmlns", "rs", Nothing, rs)
            output.WriteAttributeString("xmlns", "z", Nothing, z)
            'declare rowset schema
            output.WriteStartElement("Schema", s)
            output.WriteAttributeString("id", "RowsetSchema")
            output.WriteStartElement("ElementType", s)
            output.WriteAttributeString("name", "row")
            output.WriteAttributeString("content", "eltOnly")
            output.WriteAttributeString("CommandTimeout", rs, "60")
            Dim fields = New List(Of DataField)()
            For Each field in page.Fields
                If (Not ((field.Hidden OrElse (field.OnDemand OrElse (field.Type = "DataView")))) AndAlso Not (fields.Contains(field))) Then
                    Dim aliasField = field
                    If Not (String.IsNullOrEmpty(field.AliasName)) Then
                        aliasField = page.FindField(field.AliasName)
                    End If
                    fields.Add(aliasField)
                End If
            Next
            Dim number = 1
            For Each field in fields
                field.NormalizeDataFormatString()
                output.WriteStartElement("AttributeType", s)
                output.WriteAttributeString("name", field.Name)
                output.WriteAttributeString("number", rs, number.ToString())
                output.WriteAttributeString("nullable", rs, "true")
                output.WriteAttributeString("name", rs, field.Label)
                output.WriteStartElement("datatype", s)
                Dim type = RowsetTypeMap(field.Type)
                Dim dbType As String = Nothing
                If "{0:c}".Equals(field.DataFormatString, StringComparison.CurrentCultureIgnoreCase) Then
                    dbType = "currency"
                Else
                    If (Not (String.IsNullOrEmpty(field.DataFormatString)) AndAlso Not ((field.Type = "DateTime"))) Then
                        type = "string"
                    End If
                End If
                output.WriteAttributeString("type", dt, type)
                output.WriteAttributeString("dbtype", rs, dbType)
                output.WriteEndElement()
                output.WriteEndElement()
                number = (number + 1)
            Next
            output.WriteStartElement("extends", s)
            output.WriteAttributeString("type", "rs:rowbase")
            output.WriteEndElement()
            output.WriteEndElement()
            output.WriteEndElement()
            'output rowset data
            output.WriteStartElement("data", rs)
            Do While reader.Read()
                output.WriteStartElement("row", z)
                For Each field in fields
                    Dim v = reader(field.Name)
                    If Not (DBNull.Value.Equals(v)) Then
                        If (Not (String.IsNullOrEmpty(field.DataFormatString)) AndAlso Not (((field.DataFormatString = "{0:d}") OrElse (field.DataFormatString = "{0:c}")))) Then
                            output.WriteAttributeString(field.Name, String.Format(field.DataFormatString, v))
                        Else
                            If (field.Type = "DateTime") Then
                                output.WriteAttributeString(field.Name, CType(v,DateTime).ToString("s"))
                            Else
                                output.WriteAttributeString(field.Name, v.ToString())
                            End If
                        End If
                    End If
                Next
                output.WriteEndElement()
            Loop
            output.WriteEndElement()
            output.WriteEndElement()
            output.WriteEndDocument()
            output.Close()
        End Sub
        
        Private Sub ExportDataAsRss(ByVal page As ViewPage, ByVal reader As DbDataReader, ByVal writer As StreamWriter)
            Dim appPath = Regex.Replace(HttpContext.Current.Request.Url.AbsoluteUri, "^(.+)Export.ashx.+$", "$1", RegexOptions.IgnoreCase)
            Dim settings = New XmlWriterSettings()
            settings.CloseOutput = false
            Dim output = XmlWriter.Create(writer, settings)
            output.WriteStartDocument()
            output.WriteStartElement("rss")
            output.WriteAttributeString("version", "2.0")
            output.WriteStartElement("channel")
            output.WriteElementString("title", CType(m_View.Evaluate("string(concat(/c:dataController/@label, ' | ',  @label))", Resolver),String))
            output.WriteElementString("lastBuildDate", DateTime.Now.ToString("r"))
            output.WriteElementString("language", System.Threading.Thread.CurrentThread.CurrentCulture.Name.ToLower())
            Dim rowCount = 0
            Do While ((rowCount < MaximumRssItems) AndAlso reader.Read())
                output.WriteStartElement("item")
                Dim hasTitle = false
                Dim hasPubDate = false
                Dim desc = New StringBuilder()
                Dim i = 0
                Do While (i < page.Fields.Count)
                    Dim field = page.Fields(i)
                    If (Not (field.Hidden) AndAlso Not ((field.Type = "DataView"))) Then
                        If (rowCount = 0) Then
                            field.NormalizeDataFormatString()
                        End If
                        If Not (String.IsNullOrEmpty(field.AliasName)) Then
                            field = page.FindField(field.AliasName)
                        End If
                        Dim text = String.Empty
                        Dim v = reader(field.Name)
                        If Not (DBNull.Value.Equals(v)) Then
                            If Not (String.IsNullOrEmpty(field.DataFormatString)) Then
                                text = String.Format(field.DataFormatString, v)
                            Else
                                text = Convert.ToString(v)
                            End If
                        End If
                        If (Not (hasPubDate) AndAlso (field.Type = "DateTime")) Then
                            hasPubDate = true
                            If Not (String.IsNullOrEmpty(text)) Then
                                output.WriteElementString("pubDate", CType(reader(field.Name),DateTime).ToString("r"))
                            End If
                        End If
                        If Not (hasTitle) Then
                            hasTitle = true
                            output.WriteElementString("title", text)
                            Dim link = New StringBuilder()
                            link.Append(m_Config.Evaluate("string(/c:dataController/@name)"))
                            For Each pkf in page.Fields
                                If pkf.IsPrimaryKey Then
                                    link.Append(String.Format("&{0}={1}", pkf.Name, reader(pkf.Name)))
                                End If
                            Next
                            Dim itemGuid = String.Format("{0}Details.aspx?l={1}", appPath, HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.Default.GetBytes(link.ToString()))))
                            output.WriteElementString("link", itemGuid)
                            output.WriteElementString("guid", itemGuid)
                        Else
                            If (Not (String.IsNullOrEmpty(field.OnDemandHandler)) AndAlso (field.OnDemandStyle = OnDemandDisplayStyle.Thumbnail)) Then
                                If text.Equals("1") Then
                                    desc.AppendFormat("{0}:<br /><img src=""{1}Blob.ashx?{2}=t", HttpUtility.HtmlEncode(field.Label), appPath, field.OnDemandHandler)
                                    For Each f in page.Fields
                                        If f.IsPrimaryKey Then
                                            desc.Append("|")
                                            desc.Append(reader(f.Name))
                                        End If
                                    Next
                                    desc.Append(""" style=""width:92px;height:71px;""/><br />")
                                End If
                            Else
                                desc.AppendFormat("{0}: {1}<br />", HttpUtility.HtmlEncode(field.Label), HttpUtility.HtmlEncode(text))
                            End If
                        End If
                    End If
                    i = (i + 1)
                Loop
                output.WriteStartElement("description")
                output.WriteCData(String.Format("<span style=\""font-size:small;\"">{0}</span>", desc.ToString()))
                output.WriteEndElement()
                output.WriteEndElement()
                rowCount = (rowCount + 1)
            Loop
            output.WriteEndElement()
            output.WriteEndElement()
            output.WriteEndDocument()
            output.Close()
        End Sub
        
        Private Sub ExportDataAsCsv(ByVal page As ViewPage, ByVal reader As DbDataReader, ByVal writer As StreamWriter)
            Dim firstField = true
            Dim i = 0
            Do While (i < page.Fields.Count)
                Dim field = page.Fields(i)
                If (Not (field.Hidden) AndAlso (field.Type <> "DataView")) Then
                    If firstField Then
                        firstField = false
                    Else
                        writer.Write(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator)
                    End If
                    If Not (String.IsNullOrEmpty(field.AliasName)) Then
                        field = page.FindField(field.AliasName)
                    End If
                    writer.Write("""{0}""", field.Label.Replace("""", """"""))
                End If
                field.NormalizeDataFormatString()
                i = (i + 1)
            Loop
            writer.WriteLine()
            Do While reader.Read()
                firstField = true
                Dim j = 0
                Do While (j < page.Fields.Count)
                    Dim field = page.Fields(j)
                    If (Not (field.Hidden) AndAlso (field.Type <> "DataView")) Then
                        If firstField Then
                            firstField = false
                        Else
                            writer.Write(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator)
                        End If
                        If Not (String.IsNullOrEmpty(field.AliasName)) Then
                            field = page.FindField(field.AliasName)
                        End If
                        Dim text = String.Empty
                        Dim v = reader(field.Name)
                        If Not (DBNull.Value.Equals(v)) Then
                            If Not (String.IsNullOrEmpty(field.DataFormatString)) Then
                                text = String.Format(field.DataFormatString, v)
                            Else
                                text = Convert.ToString(v)
                            End If
                            writer.Write("""{0}""", text.Replace("""", """"""))
                        Else
                            writer.Write("""""")
                        End If
                    End If
                    j = (j + 1)
                Loop
                writer.WriteLine()
            Loop
        End Sub
    End Class
End Namespace
