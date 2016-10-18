using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor
{
    public class TypeViewModel
    {
        public string Name { get; set; }

        public string CamelCaseName { get; set; }

        public IEnumerable<MethodCall> Methods;

        public IType Type { get; set; }

        public IEnumerable<string> Usings { get; set; }

        public IEnumerable<PropertyModel> Properties { get; set; }

        public class PropertyModel
        {
            public string Name { get; set; }

            public IProperty Property { get; set; }

            public string ReturnTypeName { get; set; }

            public string CamelCaseName { get; set; }
        }

        public class MethodCall
        {
            public string Name { get; set; }

            public IMethod Method { get; set; }

            public string CamelCaseName { get; set; }

            public IType ReturnType { get; set; }

            public IEnumerable<ParameterModel> Parameters { get; set; }

            public string ReturnTypeName { get; set; }

            public IEnumerable<string> ParameterStrings { get; set; }
        }

        public class ParameterModel
        {
            public string Name { get; set; }

            public IParameter Parameter { get; set; }

            public string TypeName { get; set; }
        }
    }
}
