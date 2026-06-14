namespace ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo
{
	public class ForerunnerInfo : IBuildInfo
	{
		public static readonly string MANAGED_RELATIVE_PATH = $"bin\\ManagedBlam.dll";

		public string[] GetProjects() => [];
		public string[] GetRelativePaths() => [];
		public string[] GetXMLRelativePaths() => [];
		public string[] GetExternalRelativePaths() => [];
		public string? GetManagedRelativePath() => MANAGED_RELATIVE_PATH;
		public string? FilterRelativePath(string? path) => null;

		public void CleanupProjectDump(string? project, string? outputPath)
		{
			return;
		}
	}
}
