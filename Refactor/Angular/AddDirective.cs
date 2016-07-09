using System.IO;

namespace Refactor.Angular
{
    public class AddDirective : ArgsRefactorFileStrategy<AddDirectiveOptions>,IRefactorProjectStrategy
    {
        public AddDirective(AddDirectiveOptions options) : base(options)
        {
        }

        public override void RefactorFile(FileEntry entry)
        {    
            NgManager.AddJsFileToBundle(entry, options.BundleId, options.JsRoot, options.Area, options.Directive + ".js");
        }

        public void RefactorProject(CSharpProject project)
        {
            var projectPath = Path.GetDirectoryName(project.FileName);
            var areapart = "Content\\js\\" + options.Area;
            var modulepart = areapart + "\\" + options.Area + ".module.js";
            var directivepart = areapart + "\\" + options.Directive + ".js";
            var htmlpart = areapart + "\\" + options.Directive + ".html";

            var areaPath = Path.Combine(projectPath, areapart);
            var modulePath = Path.Combine(projectPath, modulepart);
            var directivePath = Path.Combine(projectPath, directivepart);
            var htmlPath = Path.Combine(projectPath, htmlpart);

            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            FileManager.CreateFileFromTemplate(modulePath, "Refactor.Angular.area.module.cshtml", new {Module = options.Area});
            FileManager.CreateFileFromTemplate(directivePath, "Refactor.Angular.directive.cshtml", options);
            FileManager.CreateFileFromTemplate(htmlPath, "Refactor.Angular.directive.html.cshtml", options);

            FileManager.AddContentToProject(project.MsbuildProject, modulepart, project.BackupId);
            FileManager.AddContentToProject(project.MsbuildProject, directivepart, project.BackupId);
            FileManager.AddContentToProject(project.MsbuildProject, htmlpart, project.BackupId);

            NgManager.AddJsModuleToAppJs(projectPath, "app." + options.Area, project.BackupId);
        }
    }
}
