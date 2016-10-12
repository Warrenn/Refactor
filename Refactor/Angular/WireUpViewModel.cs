using System.Collections.Generic;

namespace Refactor.Angular
{
    public class WireupViewModel : TypeViewModel
    {
        public new IEnumerable<WireUpMethodCall> Methods;

        public class WireUpMethodCall : MethodCall
        {
            public bool IsPageMethod { get; set; }
        }
    }
}
