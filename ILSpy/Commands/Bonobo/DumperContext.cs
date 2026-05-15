using System;
using System.IO;
using System.Windows;

using ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
    public static class DumperContext
    {
		public static BuildType Build { get; set; } = BuildType.Invalid;
		public static PlatformType Platform { get; set; } = PlatformType.Invalid;
        public static IBuildInfo BuildInfo { get; private set; }

        public static string OutputPath { get; set; }
        public static string BonoboPath { get; set; }
        public static string BonoboProjectDumpPath { get; set; }
        public static string BonoboProjectOutputPath { get; set; }
		public static string BonoboProjectDependenciesPath { get; set; }

		public static string ManagedRelativePath { get; set; }
		public static string ManagedProjectDumpPath { get; set; }
		public static string ManagedProjectOutputPath { get; set; }

		public static string[] Projects { get; set; }
        public static string[] RelativePaths { get; set; }
        public static string[] XMLRelativePaths { get; set; }
		public static string[] ExternalRelativePaths { get; set; }

        public static bool Init(SettingsService settings) 
        {
            Build = settings.BonoboDumperSettings.Build;
			Platform = settings.BonoboDumperSettings.Platform;
			OutputPath = settings.BonoboDumperSettings.OutputPath;
            BonoboPath = settings.BonoboDumperSettings.BonoboPath;

			if (Build == BuildType.Invalid)
			{
				MessageBox.Show("Build was invalid!");
				return false;
			}

            if (!Directory.Exists(OutputPath)) 
            {
				MessageBox.Show("OutputPath does not exist!");
                return false;
            }

            if (!Directory.Exists(BonoboPath)) 
            {
				MessageBox.Show("BonoboPath does not exist!");
                return false;
            }

            InitializeBuildInfo();

            Projects = BuildInfo?.GetProjects();
            RelativePaths = BuildInfo?.GetRelativePaths();
            XMLRelativePaths = BuildInfo?.GetXMLRelativePaths();
			ExternalRelativePaths = BuildInfo?.GetExternalRelativePaths();
			ManagedRelativePath = BuildInfo?.GetManagedRelativePath();

			BonoboProjectDumpPath = $"{OutputPath}\\{Build}\\Bonobo\\Dump";
            BonoboProjectOutputPath = $"{OutputPath}\\{Build}\\Bonobo\\Output";
			BonoboProjectDependenciesPath = $"{OutputPath}\\{Build}\\Bonobo\\Output\\Dependencies";

			ManagedProjectDumpPath = $"{OutputPath}\\{Build}\\ManagedBlam\\Dump";
			ManagedProjectOutputPath = $"{OutputPath}\\{Build}\\ManagedBlam\\Output";

			return true;
        }

        public static void InitializeBuildInfo()
        {
            BuildInfo = Build switch
            {
                BuildType.Omaha => new OmahaInfo(),
                BuildType.Midnight => new MidnightInfo(),
                BuildType.Groundhog => new GroundhogInfo(),
                _ => throw new InvalidOperationException($"Unsupported build type: {Build}"),
            };
        }

		public static void ValidateBonoboDumpPath()
		{
			if (!Directory.Exists(BonoboProjectDumpPath))
			{
				Directory.CreateDirectory(BonoboProjectDumpPath);
			}
		}

		public static void ValidateBonoboOutputPath()
		{
			if (!Directory.Exists(BonoboProjectOutputPath))
			{
				Directory.CreateDirectory(BonoboProjectOutputPath);
			}
		}

		public static void ValidateBonoboDependenciesPath()
		{
			if (!Directory.Exists(BonoboProjectDependenciesPath))
			{
				Directory.CreateDirectory(BonoboProjectDependenciesPath);
			}
		}

		public static void ValidateManagedDumpPath() 
		{
			if (!Directory.Exists(ManagedProjectDumpPath))
			{
				Directory.CreateDirectory(ManagedProjectDumpPath);
			}
		}

		public static void ValidateManagedOutputPath() 
		{
			if (!Directory.Exists(ManagedProjectOutputPath))
			{
				Directory.CreateDirectory(ManagedProjectOutputPath);
			}
		}
	}
}
