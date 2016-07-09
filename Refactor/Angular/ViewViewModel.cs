using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor.Angular
{
    public class ViewViewModel
    {
        public string Title { get; set; }
        public string Prefix { get; set; }
        public string Name { get; set; }
        public string CamelCaseName { get; set; }
        public string Type { get; set; }
        public IEnumerable<string> Properties { get; set; }
        public string ControllerName { get; set; }
        public string ResultName { get; set; }
        public IEnumerable<string> Usings { get; set; }
    }
}
