namespace ICSharpCode.ILSpy.Commands.Bonobo.BuildInfo
{
	public class ForerunnerInfo : IBuildInfo
	{
		public static readonly string MANAGED_RELATIVE_PATH = $"bin\\ManagedBlam.dll";

		public string[] GetProjects() => null;
		public string[] GetRelativePaths() => null;
		public string[] GetXMLRelativePaths() => null;
		public string[] GetExternalRelativePaths() => null;
		public string GetManagedRelativePath() => MANAGED_RELATIVE_PATH;
		public string FilterRelativePath(string path) => null;
	}
}
