using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor.Angular
{
    public class AddDataService : ArgsRefactorFileStrategy<AddDataServiceOptions>, IRefactorProjectStrategy
    {
        private static Regex tokenExpression = new Regex("\\{([0-9a-zA-Z_]*)\\}", RegexOptions.Compiled);
        private bool addedJsToBundle;
        private readonly string routeName;
        private readonly string fileName;
        private readonly string controllerName;

        public string RouteDeclaration { get; set; }
        public DataServiceViewModel Model { get; set; }

        public AddDataService(AddDataServiceOptions options)
            : base(options)
        {
            Model = null;
            RouteDeclaration = null;
            addedJsToBundle = false;
            routeName = CreateRouteName(options.Route, options.Project);
            controllerName = options.Controller.Replace("Controller", "");
            fileName = TypeManager.CamelCase(controllerName) + "DataService.js";
        }
        
        public static string CreateRouteName(string route, string project)
        {
            if (string.IsNullOrEmpty(route))
            {
                route = project + "Api";
            }
            return "\"" + route + "\"";
        }


        public override void RefactorFile(FileEntry entry)
        {
            var file = entry.CSharpFile;
            var resolver = file.CreateResolver();

            if (!addedJsToBundle)
            {
                addedJsToBundle = NgManager.AddJsFileToBundle(entry, options.BundleId, options.JsRoot, "data", fileName);
            }

            if (Model == null)
            {
                Model = GetModel(file, resolver, options);
            }

            if (!string.IsNullOrEmpty(RouteDeclaration))
            {
                return;
            }

            RouteDeclaration = NgManager.GetRouteDeclaration(file, routeName);
        }

        public static DataServiceViewModel GetModel(CSharpFile file, CSharpAstResolver resolver,
            AddDataServiceOptions options)
        {
            var controllerDeclaration = (
                from codetype in file.SyntaxTree.Descendants.OfType<TypeDeclaration>()
                let resolvedType = resolver.Resolve(codetype)
                where
                    (resolvedType.Type.Name == options.Controller ||
                     resolvedType.Type.Name == options.Controller + "ApiController") &&
                    resolvedType.Type.DirectBaseTypes.Any(t => t.FullName == "System.Web.Http.ApiController")
                select resolvedType).FirstOrDefault();

            return controllerDeclaration == null
                ? null
                : CreateDataServiceViewModel(options.Controller, controllerDeclaration);
        }

        public static DataServiceViewModel CreateDataServiceViewModel(string serviceName,
            ResolveResult controllerDeclaration)
        {
            return new DataServiceViewModel
            {
                Name = serviceName,
                CamelCaseName = TypeManager.CamelCase(serviceName),
                Methods = controllerDeclaration
                    .Type
                    .GetMethods(m =>
                        m.IsPublic &&
                        !m.IsStatic &&
                        m.Attributes.All(a => ((CSharpAttribute) a).AttributeType.ToString() != "Ignore[Attribute]"),
                        GetMemberOptions.IgnoreInheritedMembers)
                    .Select(m => new DataServiceViewModel.MethodCall
                    {
                        Name = m.Name,
                        CamelCaseName = TypeManager.CamelCase(m.Name),
                        IsPost = m.Attributes.Any(a => a.AttributeType.Name == "HttpPostAttribute")
                    }).ToArray()
            };
        }

        public static string CreateRoutePath(string controllerName, string actionName, string routeDeclaration)
        {
            var path =
                routeDeclaration
                    .Replace("{controller}", controllerName)
                    .Replace("{action}", actionName);
            return tokenExpression.Replace(path, "' + request['$1'] + '");
        }

        public void RefactorProject(CSharpProject project)
        {
            if ((Model == null) || string.IsNullOrEmpty(RouteDeclaration))
            {
                Trace.TraceError("The model or route could not be found");
                return;
            }
            var projectPath = Path.GetDirectoryName(project.FileName);
            var servicePart = "Content\\js\\data\\" + fileName;
            var servicePath = Path.Combine(projectPath, servicePart);
            RouteDeclaration = RouteDeclaration.Replace("\"", "");
            Model.Methods = Model
                .Methods
                .Select(m =>
                {
                    m.Path = CreateRoutePath(controllerName, m.Name, RouteDeclaration);
                    return m;
                });

            FileManager.CreateFileFromTemplate(servicePath, "dataservice.cshtml", Model);
            FileManager.AddContentToProject(project.MsbuildProject, servicePart);
        }
    }
}
