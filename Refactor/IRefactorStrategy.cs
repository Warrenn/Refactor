namespace Refactor
{
    public interface IRefactorStrategy
    {
        void Refactor(FileEntry entry, string[] args);
    }
}
