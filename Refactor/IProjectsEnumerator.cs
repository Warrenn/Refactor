using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refactor
{
    public interface IProjectsEnumerator
    {
        IEnumerable<CSharpProject> GetCSharpProjects(Solution solution);
    }
}
