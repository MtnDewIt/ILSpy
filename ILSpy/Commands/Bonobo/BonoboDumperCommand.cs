using System;
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

		public override void Execute(object parameter)
		{
			bool initialized = DumperContext.Init(settingsService);

			if (!initialized)
			{
				return;
			}

			DumpBonobo();
			DumpManaged();
		}

		public void DumpBonobo() 
		{
			DumperContext.ValidateBonoboDumpPath();
			DumperContext.ValidateBonoboOutputPath();

			for (int projectIndex = 0; projectIndex < DumperContext.Projects.Length; projectIndex++)
			{
				string project = DumperContext.Projects[projectIndex];

				dumper.DumpBonoboProject(project, projectIndex);

				string outputPath = $"{DumperContext.BonoboProjectOutputPath}\\{project}";
				string dumpPath = $"{DumperContext.BonoboProjectDumpPath}\\{project}";

				if (!Directory.Exists(outputPath))
				{
					Directory.CreateDirectory(outputPath);
				}

				dumper.GenerateBonoboProjectFile(project);

				Dumper.GenerateBonoboProjectSolution(project);

				AssemblyInfoGenerator.BonoboInit(project);
				AssemblyInfoGenerator.GenerateBonoboAssemblyInfo(project);

				// Filter XAML Files (They never get put in the right directory)
				FilterBonoboFiles(dumpPath);

				dumper.Clear();
			}

			DumperContext.ValidateBonoboDependenciesPath();

			for (int dependencyIndex = 0; dependencyIndex < DumperContext.XMLRelativePaths.Length; dependencyIndex++)
			{
				string source = $"{DumperContext.BonoboPath}\\{DumperContext.XMLRelativePaths[dependencyIndex]}";
				string destination = $"{DumperContext.BonoboProjectDependenciesPath}\\{DumperContext.XMLRelativePaths[dependencyIndex]}";
				string directory = Path.GetDirectoryName(destination);

				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				File.Copy(source, destination, true);
			}

			Dumper.GenerateMainBonoboSolution();
			Dumper.GenerateBonoboBuildProps();
		}

		public void DumpManaged() 
		{
			DumperContext.ValidateManagedDumpPath();
			DumperContext.ValidateManagedOutputPath();

			dumper.DumpManagedProject();

			string outputPath = $"{DumperContext.ManagedProjectOutputPath}\\ManagedBlam";

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			dumper.GenerateManagedProjectFile();

			AssemblyInfoGenerator.ManagedInit();
			AssemblyInfoGenerator.GenerateManagedAssemblyInfo();

			Dumper.GenerateMainManagedSolution();
			Dumper.GenerateManagedBuildProps();

			dumper.Clear();
		}

		public static void FilterBonoboFiles(string projectPath) 
		{
			try
			{
				string[] xamlFiles = Directory.GetFiles(projectPath, "*.xaml", SearchOption.AllDirectories);

				foreach (string xamlFile in xamlFiles)
				{
					string fileName = Path.GetFileName(xamlFile);
					string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
					string parentDir = Path.GetDirectoryName(xamlFile);

					string convertedName = nameWithoutExt.Replace('.', Path.DirectorySeparatorChar);

					string newBasePath = Path.Combine(parentDir, convertedName);
					string newXamlPath = newBasePath + ".xaml";

					string codeFile = newBasePath + ".cs";
					string xamlCodeFile = newBasePath + ".xaml.cs";

					try
					{
						string newXamlDir = Path.GetDirectoryName(newXamlPath);
						if (!Directory.Exists(newXamlDir))
						{
							Directory.CreateDirectory(newXamlDir);
						}

						File.Move(xamlFile, newXamlPath, true);

						if (File.Exists(codeFile))
						{
							File.Move(codeFile, xamlCodeFile, true);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("FATAL ERROR: " + ex.Message);
					}
				}

				DeleteEmptyFolders(projectPath);
			}
			catch (IOException ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		public static void DeleteEmptyFolders(string path)
		{
			if (!Directory.Exists(path))
			{
				return;
			}

			foreach (string directory in Directory.GetDirectories(path))
			{
				DeleteEmptyFolders(directory);
			}

			if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
			{
				Directory.Delete(path);
			}
		}
	}
}
