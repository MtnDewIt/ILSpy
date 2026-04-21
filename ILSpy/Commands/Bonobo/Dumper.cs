namespace ICSharpCode.ILSpy.Commands.Bonobo
{
    public class Dumper
    {
        public static string Project { get; set; }
        public static int ProjectIndex { get; set; }

        public Dumper(string project, int projectIndex) 
        {
            Project = project;
            ProjectIndex = projectIndex;
        }

        public void Dump() 
        {
            string outputPath = $"{DumperContext.ProjectDumpPath}\\{DumperContext.Projects![ProjectIndex]}";
            string projectPath = $"{DumperContext.BonoboPath}\\{DumperContext.Projects[ProjectIndex]}";
        }
    }
}
