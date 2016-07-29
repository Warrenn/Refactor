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
            return (
                from codetype in file.SyntaxTree.Descendants.OfType<TypeDeclaration>()
                let resolvedType = resolver.Resolve(codetype)
                where
                    codetype.Attributes.Any(s => s.Attributes.Any(a => a.Type.ToString() == "ServiceContract")) &&
                    resolvedType.Type.Name == serviceName
                select resolvedType).FirstOrDefault();
        }

        private static bool IsPage(IParameterizedMember method)
        {
            return
                method.Parameters.Any(p => p.Type.Name == "Request" && p.Type.TypeParameterCount > 0) &&
                method.ReturnType.Name == "Page" &&
                method.ReturnType.TypeParameterCount > 0;
        }

        private static string GetTypeName(IType type)
        {
            var name = type.Name;
            if (!type.IsParameterized) return name;
            var generics = type.TypeArguments.Select(GetTypeName).ToArray();
            return name + "<" + string.Join(",", generics) + ">";
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

            var viewModel = new WireUpViewModel
            {
                CamelCaseName = NgManager.CamelCase(module),
                Name = module,
                Type = serviceDeclaration.Type,
                Project = project,
                Usings = GetUsings(serviceDeclaration.Type),
                Methods = serviceDeclaration
                    .Type
                    .GetMethods(m => m.IsPublic && !m.IsStatic && m.Attributes.Any(a => ((CSharpAttribute)a).AttributeType.ToString() == "OperationContract[Attribute]"), GetMemberOptions.IgnoreInheritedMembers)
                    .Select(m => new WireUpViewModel.MethodCall
                    {
                        CamelCaseName = NgManager.CamelCase(m.Name),
                        Name = m.Name,
                        IsPageMethod = IsPage(m),
                        ReturnType = m.ReturnType,
                        ReturnTypeName = GetTypeName(m.ReturnType),
                        Parameters = m.Parameters.Select(p => new WireUpViewModel.ParameterModel
                        {
                            Parameter = p,
                            Name = p.Name,
                            TypeName = GetTypeName(p.Type)
                        }),
                        ParameterStrings = m.Parameters.Select(p => GetTypeName(p.Type) + " " + p.Name).ToArray()
                    })
            };

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
            var templateFolder = string.IsNullOrEmpty(options.Template) ? "NgTemplates" : options.Template;
            var templatePath = Path.Combine(project.Solution.Directory, templateFolder);

            if (string.IsNullOrEmpty(jsRoot))
            {
                jsRoot = "~/Areas/" + project.Title + "/";
            }

            FileManager.CreateFileFromTemplate(appJsPath, "app.module.cshtml", new AddModuleOptions(), templatePath);
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
                FileManager.CreateFileFromTemplate(apiControllerPath, "webapi.cshtml", viewModel, templatePath);
                FileManager.AddCompileToProject(project.MsbuildProject, apiControllerPart, project.BackupId);
            }

            if (mvcControllerDeclaration == null)
            {
                FileManager.CreateFileFromTemplate(mvcControllerPath, "mvccontroller.cshtml", viewModel, templatePath);
                FileManager.AddCompileToProject(project.MsbuildProject, mvcControllerPart, project.BackupId);
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
                    Type = GetTypeName(method.Parameters.First().Parameter.Type),
                    Name = method.Name,
                    ResultName = GetTypeName(method.ReturnType.TypeArguments.First()),
                    Prefix = viewModel.CamelCaseName,
                    PropertyNames = method
                        .ReturnType
                        .TypeArguments
                        .First()
                        .GetProperties(p => p.IsPublic && p.CanGet && p.CanSet, GetMemberOptions.IgnoreInheritedMembers)
                        .Select(p => NgManager.SplitByCase(p.Name)),
                    Properties = method
                        .ReturnType
                        .TypeArguments
                        .First()
                        .GetProperties(p => p.IsPublic && p.CanGet && p.CanSet, GetMemberOptions.IgnoreInheritedMembers)
                        .Select(p => NgManager.CamelCase(p.Name)),
                    Title = viewModel.Name,
                    Usings = viewModel.Usings
                };
                var cshtmlPart = "Views\\" + viewModel.Name + "\\" + method.Name + ".cshtml";
                var cshtmlPath = Path.Combine(projectPath, cshtmlPart);

                FileManager.CreateFileFromTemplate(cshtmlPath, "view.cshtml", viewViewModel, templatePath);
                FileManager.AddContentToProject(project.MsbuildProject, cshtmlPart, project.BackupId);
            }

            NgManager.AddJsFileToNode(cloneExpression, viewModel.CamelCaseName,
                jsRoot + "Content/js/" + viewModel.CamelCaseName + "/" + viewModel.CamelCaseName + ".module.js", true);

            bundleFileEntry.Script.Replace(invokeExpression, cloneExpression);
            FileManager.CopyIfChanged(bundleFileEntry, project.BackupId);
        }

        private IEnumerable<string> GetUsings(IType type)
        {
            var list = new List<string> {type.Namespace};
            list.AddRange(
                from method in type.GetMethods(m => true)
                let returnType = method.ReturnType
                select returnType.Namespace);
            list.AddRange(
                from method in type.GetMethods(m => true)
                let returnType = method.ReturnType
                from typeArgument in returnType.TypeArguments
                select typeArgument.Namespace);
            list.AddRange(
                from method in type.GetMethods(m => true)
                from parameter in method.Parameters
                let parameterType = parameter.Type
                select parameterType.Namespace);
            list.AddRange(
                from method in type.GetMethods(m => true)
                from parameter in method.Parameters
                let parameterType = parameter.Type
                from typeArgument in parameterType.TypeArguments
                select typeArgument.Namespace);
            return list.Distinct();
        }
    }
}
