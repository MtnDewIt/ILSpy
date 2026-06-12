using System.IO;

using ICSharpCode.ILSpy.Commands.Bonobo.Extensions;

namespace ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo
{
    public class GroundhogInfo : IBuildInfo
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

		public void CleanupProjectDump(string project, string outputPath)
		{
			string path = $"{outputPath}\\{project}";

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
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\AssetPlugin\\GenericResourceDictionary.xaml", $"{path}\\Themes\\Generic.xaml");
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\AssetPlugin\\GenericResourceDictionary.xaml.cs", $"{path}\\Themes\\Generic.xaml.cs");

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupBlamPlugin(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\BlamPlugin", $"{path}");
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\Plugins", $"{path}");

			DirectoryHelper.Rename($"{path}\\images", $"{path}\\Images");

			DirectoryHelper.Rename($"{path}\\DemoCustomSectionComplete\\DemoScenarioSection.cs", $"{path}\\DemoCustomSectionComplete\\DemoScenarioSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\demo\\democustomsectioncomplete\\demoscenariosection.xaml", $"{path}\\DemoCustomSectionComplete\\DemoScenarioSection.xaml");
			DirectoryHelper.Rename($"{path}\\DemoCustomSectionWithDelete\\DemoScenarioSection.cs", $"{path}\\DemoCustomSectionWithDelete\\DemoScenarioSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\demo\\democustomsectionwithdelete\\demoscenariosection.xaml", $"{path}\\DemoCustomSectionWithDelete\\DemoScenarioSection.xaml");

			DirectoryHelper.Rename($"{path}\\rendermodel", $"{path}\\RenderModel");
			DirectoryHelper.Rename($"{path}\\RenderModel\\RenderModelSection.cs", $"{path}\\RenderModel\\RenderModelSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\RenderModel\\rendermodelsection.xaml", $"{path}\\RenderModel\\RenderModelSection.xaml");

			DirectoryHelper.Rename($"{path}\\tagannotations", $"{path}\\TagAnnotations");
			DirectoryHelper.Rename($"{path}\\TagAnnotations\\AreaAnnotationViewItemPanel.cs", $"{path}\\TagAnnotations\\AreaAnnotationViewItemPanel.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagAnnotations\\areaannotationviewfield.xaml", $"{path}\\TagAnnotations\\AreaAnnotationViewItemPanel.xaml");

			DirectoryHelper.Rename($"{path}\\tagcustomsection", $"{path}\\TagCustomSection");

			DirectoryHelper.Rename($"{path}\\TagCustomSection\\BitmapImportSection.cs", $"{path}\\TagCustomSection\\BitmapImportSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\bitmapimportsection.xaml", $"{path}\\TagCustomSection\\BitmapImportSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\BitmapSection.cs", $"{path}\\TagCustomSection\\BitmapSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\bitmapsection.xaml", $"{path}\\TagCustomSection\\BitmapSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\BlendScreenSection.cs", $"{path}\\TagCustomSection\\BlendScreenSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\blendscreensection\\blendscreensection.xaml", $"{path}\\TagCustomSection\\BlendScreenSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\FormationSection.cs", $"{path}\\TagCustomSection\\FormationSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\formationsection.xaml", $"{path}\\TagCustomSection\\FormationSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\FrameEventsSection.cs", $"{path}\\TagCustomSection\\FrameEventsSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\frameeventssection.xaml", $"{path}\\TagCustomSection\\FrameEventsSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\SoundClassesSection.cs", $"{path}\\TagCustomSection\\SoundClassesSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\soundclassessection.xaml", $"{path}\\TagCustomSection\\SoundClassesSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\SourceFileListSection.cs", $"{path}\\TagCustomSection\\SourceFileListSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\sourcefilelistsection.xaml", $"{path}\\TagCustomSection\\SourceFileListSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\StringsSection.cs", $"{path}\\TagCustomSection\\StringsSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\stringssection.xaml", $"{path}\\TagCustomSection\\StringsSection.xaml");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\UsurpPreviewCustomSection.cs", $"{path}\\TagCustomSection\\UsurpPreviewCustomSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagCustomSection\\usurppreviewcustomsection.xaml", $"{path}\\TagCustomSection\\UsurpPreviewCustomSection.xaml");

			Directory.Delete($"{path}\\TagCustomSection\\blendscreensection", true);

			DirectoryHelper.Rename($"{path}\\taggridview", $"{path}\\TagGridView");
			DirectoryHelper.Rename($"{path}\\taggridview\\themes", $"{path}\\TagGridView\\Themes");
			DirectoryHelper.Rename($"{path}\\taggridview\\themes\\generic.xaml", $"{path}\\TagGridView\\Themes\\Generic.xaml");

			Directory.CreateDirectory($"{path}\\TagGridView\\Themes\\Resources");
			DirectoryHelper.Rename($"{path}\\TagGridView\\GridFields\\TagFieldCustomGridCellResources.xaml", $"{path}\\TagGridView\\Themes\\Resources\\TagFieldCustomGridCellResources.xaml");
			DirectoryHelper.Rename($"{path}\\TagGridView\\GridFields\\TagFieldCustomGridCellResources.xaml.cs", $"{path}\\TagGridView\\Themes\\Resources\\TagFieldCustomGridCellResources.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagGridView\\GridFields\\TagFieldGridCellResources.xaml", $"{path}\\TagGridView\\Themes\\Resources\\TagFieldGridCellResources.xaml");
			DirectoryHelper.Rename($"{path}\\TagGridView\\GridFields\\TagFieldGridCellResources.xaml.cs", $"{path}\\TagGridView\\Themes\\Resources\\TagFieldGridCellResources.xaml.cs");

			DirectoryHelper.Rename($"{path}\\tagtemplateview", $"{path}\\TagTemplateView");

			Directory.CreateDirectory($"{path}\\TagTemplateView\\Themes\\Resources");
			DirectoryHelper.Rename($"{path}\\TagTemplateView\\Values\\TagValuePanelResources.xaml", $"{path}\\TagTemplateView\\Themes\\Resources\\TagValuePanelResources.xaml");
			DirectoryHelper.Rename($"{path}\\TagTemplateView\\Values\\TagValuePanelResources.xaml.cs", $"{path}\\TagTemplateView\\Themes\\Resources\\TagValuePanelResources.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagTemplateView\\ParameterResources.xaml", $"{path}\\TagTemplateView\\Themes\\Resources\\ParameterResources.xaml");
			DirectoryHelper.Rename($"{path}\\TagTemplateView\\ParameterResources.xaml.cs", $"{path}\\TagTemplateView\\Themes\\Resources\\ParameterResources.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagTemplateView\\shared\\colorswatchtagtemplatevalue.xaml", $"{path}\\TagTemplateView\\ColorSwatchTagTemplateValue.xaml");
			DirectoryHelper.Rename($"{path}\\TagTemplateView\\ColorSwatchTagTemplateValue.cs", $"{path}\\TagTemplateView\\ColorSwatchTagTemplateValue.xaml.cs");

			Directory.Delete($"{path}\\TagTemplateView\\shared", true);

			DirectoryHelper.Rename($"{path}\\tagview", $"{path}\\TagView");
			DirectoryHelper.Rename($"{path}\\tagview\\images", $"{path}\\TagView\\Images");

			Directory.CreateDirectory($"{path}\\TagView\\Themes\\Resources");
			DirectoryHelper.Rename($"{path}\\TagView\\Fields\\TagFieldBlockPanelMenuToggleResources.xaml", $"{path}\\TagView\\Themes\\Resources\\TagFieldBlockPanelMenuToggleResources.xaml");
			DirectoryHelper.Rename($"{path}\\TagView\\Fields\\TagFieldBlockPanelMenuToggleResources.xaml.cs", $"{path}\\TagView\\Themes\\Resources\\TagFieldBlockPanelMenuToggleResources.xaml.cs");
			DirectoryHelper.Rename($"{path}\\TagView\\Fields\\TagFieldPanelResourceDictionary.xaml", $"{path}\\TagView\\Themes\\Resources\\TagFieldPanelResourceDictionary.xaml");
			DirectoryHelper.Rename($"{path}\\TagView\\Fields\\TagFieldPanelResourceDictionary.xaml.cs", $"{path}\\TagView\\Themes\\Resources\\TagFieldPanelResourceDictionary.xaml.cs");

			File.Delete($"{path}\\Properties\\Resources.cs");

			Directory.Delete($"{path}\\Bonobo", true);
			Directory.Delete($"{path}\\demo", true);
		}

		public static void CleanupBonobo(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\Application", $"{path}");
			DirectoryHelper.Rename($"{path}\\app.config", $"{path}\\App.config");

			File.Delete($"{path}\\Properties\\Resources.cs");
			File.Delete($"{path}\\Properties\\Settings.cs");

			Directory.Delete($"{path}\\images", true);
			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupBonoboConsole(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\BonoboConsole", $"{path}");
			DirectoryHelper.Rename($"{path}\\app.config", $"{path}\\App.config");
		}

		public static void CleanupBonoboInterfaces(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\PluginSystem", $"{path}\\PluginSystem");
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\Shared", $"{path}\\Shared");

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupBonoboManagedBlamInterfaces(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\PluginSystem\\Custom", $"{path}");

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupBonoboPluginSystem(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\PluginSystem", $"{path}");

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupCorinthAsset(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\Asset", $"{path}");

			Directory.Delete($"{path}\\Corinth", true);
		}

		public static void CleanupCorinthBlam(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\Blam", $"{path}");

			Directory.Delete($"{path}\\Corinth", true);
		}

		public static void CleanupCorinthCore(string path)
		{
			//DirectoryHelper.Rename($"{path}\\Corinth\\UI\\WinForms\\InputBoxForm.cs", $"{path}\\Corinth\\UI\\WinForms\\InputBoxForm.Designer.cs");
			//DirectoryHelper.Rename($"{path}\\Corinth\\UI\\WinForms\\ProgressBoxForm.cs", $"{path}\\Corinth\\UI\\WinForms\\ProgressBoxForm.Designer.cs");

			DirectoryHelper.MoveFiles($"{path}\\Corinth.UI.WinForms.InputBoxForm.resx", $"{path}\\Corinth\\UI\\WinForms\\InputBoxForm.resx");
			DirectoryHelper.MoveFiles($"{path}\\Corinth.UI.WinForms.ProgressBoxForm.resx", $"{path}\\Corinth\\UI\\WinForms\\ProgressBoxForm.resx");

			DirectoryHelper.Rename($"{path}\\app.config", $"{path}\\App.config");
		}

		public static void CleanupCorinthCoreDatastore(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth", $"{path}");
		}

		public static void CleanupCorinthCoreWpf(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\UI", $"{path}\\UI");

			DirectoryHelper.Rename($"{path}\\shaders", $"{path}\\Shaders");
			DirectoryHelper.Rename($"{path}\\shaders\\ps", $"{path}\\Shaders\\PS");

			DirectoryHelper.Rename($"{path}\\UI\\Wpf\\NoGlowProgressBar.cs", $"{path}\\UI\\Wpf\\NoGlowProgressBar.xaml.cs");
			DirectoryHelper.Rename($"{path}\\progressbars\\noglowprogressbar.xaml", $"{path}\\UI\\Wpf\\NoGlowProgressBar.xaml");

			Directory.CreateDirectory($"{path}\\UI\\Wpf\\StatusLightExpander\\Themes");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\StatusLightExpander\\GenericResourceDictionary.xaml", $"{path}\\UI\\Wpf\\StatusLightExpander\\Themes\\Generic.xaml");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\StatusLightExpander\\GenericResourceDictionary.xaml.cs", $"{path}\\UI\\Wpf\\StatusLightExpander\\Themes\\Generic.xaml.cs");

			Directory.CreateDirectory($"{path}\\Themes");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\GenericResourceDictionary.xaml", $"{path}\\Themes\\Generic.xaml");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\GenericResourceDictionary.xaml.cs", $"{path}\\Themes\\Generic.xaml.cs");

			Directory.CreateDirectory($"{path}\\Themes\\Resources");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\ButtonResources.xaml", $"{path}\\Themes\\Resources\\ButtonResources.xaml");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\ButtonResources.xaml.cs", $"{path}\\Themes\\Resources\\ButtonResources.xaml.cs");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\ColorResources.xaml", $"{path}\\Themes\\Resources\\ColorResources.xaml");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\ColorResources.xaml.cs", $"{path}\\Themes\\Resources\\ColorResources.xaml.cs");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\IconResources.xaml", $"{path}\\Themes\\Resources\\IconResources.xaml");
			DirectoryHelper.MoveFiles($"{path}\\UI\\Wpf\\IconResources.xaml.cs", $"{path}\\Themes\\Resources\\IconResources.xaml.cs");

			Directory.Delete($"{path}\\Corinth", true);
			Directory.Delete($"{path}\\progressbars", true);
		}

		public static void CleanupCorinthPerforce(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\P4", $"{path}");

			Directory.Delete($"{path}\\Corinth", true);
		}

		public static void CleanupCorinthSchema(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\Schema", $"{path}");

			Directory.Delete($"{path}\\Corinth", true);
		}

		public static void CleanupCorinthSchemaBlam(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\Schema\\Blam", $"{path}");

			Directory.Delete($"{path}\\Corinth", true);
		}

		public static void CleanupCorinthSourceDepot(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\SourceDepot", $"{path}");
			DirectoryHelper.Rename($"{path}\\icons", $"{path}\\Icons");

			Directory.Delete($"{path}\\Corinth", true);
		}

		public static void CleanupCorinthXbox(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\Utilities", $"{path}\\Utilities");
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\Xbox", $"{path}\\Xbox");

			Directory.Delete($"{path}\\Corinth", true);
		}

		public static void CleanupGuiPlugin(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo", $"{path}");

			Directory.CreateDirectory($"{path}\\Themes");
			DirectoryHelper.MoveFiles($"{path}\\GuiPlugin\\GenericResourceDictionary.xaml", $"{path}\\Themes\\Generic.xaml");
			DirectoryHelper.MoveFiles($"{path}\\GuiPlugin\\GenericResourceDictionary.xaml.cs", $"{path}\\Themes\\Generic.xaml.cs");

			Directory.CreateDirectory($"{path}\\GuiPlugin\\GuiDesigner\\Themes");
			DirectoryHelper.MoveFiles($"{path}\\GuiPlugin\\GuiDesigner\\GenericResourceDictionary.xaml", $"{path}\\GuiPlugin\\GuiDesigner\\Themes\\Generic.xaml");
			DirectoryHelper.MoveFiles($"{path}\\GuiPlugin\\GuiDesigner\\GenericResourceDictionary.xaml.cs", $"{path}\\GuiPlugin\\GuiDesigner\\Themes\\Generic.xaml.cs");
		}

		public static void CleanupLibrarianPlugin(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\Plugins", $"{path}");

			DirectoryHelper.Rename($"{path}\\librarian", $"{path}\\Librarian");
			DirectoryHelper.Rename($"{path}\\modelanimationgraphcustomsection", $"{path}\\ModelAnimationGraphCustomSection");
			DirectoryHelper.Rename($"{path}\\themes", $"{path}\\Themes");
			DirectoryHelper.Rename($"{path}\\Librarian\\images", $"{path}\\Librarian\\Images");

			DirectoryHelper.Rename($"{path}\\ModelAnimationGraphCustomSection\\ModelAnimationGraphSection.cs", $"{path}\\ModelAnimationGraphCustomSection\\ModelAnimationGraphSection.xaml.cs");
			DirectoryHelper.Rename($"{path}\\ModelAnimationGraphCustomSection\\modelanimationgraphsection.xaml", $"{path}\\ModelAnimationGraphCustomSection\\ModelAnimationGraphSection.xaml");
			DirectoryHelper.Rename($"{path}\\Themes\\generic.xaml", $"{path}\\Themes\\Generic.xaml");

			File.Delete($"{path}\\Properties\\Resources.cs");
			File.Delete($"{path}\\Properties\\Settings.cs");

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupNormalPlugin(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\Plugins", $"{path}");

			DirectoryHelper.Rename($"{path}\\application", $"{path}\\Application");
			DirectoryHelper.Rename($"{path}\\application\\images", $"{path}\\Application\\Images");

			DirectoryHelper.Rename($"{path}\\toolcommand", $"{path}\\ToolCommand");
			DirectoryHelper.Rename($"{path}\\toolcommand\\images", $"{path}\\ToolCommand\\Images");

			DirectoryHelper.Rename($"{path}\\help", $"{path}\\Help");

			DirectoryHelper.Rename($"{path}\\tagfilelist", $"{path}\\TagFileList");
			DirectoryHelper.Rename($"{path}\\tagfilelist\\images", $"{path}\\TagFileList\\Images");
			DirectoryHelper.Rename($"{path}\\tagfilelist\\themes", $"{path}\\TagFileList\\Themes");

			DirectoryHelper.Rename($"{path}\\TagFileList\\Themes\\generic.xaml", $"{path}\\TagFileList\\Themes\\Generic.xaml");
			DirectoryHelper.Rename($"{path}\\TagFileList\\Generic.cs", $"{path}\\TagFileList\\Themes\\Generic.xaml.cs");

			Directory.CreateDirectory($"{path}\\Themes");
			DirectoryHelper.MoveFiles($"{path}\\Bonobo\\NormalPlugin", $"{path}\\Themes");

			Directory.Delete($"{path}\\Bonobo", true);
		}

		public static void CleanupTAEShared(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\TAE", $"{path}");

			DirectoryHelper.MoveFiles($"{path}\\Shared\\Properties\\Resources.cs", $"{path}\\Properties\\Resources.Designer.cs");
			DirectoryHelper.MoveFiles($"{path}\\TAE.Shared.Properties.Resources.resx", $"{path}\\Properties\\Resources.resx");

			Directory.Delete($"{path}\\Shared\\Properties", true);
			File.Delete($"{path}\\TAE.Shared.Resources.Resources.resx");
		}

		public static void CleanupTAESharedTags(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\TAE\\Shared\\Tags", $"{path}");

			Directory.Delete($"{path}\\TAE", true);
		}

		public static void CleanupTAESharedTagsManagedBlam(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\TAE\\Shared\\Tags\\ManagedBlam", $"{path}");

			Directory.Delete($"{path}\\TAE", true);
		}

		public static void CleanupTAESharedTagsService(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\TAE\\Shared\\Tags\\Service", $"{path}");

			Directory.Delete($"{path}\\TAE", true);
		}

		public static void CleanupTAESharedTagsServiceClient(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\TAE\\Shared\\Tags\\ServiceClient", $"{path}");

			Directory.Delete($"{path}\\TAE", true);
		}

		public static void CleanupTAESharedTagsServiceHost(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\TAE\\Shared\\Tags\\ServiceHost", $"{path}");

			Directory.Delete($"{path}\\TAE", true);
		}

		public static void CleanupTagService(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\TagService", $"{path}");
			DirectoryHelper.Rename($"{path}\\app.config", $"{path}\\App.config");
		}

		public static void CleanupTagWatcher(string path)
		{
			DirectoryHelper.MoveFiles($"{path}\\Corinth\\TagWatcher", $"{path}");
			DirectoryHelper.Rename($"{path}\\app.config", $"{path}\\App.config");

			Directory.Delete($"{path}\\Corinth", true);
		}
	}
}
