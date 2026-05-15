using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpy.AssemblyTree;
using ICSharpCode.ILSpy.Docking;
using ICSharpCode.ILSpy.ViewModels;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
	public class Dumper
	{
		AssemblyTreeModel assemblyTreeModel;
		LanguageService languageService;
		DockWorkspace dockWorkspace;

		static DecompilationOptions options;
		static MetadataFile metadataFile;

		static ProjectType[] projectTypes = [];
		static List<string> embeddedResources = [];

		const int indentSize = 4;
		static readonly string indent = new(' ', indentSize);

		const int solutionIndentSize = 2;
		static readonly string solutionIndent = new(' ', solutionIndentSize);

		static readonly string platform = DumperContext.Platform.ToString();

		const string aspNetCorePrefix = "Microsoft.AspNetCore";
		const string presentationFrameworkName = "PresentationFramework";
		const string windowsFormsName = "System.Windows.Forms";

		static readonly string[] implicitReferences = new string[]
		{
			"mscorlib",
			"netstandard",
			"PresentationCore",
			"PresentationFramework",
			"System",
			"System.Core",
			"System.Diagnostics.Debug",
			"System.Diagnostics.Tools",
			"System.Drawing",
			"System.Runtime",
			"System.Runtime.Extensions",
			"System.Runtime.Serialization",
			"System.Windows.Forms",
			"System.Xaml",
			"System.Xml",
			"System.Xml.Linq",
			"WindowsBase"
		};

		public void Init(AssemblyTreeModel assemblyTreeModel, LanguageService languageService, DockWorkspace dockWorkspace)
		{
			this.assemblyTreeModel = assemblyTreeModel;
			this.languageService = languageService;
			this.dockWorkspace = dockWorkspace;
		}

		public void DumpBonoboProject(string project, int projectIndex)
		{
			string outputPath = $"{DumperContext.BonoboProjectDumpPath}\\{DumperContext.Projects![projectIndex]}";
			string projectPath = $"{DumperContext.BonoboPath}\\{DumperContext.RelativePaths[projectIndex]}";

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			assemblyTreeModel.OpenFiles([projectPath]);

			var loadedAssembly = assemblyTreeModel.AssemblyList.FindAssembly(projectPath);
			metadataFile = loadedAssembly.GetMetadataFileOrNull();

			options = dockWorkspace.ActiveTabPage.CreateDecompilationOptions();
			options.FullDecompilation = true;
			options.SaveAsProjectDirectory = outputPath;
			options.DecompilerSettings.FileScopedNamespaces = false;

			string projectFileName = Path.Combine(outputPath, loadedAssembly.ShortName + languageService.Language.ProjectFileExtension);

			using (var projectFileWriter = new StreamWriter(projectFileName))
			{
				var projectFileOutput = new PlainTextOutput(projectFileWriter);
				languageService.Language.DecompileAssembly(loadedAssembly, projectFileOutput, options);
			}

			embeddedResources = WholeProjectDecompiler.fileTable.Where(x => string.Equals(x.ItemType, "EmbeddedResource")).ToList().ConvertAll(x => x.FileName);
		}

		public void DumpManagedProject() 
		{
			string outputPath = $"{DumperContext.ManagedProjectDumpPath}";
			string projectPath = $"{DumperContext.BonoboPath}\\{DumperContext.ManagedRelativePath}";

			if (!Directory.Exists(outputPath))
			{
				Directory.CreateDirectory(outputPath);
			}

			assemblyTreeModel.OpenFiles([projectPath]);

			var loadedAssembly = assemblyTreeModel.AssemblyList.FindAssembly(projectPath);
			metadataFile = loadedAssembly.GetMetadataFileOrNull();

			options = dockWorkspace.ActiveTabPage.CreateDecompilationOptions();
			options.FullDecompilation = true;
			options.SaveAsProjectDirectory = outputPath;
			options.DecompilerSettings.FileScopedNamespaces = false;

			string projectFileName = Path.Combine(outputPath, loadedAssembly.ShortName + languageService.Language.ProjectFileExtension);

			using (var projectFileWriter = new StreamWriter(projectFileName))
			{
				var projectFileOutput = new PlainTextOutput(projectFileWriter);
				languageService.Language.DecompileAssembly(loadedAssembly, projectFileOutput, options);
			}

			embeddedResources = WholeProjectDecompiler.fileTable.Where(x => string.Equals(x.ItemType, "EmbeddedResource")).ToList().ConvertAll(x => x.FileName);
		}

		public void Clear()
		{
			options = null;
			metadataFile = null;
			embeddedResources.Clear();
			assemblyTreeModel.AssemblyList.Clear();
		}

		public static void GenerateBonoboProjectSolution(string project)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"<Solution>");
			sb.AppendLine($"{solutionIndent}<Configurations>");
			sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Name=\"{platform}\" />");
			sb.AppendLine($"{solutionIndent}</Configurations>");
			sb.AppendLine($"{solutionIndent}<Project Path=\"{project}.csproj\">");
			sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Project=\"{platform}\" />");
			sb.AppendLine($"{solutionIndent}</Project>");
			sb.AppendLine($"</Solution>");

			File.WriteAllText($"{DumperContext.BonoboProjectOutputPath}\\{project}\\{project}.slnx", sb.ToString());
		}

		public static void GenerateMainManagedSolution() 
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"<Solution>");
			sb.AppendLine($"{solutionIndent}<Configurations>");
			sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Name=\"{platform}\" />");
			sb.AppendLine($"{solutionIndent}</Configurations>");
			sb.AppendLine($"{solutionIndent}<Project Path=\"ManagedBlam.csproj\">");
			sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Project=\"{platform}\" />");
			sb.AppendLine($"{solutionIndent}</Project>");
			sb.AppendLine($"</Solution>");

			File.WriteAllText($"{DumperContext.ManagedProjectOutputPath}\\ManagedBlam.slnx", sb.ToString());
		}

		public static void GenerateMainBonoboSolution()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"<Solution>");
			sb.AppendLine($"{solutionIndent}<Configurations>");
			sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Name=\"{platform}\" />");
			sb.AppendLine($"{solutionIndent}</Configurations>");

			foreach (string project in DumperContext.Projects)
			{
				sb.AppendLine($"{solutionIndent}<Project Path=\"{project}\\{project}.csproj\">");
				sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Project=\"{platform}\" />");
				sb.AppendLine($"{solutionIndent}</Project>");
			}

			sb.AppendLine($"</Solution>");

			File.WriteAllText($"{DumperContext.BonoboProjectOutputPath}\\Bonobo.slnx", sb.ToString());
		}

		public static void GenerateBonoboBuildProps()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"<Project>");
			sb.AppendLine($"{indent}<PropertyGroup>");
			sb.AppendLine($"{indent}{indent}<Platforms>{platform}</Platforms>");
			sb.AppendLine($"{indent}{indent}<TargetFramework>net48</TargetFramework>");
			sb.AppendLine($"{indent}{indent}<AllowUnsafeBlocks>true</AllowUnsafeBlocks>");
			sb.AppendLine($"{indent}{indent}<GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
			sb.AppendLine($"{indent}{indent}<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>");
			sb.AppendLine($"{indent}{indent}<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>");
			sb.AppendLine($"{indent}</PropertyGroup>");
			sb.AppendLine($"</Project>");

			File.WriteAllText($"{DumperContext.BonoboProjectOutputPath}\\Directory.Build.props", sb.ToString());
		}

		public static void GenerateManagedBuildProps()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"<Project>");
			sb.AppendLine($"{indent}<PropertyGroup>");
			sb.AppendLine($"{indent}{indent}<Platforms>{platform}</Platforms>");
			sb.AppendLine($"{indent}{indent}<TargetFramework>net48</TargetFramework>");
			sb.AppendLine($"{indent}{indent}<AllowUnsafeBlocks>true</AllowUnsafeBlocks>");
			sb.AppendLine($"{indent}{indent}<GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
			sb.AppendLine($"{indent}{indent}<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>");
			sb.AppendLine($"{indent}{indent}<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>");
			sb.AppendLine($"{indent}</PropertyGroup>");
			sb.AppendLine($"</Project>");

			File.WriteAllText($"{DumperContext.ManagedProjectOutputPath}\\Directory.Build.props", sb.ToString());
		}

		public void GenerateBonoboProjectFile(string project)
		{
			StringBuilder sb = new StringBuilder();

			GetProjectTypes();

			string outputType = GetOutputType();

			sb.AppendLine($"<Project Sdk=\"Microsoft.NET.Sdk\">");

			sb.AppendLine($"{solutionIndent}<PropertyGroup>");

			sb.AppendLine($"{solutionIndent}{solutionIndent}<OutputType>{outputType}</OutputType>");

			if (embeddedResources.Any(x => x.EndsWith(".ico")))
			{
				string iconName = embeddedResources.Where(x => x.EndsWith(".ico")).FirstOrDefault();

				sb.AppendLine($"{solutionIndent}{solutionIndent}<ApplicationIcon>{FilterResource(iconName)}</ApplicationIcon>");
			}

			switch (project)
			{
				// #TODO: Would love to be able to check if we have multiple main entry points,
				// however that might require a lot more work (track Main function references :/)
				case "Bonobo":
					sb.AppendLine($"{solutionIndent}{solutionIndent}<StartupObject>Bonobo.Application.Program</StartupObject>");
					break;
				case "BonoboConsole":
					sb.AppendLine($"{solutionIndent}{solutionIndent}<StartupObject>BonoboConsole.Program</StartupObject>");
					break;
			}

			if (projectTypes.Any(x => x == ProjectType.Wpf))
			{
				sb.AppendLine($"{solutionIndent}{solutionIndent}<UseWPF>True</UseWPF>");
			}

			if (projectTypes.Any(x => x == ProjectType.WinForms))
			{
				sb.AppendLine($"{solutionIndent}{solutionIndent}<UseWindowsForms>True</UseWindowsForms>");
			}

			string outputPath = DumperContext.RelativePaths.Where(x => x.Contains(DumperContext.BuildInfo.FilterRelativePath(project))).FirstOrDefault();

			outputPath = Path.GetDirectoryName(outputPath);

			if (!string.IsNullOrEmpty(outputPath))
			{
				outputPath += "\\";
				outputPath = outputPath.Replace("Foundation", "Bonobo");
			}

			sb.AppendLine($"{solutionIndent}{solutionIndent}<OutputPath>..\\bin\\$(Platform)\\$(Configuration)\\{outputPath}</OutputPath>");

			outputPath = string.Empty;

			sb.AppendLine($"{solutionIndent}</PropertyGroup>");

			List<string> dependencies = [];
			List<string> references = [];
			List<string> externalDependencies = [];
			List<string> internalDependencies = [];

			foreach (var reference in metadataFile.AssemblyReferences.Where(r => !implicitReferences.Contains(r.Name)))
			{
				if (string.Equals(reference.Name, "managedblam") && !dependencies.Contains(reference.Name))
				{
					dependencies.Add("bin\\ManagedBlam.dll");
				}

				if (string.Equals(reference.Name, "Foundation") && !references.Contains("Bonobo"))
				{
					references.Add("Bonobo");
				}

				if (DumperContext.Projects.Any(x => x.Contains(reference.Name)))
				{
					string projectName = DumperContext.Projects.Where(x => x.StartsWith(reference.Name)).FirstOrDefault();
					string relativePath = DumperContext.RelativePaths.Where(x => x.Contains(reference.Name)).FirstOrDefault();

					if (!string.IsNullOrEmpty(projectName) && !references.Contains(projectName))
					{
						references.Add(reference.Name);
					}

					if (!string.IsNullOrEmpty(relativePath) && !dependencies.Contains(relativePath))
					{
						dependencies.Add(relativePath);
					}
				}

				if (DumperContext.ExternalRelativePaths.All(x => !x.Contains(reference.Name)) && DumperContext.Projects.All(x => !x.Contains(reference.Name)))
				{
					string dependencyName = DumperContext.Projects.Where(x => !x.StartsWith(reference.Name)).FirstOrDefault();

					if (!string.IsNullOrEmpty(dependencyName) && !externalDependencies.Contains(dependencyName))
					{
						if (!string.Equals(reference.Name, "managedblam") && !string.Equals(reference.Name, "Foundation"))
						{
							externalDependencies.Add(reference.Name);
						}
					}
				}

				if (DumperContext.ExternalRelativePaths.Any(x => x.Contains($"{reference.Name}.dll")) && DumperContext.Projects.All(x => !x.Contains(reference.Name)))
				{
					string dependencyName = DumperContext.ExternalRelativePaths.Where(x => x.Contains($"{reference.Name}.dll")).FirstOrDefault();

					string dependencyPath = $"{DumperContext.BonoboPath}\\{dependencyName}";

					assemblyTreeModel.OpenFiles([dependencyPath]);
					var loadedAssembly = assemblyTreeModel.AssemblyList.FindAssembly(dependencyPath);
					MetadataFile dependencyMetadata = loadedAssembly.GetMetadataFileOrNull();

					foreach (var dependency in dependencyMetadata.AssemblyReferences)
					{
						if (DumperContext.ExternalRelativePaths.Any(x => x.Contains($"{dependency.Name}.dll")) && DumperContext.Projects.All(x => !x.Contains(dependency.Name)))
						{
							string name = DumperContext.ExternalRelativePaths.Where(x => x.Contains($"{dependency.Name}.dll")).FirstOrDefault();

							// System.CoreEx is never explicitly referenced in any project, so we don't need to include it
							if (!string.IsNullOrEmpty(name) && !internalDependencies.Contains(name) && !name.Contains("System.CoreEx"))
							{
								internalDependencies.Add(name);
							}
						}
					}

					// System.CoreEx is never explicitly referenced in any project, so we don't need to include it
					if (!string.IsNullOrEmpty(dependencyName) && !internalDependencies.Contains(dependencyName) && !dependencyName.Contains("System.CoreEx"))
					{
						internalDependencies.Add(dependencyName);
					}
				}
			}

			dependencies = [.. dependencies.OrderBy(Path.GetFileNameWithoutExtension)];
			references.Sort();
			externalDependencies.Sort();
			internalDependencies.Sort();
			embeddedResources.Sort();

			// Parse Project DLL References (The existing plugin DLLs that we are decompiling)
			if (dependencies.Count > 0)
			{
				sb.AppendLine($"{solutionIndent}<ItemGroup>");

				foreach (var dependency in dependencies.Where(x => !x.Contains("\\ManagedBlam.dll")))
				{
					sb.AppendLine($"{solutionIndent}{solutionIndent}<!-- <Reference Include=\"..\\Dependencies\\{dependency}\" Private=\"false\" /> -->");
				}

				if (dependencies.Any(x => x.Contains("\\ManagedBlam.dll")))
				{
					var dependency = dependencies.Where(x => x.Contains("\\ManagedBlam.dll")).FirstOrDefault();

					sb.AppendLine($"{solutionIndent}{solutionIndent}<Reference Include=\"..\\Dependencies\\{dependency}\" Private=\"false\" />");
				}

				sb.AppendLine($"{solutionIndent}</ItemGroup>");
			}

			// Parse Project References (The decompiled plugin projects)
			if (references.Count > 0)
			{
				sb.AppendLine($"{solutionIndent}<!-- #TODO: Replace static dll references with project references -->");

				sb.AppendLine($"{solutionIndent}<ItemGroup>");

				foreach (var reference in references)
				{
					sb.AppendLine($"{solutionIndent}{solutionIndent}<ProjectReference Include=\"..\\{reference}\\{reference}.csproj\" Private=\"false\" />");
				}

				sb.AppendLine($"{solutionIndent}</ItemGroup>");
			}

			// Parse External Dependencies (Libraries that are used inside of the code, but aren't part of bonobo)
			if (externalDependencies.Count > 0)
			{
				sb.AppendLine($"{solutionIndent}<ItemGroup>");

				foreach (var externalDependency in externalDependencies)
				{
					if (externalDependency.Contains("Corinth.Farm.ServiceContracts"))
					{
						sb.AppendLine($"{solutionIndent}{solutionIndent}<!-- <Reference Include=\"{externalDependency}\" Private=\"false\" /> -->");
					}
					else
					{
						sb.AppendLine($"{solutionIndent}{solutionIndent}<Reference Include=\"{externalDependency}\" Private=\"false\" />");
					}
				}

				sb.AppendLine($"{solutionIndent}</ItemGroup>");
			}

			// Parse Internal Dependencies (Libraries that are used inside of the code, that are a part of bonobo)
			if (internalDependencies.Count > 0)
			{
				sb.AppendLine($"{solutionIndent}<ItemGroup>");

				foreach (var internalDependency in internalDependencies)
				{
					sb.AppendLine($"{solutionIndent}{solutionIndent}<Reference Include=\"..\\Dependencies\\{internalDependency}\" Private=\"false\" />");
				}

				sb.AppendLine($"{solutionIndent}</ItemGroup>");
			}

			// Parse Embedded Resources (Files and resources referenced inside of the project)
			if (embeddedResources.Count > 0)
			{
				if (embeddedResources.All(x => !x.EndsWith(".resx")))
				{
					sb.AppendLine($"{solutionIndent}<ItemGroup>");

					foreach (var embeddedResource in embeddedResources)
					{
						if (!embeddedResource.EndsWith(".resx"))
						{
							sb.AppendLine($"{solutionIndent}{solutionIndent}<Resource Include=\"{FilterResource(embeddedResource)}\" />");
						}
					}

					sb.AppendLine($"{solutionIndent}</ItemGroup>");
				}
			}

			sb.AppendLine($"</Project>");

			File.WriteAllText($"{DumperContext.BonoboProjectOutputPath}\\{project}\\{project}.csproj", sb.ToString());
		}

		public void GenerateManagedProjectFile() 
		{
			
		}

		public static void GetProjectTypes()
		{
			List<ProjectType> types = [];

			foreach (var referenceName in metadataFile.AssemblyReferences.Select(r => r.Name))
			{
				if (referenceName.StartsWith(aspNetCorePrefix, StringComparison.Ordinal))
				{
					types.Add(ProjectType.Web);
				}

				if (referenceName == presentationFrameworkName)
				{
					types.Add(ProjectType.Wpf);
				}

				if (referenceName == windowsFormsName)
				{
					types.Add(ProjectType.WinForms);
				}
			}

			types.Add(ProjectType.Default);

			projectTypes = [.. types];
		}

		public static string GetOutputType()
		{
			Subsystem moduleSubsystem = Subsystem.Unknown;
			bool isDll = true;

			if (metadataFile is PEFile { Reader.PEHeaders: var headers })
			{
				isDll = headers.IsDll;
				moduleSubsystem = headers.PEHeader.Subsystem;
			}

			if (!isDll)
			{
				switch (moduleSubsystem)
				{
					case Subsystem.WindowsGui:
						return "WinExe";
					case Subsystem.WindowsCui:
						return "Exe";
				}
			}

			return "Library";
		}

		public static string FilterResource(string resource) 
		{
			if (resource.Contains("bonobo.ico"))
			{
				resource = resource.Replace("bonobo.ico", "Bonobo.ico");
				return $"..\\Assets\\Bonobo\\{Path.GetFileName(resource)}";
			}

			if (resource.Contains("splash.png"))
			{
				resource = resource.Replace("splash.png", "BonoboSplash.png");
				return $"..\\Assets\\Bonobo\\{Path.GetFileName(resource)}";
			}

			return $"{Path.GetDirectoryName(resource).PathToPascalCase()}\\{Path.GetFileName(resource)}";
		}
	}
}
