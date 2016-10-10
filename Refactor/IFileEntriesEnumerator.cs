using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refactor
{
    public interface IFileEntriesEnumerator
    {
        IEnumerable<FileEntry> GetFileEntries(CSharpProject project);
    }
}
