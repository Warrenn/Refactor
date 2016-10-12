using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Microsoft.Ajax.Utilities;
using AstNode = ICSharpCode.NRefactory.CSharp.AstNode;

namespace Refactor.Angular
{
    public static class NgManager
    {
        public static bool AddJsFileToNode(InvocationExpression cloneExpression, string comment, string newValue, bool firstInGroup = false)
        {
            var addNewRef = true;
            var changed = false;
            var foundLastNode = false;
            Comment commentNode = null;
            AstNode lastNode = null;

            foreach (var child in cloneExpression.Children)
            {
                if ((child.Role == Roles.Comment) &&
                    (string.IsNullOrEmpty(comment) || (child.ToString() == "/*" + comment + "*/")))
                {
                    commentNode = (Comment)child;
                    continue;
                }

                if ((child.Role == Roles.Argument) && (child.ToString() == "\"" + newValue + "\""))
                {
                    addNewRef = false;
                }

                if (foundLastNode)
                {
                    continue;
                }

                if (child.Role == Roles.Argument)
                {
                    lastNode = child;
                }

                if ((child.Role == Roles.Comment) && (commentNode != null))
                {
                    foundLastNode = true;
                }
            }

            if (commentNode == null)
            {
                commentNode = new Comment(comment, CommentType.MultiLine);
                cloneExpression.AddChild(commentNode, Roles.Comment);
                lastNode = commentNode;
                changed = true;
            }

            if (!addNewRef)
            {
                return changed;
            }

            lastNode = lastNode ?? commentNode;
            var afterNode = firstInGroup ? commentNode : lastNode;

            cloneExpression.InsertChildAfter(afterNode, new PrimitiveExpression(newValue), Roles.Argument);
            return true;
        }

        public static bool AddJsFileToBundle(FileEntry entry, string bundleId, string jsRoot, string area,
            string fileName)
        {
            var creationNode = GetBundleCreationNode(entry, bundleId);
            if (creationNode == null)
            {
                return false;
            }

            var changed = false;
            var invokeExpression = (InvocationExpression)creationNode.Parent.Parent;
            var cloneExpression = (InvocationExpression)invokeExpression.Clone();
            var script = entry.Script;

            if (string.IsNullOrEmpty(jsRoot))
            {
                jsRoot = "~/Areas/" + entry.CSharpFile.Project.Title + "/";
            }

            var project = entry.CSharpFile.Project.MsbuildProject;
            if (!string.IsNullOrEmpty(fileName))
            {
                FileManager.AddContentToProject(project, "Content\\js\\" + area + "\\" + fileName);
                changed = AddJsFileToNode(cloneExpression, area, jsRoot + "Content/js/" + area + "/" + fileName);
            }

            FileManager.AddContentToProject(project, "Content\\js\\" + area + "\\" + area + ".module.js");
            changed = AddJsFileToNode(cloneExpression, area, jsRoot + "Content/js/" + area + "/" + area + ".module.js", true) || changed;
            script.Replace(invokeExpression, cloneExpression);

            return changed;
        }


        public static ObjectCreateExpression GetBundleCreationNode(FileEntry entry, string bundleId)
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
                return null;
            }

            if (string.IsNullOrEmpty(bundleId))
            {
                bundleId = "~/" + entry.CSharpFile.Project.Title + "/js";
            }

            bundleId = "\"" + bundleId + "\"";
            var creationNode = (
                from node in file.SyntaxTree.Descendants.OfType<ObjectCreateExpression>()
                from argument in node.Arguments
                where
                    argument.ToString() == bundleId
                select node).FirstOrDefault();
            return creationNode;
        }

        public static void AddJsModuleToAppJs(string projectPath, string moduleName)
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

        public static string GetRouteDeclaration(CSharpFile file, string routeName)
        {
            return (
                from method in file.SyntaxTree.Descendants.OfType<InvocationExpression>()
                let target = method.Target as MemberReferenceExpression
                where
                    target != null &&
                    target.MemberName == "MapHttpRoute" &&
                    method.Arguments.Count == 2
                let namePrimitive = method.Arguments.First().Descendants.OfType<PrimitiveExpression>()
                where
                    namePrimitive.Any() &&
                    namePrimitive.First().LiteralValue == routeName
                select
                    method
                        .Arguments
                        .ToArray()[1]
                        .Descendants
                        .OfType<PrimitiveExpression>()
                        .First()
                        .LiteralValue).FirstOrDefault();
        }
    }
}
