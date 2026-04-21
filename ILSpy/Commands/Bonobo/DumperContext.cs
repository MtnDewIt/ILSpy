using System;
using System.IO;

using ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo;

namespace ICSharpCode.ILSpy.Commands.Bonobo
{
    public static class DumperContext
    {
        public static BuildType Build { get; set; } = BuildType.Invalid;
        public static IBuildInfo BuildInfo { get; private set; }

        public static string AppDataPath { get; set; }

        public static string OutputPath { get; set; }
        public static string BonoboPath { get; set; }
        public static string ProjectDumpPath { get; set; }
        public static string ProjectOutputPath { get; set; }

        public static string[] Projects { get; set; }
        public static string[] RelativePaths { get; set; }
        public static string[] XMLRelativePaths { get; set; }

        public static bool Init(string[] arguments) 
        {
            if (arguments.Length > 2 || arguments.Length == 0) 
            {
                Console.WriteLine("Usage: BonoboDumper <Build> <OutputPath> <BonoboPath>");
                return false;
            }

            Build = Enum.Parse<BuildType>(arguments[0], true);
            OutputPath = arguments[1];
            BonoboPath = arguments[2];

            if (!Directory.Exists(OutputPath)) 
            {
                Console.WriteLine("OutputPath does not exist!");
                return false;
            }

            if (!Directory.Exists(BonoboPath)) 
            {
                Console.WriteLine("BonoboPath does not exist!");
                return false;
            }

            InitializeBuildInfo();

            AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            Projects = BuildInfo?.GetProjects();
            RelativePaths = BuildInfo?.GetRelativePaths();
            XMLRelativePaths = BuildInfo?.GetXMLRelativePaths();

            ProjectDumpPath = $"{OutputPath!}/Bonobo-{Build}/Dump";
            ProjectOutputPath = $"{OutputPath!}/Bonobo-{Build}/Output";

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
    }
}
