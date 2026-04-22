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
        public static string ProjectDumpPath { get; set; }
        public static string ProjectOutputPath { get; set; }
		public static string ProjectDependenciesPath { get; set; }

        public static string[] Projects { get; set; }
        public static string[] RelativePaths { get; set; }
        public static string[] XMLRelativePaths { get; set; }

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

            ProjectDumpPath = $"{OutputPath}\\Bonobo-{Build}\\Dump";
            ProjectOutputPath = $"{OutputPath}\\Bonobo-{Build}\\Output";
			ProjectDependenciesPath = $"{OutputPath}\\Bonobo-{Build}\\Output\\Dependencies";

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

		public static void ValidateProjectDumpPath()
		{
			if (!Directory.Exists(ProjectDumpPath))
			{
				Directory.CreateDirectory(ProjectDumpPath);
			}
		}

		public static void ValidateProjectOutputPath()
		{
			if (!Directory.Exists(ProjectOutputPath))
			{
				Directory.CreateDirectory(ProjectOutputPath);
			}
		}

		public static void ValidateDependenciesPath()
		{
			if (!Directory.Exists(ProjectDependenciesPath))
			{
				Directory.CreateDirectory(ProjectDependenciesPath);
			}
		}
	}
}
