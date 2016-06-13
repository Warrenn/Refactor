using System.CodeDom;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor.Angular
{
    public class AddDataService : ArgsRefactorFileStrategy<AddDataServiceOptions>,IRefactorProjectStrategy
    {
        private DataServiceViewModel model;
        private string routeDeclaration;
        private bool addedJsToBundle;
        private readonly string routeName;

        public AddDataService(AddDataServiceOptions options)
            : base(options)
        {
            model = null;
            routeDeclaration = null;
            addedJsToBundle = false;
            routeName = "\"" + options.Project + "_DefaultApi\"";
        }


        public override void RefactorFile(FileEntry entry)
        {
            var file = entry.CSharpFile;
            var resolver = file.CreateResolver();

            if (!addedJsToBundle)
            {
                addedJsToBundle = NgManager.AddJsFileToBundle(entry, entry.CSharpFile.Project.Title, "data",
                    options.Controller.ToLower() + "dataservice");
            }

            if (model == null)
            {
                model = NgManager.CreateModel(file, resolver, options);
            }

            if (!string.IsNullOrEmpty(routeDeclaration))
            {
                return;
            }

            routeDeclaration = NgManager.GetRouteDeclaration(file, routeName);
        }

        public void RefactorProject(CSharpProject project)
        {
            if (!addedJsToBundle || (model == null) || string.IsNullOrEmpty(routeDeclaration))
            {
                return;
            }

            var projectPath = Path.GetDirectoryName(project.FileName);
            var servicePart = "Content\\js\\data\\" + options.Controller.ToLower() + "dataservice.js";
            var servicePath = Path.Combine(projectPath, servicePart);
            routeDeclaration = routeDeclaration.Replace("\"", "");
            model.Methods = model
                .Methods
                .Select(m =>
                {
                    m.Path = routeDeclaration
                        .Replace("{controller}", options.Controller)
                        .Replace("{action}", m.Name);
                    return m;
                });

            FileManager.CreateFileFromTemplate(servicePath, "Refactor.Angular.dataservice.cshtml",
                typeof (DataServiceViewModel), model);
            FileManager.AddContentToProject(project.MsbuildProject, servicePart);
        }
    }
}
