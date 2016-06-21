using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.Editor;

namespace Refactor
{
    public class FileEntry
    {
        public CSharpFile CSharpFile { get; set; }
        public Script Script { get; set; }
        public IDocument Document { get; set; }
        public string BackupId { get; set; }
    }
}
