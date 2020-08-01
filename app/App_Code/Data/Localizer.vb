Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Web
Imports System.Web.Caching

Namespace Norwindt.Data
    
    Public Class LocalizationDictionary
        Inherits SortedDictionary(Of String, String)
    End Class
    
    Public Class Localizer
        
        Public Shared TokenRegex As Regex = New Regex("\^(\w+)\^([\s\S]+?)\^(\w+)\^", RegexOptions.Compiled)
        
        Public Shared ScriptRegex As Regex = New Regex("<script.+?>([\s\S]*?)</script>", (RegexOptions.Compiled Or RegexOptions.IgnoreCase))
        
        Public Shared StateRegex As Regex = New Regex("(<input.+?name=.__VIEWSTATE.+?/>)", (RegexOptions.Compiled Or RegexOptions.IgnoreCase))
        
        Public Const StateRegexReplace As String = "$1" & ControlChars.CrLf &"<input type=""text"" name=""__COTSTATE"" id=""__COTSTATE"" style=""display:none"" />"
        
        Public Const StateRegexReplaceIE As String = "$1" & ControlChars.CrLf &"<input type=""hidden"" name=""__COTSTATE"" id=""__COTSTATE"" />"
        
        Private m_BaseName As String
        
        Private m_ObjectName As String
        
        Private m_Text As String
        
        Private m_Dictionary As LocalizationDictionary
        
        Private m_SharedDictionary As LocalizationDictionary
        
        Private m_ScriptMode As Boolean
        
        Public Sub New(ByVal baseName As String, ByVal objectName As String, ByVal text As String)
            MyBase.New
            m_BaseName = baseName
            If Not (String.IsNullOrEmpty(baseName)) Then
                m_BaseName = (m_BaseName + ".")
            End If
            m_ObjectName = objectName
            m_Text = text
        End Sub
        
        Public Overloads Shared Function Replace(ByVal token As String, ByVal text As String) As String
            Return Replace(String.Empty, "Resources", token, text)
        End Function
        
        Public Overloads Shared Function Replace(ByVal baseName As String, ByVal objectName As String, ByVal token As String, ByVal text As String) As String
            Return Replace(baseName, objectName, String.Format("^{0}^{1}^{0}^", token, text))
        End Function
        
        Public Overloads Shared Function Replace(ByVal baseName As String, ByVal objectName As String, ByVal text As String) As String
            If Not (TokenRegex.IsMatch(text)) Then
                Return text
            End If
            Dim l = New Localizer(baseName, objectName, text)
            Return l.Replace()
        End Function
        
        Public Overloads Overridable Function Replace() As String
            m_SharedDictionary = CreateDictionary(String.Empty, "CombinedSharedResources")
            m_Dictionary = CreateDictionary(m_BaseName, m_ObjectName)
            If (m_BaseName = "Pages.") Then
                m_ScriptMode = true
                Dim stateInput = StateRegexReplace
                If (HttpContext.Current.Request.Browser.Browser = "IE") Then
                    stateInput = StateRegexReplaceIE
                End If
                Dim output = StateRegex.Replace(ScriptRegex.Replace(m_Text.Trim(), AddressOf DoReplaceScript), stateInput)
                m_ScriptMode = false
                Return TokenRegex.Replace(output, AddressOf DoReplaceToken)
            Else
                Return TokenRegex.Replace(m_Text, AddressOf DoReplaceToken)
            End If
        End Function
        
        Private Function DoReplaceScript(ByVal m As Match) As String
            Return TokenRegex.Replace(m.Value, AddressOf DoReplaceToken)
        End Function
        
        Private Function DoReplaceToken(ByVal m As Match) As String
            Dim token = m.Groups(1).Value
            If (token = m.Groups(3).Value) Then
                Dim result As String = Nothing
                If Not (m_Dictionary.TryGetValue(token, result)) Then
                    If Not (m_SharedDictionary.TryGetValue(token, result)) Then
                        result = m.Groups(2).Value
                    End If
                End If
                If m_ScriptMode Then
                    result = BusinessRules.JavaScriptString(result)
                End If
                Return result
            Else
                Return m.Value
            End If
        End Function
        
        Public Shared Function CreateDictionaryStream(ByVal culture As String, ByVal baseName As String, ByVal objectName As String, ByRef files() As String) As Stream
            files = Nothing
            Dim fileName = String.Format("{0}.txt", objectName)
            Dim t = GetType(Controller)
            Dim result = t.Assembly.GetManifestResourceStream(CultureManager.ResolveEmbeddedResourceName(String.Format("Norwindt.{0}{1}", baseName, fileName), culture))
            If (result Is Nothing) Then
                result = t.Assembly.GetManifestResourceStream(CultureManager.ResolveEmbeddedResourceName(String.Format("Norwindt.{0}", fileName), culture))
            End If
            If (result Is Nothing) Then
                fileName = String.Format("{0}.{1}.txt", objectName, culture)
                Dim objectPath = Path.Combine(Path.Combine(HttpRuntime.AppDomainAppPath, baseName), fileName)
                If File.Exists(objectPath) Then
                    files = New String() {objectPath}
                    result = New FileStream(objectPath, FileMode.Open, FileAccess.Read)
                Else
                    If (String.IsNullOrEmpty(baseName) AndAlso (objectName = "CombinedSharedResources")) Then
                        Dim dependencies = New List(Of String)()
                        result = New MemoryStream()
                        Dim root = HttpContext.Current.Server.MapPath("~/")
                        Dim list() As String = Nothing
                        'try loading "Resources.CULTURE-NAME.txt" files
                        Dim rs = CreateDictionaryStream(culture, String.Empty, "Resources", list)
                        MergeStreams(result, rs, dependencies, list)
                        ' try loading "Web.Sitemap.CULTURE_NAME" files
                        rs = CreateDictionaryStream(culture, String.Empty, "web.sitemap", list)
                        MergeStreams(result, rs, dependencies, list)
                        'try loading "Controls\ControlName.ascx.CULTURE_NAME" files
                        Dim controlsPath = Path.Combine(root, "Controls")
                        If Directory.Exists(controlsPath) Then
                            For Each f in Directory.GetFiles(controlsPath, "*.ascx")
                                rs = CreateDictionaryStream(culture, "Controls", Path.GetFileName(f), list)
                                MergeStreams(result, rs, dependencies, list)
                            Next
                        End If
                        'complete processing of combined shared resources
                        result.Position = 0
                        files = dependencies.ToArray()
                    End If
                End If
            End If
            Return result
        End Function
        
        Private Shared Sub MergeStreams(ByVal result As Stream, ByVal source As Stream, ByVal dependencies As List(Of String), ByVal list() As String)
            If (Not (source) Is Nothing) Then
                Dim buffer((32768) - 1) As Byte
                Dim bytesRead = source.Read(buffer, 0, buffer.Length)
                Do While (bytesRead > 0)
                    result.Write(buffer, 0, buffer.Length)
                    bytesRead = source.Read(buffer, 0, buffer.Length)
                Loop
                source.Close()
                If (Not (list) Is Nothing) Then
                    dependencies.AddRange(list)
                End If
            End If
        End Sub
        
        Public Shared Function CreateDictionary(ByVal baseName As String, ByVal objectName As String) As LocalizationDictionary
            Dim culture = Thread.CurrentThread.CurrentUICulture.Name
            Dim fileName = String.Format("Norwindt.{0}.{1}.txt", objectName, culture)
            Dim dictionary = CType(HttpRuntime.Cache(fileName),LocalizationDictionary)
            If (dictionary Is Nothing) Then
                dictionary = New LocalizationDictionary()
                Dim files() As String = Nothing
                Dim s = CreateDictionaryStream(culture, baseName, objectName, files)
                If ((s Is Nothing) AndAlso culture.Contains("-")) Then
                    culture = culture.Substring(0, culture.IndexOf("-"))
                    s = CreateDictionaryStream(culture, baseName, objectName, files)
                End If
                If (Not (s) Is Nothing) Then
                    PopulateDictionary(dictionary, New StreamReader(s).ReadToEnd())
                    s.Close()
                End If
                Dim dependency As CacheDependency = Nothing
                If (Not (files) Is Nothing) Then
                    dependency = New CacheDependency(files)
                End If
                HttpRuntime.Cache.Insert(fileName, dictionary, dependency)
            End If
            Return dictionary
        End Function
        
        Private Shared Sub PopulateDictionary(ByVal dictionary As LocalizationDictionary, ByVal text As String)
            Dim m = TokenRegex.Match(text)
            Do While m.Success
                Dim token = m.Groups(1).Value
                If (token = m.Groups(3).Value) Then
                    dictionary(token) = m.Groups(2).Value
                End If
                m = m.NextMatch()
            Loop
        End Sub
    End Class
End Namespace
