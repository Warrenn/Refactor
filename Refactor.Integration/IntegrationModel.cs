using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;

namespace Refactor.Integration
{
    public class IntegrationModel
    {
        public IList<TypeDeclaration> DataContracts { get; set; }

        public IList<TypeViewModel> ViewModelContracts { get; set; }

        public TypeDeclaration ManagerDeclaration { get; set; }

        public TypeViewModel ManagerViewModel { get; set; }

        public TypeDeclaration ServiceDeclaration { get; set; }

        public TypeViewModel ServiceViewModel { get; set; }

        public string Name { get; set; }

        public string ServiceReferencePath { get; set; }
    }
}
