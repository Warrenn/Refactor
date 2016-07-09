using System.IO;

namespace Refactor.Angular
{
    public class AddController : ArgsRefactorFileStrategy<AddControllerOptions>, IRefactorProjectStrategy
    {
        public AddController(AddControllerOptions options)
            : base(options)
        {
        }

        public override void RefactorFile(FileEntry entry)
        {
            NgManager.AddJsFileToBundle(entry, options.BundleId, options.JsRoot, options.Area, options.Controller + ".js");
        }

        public void RefactorProject(CSharpProject project)
        {
            var projectPath = Path.GetDirectoryName(project.FileName);
            var areapart = "Content\\js\\" + options.Area;
            var modulepart = areapart + "\\" + options.Area + ".module.js";
            var controllerpart = areapart + "\\" + NgManager.CamelCase(options.Controller) + ".js";
            var serviceParts = options.Service.Split('.');

            var areaPath = Path.Combine(projectPath, areapart);
            var modulePath = Path.Combine(projectPath, modulepart);
            var controllerPath = Path.Combine(projectPath, controllerpart);

            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            if (serviceParts.Length != 2)
            {
                return;
            }

            var model = new ControllerViewModel
            {
                Area = options.Area,
                CamelCaseName = NgManager.CamelCase(options.Controller),
                ServiceName = NgManager.CamelCase(serviceParts[0]),
                ServiceMethod = NgManager.CamelCase(serviceParts[1])
            };

            FileManager.CreateFileFromTemplate(modulePath, "Refactor.Angular.area.module.cshtml", new { Module = options.Area });
            FileManager.CreateFileFromTemplate(controllerPath, "Refactor.Angular.controller.cshtml",
                typeof(ControllerViewModel), model);

            FileManager.AddContentToProject(project.MsbuildProject, modulepart, project.BackupId);
            FileManager.AddContentToProject(project.MsbuildProject, controllerpart, project.BackupId);

            NgManager.AddJsModuleToAppJs(projectPath, "app." + options.Area, project.BackupId);
        }
    }
}
