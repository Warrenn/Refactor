using System.Collections.Generic;

namespace Refactor.Angular
{
    public class DataServiceViewModel
    {
        public string Name { get; set; }

        public string CamelCaseName { get; set; }

        public IEnumerable<MethodCall> Methods;

        public class MethodCall
        {
            public string Name { get; set; }

            public string CamelCaseName { get; set; }

            public string Path { get; set; }

            public bool IsPost { get; set; }
        }
    }
}
