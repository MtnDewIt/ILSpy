using ICSharpCode.ILSpy.Commands.Bonobo;

namespace ILSpy.Bonobo.Tests 
{
	public class Program 
	{
		public static readonly string OutputPath = "C:\\Users\\Harry Ricketts\\Downloads\\BONOBO_TEST";

		public static DumperContext? Context;

		public static async Task Main(string[] args) 
		{
			Dictionary<BuildType, string> builds = RegistryHandler.FindEKPaths();

			foreach (var build in builds)
			{
				Context = new DumperContext(build.Key, build.Value);

				bool isInitialized = Context.Init(OutputPath);

				if (!isInitialized)
				{
					continue;
				}

				if (Context?.Build != BuildType.Forerunner && Context?.Build != BuildType.Atlas)
				{
					await BonoboCompiler.CleanProjectAsync($"{Context?.BonoboProjectOutputPath}");

					int successCount = 0;

					for (int projectIndex = 0; projectIndex < Context?.Projects.Length; projectIndex++)
					{
						string currentProject = Context?.Projects[projectIndex]!;

						string projectPath = $"{Context?.BonoboProjectOutputPath}\\{currentProject}\\{currentProject}.csproj";

						BonoboCompiler.SetupProjectFile(projectPath, build.Value);

						List<string> errors = await BonoboCompiler.BuildAndLogErrorsAsync(projectPath);

						bool failed = errors.Any(e => e.Contains("Build FAILED."));

						if (!failed)
						{
							successCount++;
						}

						foreach (string error in errors)
						{
							Console.WriteLine($"[{Context?.Build} - {currentProject}]: {error}");
						}

						Console.WriteLine();

						BonoboCompiler.CleanupProjectFile(projectPath, build.Value);
					}

					double? successRate = Context?.Projects.Length > 0 ? (double)successCount / Context?.Projects.Length : 0.0;

					Console.WriteLine($"{successCount}/{Context?.Projects.Length} BUILT - {successRate:P0} SUCCESS RATE\n");
				}
			}
		}
	}
}