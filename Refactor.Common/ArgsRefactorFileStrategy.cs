namespace Refactor
{
    public abstract class ArgsRefactorFileStrategy<T> : IRefactorFileStrategy
    {
        protected readonly T options;

        protected ArgsRefactorFileStrategy(T options)
        {
            this.options = options;
        }

        public abstract void RefactorFile(FileEntry entry);
    }
}
