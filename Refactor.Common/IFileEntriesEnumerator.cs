using System.Collections.Generic;

namespace Refactor
{
    public interface IFileEntriesEnumerator
    {
        IEnumerable<FileEntry> GetFileEntries(CSharpProject project);
    }
}
