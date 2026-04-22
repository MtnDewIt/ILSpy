using System.IO;
using System.Text;

using ICSharpCode.Decompiler;
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

		static readonly int indentSize = 4;
		static readonly string indent = new(' ', indentSize);

		static readonly int solutionIndentSize = 2;
		static readonly string solutionIndent = new(' ', solutionIndentSize);

		static readonly string platform = DumperContext.Platform.ToString();

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
	}
}
