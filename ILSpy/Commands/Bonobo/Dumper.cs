using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		static MetadataFile metadataFile;

		static readonly int indentSize = 4;
		static readonly string indent = new(' ', indentSize);

		static readonly int solutionIndentSize = 2;
		static readonly string solutionIndent = new(' ', solutionIndentSize);

		static readonly string platform = DumperContext.Platform.ToString();

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

			string projectFileName = Path.Combine(outputPath, loadedAssembly.ShortName + languageService.Language.ProjectFileExtension);

			var options = dockWorkspace.ActiveTabPage.CreateDecompilationOptions();
			options.FullDecompilation = true;
			options.SaveAsProjectDirectory = outputPath;

			using (var projectFileWriter = new StreamWriter(projectFileName))
			{
				var projectFileOutput = new PlainTextOutput(projectFileWriter);
				languageService.Language.DecompileAssembly(loadedAssembly, projectFileOutput, options);
			}

			assemblyTreeModel.AssemblyList.Clear();
		}

		public void Clear() 
		{
			metadataFile = null;
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

		public static void GenerateProjectFile(string project) 
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"<Project Sdk=\"Microsoft.NET.Sdk\">");

			sb.AppendLine($"{indent}<PropertyGroup>");
			sb.AppendLine($"{indent}</PropertyGroup>");

			sb.AppendLine($"{indent}<ItemGroup>");

			List<string> dependencies = [];

			foreach (var reference in metadataFile.AssemblyReferences.Where(r => !implicitReferences.Contains(r.Name)))
			{
				if (DumperContext.Projects.Any(x => x.Contains(reference.Name)))
				{
					string relativePath = DumperContext.RelativePaths.Where(x => x.Contains(reference.Name)).FirstOrDefault();

					if (!string.IsNullOrEmpty(relativePath) && !dependencies.Contains(relativePath))
					{
						dependencies.Add(relativePath);
					}
				}
			}

			dependencies = [.. dependencies.OrderBy(Path.GetFileNameWithoutExtension)];

			foreach (var dependency in dependencies)
			{
				sb.AppendLine($"{indent}{indent}<!-- <Reference Include=\"..\\Dependencies\\{dependency}\" Private=\"false\" /> -->");
			}

			sb.AppendLine($"{indent}</ItemGroup>");

			sb.AppendLine($"{indent}<!-- #TODO: Replace static dll references with project references -->");

			sb.AppendLine($"{indent}<ItemGroup>");

			List<string> references = [];

			foreach (var reference in metadataFile.AssemblyReferences.Where(r => !implicitReferences.Contains(r.Name)))
			{
				if (DumperContext.Projects.Any(x => x.Contains(reference.Name)))
				{
					string projectName = DumperContext.Projects.Where(x => x.StartsWith(reference.Name)).FirstOrDefault();

					if (!string.IsNullOrEmpty(projectName) && !references.Contains(projectName))
					{
						references.Add(reference.Name);
					}
				}
			}

			references.Sort();

			foreach (var reference in references)
			{
				sb.AppendLine($"{indent}{indent}<ProjectReference Include=\"..\\{reference}\\{reference}.csproj\" Private=\"false\" />");
			}

			sb.AppendLine($"{indent}</ItemGroup>");

			sb.AppendLine($"{indent}<ItemGroup>");

			List<string> externalDependencies = [];
			
			foreach (var reference in metadataFile.AssemblyReferences.Where(r => !implicitReferences.Contains(r.Name)))
			{
				if (DumperContext.Projects.All(x => !x.Contains(reference.Name)))
				{
					string projectName = DumperContext.Projects.Where(x => !x.StartsWith(reference.Name)).FirstOrDefault();
			
					if (!string.IsNullOrEmpty(projectName) && !externalDependencies.Contains(projectName))
					{
						externalDependencies.Add(reference.Name);
					}
				}
			}
			
			externalDependencies.Sort();
			
			foreach (var externalDependency in externalDependencies)
			{
				sb.AppendLine($"{indent}{indent}<Reference Include=\"{externalDependency}\" Private=\"false\" />");
			}

			sb.AppendLine($"{indent}</ItemGroup>");

			sb.AppendLine($"{indent}<ItemGroup>");

			//List<string> internalDependencies = [];
			//
			//foreach (var reference in metadataFile.AssemblyReferences.Where(r => !implicitReferences.Contains(r.Name)))
			//{
			//	if (DumperContext.Projects.All(x => !x.Contains(reference.Name)))
			//	{
			//		string projectName = DumperContext.Projects.Where(x => !x.StartsWith(reference.Name)).FirstOrDefault();
			//
			//		if (!string.IsNullOrEmpty(projectName) && !internalDependencies.Contains(projectName))
			//		{
			//			internalDependencies.Add(reference.Name);
			//		}
			//	}
			//}
			//
			//internalDependencies.Sort();
			//
			//foreach (var internalDependency in internalDependencies)
			//{
			//	sb.AppendLine($"{indent}{indent}");
			//}

			sb.AppendLine($"{indent}</ItemGroup>");

			sb.AppendLine($"</Project>");

			File.WriteAllText($"{DumperContext.ProjectOutputPath}\\{project}\\{project}.csproj", sb.ToString());
		}
	}
}
