using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
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
            var parser = new Parser(s => s.IgnoreUnknownArguments = true);
            var options = new Options();
            if (!parser.ParseArguments(args, options))
            {
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(options.Project) && string.IsNullOrEmpty(options.Solution))
            {
                Trace.TraceError("Either the project or the solution must be specified");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(options.Solution) && !File.Exists(options.Project))
            {
                Trace.TraceError("The project file specified could not be found");
                Environment.Exit(1);                
            }

            if (!File.Exists(options.Solution) && string.IsNullOrEmpty(options.Project))
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

            object strategy;
            if (strategyType.BaseType != null &&
                strategyType.BaseType.IsGenericType &&
                strategyType.BaseType.GetGenericTypeDefinition() == typeof (ArgsRefactorFileStrategy<>))
            {
                var optionsType = strategyType.BaseType.GetGenericArguments()[0];
                var strategyOptions = Activator.CreateInstance(optionsType);
                if (!parser.ParseArguments(args, strategyOptions))
                {
                    Environment.Exit(1);
                }
                strategy = Activator.CreateInstance(strategyType, strategyOptions);
            }
            else
            {
                strategy = Activator.CreateInstance(strategyType);
            }

            var fileStrategy = strategy as IRefactorFileStrategy;
            var projectStrategy = strategy as IRefactorProjectStrategy;      

            if ((fileStrategy == null) && (projectStrategy == null))
            {
                Trace.TraceError("The type couldn't be cast to a valid File Strategy or Project Strategy");
                Environment.Exit(1);
            }
            
            try
            {
                IEnumerable<CSharpProject> projects;

                if (!string.IsNullOrEmpty(options.Solution))
                {
                    var solution = new Solution(options.Solution);
                    projects = solution.Projects;
                }
                else
                {
                    var title = Path.GetFileNameWithoutExtension(options.Project);
                    projects = new[] { new CSharpProject(null, title, options.Project) };
                }

                var editorOptions = new TextEditorOptions();
                var formattingOptions = FormattingOptionsFactory.CreateAllman();

                foreach (var project in projects)
                {
                    if (fileStrategy == null)
                    {
                        projectStrategy.RefactorProject(project);
                        continue;
                    }

                    var fileEntries =
                        from file in project.Files
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
                        fileStrategy.RefactorFile(fileEntry);
                        if (fileEntry.Document.Text == fileEntry.CSharpFile.OriginalText)
                        {
                            continue;
                        }
                        FileManager.BackupFile(fileName);
                        File.WriteAllText(fileName, fileEntry.Document.Text);
                    }

                    if (projectStrategy == null)
                    {
                        continue;
                    }

                    projectStrategy.RefactorProject(project);
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
