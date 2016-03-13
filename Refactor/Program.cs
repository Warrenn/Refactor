using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.Editor;
using StringIndexOf;

namespace Refactor
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(-1);
            }

            if (!File.Exists(options.Refactory))
            {
                Trace.TraceError("The refactory strategy must exist");
                Environment.Exit(-1);
            }

            if (!File.Exists(options.Solution))
            {
                Trace.TraceError("The solution file must exist");
                Environment.Exit(-1);
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(asmb => asmb.Location).ToArray();
            var results = CodeDomProvider
                .CreateProvider("csharp")
                .CompileAssemblyFromFile(new CompilerParameters(assemblies) {GenerateInMemory = true}, options.Refactory);

            if (results.Errors.HasErrors)
            {
                foreach (var output in results.Output)
                {
                    Trace.TraceError(output);
                }
                Environment.Exit(-1);
            }

            var strategyType = results
                .CompiledAssembly
                .GetTypes()
                .FirstOrDefault();

            if (strategyType == null)
            {
                Trace.TraceError("A valid type included in refactoy file");
                Environment.Exit(-1);
            }

            var strategy = Activator.CreateInstance(strategyType) as IRefactorStrategy;

            if (strategy == null)
            {
                Trace.TraceError("The type couldn't be cast to a valid strategy");
                Environment.Exit(-1);
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
                    Trace.WriteLine(fileEntry.CSharpFile.FileName);
                    strategy.Refactor(fileEntry);
                    if (fileEntry.Document.Text == fileEntry.CSharpFile.OriginalText)
                    {
                        continue;
                    }

                    File.WriteAllText(
                        Path.ChangeExtension(fileEntry.CSharpFile.FileName, ".cs.backup"),
                        fileEntry.CSharpFile.OriginalText);
                    File.WriteAllText(fileEntry.CSharpFile.FileName, fileEntry.Document.Text);
                }

            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Environment.Exit(-1);
            }
        }
    }
}
