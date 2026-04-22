using System.Composition;
using System.IO;

using ICSharpCode.ILSpy.AssemblyTree;
using ICSharpCode.ILSpy.Docking;
using ICSharpCode.ILSpy.Properties;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
	[ExportToolbarCommand(ToolTip = nameof(Resources.BonoboDumper), ToolbarIcon = "Images/Bonobo", ToolbarCategory = nameof(Resources.Open), ToolbarOrder = 0)]
	[ExportMainMenuCommand(ParentMenuID = nameof(Resources._File), Header = nameof(Resources._BonoboDumper), MenuIcon = "Images/Bonobo", MenuCategory = nameof(Resources.BonoboDumper), MenuOrder = 0)]
	[Shared]
	sealed class BonoboDumperCommand : SimpleCommand
	{
		private readonly SettingsService settingsService;

		private readonly Dumper dumper;

		[ImportingConstructor]
		public BonoboDumperCommand(AssemblyTreeModel assemblyTreeModel, SettingsService settingsService, LanguageService languageService, DockWorkspace dockWorkspace)
		{
			this.settingsService = settingsService;

			dumper = new Dumper();
			dumper.Init(assemblyTreeModel, languageService, dockWorkspace);
		}

		public override async void Execute(object parameter)
		{
			bool initialized = DumperContext.Init(settingsService);

			if (!initialized)
			{
				return;
			}

			DumperContext.ValidateProjectDumpPath();
			DumperContext.ValidateProjectOutputPath();

			for (int projectIndex = 0; projectIndex < DumperContext.Projects.Length; projectIndex++)
			{
				string project = DumperContext.Projects[projectIndex];

				dumper.Dump(project, projectIndex);

				string outputPath = $"{DumperContext.ProjectOutputPath}\\{project}";

				if (!Directory.Exists(outputPath))
				{
					Directory.CreateDirectory(outputPath);
				}

				Dumper.GenerateProjectSolution(project);

				AssemblyInfoGenerator.Init(project);
				AssemblyInfoGenerator.GenerateAssemblyInfo(project);

				// Filter XAML Files (They never get put in the right directory)

				// Update Namespace Handling (It always dumps its above the class declaration, instead of wrapping around it)
			}

			DumperContext.ValidateDependenciesPath();

			for (int dependencyIndex = 0; dependencyIndex < DumperContext.XMLRelativePaths.Length; dependencyIndex++)
			{
				string source = $"{DumperContext.BonoboPath}\\{DumperContext.XMLRelativePaths[dependencyIndex]}";
				string destination = $"{DumperContext.ProjectDependenciesPath}\\{DumperContext.XMLRelativePaths[dependencyIndex]}";
				string directory = Path.GetDirectoryName(destination);

				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				File.Copy(source, destination, true);
			}

			Dumper.GenerateMainSolution(DumperContext.Projects);
		}
	}
}
