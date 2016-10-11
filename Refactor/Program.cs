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
        public static IEnumerable<FileEntry> GetFileEntries(CSharpProject project)
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
                    Script = script
                };
        }

        public static IEnumerable<CSharpProject> GetProjects(Solution solution) => solution.Projects.Where(p =>
            string.IsNullOrEmpty(Options.CurrentOptions.Project) ||
            string.Equals(Options.CurrentOptions.Project, p.Title, StringComparison.OrdinalIgnoreCase));

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            var parser = new Parser(s => s.IgnoreUnknownArguments = true);
            var options = new Options();
            if (!parser.ParseArguments(args, options))
            {
                Environment.Exit(1);
            }

            Options.CurrentOptions = options;
            var templateFolder = string.IsNullOrEmpty(options.TemplatesFolder) ? "Templates" : options.TemplatesFolder;
            var solutionFolder = Path.GetDirectoryName(options.Solution);
            options.TemplatesFolder = Path.Combine(solutionFolder, templateFolder);
            
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
                var projectsEnumerator = strategy as IProjectsEnumerator;

                var projects = projectsEnumerator != null
                    ? projectsEnumerator.GetCSharpProjects(solution)
                    : GetProjects(solution);

                foreach (var project in projects)
                {
                    if (fileStrategy == null)
                    {
                        projectStrategy.RefactorProject(project);
                        continue;
                    }

                    var fileEnumerator = strategy as IFileEntriesEnumerator;
                    var fileEntries = fileEnumerator != null
                        ? fileEnumerator.GetFileEntries(project)
                        : GetFileEntries(project);

                    foreach (var fileEntry in fileEntries)
                    {
                        fileEntry.CSharpFile.SyntaxTree.Freeze();
                        fileStrategy.RefactorFile(fileEntry);
                        FileManager.CopyIfChanged(fileEntry);
                    }

                    projectStrategy?.RefactorProject(project);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Environment.Exit(1);
            }
        }

        static Type StrategyType(string optionsRefactory)
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

        static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceError(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}
