Imports Norwindt.Data
Imports Norwindt.Handlers
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.Common
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Web

Namespace Norwindt.Data
    
    Partial Public Class FileSystemBlobAdapter
        Inherits FileSystemBlobAdapterBase
        
        Public Sub New(ByVal controller As String, ByVal arguments As BlobAdapterArguments)
            MyBase.New(controller, arguments)
        End Sub
    End Class
    
    Public Class FileSystemBlobAdapterBase
        Inherits BlobAdapter
        
        Public Sub New(ByVal controller As String, ByVal arguments As BlobAdapterArguments)
            MyBase.New(controller, arguments)
        End Sub
        
        Public Overrides Function ReadBlob(ByVal keyValue As String) As Stream
            Dim fileName = ExtendPathTemplate(keyValue)
            Return File.OpenRead(fileName)
        End Function
        
        Public Overrides Function WriteBlob(ByVal file As HttpPostedFileBase, ByVal keyValue As String) As Boolean
            Dim fileName = ExtendPathTemplate(keyValue)
            Dim directoryName = Path.GetDirectoryName(fileName)
            If Not (Directory.Exists(directoryName)) Then
                Directory.CreateDirectory(directoryName)
            End If
            Dim stream = file.InputStream
            file.SaveAs(fileName)
            Return true
        End Function
        
        Public Overrides Sub ValidateFieldValue(ByVal fv As FieldValue)
        End Sub
    End Class
    
    Partial Public Class AzureBlobAdapter
        Inherits AzureBlobAdapterBase
        
        Public Sub New(ByVal controller As String, ByVal arguments As BlobAdapterArguments)
            MyBase.New(controller, arguments)
        End Sub
    End Class
    
    Public Class AzureBlobAdapterBase
        Inherits BlobAdapter
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Account As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Key As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Container As String
        
        Public Sub New(ByVal controller As String, ByVal arguments As BlobAdapterArguments)
            MyBase.New(controller, arguments)
        End Sub
        
        Public Overridable Property Account() As String
            Get
                Return m_Account
            End Get
            Set
                m_Account = value
            End Set
        End Property
        
        Public Overridable Property Key() As String
            Get
                Return m_Key
            End Get
            Set
                m_Key = value
            End Set
        End Property
        
        Public Overridable Property Container() As String
            Get
                Return m_Container
            End Get
            Set
                m_Container = value
            End Set
        End Property
        
        Protected Overrides Sub Initialize()
            MyBase.Initialize()
            If Arguments.ContainsKey("account") Then
                Account = Arguments("account")
            End If
            If Arguments.ContainsKey("key") Then
                Key = Arguments("key")
            End If
            If Arguments.ContainsKey("container") Then
                Container = Arguments("container")
            End If
        End Sub
        
        Public Overrides Function ReadBlob(ByVal keyValue As String) As Stream
            Dim urlPath = String.Format("{0}/{1}", Me.Container, KeyValueToPath(keyValue))
            Dim requestMethod = "GET"
            Dim storageServiceVersion = "2015-12-11"
            Dim blobType = "BlockBlob"
            Dim dateInRfc1123Format = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture)
            Dim canonicalizedHeaders = String.Format("x-ms-blob-type:{0}"&Global.Microsoft.VisualBasic.ChrW(10)&"x-ms-date:{1}"&Global.Microsoft.VisualBasic.ChrW(10)&"x-ms-version:{2}", blobType, dateInRfc1123Format, storageServiceVersion)
            Dim canonicalizedResource = String.Format("/{0}/{1}", Me.Account, urlPath)
            Dim blobLength = ""
            Dim stringToSign = String.Format("{0}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{1}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{2}"&Global.Microsoft.VisualBasic.ChrW(10)&"{3}", requestMethod, blobLength, canonicalizedHeaders, canonicalizedResource)
            Dim authorizationHeader = CreateAuthorizationHeaderForAzure(stringToSign)
            Dim uri = New Uri((("https://" + Me.Account)  _
                            + (".blob.core.windows.net/" + urlPath)))
            Dim request = CType(WebRequest.Create(uri),HttpWebRequest)
            request.Method = requestMethod
            request.Headers.Add("x-ms-blob-type", blobType)
            request.Headers.Add("x-ms-date", dateInRfc1123Format)
            request.Headers.Add("x-ms-version", storageServiceVersion)
            request.Headers.Add("Authorization", authorizationHeader)
            Try 
                Dim stream = New TemporaryFileStream()
                Using response = CType(request.GetResponse(),HttpWebResponse)
                    Using dataStream = response.GetResponseStream()
                        CopyData(dataStream, stream)
                    End Using
                End Using
                Return stream
            Catch e As Exception
                Dim message = e.Message
                Return Nothing
            End Try
        End Function
        
        Public Overrides Function WriteBlob(ByVal file As HttpPostedFileBase, ByVal keyValue As String) As Boolean
            Dim requestMethod = "PUT"
            Dim urlPath = String.Format("{0}/{1}", Me.Container, KeyValueToPath(keyValue))
            Dim storageServiceVersion = "2015-12-11"
            Dim dateInRfc1123Format = DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture)
            Dim stream = file.InputStream
            Dim utf8Encoding = New UTF8Encoding()
            Dim blobLength = CType(stream.Length,Integer)
            Dim blobContent((blobLength) - 1) As Byte
            stream.Read(blobContent, 0, blobLength)
            Dim blobType = "BlockBlob"
            Dim canonicalizedHeaders = String.Format("x-ms-blob-type:{0}"&Global.Microsoft.VisualBasic.ChrW(10)&"x-ms-date:{1}"&Global.Microsoft.VisualBasic.ChrW(10)&"x-ms-version:{2}", blobType, dateInRfc1123Format, storageServiceVersion)
            Dim canonicalizedResource = String.Format("/{0}/{1}", Me.Account, urlPath)
            Dim stringToSign = String.Format("{0}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{1}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{4}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{2}"&Global.Microsoft.VisualBasic.ChrW(10)&"{3}", requestMethod, blobLength, canonicalizedHeaders, canonicalizedResource, file.ContentType)
            Dim authorizationHeader = CreateAuthorizationHeaderForAzure(stringToSign)
            Dim uri = New Uri((("https://" + Me.Account)  _
                            + (".blob.core.windows.net/" + urlPath)))
            Dim request = CType(WebRequest.Create(uri),HttpWebRequest)
            request.Method = requestMethod
            request.Headers.Add("x-ms-blob-type", blobType)
            request.Headers.Add("x-ms-date", dateInRfc1123Format)
            request.Headers.Add("x-ms-version", storageServiceVersion)
            request.Headers.Add("Authorization", authorizationHeader)
            request.ContentLength = blobLength
            request.ContentType = file.ContentType
            Try 
                Dim bufferSize = (1024 * 64)
                Dim offset = 0
                Using requestStream = request.GetRequestStream()
                    Do While (offset < blobLength)
                        Dim bytesToWrite = (blobLength - offset)
                        If ((offset + bufferSize) < blobLength) Then
                            bytesToWrite = bufferSize
                        End If
                        requestStream.Write(blobContent, offset, bytesToWrite)
                        offset = (offset + bytesToWrite)
                    Loop
                End Using
                Using response = CType(request.GetResponse(),HttpWebResponse)
                    Dim ETag = response.Headers("ETag")
                    If (((response.StatusCode = HttpStatusCode.OK) OrElse (response.StatusCode = HttpStatusCode.Accepted)) OrElse (response.StatusCode = HttpStatusCode.Created)) Then
                        Return true
                    End If
                End Using
            Catch webEx As WebException
                If (Not (webEx) Is Nothing) Then
                    Dim resp = webEx.Response
                    If (Not (resp) Is Nothing) Then
                        Using sr = New StreamReader(resp.GetResponseStream(), true)
                            Throw New Exception(sr.ReadToEnd())
                        End Using
                    End If
                End If
            End Try
            Return false
        End Function
        
        Protected Function CreateAuthorizationHeaderForAzure(ByVal canonicalizedString As String) As String
            Dim signature = String.Empty
            Dim storageKey = Convert.FromBase64String(Me.Key)
            Using hmacSha256 = New HMACSHA256(storageKey)
                Dim dataToHmac = System.Text.Encoding.UTF8.GetBytes(canonicalizedString)
                signature = Convert.ToBase64String(hmacSha256.ComputeHash(dataToHmac))
            End Using
            Dim authorizationHeader = String.Format(CultureInfo.InvariantCulture, "{0} {1}:{2}", "SharedKey", Me.Account, signature)
            Return authorizationHeader
        End Function
    End Class
End Namespace
