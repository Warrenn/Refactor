using System.Linq;
using Microsoft.Ajax.Utilities;

namespace Refactor.Angular
{
    public class JsAppModuleVisitor : TreeVisitor
    {
        private readonly string moduleName;

        public JsAppModuleVisitor(string moduleName)
        {
            this.moduleName = moduleName;
        }

        public override void Visit(ArrayLiteral node)
        {
            base.Visit(node);
            if ((node.Parent.Children.First().ToString() != "app") ||
                (node.Elements.Any(n => n.ToString() == moduleName)))
            {
                return;
            }
            var moduleNode = new ConstantWrapper(moduleName, PrimitiveType.String, node.Context);
            node.Elements.Insert(node.Elements.Count, moduleNode);
        }
    }
}