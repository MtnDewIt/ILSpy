using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using TomsToolbox.Essentials;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
	public static class AssemblyInfoGenerator
	{
		public static List<string> UsingAttributes = [];
		public static List<string> AssemblyAttributes = [];
		public static List<string> ComAttributes = [];
		public static List<string> VersionAttributes = [];
		public static List<string> ThemeAttributes = [];
		public static List<string> ContentAttributes = [];

		public static uint MajorVersion = 0;
		public static uint MinorVersion = 0;
		public static uint BuildVersion = 0;
		public static uint PrivateVersion = 0;

		public static void Init(string project) 
		{
			int projectIndex = DumperContext.Projects.IndexOf(project);
			string projectPath = $"{DumperContext.BonoboPath}\\{DumperContext.RelativePaths[projectIndex]}";

			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(projectPath);

			MajorVersion = (uint)versionInfo.FileMajorPart;
			MinorVersion = (uint)versionInfo.FileMinorPart;
			BuildVersion = (uint)versionInfo.FileBuildPart;
			PrivateVersion = (uint)versionInfo.FilePrivatePart;

			string[] data = File.ReadAllLines($"{DumperContext.ProjectDumpPath}\\{project}\\Properties\\AssemblyInfo.cs");

			UsingAttributes = [.. data.Where(x => 
				x.StartsWith("using"))];

			AssemblyAttributes = [.. data.Where(x => 
				x.StartsWith("[assembly: Assembly") && 
				!x.Contains("Version(") &&
				!x.StartsWith("[assembly: AssemblyAssociatedContentFile"))];

			ComAttributes = [.. data.Where(x => 
				x.StartsWith("[assembly: Com"))];

			VersionAttributes = [.. data.Where(x => 
				x.StartsWith("[assembly: Assembly") && 
				x.Contains("Version("))];

			ThemeAttributes = [.. data.Where(x => 
				x.StartsWith("[assembly: Theme"))];

			ContentAttributes = [.. data.Where(x => 
				x.StartsWith("[assembly: AssemblyAssociatedContentFile"))];
		}

		public static void GenerateAssemblyInfo(string project) 
		{
			string path = $"{DumperContext.ProjectOutputPath}\\{project}\\Properties\\AssemblyInfo.cs";
			string directory = Path.GetDirectoryName(path);

			StringBuilder sb = new StringBuilder();

			foreach (string attribute in UsingAttributes)
			{
				sb.AppendLine(attribute);
			}

			string title = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyTitle")).FirstOrDefault();
			string description = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyDescription")).FirstOrDefault();
			string configuration = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyConfiguration")).FirstOrDefault();
			string company = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyCompany")).FirstOrDefault();
			string product = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyProduct")).FirstOrDefault();
			string copyright = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyCopyright")).FirstOrDefault();
			string trademark = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyTrademark")).FirstOrDefault();
			string culture = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyCulture")).FirstOrDefault();

			string visible = ComAttributes.Where(x => x.StartsWith("[assembly: ComVisible")).FirstOrDefault();

			string version = VersionAttributes.Where(x => x.StartsWith("[assembly: AssemblyVersion")).FirstOrDefault();
			string fileVersion = VersionAttributes.Where(x => x.StartsWith("[assembly: AssemblyFileVersion")).FirstOrDefault();

			string theme = ThemeAttributes.Where(x => x.StartsWith("[assembly: ThemeInfo")).FirstOrDefault();

			if (ComAttributes.Count > 0)
			{
				foreach (string attribute in ContentAttributes)
				{
					sb.AppendLine(attribute);
				}

				sb.AppendLine();
			}

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			File.WriteAllText(path, sb.ToString());
		}
	}
}
