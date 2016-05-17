using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Refactor
{
    public class CSharpProject
    {
        public string Title { get; private set; }
        public string AssemblyName { get; private set; }
        public string FileName { get; private set; }
        public CompilerSettings CompilerSettings { get; private set; }
        public IProjectContent ProjectContent { get; private set; }
        public ICompilation Compilation { get; set; }
        public IEnumerable<CSharpFile> Files { get; private set; }
        public Project MsbuildProject { get; private set; }
        public Solution Solution { get; private set; }

        public CSharpProject(Solution solution, string title, string fileName)
        {
            // Normalize the file name
            fileName = Path.GetFullPath(fileName);

            Solution = solution;
            Title = title;
            FileName = fileName;

            // Use MSBuild to open the .csproj
            MsbuildProject = new Microsoft.Build.Evaluation.Project(fileName);
            // Figure out some compiler settings
            AssemblyName = MsbuildProject.GetPropertyValue("AssemblyName");
            CompilerSettings = new CompilerSettings
            {
                AllowUnsafeBlocks = GetBoolProperty(MsbuildProject, "AllowUnsafeBlocks") ?? false,
                CheckForOverflow = GetBoolProperty(MsbuildProject, "CheckForOverflowUnderflow") ?? false
            };
            var defineConstants = MsbuildProject.GetPropertyValue("DefineConstants");
            foreach (var symbol in defineConstants.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
            {
                CompilerSettings.ConditionalSymbols.Add(symbol.Trim());
            }

            // Initialize the unresolved type system
            IProjectContent projectContent = new CSharpProjectContent();
            projectContent = projectContent.SetAssemblyName(AssemblyName);
            projectContent = projectContent.SetProjectFileName(fileName);
            projectContent = projectContent.SetCompilerSettings(CompilerSettings);

            Files = MsbuildProject
                .GetItems("Compile")
                .Select(item => new CSharpFile(this, Path.Combine(MsbuildProject.DirectoryPath, item.EvaluatedInclude)));

            // Add parsed files to the type system
            projectContent = projectContent.AddOrUpdateFiles(Files.Select(f => f.UnresolvedTypeSystemForFile));

            // Add referenced assemblies:
            projectContent = ResolveAssemblyReferences(MsbuildProject)
                .Select(solution.LoadAssembly)
                .Aggregate(projectContent, (current, assembly) => current.AddAssemblyReferences(assembly));

            // Add project references:
            projectContent = MsbuildProject
                .GetItems("ProjectReference")
                .Select(item => Path.Combine(MsbuildProject.DirectoryPath, item.EvaluatedInclude))
                .Select(Path.GetFullPath)
                .Aggregate(projectContent,
                    (current, referencedFileName) =>
                        current.AddAssemblyReferences(new ProjectReference(referencedFileName)));

            ProjectContent = projectContent;
        }

        IEnumerable<string> ResolveAssemblyReferences(Microsoft.Build.Evaluation.Project project)
        {
            // Use MSBuild to figure out the full path of the referenced assemblies
            var projectInstance = project.CreateProjectInstance();
            projectInstance.SetProperty("BuildingProject", "false");
            project.SetProperty("DesignTimeBuild", "true");

            projectInstance.Build("ResolveAssemblyReferences", new[] { new ConsoleLogger(LoggerVerbosity.Minimal) });
            var items = projectInstance.GetItems("_ResolveAssemblyReferenceResolvedFiles");
            var baseDirectory = Path.GetDirectoryName(FileName);
            return items.Select(i => baseDirectory != null ? Path.Combine(baseDirectory, i.GetMetadataValue("Identity")) : null);
        }

        static bool? GetBoolProperty(Microsoft.Build.Evaluation.Project project, string propertyName)
        {
            var value = project.GetPropertyValue(propertyName);
            bool result;
            return bool.TryParse(value, out result) ? result : (bool?)null;
        }

        public override string ToString()
        {
            return string.Format("[CSharpProject AssemblyName={0}]", AssemblyName);
        }
    }
}
