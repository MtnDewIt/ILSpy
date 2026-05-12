using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

using ICSharpCode.Decompiler;
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

		static ProjectType[] projectTypes;

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
			"System.Windows.Forms",
			"System.Xaml",
			"System.Xml.Linq",
			"WindowsBase"
		};

		public void Init(AssemblyTreeModel assemblyTreeModel, LanguageService languageService, DockWorkspace dockWorkspace)
		{
			this.assemblyTreeModel = assemblyTreeModel;
			this.languageService = languageService;
			this.dockWorkspace = dockWorkspace;
		}

		public void Dump(string project, int projectIndex)
		{
			string outputPath = $"{DumperContext.ProjectDumpPath}\\{DumperContext.Projects![projectIndex]}";
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

			string projectFileName = Path.Combine(outputPath, loadedAssembly.ShortName + languageService.Language.ProjectFileExtension);

			using (var projectFileWriter = new StreamWriter(projectFileName))
			{
				var projectFileOutput = new PlainTextOutput(projectFileWriter);
				languageService.Language.DecompileAssembly(loadedAssembly, projectFileOutput, options);
			}
		}

		public void Clear()
		{
			options = null;
			metadataFile = null;
			assemblyTreeModel.AssemblyList.Clear();
		}

		public static void GenerateProjectSolution(string project)
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

			File.WriteAllText($"{DumperContext.ProjectOutputPath}\\{project}\\{project}.slnx", sb.ToString());
		}

		public static void GenerateMainSolution(string[] projects)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"<Solution>");
			sb.AppendLine($"{solutionIndent}<Configurations>");
			sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Name=\"{platform}\" />");
			sb.AppendLine($"{solutionIndent}</Configurations>");

			foreach (string project in projects)
			{
				sb.AppendLine($"{solutionIndent}<Project Path=\"{project}\\{project}.csproj\">");
				sb.AppendLine($"{solutionIndent}{solutionIndent}<Platform Project=\"{platform}\" />");
				sb.AppendLine($"{solutionIndent}</Project>");
			}

			sb.AppendLine($"</Solution>");

			File.WriteAllText($"{DumperContext.ProjectOutputPath}\\Bonobo.slnx", sb.ToString());
		}

		public static void GenerateBuildProps()
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

			File.WriteAllText($"{DumperContext.ProjectOutputPath}\\Directory.Build.props", sb.ToString());
		}

		public void GenerateProjectFile(string project)
		{
			StringBuilder sb = new StringBuilder();

			GetProjectTypes();

			string outputType = GetOutputType();

			sb.AppendLine($"<Project Sdk=\"Microsoft.NET.Sdk\">");

			sb.AppendLine($"{solutionIndent}<PropertyGroup>");

			sb.AppendLine($"{solutionIndent}{solutionIndent}<OutputType>{outputType}</OutputType>");

			// #TODO: Handle application icon
			if (false)
			{
				sb.AppendLine($"{solutionIndent}{solutionIndent}<ApplicationIcon>..\\Assets\\</ApplicationIcon>");
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

			foreach (ProjectType type in projectTypes)
			{
				switch (type)
				{
					case ProjectType.Wpf:
						sb.AppendLine($"{solutionIndent}{solutionIndent}<UseWPF>True</UseWPF>");
						break;
					case ProjectType.WinForms:
						sb.AppendLine($"{solutionIndent}{solutionIndent}<UseWindowsForms>True</UseWindowsForms>");
						break;
				}
			}

			string outputPath = DumperContext.RelativePaths.Where(x => x.Contains(project)).FirstOrDefault();

			sb.AppendLine($"{solutionIndent}{solutionIndent}<OutputPath>..\\bin\\$(Platform)\\$(Configuration)\\{Path.GetDirectoryName(outputPath)}\\</OutputPath>");

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

				if (DumperContext.Projects.All(x => !x.Contains(reference.Name)) && DumperContext.ExternalRelativePaths.All(x => !x.Contains(reference.Name)))
				{
					string dependencyName = DumperContext.Projects.Where(x => !x.StartsWith(reference.Name)).FirstOrDefault();

					if (!string.IsNullOrEmpty(dependencyName) && !externalDependencies.Contains(dependencyName))
					{
						if (!string.Equals(reference.Name, "managedblam"))
						{
							externalDependencies.Add(reference.Name);
						}
					}
				}

				if (DumperContext.ExternalRelativePaths.Any(x => x.Contains(reference.Name)))
				{
					string dependencyName = DumperContext.ExternalRelativePaths.Where(x => x.Contains(reference.Name)).FirstOrDefault();

					string dependencyPath = $"{DumperContext.BonoboPath}\\{dependencyName}";

					assemblyTreeModel.OpenFiles([dependencyPath]);
					var loadedAssembly = assemblyTreeModel.AssemblyList.FindAssembly(dependencyPath);
					MetadataFile dependencyMetadata = loadedAssembly.GetMetadataFileOrNull();

					foreach (var dependency in dependencyMetadata.AssemblyReferences)
					{
						if (DumperContext.ExternalRelativePaths.Any(x => x.Contains(dependency.Name)))
						{
							string name = DumperContext.ExternalRelativePaths.Where(x => x.Contains(dependency.Name)).FirstOrDefault();

							if (!string.IsNullOrEmpty(name) && !internalDependencies.Contains(name))
							{
								internalDependencies.Add(name);
							}
						}
					}

					if (!string.IsNullOrEmpty(dependencyName) && !internalDependencies.Contains(dependencyName))
					{
						internalDependencies.Add(dependencyName);
					}
				}
			}

			dependencies = [.. dependencies.OrderBy(Path.GetFileNameWithoutExtension)];
			references.Sort();
			externalDependencies.Sort();
			internalDependencies.Sort();

			// Parse Project DLL References (The existing plugin DLLs that we are decompiling)
			if (dependencies.Count > 0)
			{
				sb.AppendLine($"{solutionIndent}<ItemGroup>");

				foreach (var dependency in dependencies)
				{
					if (dependency.Contains("ManagedBlam.dll"))
					{
						sb.AppendLine($"{solutionIndent}{solutionIndent}<Reference Include=\"..\\Dependencies\\{dependency}\" Private=\"false\" />");
					}
					else
					{
						sb.AppendLine($"{solutionIndent}{solutionIndent}<!-- <Reference Include=\"..\\Dependencies\\{dependency}\" Private=\"false\" /> -->");
					}
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
					sb.AppendLine($"{solutionIndent}{solutionIndent}<Reference Include=\"{externalDependency}\" Private=\"false\" />");
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

			sb.AppendLine($"</Project>");

			File.WriteAllText($"{DumperContext.ProjectOutputPath}\\{project}\\{project}.csproj", sb.ToString());
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
	}
}
