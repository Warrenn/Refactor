using System.Collections.Generic;

namespace Refactor
{
    public interface IProjectsEnumerator
    {
        IEnumerable<CSharpProject> GetCSharpProjects(Solution solution);
    }
}
