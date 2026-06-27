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

					for (int projectIndex = 0; projectIndex < Context?.Projects.Length; projectIndex++)
					{
						string currentProject = Context?.Projects[projectIndex]!;

						string projectPath = $"{Context?.BonoboProjectOutputPath}\\{currentProject}\\{currentProject}.csproj";

						BonoboCompiler.SetupProjectFile(projectPath, build.Value);

						List<string> errors = await BonoboCompiler.BuildAndLogErrorsAsync(projectPath);

						foreach (string error in errors)
						{
							Console.WriteLine($"[{currentProject}]: {error}");
						}

						Console.WriteLine();

						BonoboCompiler.CleanupProjectFile(projectPath, build.Value);
					}
				}
			}
		}
	}
}