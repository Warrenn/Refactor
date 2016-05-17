using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.Editor;

namespace Refactor
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(1);
            }

            if (!File.Exists(options.Solution))
            {
                Trace.TraceError("The solution file must exist");
                Environment.Exit(1);
            }

            var strategyType = StrategyType(options.Refactory);

            if (strategyType == null)
            {
                Trace.TraceError("Invalid type {0} specified ", options.Refactory);
                Environment.Exit(1);
            }

            var strategy = Activator.CreateInstance(strategyType) as IRefactorStrategy;

            if (strategy == null)
            {
                Trace.TraceError("The type couldn't be cast to a valid strategy");
                Environment.Exit(1);
            }

            try
            {
                var editorOptions = new TextEditorOptions();
                var formattingOptions = FormattingOptionsFactory.CreateAllman();
                var solution = new Solution(options.Solution);

                var fileEntries =
                    from file in solution.AllFiles
                    let document = new StringBuilderDocument(file.OriginalText)
                    let script = new DocumentScript(document, formattingOptions, editorOptions)
                    select new FileEntry
                    {
                        CSharpFile = file,
                        Document = document,
                        Script = script
                    };

                foreach (var fileEntry in fileEntries)
                {
                    fileEntry.CSharpFile.SyntaxTree.Freeze();
                    var fileName = fileEntry.CSharpFile.FileName;
                    Trace.WriteLine(fileName);
                    strategy.Refactor(fileEntry);
                    if (fileEntry.Document.Text == fileEntry.CSharpFile.OriginalText)
                    {
                        continue;
                    }
                    try
                    {
                        var fileAttributes = File.GetAttributes(fileName);
                        if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            File.SetAttributes(fileName, fileAttributes & ~FileAttributes.ReadOnly);
                        }

                        var backupName = Path.ChangeExtension(fileName, ".cs.backup");
                        for (var i = 1; File.Exists(backupName); i++)
                        {
                            backupName = Path.ChangeExtension(fileName, ".cs.backup" + i);
                        }

                        File.WriteAllText(backupName, fileEntry.CSharpFile.OriginalText);
                        File.WriteAllText(fileName, fileEntry.Document.Text);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Environment.Exit(1);
            }
        }

        private static Type StrategyType(string optionsRefactory)
        {
            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (!File.Exists(optionsRefactory))
            {
                return (
                    from domain in domainAssemblies
                    from t in domain.GetTypes()
                    where string.Equals(t.Name, optionsRefactory, StringComparison.InvariantCultureIgnoreCase)
                    select t).FirstOrDefault();
            }

            var assemblies = domainAssemblies.Select(asmb => asmb.Location).ToArray();
            var results = CodeDomProvider
                .CreateProvider("csharp")
                .CompileAssemblyFromFile(new CompilerParameters(assemblies) {GenerateInMemory = true}, optionsRefactory);

            if (results.Errors.HasErrors)
            {
                foreach (var output in results.Output)
                {
                    Trace.TraceError(output);
                }
                Environment.Exit(1);
            }

            var strategyType = results
                .CompiledAssembly
                .GetTypes()
                .FirstOrDefault();
            return strategyType;
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}
