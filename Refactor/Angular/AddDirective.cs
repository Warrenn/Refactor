using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor.Angular
{
    public class AddDirective : ArgsRefactorFileStrategy<AddDirectiveOptions>, IRefactorProjectStrategy
    {
        private readonly string webApiController;
        private IMethod methodDeclaration;

        public AddDirective(AddDirectiveOptions options) : base(options)
        {
            var serviceParts = options.WebApi.Split('.');

            if (string.IsNullOrEmpty(options.ServiceMethod) &&
                (!serviceParts.Any() || string.IsNullOrEmpty(serviceParts[1])))
            {
                Trace.TraceError("Method name not provided");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(options.ServiceMethod))
            {
                options.ServiceMethod = NgManager.CamelCase(serviceParts[1]);
            }

            webApiController = serviceParts[0];

            if (string.IsNullOrEmpty(options.Area))
            {
                options.Area = NgManager.CamelCase(webApiController
                    .Replace("Api", "")
                    .Replace("Controller", ""));
            }

            if (string.IsNullOrEmpty(options.Directive))
            {
                options.Directive = options.ServiceMethod;
            }

            if (string.IsNullOrEmpty(options.ServiceName))
            {
                options.ServiceName = NgManager
                    .CamelCase(webApiController
                    .Replace("Controller", "")) + "DataService";
            }
        }

        public override void RefactorFile(FileEntry entry)
        {
            var file = entry.CSharpFile;
            var resolver = file.CreateResolver();
            NgManager.AddJsFileToBundle(entry, options.BundleId, options.JsRoot, options.Area, options.Directive + ".js");

            if (methodDeclaration == null)
            {
                methodDeclaration = GetApiMethodCall(file, resolver, webApiController, options.ServiceMethod);
            }
        }

        private static IMethod GetApiMethodCall(
            CSharpFile file,
            CSharpAstResolver resolver,
            string controllerName,
            string methodName)
        {
            return (
                from codetype in file.SyntaxTree.Descendants.OfType<TypeDeclaration>()
                let resolvedType = resolver.Resolve(codetype)
                where
                    resolvedType.Type.Name == controllerName ||
                    resolvedType.Type.Name == controllerName + "Controller" &&
                    resolvedType.Type.DirectBaseTypes.Any(t => t.FullName == "System.Web.Http.ApiController")
                from methodCall in resolvedType.Type.GetMethods(options: GetMemberOptions.IgnoreInheritedMembers)
                where
                    methodCall.Name == methodName
                select methodCall).FirstOrDefault();
        }

        public void RefactorProject(CSharpProject project)
        {
            var projectPath = Path.GetDirectoryName(project.FileName);
            var areapart = "Content\\js\\" + options.Area;
            var modulepart = areapart + "\\" + NgManager.CamelCase(options.Area) + ".module.js";
            var directivepart = areapart + "\\" + options.Directive + ".js";
            var htmlpart = areapart + "\\" + options.Directive + ".html";
            var templateFolder = string.IsNullOrEmpty(options.Template) ? "NgTemplates" : options.Template;
            var templatePath = Path.Combine(project.Solution.Directory, templateFolder);
            var areaPath = Path.Combine(projectPath, areapart);
            var modulePath = Path.Combine(projectPath, modulepart);
            var directivePath = Path.Combine(projectPath, directivepart);
            var htmlPath = Path.Combine(projectPath, htmlpart);

            if (!Directory.Exists(areaPath))
            {
                Directory.CreateDirectory(areaPath);
            }

            if (methodDeclaration != null)
            {
                var viewModel = new AddDirectiveViewModel
                {
                    Heading = NgManager.SplitByCase(methodDeclaration.Name),
                    InputType = methodDeclaration.Parameters.Select(p => p.Type).FirstOrDefault(),
                    OutputType = methodDeclaration.ReturnType,
                    Properties = methodDeclaration
                        .ReturnType
                        .GetProperties(options: GetMemberOptions.IgnoreInheritedMembers)
                        .Select(p => NgManager.CamelCase(p.Name)),
                    PropertyLabels = methodDeclaration
                        .ReturnType
                        .GetProperties(options: GetMemberOptions.IgnoreInheritedMembers)
                        .Select(p => NgManager.SplitByCase(p.Name))
                };

                FileManager.CreateFileFromTemplate(htmlPath, "directive.html.cshtml", viewModel);
                FileManager.AddContentToProject(project.MsbuildProject, htmlpart);
            }

            FileManager.CreateFileFromTemplate(modulePath, "area.module.cshtml",
                new AddModuleOptions {Module = options.Area});
            FileManager.CreateFileFromTemplate(directivePath, "directive.cshtml", options);
            FileManager.AddContentToProject(project.MsbuildProject, modulepart);
            FileManager.AddContentToProject(project.MsbuildProject, directivepart);

            NgManager.AddJsModuleToAppJs(projectPath, "app." + options.Area);
        }
    }
}
