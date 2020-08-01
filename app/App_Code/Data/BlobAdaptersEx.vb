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
    
    Partial Public Class S3BlobAdapter
        Inherits S3BlobAdapterBase
        
        Public Sub New(ByVal controller As String, ByVal arguments As BlobAdapterArguments)
            MyBase.New(controller, arguments)
        End Sub
    End Class
    
    Public Class S3BlobAdapterBase
        Inherits BlobAdapter
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_AccessKeyID As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_SecretAccessKey As String
        
        <System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)>  _
        Private m_Bucket As String
        
        Public Sub New(ByVal controller As String, ByVal arguments As BlobAdapterArguments)
            MyBase.New(controller, arguments)
        End Sub
        
        Public Overridable Property AccessKeyID() As String
            Get
                Return m_AccessKeyID
            End Get
            Set
                m_AccessKeyID = value
            End Set
        End Property
        
        Public Overridable Property SecretAccessKey() As String
            Get
                Return m_SecretAccessKey
            End Get
            Set
                m_SecretAccessKey = value
            End Set
        End Property
        
        Public Overridable Property Bucket() As String
            Get
                Return m_Bucket
            End Get
            Set
                m_Bucket = value
            End Set
        End Property
        
        Protected Overrides Sub Initialize()
            MyBase.Initialize()
            If Arguments.ContainsKey("access-key-id") Then
                AccessKeyID = Arguments("access-key-id")
            End If
            If Arguments.ContainsKey("secret-access-key") Then
                SecretAccessKey = Arguments("secret-access-key")
            End If
            If Arguments.ContainsKey("bucket") Then
                Bucket = Arguments("bucket")
            End If
        End Sub
        
        Public Overrides Function ReadBlob(ByVal keyValue As String) As Stream
            Dim extendedPath = KeyValueToPath(keyValue)
            Dim httpVerb = "GET"
            Dim d = DateTime.UtcNow
            Dim canonicalizedAmzHeaders = ("x-amz-date:" + d.ToString("R", CultureInfo.InvariantCulture))
            Dim canonicalizedResource = String.Format("/{0}/{1}", Me.Bucket, extendedPath)
            Dim stringToSign = String.Format("{0}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{1}"&Global.Microsoft.VisualBasic.ChrW(10)&"{2}", httpVerb, canonicalizedAmzHeaders, canonicalizedResource)
            Dim authorization = CreateAuthorizationHeaderForS3(stringToSign)
            Dim uri = New Uri((("http://" + Me.Bucket)  _
                            + (".s3.amazonaws.com/" + extendedPath)))
            Dim request = CType(WebRequest.Create(uri),HttpWebRequest)
            request.Method = httpVerb
            request.Headers.Add("x-amz-date", d.ToString("R", CultureInfo.InvariantCulture))
            request.Headers.Add("Authorization", authorization)
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
            Dim extendedPath = KeyValueToPath(keyValue)
            Dim stream = file.InputStream
            Dim blobLength = CType(stream.Length,Integer)
            Dim blobContent((blobLength) - 1) As Byte
            stream.Read(blobContent, 0, blobLength)
            Dim httpVerb = "PUT"
            Dim d = DateTime.UtcNow
            Dim canonicalizedAmzHeaders = ("x-amz-date:" + d.ToString("R", CultureInfo.InvariantCulture))
            Dim canonicalizedResource = String.Format("/{0}/{1}", Me.Bucket, extendedPath)
            Dim stringToSign = String.Format("{0}"&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&Global.Microsoft.VisualBasic.ChrW(10)&"{1}"&Global.Microsoft.VisualBasic.ChrW(10)&"{2}", httpVerb, canonicalizedAmzHeaders, canonicalizedResource)
            Dim authorization = CreateAuthorizationHeaderForS3(stringToSign)
            Dim uri = New Uri((("http://" + Me.Bucket)  _
                            + (".s3.amazonaws.com/" + extendedPath)))
            Dim request = CType(WebRequest.Create(uri),HttpWebRequest)
            request.Method = httpVerb
            request.ContentLength = blobLength
            request.Headers.Add("x-amz-date", d.ToString("R", CultureInfo.InvariantCulture))
            request.Headers.Add("Authorization", authorization)
            Try 
                Using requestStream = request.GetRequestStream()
                    Dim bufferSize = (1024 * 64)
                    Dim offset = 0
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
        
        Protected Overridable Function CreateAuthorizationHeaderForS3(ByVal canonicalizedString As String) As String
            Dim ae = New UTF8Encoding()
            Dim signature = New HMACSHA1()
            signature.Key = ae.GetBytes(Me.SecretAccessKey)
            Dim bytes = ae.GetBytes(canonicalizedString)
            Dim moreBytes = signature.ComputeHash(bytes)
            Dim encodedCanonical = Convert.ToBase64String(moreBytes)
            Return String.Format(CultureInfo.InvariantCulture, "{0} {1}:{2}", "AWS", Me.AccessKeyID, encodedCanonical)
        End Function
    End Class
End Namespace
