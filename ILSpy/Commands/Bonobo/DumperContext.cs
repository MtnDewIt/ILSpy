using System;
using System.IO;
using System.Windows;

using ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
    public class DumperContext
    {
		public BuildType Build { get; set; } = BuildType.Invalid;
		public PlatformType Platform { get; set; } = PlatformType.Invalid;
		public IBuildInfo BuildInfo { get; private set; }

        public string? OutputPath { get; set; }
        public string? BonoboPath { get; set; }
        public string? BonoboProjectDumpPath { get; set; }
        public string? BonoboProjectOutputPath { get; set; }
		public string? BonoboProjectDependenciesPath { get; set; }

		public string? ManagedRelativePath { get; set; }
		public string? ManagedProjectDumpPath { get; set; }
		public string? ManagedProjectOutputPath { get; set; }

		public string[] Projects { get; set; }
        public string[] RelativePaths { get; set; }
        public string[] XMLRelativePaths { get; set; }
		public string[] ExternalRelativePaths { get; set; }

		public DumperContext(BuildType buildType, string buildPath)
		{
			Build = buildType;
			BonoboPath = buildPath;
			BuildInfo = null!;
			Projects = [];
			RelativePaths = [];
			XMLRelativePaths = [];
			ExternalRelativePaths = [];
		}

		public bool Init(SettingsService settings) 
        {
			Platform = settings.BonoboSettings.Platform;
			OutputPath = settings.BonoboSettings.OutputPath;

			if (Build == BuildType.Invalid)
			{
				//MessageBox.Show("Build was invalid!");
				return false;
			}

            if (!Directory.Exists(OutputPath)) 
            {
				//MessageBox.Show("OutputPath does not exist!");
                return false;
            }

            if (!Directory.Exists(BonoboPath)) 
            {
				//MessageBox.Show("BonoboPath does not exist!");
                return false;
            }

            InitializeBuildInfo();

			if (BuildInfo == null)
			{
				return false;
			}

            Projects = BuildInfo.GetProjects();
            RelativePaths = BuildInfo.GetRelativePaths();
            XMLRelativePaths = BuildInfo.GetXMLRelativePaths();
			ExternalRelativePaths = BuildInfo.GetExternalRelativePaths();
			ManagedRelativePath = BuildInfo?.GetManagedRelativePath();

			BonoboProjectDumpPath = $"{OutputPath}\\{Build}\\Bonobo\\Dump";
            BonoboProjectOutputPath = $"{OutputPath}\\{Build}\\Bonobo\\Output";
			BonoboProjectDependenciesPath = $"{OutputPath}\\{Build}\\Bonobo\\Output\\Dependencies";

			ManagedProjectDumpPath = $"{OutputPath}\\{Build}\\ManagedBlam\\Dump";
			ManagedProjectOutputPath = $"{OutputPath}\\{Build}\\ManagedBlam\\Output";

			return true;
        }

        public void InitializeBuildInfo()
        {
			// Temp hack until I can be assed to add proper handling for this.
			if (Build == BuildType.Blam || 
				Build == BuildType.Prophets)
			{
				return;
			}

            BuildInfo = Build switch
            {
				BuildType.Forerunner => new ForerunnerInfo(),
				BuildType.Atlas => new AtlasInfo(),
				BuildType.Omaha => new OmahaInfo(),
                BuildType.Midnight => new MidnightInfo(),
                BuildType.Groundhog => new GroundhogInfo(),
                _ => throw new InvalidOperationException($"Unsupported build type: {Build}"),
            };
        }

		public void ValidateBonoboDumpPath()
		{
			if (!Directory.Exists(BonoboProjectDumpPath))
			{
				Directory.CreateDirectory(BonoboProjectDumpPath!);
			}
		}

		public void ValidateBonoboOutputPath()
		{
			if (!Directory.Exists(BonoboProjectOutputPath))
			{
				Directory.CreateDirectory(BonoboProjectOutputPath!);
			}
		}

		public void ValidateBonoboDependenciesPath()
		{
			if (!Directory.Exists(BonoboProjectDependenciesPath))
			{
				Directory.CreateDirectory(BonoboProjectDependenciesPath!);
			}
		}

		public void ValidateManagedDumpPath() 
		{
			if (!Directory.Exists(ManagedProjectDumpPath))
			{
				Directory.CreateDirectory(ManagedProjectDumpPath!);
			}
		}

		public void ValidateManagedOutputPath() 
		{
			if (!Directory.Exists(ManagedProjectOutputPath))
			{
				Directory.CreateDirectory(ManagedProjectOutputPath!);
			}
		}
	}
}
