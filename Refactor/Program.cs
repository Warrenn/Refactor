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
        public static IEnumerable<FileEntry> GetFileEntries(CSharpProject project, string backupId)
        {
            var editorOptions = new TextEditorOptions();
            var formattingOptions = FormattingOptionsFactory.CreateAllman();

            return
                from file in project.Files
                let document = new StringBuilderDocument(file.OriginalText)
                let script = new DocumentScript(document, formattingOptions, editorOptions)
                select new FileEntry
                {
                    CSharpFile = file,
                    Document = document,
                    Script = script,
                    BackupId = backupId
                };
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var parser = new Parser(s => s.IgnoreUnknownArguments = true);
            var options = new Options();
            if (!parser.ParseArguments(args, options))
            {
                Environment.Exit(1);
            }

            if (!File.Exists(options.Solution))
            {
                Trace.TraceError("The solution file must exist");
                Environment.Exit(1);
            }

            var strategyType = StrategyType(options.Refactory);
            object strategy;

            if (strategyType == null)
            {
                Trace.TraceError("Invalid type {0} specified ", options.Refactory);
                Environment.Exit(1);
            }

            if (strategyType.BaseType != null &&
                strategyType.BaseType.IsGenericType &&
                strategyType.BaseType.GetGenericTypeDefinition() == typeof(ArgsRefactorFileStrategy<>))
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
                var solution = new Solution(options.Solution);
                var projects = solution.Projects;

                foreach (var project in projects.Where(p =>
                    string.IsNullOrEmpty(options.Project) ||
                    string.Equals(options.Project, p.Title, StringComparison.OrdinalIgnoreCase)))
                {
                    project.BackupId = options.BackupId;
                    if (fileStrategy == null)
                    {
                        projectStrategy.RefactorProject(project);
                        continue;
                    }

                    foreach (var fileEntry in GetFileEntries(project, options.BackupId))
                    {
                        fileEntry.CSharpFile.SyntaxTree.Freeze();
                        fileStrategy.RefactorFile(fileEntry);
                        FileManager.CopyIfChanged(fileEntry, options.BackupId);
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
                Trace.TraceError(ex.ToString());
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
                .CompileAssemblyFromFile(new CompilerParameters(assemblies) { GenerateInMemory = true }, optionsRefactory);

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
