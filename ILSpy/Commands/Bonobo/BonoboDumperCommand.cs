using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;

using ICSharpCode.ILSpy.AssemblyTree;
using ICSharpCode.ILSpy.Commands.Bonobo.Extensions;
using ICSharpCode.ILSpy.Docking;
using ICSharpCode.ILSpy.Languages;
using ICSharpCode.ILSpy.Properties;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
	[ExportToolbarCommand(ToolTip = nameof(Resources.BonoboDumper), ToolbarIcon = "Images/Bonobo", ToolbarCategory = nameof(Resources.Open), ToolbarOrder = 0)]
	[ExportMainMenuCommand(ParentMenuID = nameof(Resources._File), Header = nameof(Resources._BonoboDumper), MenuIcon = "Images/Bonobo", MenuCategory = nameof(Resources.BonoboDumper), MenuOrder = 0)]
	[Shared]
	sealed class BonoboDumperCommand : SimpleCommand
	{
		private readonly AssemblyTreeModel assemblyTreeModel;
		private readonly SettingsService settingsService;
		private readonly LanguageService languageService;
		private readonly DockWorkspace dockWorkspace;

		private Dumper? dumper;

		[ImportingConstructor]
		public BonoboDumperCommand(AssemblyTreeModel assemblyTreeModel, SettingsService settingsService, LanguageService languageService, DockWorkspace dockWorkspace)
		{
			this.assemblyTreeModel = assemblyTreeModel;
			this.settingsService = settingsService;
			this.languageService = languageService;
			this.dockWorkspace = dockWorkspace;
		}

		public override void Execute(object? parameter)
		{
			Dictionary<BuildType, string> builds = RegistryHandler.FindEKPaths();

			// #TODO: MAYBE MAKE ASYNC
			foreach (var build in builds)
			{
				dumper = new Dumper();

				bool initialized = dumper.Init(build.Key, build.Value, assemblyTreeModel, settingsService, languageService, dockWorkspace);

				if (!initialized)
				{
					continue;
				}

				Dump();

				Clear();
			}
		}

		public void Clear() 
		{
			dumper = null;
		}

		public void Dump() 
		{
			if (dumper?.Context?.Build != BuildType.Forerunner && dumper?.Context?.Build != BuildType.Atlas)
			{
				DumpBonobo();
			}

			DumpManaged();
		}

		public void DumpBonobo() 
		{
			dumper?.Context?.ValidateBonoboDumpPath();
			dumper?.Context?.ValidateBonoboOutputPath();

			for (int projectIndex = 0; projectIndex < dumper?.Context?.Projects?.Length; projectIndex++)
			{
				string? project = dumper?.Context?.Projects[projectIndex];

				dumper?.DumpBonoboProject(project, projectIndex);

				string outputPath = $"{dumper?.Context?.BonoboProjectOutputPath}\\{project}";
				string dumpPath = $"{dumper?.Context?.BonoboProjectDumpPath}\\{project}";

				if (!Directory.Exists(outputPath))
				{
					Directory.CreateDirectory(outputPath);
				}

				dumper?.GenerateBonoboProjectFile(project);

				dumper?.GenerateBonoboProjectSolution(project);

				dumper?.AssemblyInfoGenerator?.BonoboInit(project);
				dumper?.AssemblyInfoGenerator?.GenerateBonoboAssemblyInfo(project);

				FilterBonoboFiles(dumpPath);

				dumper?.Context?.BuildInfo?.CleanupProjectDump(project, dumper?.Context?.BonoboProjectDumpPath);

				dumper?.Clear();
			}

			dumper?.Context?.ValidateBonoboDependenciesPath();

			for (int dependencyIndex = 0; dependencyIndex < dumper?.Context?.XMLRelativePaths?.Length; dependencyIndex++)
			{
				string source = $"{dumper?.Context?.BonoboPath}\\{dumper?.Context?.XMLRelativePaths[dependencyIndex]}";
				string destination = $"{dumper?.Context?.BonoboProjectDependenciesPath}\\{dumper?.Context?.XMLRelativePaths[dependencyIndex]}";
				
				string? directory = Path.GetDirectoryName(destination);

				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory!);
				}

				File.Copy(source, destination, true);
			}

			MigrateBonoboDump();

			dumper?.GenerateMainBonoboSolution();
			dumper?.GenerateBonoboBuildProps();
		}

		public void DumpManaged() 
		{
			dumper?.Context?.ValidateManagedDumpPath();
			dumper?.Context?.ValidateManagedOutputPath();

			dumper?.DumpManagedProject();

			string outputPath = $"{dumper?.Context?.ManagedProjectOutputPath}\\ManagedBlam";

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			dumper?.GenerateManagedProjectFile();

			dumper?.AssemblyInfoGenerator?.ManagedInit();
			dumper?.AssemblyInfoGenerator?.GenerateManagedAssemblyInfo();

			dumper?.GenerateMainManagedSolution();

			dumper?.Clear();
		}

		public void MigrateBonoboDump()
		{
			string[] allowedExtensions =
			[
				".cs",
				".xaml",
				".resx",
				".ps",
				".png",
				".jpg",
				".ico",
				".bmp",
				".config",
			];

			string[] excludedFiles = 
			[
				"AssemblyInfo.cs",
			];

			string dumpPath = $"{dumper?.Context?.BonoboProjectDumpPath}";
			string outputPath = $"{dumper?.Context?.BonoboProjectOutputPath}";

			DirectoryHelper.CopyDirectory(dumpPath, outputPath, file => 
			{
				bool matchExtension = Array.Exists(allowedExtensions, extension =>
					extension.Equals(file.Extension, StringComparison.OrdinalIgnoreCase));

				bool matchName = Array.Exists(excludedFiles, name =>
					!name.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

				return matchExtension && matchName;
			});

			Directory.CreateDirectory($"{outputPath}\\Assets\\Bonobo");
			DirectoryHelper.MoveFiles($"{outputPath}\\Bonobo\\Images\\bonobo.ico", $"{outputPath}\\Assets\\Bonobo\\Bonobo.ico");
			DirectoryHelper.MoveFiles($"{outputPath}\\Bonobo\\splash.png", $"{outputPath}\\Assets\\Bonobo\\BonoboSplash.png");

			Directory.Delete($"{outputPath}\\Bonobo\\Images", true);
			Directory.Delete(dumpPath, true);
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

					string? parentDir = Path.GetDirectoryName(xamlFile);

					string convertedName = nameWithoutExt.Replace('.', Path.DirectorySeparatorChar);

					string newBasePath = Path.Combine(parentDir!, convertedName);
					string newXamlPath = newBasePath + ".xaml";

					string codeFile = newBasePath + ".cs";
					string xamlCodeFile = newBasePath + ".xaml.cs";

					try
					{
						string? newXamlDir = Path.GetDirectoryName(newXamlPath);

						if (!Directory.Exists(newXamlDir))
						{
							Directory.CreateDirectory(newXamlDir!);
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
