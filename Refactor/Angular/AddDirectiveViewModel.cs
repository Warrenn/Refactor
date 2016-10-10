using System.Collections.Generic;
using CommandLine;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor.Angular
{
    public class AddDirectiveViewModel
    {
        public IType InputType { get; set; }

        public IType OutputType { get; set; }

        public string Heading { get; set; }

        public IEnumerable<string> PropertyLabels { get; set; }

        public IEnumerable<string> Properties { get; set; }
    }
}
