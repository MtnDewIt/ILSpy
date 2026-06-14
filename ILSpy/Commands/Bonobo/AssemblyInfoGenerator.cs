using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
	public class AssemblyInfoGenerator
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

		public static string? ProjectName;

		public DumperContext Context;

		public AssemblyInfoGenerator(DumperContext context) 
		{
			Context = context;
		}

		public void BonoboInit(string? project)
		{
			ProjectName = project;

			int projectIndex = Context.Projects.IndexOf(project);
			string projectPath = $"{Context.BonoboPath}\\{Context.RelativePaths[projectIndex]}";

			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(projectPath);

			MajorVersion = (uint)versionInfo.FileMajorPart;
			MinorVersion = (uint)versionInfo.FileMinorPart;
			BuildVersion = (uint)versionInfo.FileBuildPart;
			PrivateVersion = (uint)versionInfo.FilePrivatePart;

			string[] data = File.ReadAllLines($"{Context.BonoboProjectDumpPath}\\{project}\\Properties\\AssemblyInfo.cs");

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

		public void GenerateBonoboAssemblyInfo(string? project)
		{
			string path = $"{Context.BonoboProjectOutputPath}\\{project}\\Properties\\AssemblyInfo.cs";
			string? directory = Path.GetDirectoryName(path);

			StringBuilder sb = new StringBuilder();

			foreach (string attribute in UsingAttributes)
			{
				sb.AppendLine(attribute);
			}

			sb.AppendLine();
			
			string? visible = ComAttributes.Where(x => x.StartsWith("[assembly: ComVisible")).FirstOrDefault();
			string? theme = ThemeAttributes.Where(x => x.StartsWith("[assembly: ThemeInfo")).FirstOrDefault();

			ParseTitle(sb);
			ParseDescription(sb);
			ParseConfiguration(sb);
			ParseCompany(sb);
			ParseProduct(sb);
			ParseCopyright(sb);
			ParseTrademark(sb);
			ParseCulture(sb);

			if (!string.IsNullOrEmpty(visible))
			{
				sb.AppendLine();
				sb.AppendLine(visible);
				sb.AppendLine();
			}

			ParseVersion(sb);
			ParseFileVersion(sb);

			if (!string.IsNullOrEmpty(theme))
			{
				sb.AppendLine();
				sb.AppendLine(theme);
				sb.AppendLine();
			}

			if (ContentAttributes.Count > 0)
			{
				foreach (string attribute in ContentAttributes)
				{
					sb.AppendLine(attribute);
				}

				sb.AppendLine();
			}

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory!);
			}

			File.WriteAllText(path, sb.ToString());
		}

		public void ManagedInit() 
		{
			ProjectName = "ManagedBlam";

			string projectPath = $"{Context.BonoboPath}\\{Context.ManagedRelativePath}";

			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(projectPath);

			MajorVersion = (uint)versionInfo.FileMajorPart;
			MinorVersion = (uint)versionInfo.FileMinorPart;
			BuildVersion = (uint)versionInfo.FileBuildPart;
			PrivateVersion = (uint)versionInfo.FilePrivatePart;

			string[] data = File.ReadAllLines($"{Context.ManagedProjectDumpPath}\\Properties\\AssemblyInfo.cs");

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

		public void GenerateManagedAssemblyInfo() 
		{
			string path = $"{Context.ManagedProjectOutputPath}\\ManagedBlam\\Properties\\AssemblyInfo.cs";
			string? directory = Path.GetDirectoryName(path);

			StringBuilder sb = new StringBuilder();

			foreach (string attribute in UsingAttributes)
			{
				sb.AppendLine(attribute);
			}

			sb.AppendLine();

			string? visible = ComAttributes.Where(x => x.StartsWith("[assembly: ComVisible")).FirstOrDefault();
			string? theme = ThemeAttributes.Where(x => x.StartsWith("[assembly: ThemeInfo")).FirstOrDefault();

			ParseTitle(sb);
			ParseDescription(sb);
			ParseConfiguration(sb);
			ParseCompany(sb);
			ParseProduct(sb);
			ParseCopyright(sb);
			ParseTrademark(sb);
			ParseCulture(sb);

			if (!string.IsNullOrEmpty(visible))
			{
				sb.AppendLine();
				sb.AppendLine(visible);
				sb.AppendLine();
			}

			ParseVersion(sb);
			ParseFileVersion(sb);

			if (!string.IsNullOrEmpty(theme))
			{
				sb.AppendLine();
				sb.AppendLine(theme);
				sb.AppendLine();
			}

			if (ContentAttributes.Count > 0)
			{
				foreach (string attribute in ContentAttributes)
				{
					sb.AppendLine(attribute);
				}

				sb.AppendLine();
			}

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory!);
			}

			File.WriteAllText(path, sb.ToString());
		}

		public static void ParseTitle(StringBuilder sb)
		{
			string? title = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyTitle")).FirstOrDefault();

			if (string.IsNullOrEmpty(title))
			{
				title = $"[assembly: AssemblyTitle(\"{ProjectName}\")]";
			}

			if (!string.IsNullOrEmpty(title))
			{
				sb.AppendLine(title);
			}
		}

		public static void ParseDescription(StringBuilder sb)
		{
			string? description = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyDescription")).FirstOrDefault();

			if (string.IsNullOrEmpty(description))
			{
				description = $"[assembly: AssemblyDescription(\"\")]";
			}

			if (!string.IsNullOrEmpty(description))
			{
				sb.AppendLine(description);
			}
		}

		public static void ParseConfiguration(StringBuilder sb)
		{
			string? configuration = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyConfiguration")).FirstOrDefault();

			if (string.IsNullOrEmpty(configuration))
			{
				configuration = $"[assembly: AssemblyConfiguration(\"\")]";
			}

			if (!string.IsNullOrEmpty(configuration))
			{
				sb.AppendLine(configuration);
			}
		}

		public static void ParseCompany(StringBuilder sb)
		{
			string? company = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyCompany")).FirstOrDefault();

			if (string.IsNullOrEmpty(company))
			{
				company = $"[assembly: AssemblyCompany(\"\")]";
			}

			if (!string.IsNullOrEmpty(company))
			{
				sb.AppendLine(company);
			}
		}

		public static void ParseProduct(StringBuilder sb)
		{
			string? product = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyProduct")).FirstOrDefault();

			if (string.IsNullOrEmpty(product))
			{
				product = $"[assembly: AssemblyProduct(\"{ProjectName}\")]";
			}

			if (!string.IsNullOrEmpty(product))
			{
				sb.AppendLine(product);
			}
		}

		public static void ParseCopyright(StringBuilder sb)
		{
			string? copyright = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyCopyright")).FirstOrDefault();

			if (string.IsNullOrEmpty(copyright))
			{
				copyright = $"[assembly: AssemblyCopyright(\"Copyright © {DateTime.Now.Year}\")]";
			}

			if (!string.IsNullOrEmpty(copyright))
			{
				sb.AppendLine(copyright);
			}
		}

		public static void ParseTrademark(StringBuilder sb)
		{
			string? trademark = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyTrademark")).FirstOrDefault();

			if (string.IsNullOrEmpty(trademark))
			{
				trademark = $"[assembly: AssemblyTrademark(\"HaloMods ™ {DateTime.Now.Year}\")]";
			}

			if (!string.IsNullOrEmpty(trademark))
			{
				sb.AppendLine(trademark);
			}
		}

		public static void ParseCulture(StringBuilder sb)
		{
			string? culture = AssemblyAttributes.Where(x => x.StartsWith("[assembly: AssemblyCulture")).FirstOrDefault();

			if (string.IsNullOrEmpty(culture))
			{
				culture = $"[assembly: AssemblyCulture(\"\")]";
			}

			if (!string.IsNullOrEmpty(culture))
			{
				sb.AppendLine(culture);
			}
		}

		public static void ParseVersion(StringBuilder sb) 
		{
			string? version = VersionAttributes.Where(x => x.StartsWith("[assembly: AssemblyVersion")).FirstOrDefault();

			if (string.IsNullOrEmpty(version))
			{
				version = $"[assembly: AssemblyVersion(\"{MajorVersion}.{MinorVersion}.{BuildVersion}.{PrivateVersion}\")]";
			}

			if (!string.IsNullOrEmpty(version))
			{
				sb.AppendLine(version);
			}
		}

		public static void ParseFileVersion(StringBuilder sb)
		{
			string? fileVersion = VersionAttributes.Where(x => x.StartsWith("[assembly: AssemblyFileVersion")).FirstOrDefault();

			if (string.IsNullOrEmpty(fileVersion))
			{
				fileVersion = $"[assembly: AssemblyFileVersion(\"{MajorVersion}.{MinorVersion}.{BuildVersion}.{PrivateVersion}\")]";
			}

			if (!string.IsNullOrEmpty(fileVersion))
			{
				sb.AppendLine(fileVersion);
			}
		}
	}
}
