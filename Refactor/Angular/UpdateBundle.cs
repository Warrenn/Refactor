using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace Refactor.Angular
{
    public class UpdateBundle : ArgsRefactorFileStrategy<UpdateBundleOptions>
    {
        public UpdateBundle(UpdateBundleOptions options)
            : base(options)
        {
        }

        public override void RefactorFile(FileEntry entry)
        {
            var creationNode = NgManager.GetBundleCreationNode(entry, options.BundleId);
            if (creationNode == null)
            {
                return;
            }
            var project = entry.CSharpFile.Project.MsbuildProject;
            var projectPath = Path.GetDirectoryName(project.FullPath);
            var jsPath = Path.Combine(projectPath, "Content/js");
            var appJsPath = Path.Combine(jsPath, "app.module.js");
            var jsRoot = options.JsRoot;
            var invokeExpression = (InvocationExpression) creationNode.Parent.Parent;
            var cloneExpression = (InvocationExpression) invokeExpression.Clone();
            var script = entry.Script;

            if (string.IsNullOrEmpty(jsRoot))
            {
                jsRoot = "~/Areas/" + entry.CSharpFile.Project.Title + "/";
            }

            foreach (var fileName in Directory
                .GetFiles(jsPath, "*.*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(filename => filename != "app.module.js"))
            {
                FileManager.AddContentToProject(project, "Content\\js\\" + fileName, entry.BackupId);
                if (Path.GetExtension(fileName) != ".js")
                {
                    continue;
                }
                NgManager.AddJsFileToNode(cloneExpression, "external", jsRoot + fileName);
            }

            FileManager.CreateFileFromTemplate(appJsPath, "Refactor.Angular.app.module.cshtml", null);
            NgManager.AddJsFileToNode(cloneExpression, "app", "app.module.js", true);
            FileManager.AddContentToProject(project, "Content\\js\\app.module.js", entry.BackupId);

            foreach (var directory in Directory.GetDirectories(jsPath))
            {
                var folderName = Path.GetFileName(directory);
                foreach (var fileName in Directory
                    .GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(filename => filename != folderName + ".module.js"))
                {
                    FileManager.AddContentToProject(project, "Content\\js\\" + folderName + "\\" + fileName, entry.BackupId);
                    if (Path.GetExtension(fileName) != ".js")
                    {
                        continue;
                    }
                    NgManager.AddJsFileToNode(cloneExpression, folderName,
                        jsRoot + "Content/js/" + folderName + "/" + fileName);
                }
                var strategy = new AddModule(new AddModuleOptions
                {
                    BundleId = options.BundleId,
                    JsRoot = options.JsRoot,
                    Module = folderName
                });
                strategy.RefactorProject(entry.CSharpFile.Project);

                NgManager.AddJsFileToNode(cloneExpression, folderName,
                    jsRoot + "Content/js/" + folderName + "/" + folderName + ".module.js", true);
            }

            script.Replace(invokeExpression, cloneExpression);
        }
    }
}
