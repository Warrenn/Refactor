using System.Diagnostics;
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
            var modulepart = areapart + "\\" + TypeManager.CamelCase(options.Area) + ".module.js";
            var controllerpart = areapart + "\\" + TypeManager.CamelCase(options.Controller) + ".js";
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
                Trace.TraceError("Service was not in the correct format needs to be servicename.methodname format.");
                return;
            }

            var model = new ControllerViewModel
            {
                Area = options.Area,
                CamelCaseName = TypeManager.CamelCase(options.Controller),
                ServiceName = TypeManager.CamelCase(serviceParts[0]),
                ServiceMethod = TypeManager.CamelCase(serviceParts[1])
            };

            FileManager.CreateFileFromTemplate(modulePath, "area.module.cshtml", new {Module = options.Area});
            FileManager.CreateFileFromTemplate(controllerPath, "controller.cshtml", model);

            FileManager.AddContentToProject(project.MsbuildProject, modulepart);
            FileManager.AddContentToProject(project.MsbuildProject, controllerpart);

            NgManager.AddJsModuleToAppJs(projectPath, "app." + options.Area);
        }
    }
}
