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
            NgManager.AddJsFileToBundle(entry, entry.CSharpFile.Project.Title, options.Area, options.Directive);
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

            FileManager.AddContentToProject(project.MsbuildProject, modulepart);
            FileManager.AddContentToProject(project.MsbuildProject, directivepart);
            FileManager.AddContentToProject(project.MsbuildProject, htmlpart);

            NgManager.AddJsModuleToAppJs(projectPath, "app." + options.Area);
        }
    }
}
