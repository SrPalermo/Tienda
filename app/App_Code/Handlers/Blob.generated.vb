Namespace Norwindt.Handlers
    
    Partial Public Class BlobFactoryConfig
        Inherits BlobFactory
        
        Public Shared Sub Initialize()
            'register blob handlers
            RegisterHandler("CategoriesPicture", """dbo"".""Categories""", """Picture""", New String() {"""CategoryID"""}, "Categories Picture", "Categories", "Picture")
            RegisterHandler("EmployeesPhoto", """dbo"".""Employees""", """Photo""", New String() {"""EmployeeID"""}, "Employees Photo", "Employees", "Photo")
        End Sub
    End Class
End Namespace
