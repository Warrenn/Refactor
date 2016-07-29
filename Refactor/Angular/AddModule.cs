using System.IO;

namespace Refactor.Angular
{
    public class AddModule : ArgsRefactorFileStrategy<AddModuleOptions>, IRefactorProjectStrategy
    {
        private readonly string module;

        public AddModule(AddModuleOptions options)
            : base(options)
        {
            module = NgManager.CamelCase(options.Module);
        }

        public override void RefactorFile(FileEntry entry)
        {
            NgManager.AddJsFileToBundle(entry, options.BundleId, options.JsRoot, module, string.Empty);
        }

        public void RefactorProject(CSharpProject project)
        {
            var projectPath = Path.GetDirectoryName(project.FileName);
            var areapart = "Content\\js\\" + module;
            var modulepart = areapart + "\\" + module + ".module.js";
            var templateFolder = string.IsNullOrEmpty(options.Template) ? "NgTemplates" : options.Template;
            var templatePath = Path.Combine(project.Solution.Directory, templateFolder);
            var areaPath = Path.Combine(projectPath, areapart);
            var modulePath = Path.Combine(projectPath, modulepart);

            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            FileManager.CreateFileFromTemplate(modulePath, "area.module.cshtml", options,templatePath);
            FileManager.AddContentToProject(project.MsbuildProject, modulepart, project.BackupId);

            NgManager.AddJsModuleToAppJs(projectPath, "app." + module, project.BackupId);
        }
    }
}
