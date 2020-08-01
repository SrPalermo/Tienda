Imports Norwindt.Handlers
Imports Norwindt.Web
Imports System.Web.Configuration

Namespace Norwindt.Services
    
    Public Class AppFrameworkConfig
        
        Public Overridable Sub Initialize()
            ApplicationServices.FrameworkAppName = "Norwintd"
            ApplicationServices.Version = "8.9.5.0"
            ApplicationServices.JqmVersion = "1.4.6"
            ApplicationServices.HostVersion = "1.2.5.0"
            Dim compilation = CType(WebConfigurationManager.GetSection("system.web/compilation"),CompilationSection)
            Dim releaseMode = Not (compilation.Debug)
            AquariumExtenderBase.EnableMinifiedScript = releaseMode
            AquariumExtenderBase.EnableCombinedScript = releaseMode
            ApplicationServices.EnableMinifiedCss = releaseMode
            ApplicationServices.EnableCombinedCss = releaseMode
            ApplicationServicesBase.AuthorizationIsSupported = false
            BlobFactoryConfig.Initialize()
        End Sub
    End Class
End Namespace
