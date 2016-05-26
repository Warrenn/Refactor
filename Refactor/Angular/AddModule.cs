using System.IO;

namespace Refactor.Angular
{
    public class AddModule : ArgsRefactorFileStrategy<AddModuleOptions>,IRefactorProjectStrategy
    {
        public AddModule(AddModuleOptions options)
            : base(options)
        {
        }

        public override void RefactorFile(FileEntry entry)
        {
            NgManager.AddJsFileToBundle(entry, entry.CSharpFile.Project.Title, options.Module, string.Empty);
        }

        public void RefactorProject(CSharpProject project)
        {
            var projectPath = Path.GetDirectoryName(project.FileName);
            var areapart = "Content\\js\\" + options.Module;
            var modulepart = areapart + "\\" + options.Module + ".module.js";

            var areaPath = Path.Combine(projectPath, areapart);
            var modulePath = Path.Combine(projectPath, modulepart);

            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            FileManager.CreateFileFromTemplate(modulePath, "Refactor.Angular.area.module.cshtml", options);
            FileManager.AddContentToProject(project.MsbuildProject, modulepart);

            NgManager.AddJsModuleToAppJs(projectPath, "app." + options.Module);
        }
    }
}
