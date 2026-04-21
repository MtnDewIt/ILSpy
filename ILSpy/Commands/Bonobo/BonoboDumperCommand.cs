using System.Composition;
using System.IO;

using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AssemblyTree;
using ICSharpCode.ILSpy.Docking;
using ICSharpCode.ILSpy.Properties;
using ICSharpCode.ILSpy.ViewModels;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
	[ExportToolbarCommand(ToolTip = nameof(Resources.BonoboDumper), ToolbarIcon = "Images/Open", ToolbarCategory = nameof(Resources.Open), ToolbarOrder = 0)]
	[ExportMainMenuCommand(ParentMenuID = nameof(Resources._File), Header = nameof(Resources._BonoboDumper), MenuIcon = "Images/Open", MenuCategory = nameof(Resources.BonoboDumper), MenuOrder = 0)]
	[Shared]
	sealed class BonoboDumperCommand : SimpleCommand
	{
		private readonly AssemblyTreeModel assemblyTreeModel;
		private readonly LanguageService languageService;
		private readonly DockWorkspace dockWorkspace;

		[ImportingConstructor]
		public BonoboDumperCommand(AssemblyTreeModel assemblyTreeModel, LanguageService languageService, DockWorkspace dockWorkspace)
		{
			this.assemblyTreeModel = assemblyTreeModel;
			this.languageService = languageService;
			this.dockWorkspace = dockWorkspace;
		}

		public override async void Execute(object parameter)
		{
			string inputAssembly = @"D:\SteamLibrary\steamapps\common\HREK\Foundation.exe";
			string outputDirectory = @"F:\ILSPY_TEST\Bonobo";

			assemblyTreeModel.OpenFiles([inputAssembly]);

			var loadedAssembly = assemblyTreeModel.AssemblyList.FindAssembly(inputAssembly);
			string projectFileName = Path.Combine(outputDirectory, loadedAssembly.ShortName + languageService.Language.ProjectFileExtension);

			var options = dockWorkspace.ActiveTabPage.CreateDecompilationOptions();
			options.FullDecompilation = true;
			options.SaveAsProjectDirectory = outputDirectory;

			using (var projectFileWriter = new StreamWriter(projectFileName))
			{
				var projectFileOutput = new PlainTextOutput(projectFileWriter);
				languageService.Language.DecompileAssembly(loadedAssembly, projectFileOutput, options);
			}

			assemblyTreeModel.AssemblyList.Clear();
		}
	}
}
