using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;

namespace Refactor.Integration
{
    public class IntegrationModel
    {
        public IList<TypeDeclaration> DataContracts { get; set; }

        public TypeDeclaration ImplementationEntry { get; set; }

        public string Name { get; set; }

        public string WsdlPath { get; set; }
    }
}
