namespace ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo
{
    public class OmahaInfo : IBuildInfo
    {
        public static readonly string[] DOTNET_PROJECTS =
        [
            $"AssetPlugin",
            $"BlamPlugin",
            $"Bonobo",
            $"BonoboConsole",
            $"BonoboInterfaces",
            $"BonoboManagedBlamInterfaces",
            $"BonoboPluginSystem",
            $"Bungie.Asset",
            $"Bungie.Blam",
            $"Bungie.Core",
            $"Bungie.Core.Datastore",
            $"Bungie.Core.Wpf",
            $"Bungie.Schema",
            $"Bungie.Schema.Blam",
            $"Bungie.SourceDepot",
            $"Bungie.Xbox",
            $"GuiPlugin",
            $"LibrarianPlugin",
            $"NormalPlugin",
            $"ProjectChooser",
            $"TAE.Shared",
            $"TAE.Shared.Tags",
            $"TAE.Shared.Tags.ManagedBlam",
            $"TAE.Shared.Tags.Service",
            $"TAE.Shared.Tags.ServiceClien",
            $"TAE.Shared.Tags.ServiceHost",
            $"TagService",
            $"TagWatcher",
        ];

        public static readonly string[] DOTNET_RELATIVE_PATHS =
        [
            $"bin/tools/bonobo/AssetPlugin/AssetPlugin.dll",
            $"bin/tools/bonobo/BlamPlugin/BlamPlugin.dll",
            $"Foundation.exe",
            $"bin/tools/bonobo/FoundationConsole.exe",
            $"bin/tools/bonobo/BonoboInterfaces.dll",
            $"bin/tools/bonobo/BonoboManagedBlamInterfaces/BonoboManagedBlamInterfaces.dll",
            $"bin/tools/bonobo/BonoboPluginSystem.dll",
            $"bin/tools/bonobo/Bungie.Asset.dll",
            $"bin/tools/bonobo/Bungie.Blam.dll",
            $"bin/tools/bonobo/Bungie.Core.dll",
            $"bin/tools/bonobo/Bungie.Core.Datastore.dll",
            $"bin/tools/bonobo/Bungie.Core.Wpf.dll",
            $"bin/tools/bonobo/Bungie.Schema.dll",
            $"bin/tools/bonobo/Bungie.Schema.Blam.dll",
            $"bin/tools/bonobo/Bungie.SourceDepot.dll",
            $"bin/tools/bonobo/Bungie.Xbox.dll",
            $"bin/tools/bonobo/GuiPlugin/GuiPlugin.dll",
            $"bin/tools/bonobo/LibrarianPlugin/LibrarianPlugin.dll",
            $"bin/tools/bonobo/NormalPlugin/NormalPlugin.dll",
            $"ProjectChooser.exe",
            $"bin/tools/bonobo/TAE.Shared.dll",
            $"bin/tools/bonobo/TAE.Shared.Tags.dll",
            $"bin/tools/bonobo/TAE.Shared.Tags.ManagedBlam.dll",
            $"bin/tools/bonobo/TAE.Shared.Tags.Service.dll",
            $"bin/tools/bonobo/TAE.Shared.Tags.ServiceClient.dll",
            $"bin/tools/bonobo/TAE.Shared.Tags.ServiceHost.dll",
            $"bin/tools/bonobo/TagService.exe",
            $"bin/tools/bonobo/TagWatcher.exe",
        ];

        public static readonly string[] PROJECT_XML_RELATIVE_PATHS =
        [
            $"project.root",
            $"project.xml",
            $"steam_appid.txt",
            $"bin/tools/bonobo/menus.bonobo.xml",
            $"bin/tools/bonobo/plugins.assetbrowser.xml",
            $"bin/tools/bonobo/plugins.bonobo.xml",
            $"bin/tools/bonobo/plugins.console.xml",
            $"bin/tools/bonobo/plugins.matman.xml",
            $"bin/tools/bonobo/plugins.world.xml",
            $"bin/tools/bonobo/TagWatcherConfig.xml",
        ];

        public string[] GetProjects() => DOTNET_PROJECTS;
        public string[] GetRelativePaths() => DOTNET_RELATIVE_PATHS;
        public string[] GetXMLRelativePaths() => PROJECT_XML_RELATIVE_PATHS;
    }
}
