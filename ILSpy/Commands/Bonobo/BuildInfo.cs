namespace ICSharpCode.ILSpy.Commands.Bonobo
{
    public interface IBuildInfo
    {
        public string[] GetProjects();
        public string[] GetRelativePaths();
        public string[] GetXMLRelativePaths();
		public string[] GetExternalRelativePaths();
		public string GetManagedRelativePath();
		public string FilterRelativePath(string path);
		public void CleanupProjectDump(string project, string outputPath);
	}
}
