using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor.Angular
{
    public class WireUpViewModel
    {
        public string Name { get; set; }

        public string CamelCaseName { get; set; }

        public IEnumerable<MethodCall> Methods;

        public IType Type { get; set; }

        public CSharpProject Project { get; set; }

        public IEnumerable<string> Usings { get; set; }

        public class MethodCall
        {
            public string Name { get; set; }

            public string CamelCaseName { get; set; }

            public bool IsPageMethod { get; set; }

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
