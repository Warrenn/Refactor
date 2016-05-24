using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Microsoft.Ajax.Utilities;

namespace Refactor.Angular
{
    public static class NgManager
    {
        public static void AddJsFileToBundle(FileEntry entry, string project, string area, string fileName)
        {
            var file = entry.CSharpFile;
            var method = (
                from typeDef in file.UnresolvedTypeSystemForFile.TopLevelTypeDefinitions
                from member in typeDef.Members
                where
                    member.IsPublic &&
                    member.IsStatic
                let umember = member as DefaultUnresolvedMethod
                where
                    umember != null &&
                    umember.HasBody &&
                    umember.Parameters.Count() == 1
                let parameter = umember.Parameters[0] as DefaultUnresolvedParameter
                where
                    parameter != null
                let parameterType = parameter.Type as SimpleTypeOrNamespaceReference
                where
                    parameterType != null &&
                    parameterType.Identifier == "BundleCollection"
                select member).FirstOrDefault();

            if (method == null)
            {
                return;
            }

            var creationNode = (
                from node in file.SyntaxTree.Descendants.OfType<ObjectCreateExpression>()
                from argument in node.Arguments
                where
                    argument.ToString() == "\"~/" + project + "/js\""
                select node).FirstOrDefault();

            if (creationNode == null)
            {
                return;
            }

            var invokeExpression = (InvocationExpression)creationNode.Parent.Parent;
            var cloneExpression = (InvocationExpression)invokeExpression.Clone();
            var script = entry.Script;
            Comment areaComment = null;
            PrimitiveExpression moduleArgument = null;
            var addFileRef = true;
            var jsFileName = string.Empty;

            if (!string.IsNullOrEmpty(fileName))
            {
                jsFileName ="~/Areas/" + project + "/Content/js/" + area + "/" + fileName + ".js";
            }

            var moduleFileName = "~/Areas/" + project + "/Content/js/" + area + "/" + area + ".module.js";

            foreach (var child in cloneExpression.Children)
            {
                if ((child.Role == Roles.Comment) && (child.ToString() == "/*" + area + "*/"))
                {
                    areaComment = (Comment)child;
                    continue;
                }
                if ((child.Role == Roles.Argument) && (child.ToString() == "\"" + moduleFileName + "\""))
                {
                    moduleArgument = (PrimitiveExpression)child;
                    continue;
                }
                if (!string.IsNullOrEmpty(fileName) && (child.Role == Roles.Argument) && (child.ToString() == "\"" + jsFileName + "\""))
                {
                    addFileRef = false;
                }
            }

            if (areaComment == null)
            {
                areaComment = new Comment(area, CommentType.MultiLine);
                cloneExpression.AddChild(areaComment, Roles.Comment);
            }

            if (moduleArgument == null)
            {
                moduleArgument = new PrimitiveExpression(moduleFileName);
                cloneExpression.InsertChildAfter(areaComment, moduleArgument, Roles.Argument);
            }

            if (!string.IsNullOrEmpty(fileName) && addFileRef)
            {
                cloneExpression.InsertChildAfter(moduleArgument, new PrimitiveExpression(jsFileName), Roles.Argument);
            }

            script.Replace(invokeExpression, cloneExpression);
        }

        public static void AddJsModuleToAppJs(string projectPath,string moduleName)
        {
            var appPath = Path.Combine(projectPath, "Content\\js\\app.module.js");
            if (!File.Exists(appPath))
            {
                throw new FileNotFoundException(appPath);
            }

            var parser = new JSParser();
            var source = File.ReadAllText(appPath);

            var settings = new CodeSettings
            {
                OutputMode = OutputMode.MultipleLines,
                KillSwitch = 8192,
                Format = JavaScriptFormat.JSON,
                PreserveImportantComments = true,
                RemoveUnneededCode = false,
                TermSemicolons = true
            };

            var block = parser.Parse(source, settings);
            var visitor = new JsAppModuleVisitor(moduleName);
            visitor.Visit(block);

            var builder = new StringBuilder();
            var stringWriter = new StringWriter(builder);
            OutputVisitor.Apply(stringWriter, block, settings);
            var output = builder.ToString();

            if (source == output)
            {
                return;
            }

            FileManager.BackupFile(appPath);
            File.WriteAllText(appPath, output);
        }
    }
}
