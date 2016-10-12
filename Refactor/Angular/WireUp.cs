using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using Microsoft.Ajax.Utilities;

namespace Refactor.Angular
{
    public class WireUp : ArgsRefactorFileStrategy<WireUpOptions>, IRefactorProjectStrategy
    {
        private readonly string module;
        private readonly string routeName;
        private string routeDeclaration;
        private ObjectCreateExpression bundleNode;
        private ResolveResult apiControllerDeclaration;
        private ResolveResult mvcControllerDeclaration;
        private ResolveResult serviceDeclaration;
        private FileEntry bundleFileEntry;

        public WireUp(WireUpOptions options)
            : base(options)
        {
            module = options.Service.TrimStart('I').Replace("Service", "");
            routeName = AddDataService.CreateRouteName(options.Route, options.Project);
        }

        public override void RefactorFile(FileEntry entry)
        {
            var file = entry.CSharpFile;
            var resolver = file.CreateResolver();

            if ((bundleNode == null) || (bundleFileEntry == null))
            {
                bundleNode = NgManager.GetBundleCreationNode(entry, options.BundleId);
                bundleFileEntry = entry;
            }

            if (serviceDeclaration == null)
            {
                serviceDeclaration = GetService(file, resolver, options.Service);
            }

            if (apiControllerDeclaration == null)
            {
                apiControllerDeclaration = GetApiController(file, resolver, module);
            }

            if (mvcControllerDeclaration == null)
            {
                mvcControllerDeclaration = GetMvcController(file, resolver, module);
            }

            if (string.IsNullOrEmpty(routeDeclaration))
            {
                routeDeclaration = NgManager.GetRouteDeclaration(file, routeName);
            }
        }

        private static ResolveResult GetApiController(
            CSharpFile file,
            CSharpAstResolver resolver,
            string controllerName)
        {
            return (
                from codetype in file.SyntaxTree.Descendants.OfType<TypeDeclaration>()
                let resolvedType = resolver.Resolve(codetype)
                where
                    resolvedType.Type.Name == controllerName ||
                    resolvedType.Type.Name == controllerName + "ApiController" &&
                    resolvedType.Type.DirectBaseTypes.Any(t => t.FullName == "System.Web.Http.ApiController")
                select resolvedType).FirstOrDefault();
        }

        private static ResolveResult GetMvcController(
            CSharpFile file,
            CSharpAstResolver resolver,
            string controllerName)
        {
            return (
                from codetype in file.SyntaxTree.Descendants.OfType<TypeDeclaration>()
                let resolvedType = resolver.Resolve(codetype)
                where
                    resolvedType.Type.Name == controllerName ||
                    resolvedType.Type.Name == controllerName + "Controller" &&
                    resolvedType.Type.DirectBaseTypes.Any(t => t.FullName == "System.Web.Mvc.Controller")
                select resolvedType).FirstOrDefault();
        }

        private static ResolveResult GetService(
            CSharpFile file,
            CSharpAstResolver resolver,
            string serviceName)
        {
            return TypeManager.GetServices(file, resolver).FirstOrDefault(r => r.Type.Name == serviceName);
        }

        public void RefactorProject(CSharpProject project)
        {
            if (serviceDeclaration == null)
            {
                serviceDeclaration = (
                    from p in project.Solution.Projects
                    from file in p.Files
                    let resolver = file.CreateResolver()
                    let service = GetService(file, resolver, options.Service)
                    where service != null
                    select service).FirstOrDefault();
            }

            if (serviceDeclaration == null || bundleNode == null)
            {
                Trace.TraceError("Couldn't find the service or bundle declaration");
                return;
            }

            var viewModel = TypeManager.CreateTypeViewModel<WireupViewModel>(serviceDeclaration.Type);
            viewModel.Name = module;
            viewModel.CamelCaseName = TypeManager.CamelCase(module);
            viewModel.Methods.ForEach(m => m.IsPageMethod = TypeManager.IsPage(m.Method));

            var projectPath = Path.GetDirectoryName(project.MsbuildProject.FullPath);
            var apiControllerPart = "Controllers\\" + viewModel.Name + "ApiController.cs";
            var apiControllerPath = Path.Combine(projectPath, apiControllerPart);
            var mvcControllerPart = "Controllers\\" + viewModel.Name + "Controller.cs";
            var mvcControllerPath = Path.Combine(projectPath, mvcControllerPart);
            var jsPath = Path.Combine(projectPath, "Content/js");
            var appJsPath = Path.Combine(jsPath, "app.module.js");
            var jsRoot = options.JsRoot;
            var invokeExpression = (InvocationExpression)bundleNode.Parent.Parent;
            var cloneExpression = (InvocationExpression)invokeExpression.Clone();

            if (string.IsNullOrEmpty(jsRoot))
            {
                jsRoot = "~/Areas/" + project.Title + "/";
            }

            FileManager.CreateFileFromTemplate(appJsPath, "app.module.cshtml", new AddModuleOptions());
            var addModule = new AddModule(new AddModuleOptions
            {
                BundleId = options.BundleId,
                JsRoot = options.JsRoot,
                Module = viewModel.CamelCaseName
            });
            addModule.RefactorProject(project);

            var addDataService = new AddDataService(new AddDataServiceOptions
            {
                Controller = viewModel.Name + "Api"
            })
            {
                Model = new DataServiceViewModel
                {
                    CamelCaseName = viewModel.CamelCaseName + "Api",
                    Name = viewModel.Name + "Api",
                    Methods = viewModel.Methods.Select(m => new DataServiceViewModel.MethodCall
                    {
                        CamelCaseName = m.CamelCaseName,
                        IsPost = !m.IsPageMethod,
                        Path = AddDataService.CreateRoutePath(viewModel.Name + "Api", m.Name, routeDeclaration),
                        Name = m.Name
                    })
                },
                RouteDeclaration = routeDeclaration
            };

            addDataService.RefactorProject(project);
            NgManager.AddJsFileToNode(cloneExpression, "data",
                jsRoot + "Content/js/data/" + viewModel.Name.ToLower() + "ApiDataService.js");

            if (apiControllerDeclaration == null)
            {
                FileManager.CreateFileFromTemplate(apiControllerPath, "webapi.cshtml", viewModel);
                FileManager.AddCompileToProject(project.MsbuildProject, apiControllerPart);
            }

            if (mvcControllerDeclaration == null)
            {
                FileManager.CreateFileFromTemplate(mvcControllerPath, "mvccontroller.cshtml", viewModel);
                FileManager.AddCompileToProject(project.MsbuildProject, mvcControllerPart);
            }

            foreach (var method in viewModel.Methods.Where(method =>
                method.IsPageMethod &&
                method.Parameters.Count() == 1 &&
                method.Parameters.First().Parameter.Type.IsParameterized &&
                method.Parameters.First().Parameter.Type.TypeParameterCount == 1 &&
                method.ReturnType.IsParameterized &&
                method.ReturnType.TypeArguments.Count() == 1))
            {
                var controller = method.CamelCaseName;
                var path = jsRoot + "Content/js/" + viewModel.CamelCaseName + "/" + controller + ".js";
                var addController = new AddController(new AddControllerOptions
                {
                    Area = viewModel.CamelCaseName,
                    Controller = controller,
                    Service = viewModel.CamelCaseName + "ApiDataService." + method.CamelCaseName
                });
                addController.RefactorProject(project);

                NgManager.AddJsFileToNode(cloneExpression, viewModel.CamelCaseName, path);

                var viewViewModel = new ViewViewModel
                {
                    CamelCaseName = method.CamelCaseName,
                    ControllerName = controller + "Controller",
                    Type = TypeManager.GetTypeName(method.Parameters.First().Parameter.Type),
                    Name = method.Name,
                    ResultName = TypeManager.GetTypeName(method.ReturnType.TypeArguments.First()),
                    Prefix = viewModel.CamelCaseName,
                    PropertyNames = method
                        .ReturnType
                        .TypeArguments
                        .First()
                        .GetProperties(p => p.IsPublic && p.CanGet && p.CanSet, GetMemberOptions.IgnoreInheritedMembers)
                        .Select(p => TypeManager.SplitByCase(p.Name)),
                    Properties = method
                        .ReturnType
                        .TypeArguments
                        .First()
                        .GetProperties(p => p.IsPublic && p.CanGet && p.CanSet, GetMemberOptions.IgnoreInheritedMembers)
                        .Select(p => TypeManager.CamelCase(p.Name)),
                    Title = viewModel.Name,
                    Usings = viewModel.Usings
                };
                var cshtmlPart = "Views\\" + viewModel.Name + "\\" + method.Name + ".cshtml";
                var cshtmlPath = Path.Combine(projectPath, cshtmlPart);

                FileManager.CreateFileFromTemplate(cshtmlPath, "view.cshtml", viewViewModel);
                FileManager.AddContentToProject(project.MsbuildProject, cshtmlPart);
            }

            NgManager.AddJsFileToNode(cloneExpression, viewModel.CamelCaseName,
                jsRoot + "Content/js/" + viewModel.CamelCaseName + "/" + viewModel.CamelCaseName + ".module.js", true);

            bundleFileEntry.Script.Replace(invokeExpression, cloneExpression);
            FileManager.CopyIfChanged(bundleFileEntry);
        }
    }
}
