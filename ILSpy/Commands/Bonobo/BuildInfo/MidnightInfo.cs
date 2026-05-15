using System.IO;

using ICSharpCode.ILSpy.Commands.Bonobo.Extensions;

namespace ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo
{
    public class MidnightInfo : IBuildInfo
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
            $"Corinth.Asset",
            $"Corinth.Blam",
            $"Corinth.Core",
            $"Corinth.Core.Datastore",
            $"Corinth.Core.Wpf",
            $"Corinth.Perforce",
            $"Corinth.Schema",
            $"Corinth.Schema.Blam",
            $"Corinth.SourceDepot",
            $"Corinth.Xbox",
            $"GuiPlugin",
            $"LibrarianPlugin",
            $"NormalPlugin",
            $"TAE.Shared",
            $"TAE.Shared.Tags",
            $"TAE.Shared.Tags.ManagedBlam",
            $"TAE.Shared.Tags.Service",
            $"TAE.Shared.Tags.ServiceClient",
            $"TAE.Shared.Tags.ServiceHost",
            $"TagService",
            $"TagWatcher",
        ];

        public static readonly string[] DOTNET_RELATIVE_PATHS =
        [
            $"bin\\tools\\bonobo\\AssetPlugin\\AssetPlugin.dll",
            $"bin\\tools\\bonobo\\BlamPlugin\\BlamPlugin.dll",
            $"Foundation.exe",
            $"bin\\tools\\bonobo\\FoundationConsole.exe",
            $"bin\\tools\\bonobo\\BonoboInterfaces.dll",
            $"bin\\tools\\bonobo\\BonoboManagedBlamInterfaces\\BonoboManagedBlamInterfaces.dll",
            $"bin\\tools\\bonobo\\BonoboPluginSystem.dll",
            $"bin\\tools\\bonobo\\Corinth.Asset.dll",
            $"bin\\tools\\bonobo\\Corinth.Blam.dll",
            $"bin\\tools\\bonobo\\Corinth.Core.dll",
            $"bin\\tools\\bonobo\\Corinth.Core.Datastore.dll",
            $"bin\\tools\\bonobo\\Corinth.Core.Wpf.dll",
            $"bin\\tools\\bonobo\\Corinth.Perforce.dll",
            $"bin\\tools\\bonobo\\Corinth.Schema.dll",
            $"bin\\tools\\bonobo\\Corinth.Schema.Blam.dll",
            $"bin\\tools\\bonobo\\Corinth.SourceDepot.dll",
            $"bin\\tools\\bonobo\\Corinth.Xbox.dll",
            $"bin\\tools\\bonobo\\GuiPlugin\\GuiPlugin.dll",
            $"bin\\tools\\bonobo\\LibrarianPlugin\\LibrarianPlugin.dll",
            $"bin\\tools\\bonobo\\NormalPlugin\\NormalPlugin.dll",
            $"bin\\tools\\bonobo\\TAE.Shared.dll",
            $"bin\\tools\\bonobo\\TAE.Shared.Tags.dll",
            $"bin\\tools\\bonobo\\TAE.Shared.Tags.ManagedBlam.dll",
            $"bin\\tools\\bonobo\\TAE.Shared.Tags.Service.dll",
            $"bin\\tools\\bonobo\\TAE.Shared.Tags.ServiceClient.dll",
            $"bin\\tools\\bonobo\\TAE.Shared.Tags.ServiceHost.dll",
            $"bin\\tools\\bonobo\\TagService.exe",
            $"bin\\tools\\bonobo\\TagWatcher.exe",
        ];

        public static readonly string[] PROJECT_XML_RELATIVE_PATHS =
        [
            $"project.root",
            $"project.xml",
            $"steam_appid.txt",
            $"bin\\tools\\bonobo\\menus.bonobo.xml",
            $"bin\\tools\\bonobo\\plugins.assetbrowser.xml",
            $"bin\\tools\\bonobo\\plugins.bonobo.xml",
            $"bin\\tools\\bonobo\\plugins.console.xml",
            $"bin\\tools\\bonobo\\plugins.matman.xml",
            $"bin\\tools\\bonobo\\plugins.world.xml",
            $"bin\\tools\\bonobo\\TagWatcherConfig.xml",
        ];

		public static readonly string[] PROJECT_EXTERNAL_DEPENDENCIES =
		[
			$"bin\\tools\\bonobo\\Interop.WMPLib.dll",
			$"bin\\tools\\bonobo\\Microsoft.Practices.Composite.dll",
			$"bin\\tools\\bonobo\\Microsoft.Practices.Composite.Presentation.dll",
			$"bin\\tools\\bonobo\\Microsoft.WindowsAPICodePack.dll",
			$"bin\\tools\\bonobo\\Microsoft.WindowsAPICodePack.Shell.dll",
			$"bin\\tools\\bonobo\\p4api.net.dll",
			$"bin\\tools\\bonobo\\p4bridge.dll",
			$"bin\\tools\\bonobo\\sqlceca35.dll",
			$"bin\\tools\\bonobo\\sqlcecompact35.dll",
			$"bin\\tools\\bonobo\\sqlceer35EN.dll",
			$"bin\\tools\\bonobo\\sqlceme35.dll",
			$"bin\\tools\\bonobo\\sqlceoledb35.dll",
			$"bin\\tools\\bonobo\\sqlceqp35.dll",
			$"bin\\tools\\bonobo\\sqlcese35.dll",
			$"bin\\tools\\bonobo\\System.CoreEx.dll",
			$"bin\\tools\\bonobo\\System.Data.SqlServerCe.dll",
			$"bin\\tools\\bonobo\\System.Data.SqlServerCe.Entity.dll",
			$"bin\\tools\\bonobo\\System.Reactive.dll",
		];

		public static readonly string MANAGED_RELATIVE_PATH = $"bin\\ManagedBlam.dll";

		public string[] GetProjects() => DOTNET_PROJECTS;
        public string[] GetRelativePaths() => DOTNET_RELATIVE_PATHS;
        public string[] GetXMLRelativePaths() => PROJECT_XML_RELATIVE_PATHS;
		public string[] GetExternalRelativePaths() => PROJECT_EXTERNAL_DEPENDENCIES;
		public string GetManagedRelativePath() => MANAGED_RELATIVE_PATH;

		public string FilterRelativePath(string path)
		{
			if (path.Contains("Bonobo") && !path.Contains("Interfaces") && !path.Contains("PluginSystem"))
			{
				return path.Replace("Bonobo", "Foundation");
			}

			return path;
		}

		public void CleanupProjectDump(string project) 
		{
			string path = $"{DumperContext.BonoboProjectDumpPath}\\{project}";

			switch (project)
			{
				case $"AssetPlugin":
					CleanupAssetPlugin(path);
					break;
				case $"BlamPlugin":
					CleanupBlamPlugin(path);
					break;
				case $"Bonobo":
					CleanupBonobo(path);
					break;
				case $"BonoboConsole":
					CleanupBonoboConsole(path);
					break;
				case $"BonoboInterfaces":
					CleanupBonoboInterfaces(path);
					break;
				case $"BonoboManagedBlamInterfaces":
					CleanupBonoboManagedBlamInterfaces(path);
					break;
				case $"BonoboPluginSystem":
					CleanupBonoboPluginSystem(path);
					break;
				case $"Corinth.Asset":
					CleanupCorinthAsset(path);
					break;
				case $"Corinth.Blam":
					CleanupCorinthBlam(path);
					break;
				case $"Corinth.Core":
					CleanupCorinthCore(path);
					break;
				case $"Corinth.Core.Datastore":
					CleanupCorinthCoreDatastore(path);
					break;
				case $"Corinth.Core.Wpf":
					CleanupCorinthCoreWpf(path);
					break;
				case $"Corinth.Perforce":
					CleanupCorinthPerforce(path);
					break;
				case $"Corinth.Schema":
					CleanupCorinthSchema(path);
					break;
				case $"Corinth.Schema.Blam":
					CleanupCorinthSchemaBlam(path);
					break;
				case $"Corinth.SourceDepot":
					CleanupCorinthSourceDepot(path);
					break;
				case $"Corinth.Xbox":
					CleanupCorinthXbox(path);
					break;
				case $"GuiPlugin":
					CleanupGuiPlugin(path);
					break;
				case $"LibrarianPlugin":
					CleanupLibrarianPlugin(path);
					break;
				case $"NormalPlugin":
					CleanupNormalPlugin(path);
					break;
				case $"TAE.Shared":
					CleanupTAEShared(path);
					break;
				case $"TAE.Shared.Tags":
					CleanupTAESharedTags(path);
					break;
				case $"TAE.Shared.Tags.ManagedBlam":
					CleanupTAESharedTagsManagedBlam(path);
					break;
				case $"TAE.Shared.Tags.Service":
					CleanupTAESharedTagsService(path);
					break;
				case $"TAE.Shared.Tags.ServiceClient":
					CleanupTAESharedTagsServiceClient(path);
					break;
				case $"TAE.Shared.Tags.ServiceHost":
					CleanupTAESharedTagsServiceHost(path);
					break;
				case $"TagService":
					CleanupTagService(path);
					break;
				case $"TagWatcher":
					CleanupTagWatcher(path);
					break;
			}
		}

		public static void CleanupAssetPlugin(string path) 
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\AssetBrowser", $"{path}\\AssetBrowser");
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\Plugins", $"{path}\\Plugins");

			Directory.CreateDirectory($"{path}\\Themes");
			File.Move($"{path}\\Bonobo\\AssetPlugin\\GenericResourceDictionary.xaml", $"{path}\\Themes\\Generic.xaml", true);
			File.Move($"{path}\\Bonobo\\AssetPlugin\\GenericResourceDictionary.xaml.cs", $"{path}\\Themes\\Generic.xaml.cs", true);

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupBlamPlugin(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\BlamPlugin", $"{path}");
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\Plugins", $"{path}");

			DirectoryHelper.Rename($"{path}\\images", $"{path}\\Images");

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupBonobo(string path)
		{

		}

		public static void CleanupBonoboConsole(string path)
		{

		}

		public static void CleanupBonoboInterfaces(string path)
		{

		}

		public static void CleanupBonoboManagedBlamInterfaces(string path)
		{

		}

		public static void CleanupBonoboPluginSystem(string path)
		{

		}

		public static void CleanupCorinthAsset(string path)
		{

		}

		public static void CleanupCorinthBlam(string path)
		{

		}

		public static void CleanupCorinthCore(string path)
		{

		}

		public static void CleanupCorinthCoreDatastore(string path)
		{

		}

		public static void CleanupCorinthCoreWpf(string path)
		{

		}

		public static void CleanupCorinthPerforce(string path)
		{

		}

		public static void CleanupCorinthSchema(string path)
		{

		}

		public static void CleanupCorinthSchemaBlam(string path)
		{

		}

		public static void CleanupCorinthSourceDepot(string path)
		{

		}

		public static void CleanupCorinthXbox(string path)
		{

		}

		public static void CleanupGuiPlugin(string path)
		{

		}

		public static void CleanupLibrarianPlugin(string path)
		{

		}

		public static void CleanupNormalPlugin(string path)
		{

		}

		public static void CleanupTAEShared(string path)
		{

		}

		public static void CleanupTAESharedTags(string path)
		{

		}

		public static void CleanupTAESharedTagsManagedBlam(string path)
		{

		}

		public static void CleanupTAESharedTagsService(string path)
		{

		}

		public static void CleanupTAESharedTagsServiceClient(string path)
		{

		}

		public static void CleanupTAESharedTagsServiceHost(string path)
		{

		}

		public static void CleanupTagService(string path)
		{

		}

		public static void CleanupTagWatcher(string path)
		{

		}
	}
}
