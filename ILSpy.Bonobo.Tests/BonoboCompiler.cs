using System.Diagnostics;

namespace ILSpy.Bonobo.Tests
{
	public class BonoboCompiler
	{
		public static async Task<List<string>> BuildAndLogErrorsAsync(string projectPath)
		{
			List<string> errorLogs = [];

			using var process = new Process 
			{
				StartInfo = new ProcessStartInfo 
				{
					FileName = "dotnet",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};

			process.StartInfo.ArgumentList.Add("build");
			process.StartInfo.ArgumentList.Add(projectPath);
			process.StartInfo.ArgumentList.Add("-clp:ErrorsOnly");

			process.OutputDataReceived += (sender, e) => 
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					errorLogs.Add(e.Data);
				}
			};

			process.ErrorDataReceived += (sender, e) => 
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					errorLogs.Add(e.Data);
				}
			};

			try
			{
				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				await process.WaitForExitAsync();
			}
			catch (Exception ex)
			{
				errorLogs.Add($"Process execution failed: {ex.Message}");
			}

			return errorLogs;
		}

		public static void CleanProject(string projectRoot) 
		{
			string[] targetFolders =
			[
				"bin", 
				"obj", 
				".vs", 
			];

			if (!Directory.Exists(projectRoot))
			{
				// #TODO: Error Handling
				return;
			}

			try
			{
				var allDirectories = Directory.EnumerateDirectories(projectRoot, "*", SearchOption.AllDirectories).ToList();

				foreach (string dir in allDirectories)
				{
					string folderName = Path.GetFileName(dir);

					if (targetFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
					{
						string dependencyPattern = $"\\Dependencies\\";

						bool isDependency = dir.Contains(dependencyPattern, StringComparison.OrdinalIgnoreCase);

						if (!isDependency)
						{
							if (Directory.Exists(dir))
							{
								Directory.Delete(dir, true);
							}
						}
						else
						{
							// #TODO: Error Handling
						}
					}
				}
			}
			catch (UnauthorizedAccessException)
			{
				// #TODO: Error Handling
				return;
			}
			catch (Exception)
			{
				// #TODO: Error Handling
				return;
			}
		}
	}
}
