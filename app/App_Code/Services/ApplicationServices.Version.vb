Namespace Norwindt.Services
    
    Partial Public Class ApplicationServices
        
        ''' The first three numbers in the version reflect the version of the app generator.
        ''' The last number is the value stored in DataAquarium.Version.xml file located in the root of the project.
        ''' The number is automatically incremented when the application is published from the app generator.
        Public Shared Version As String
        
        ''' The version of jQuery Mobile integrated in the app generator.
        Public Shared JqmVersion As String
        
        ''' The version reported to mobile clients adding this application.
        Public Shared HostVersion As String
    End Class
End Namespace
