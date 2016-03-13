using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;
using Refactor;

namespace StringIndexOf
{
	public class Solution
	{
	    const string ProjectGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        readonly ConcurrentDictionary<string, IUnresolvedAssembly> assemblyDict = 
            new ConcurrentDictionary<string, IUnresolvedAssembly>(Platform.FileNameComparer);
        static readonly Regex ProjectLinePattern = new Regex(
	        "Project\\(\"(?<TypeGuid>.*)\"\\)\\s+=\\s+\"(?<Title>.*)\",\\s*\"(?<Location>.*)\",\\s*\"(?<Guid>.*)\"",
	        RegexOptions.Compiled);

        public string Directory { get; }
		public IEnumerable<CSharpProject> Projects { get; }
		public IEnumerable<CSharpFile> AllFiles { get; }

        public Solution(string fileName)
		{
			Directory = Path.GetDirectoryName(fileName);
		    Projects = (
		        from line in File.ReadLines(fileName)
		        let match = ProjectLinePattern.Match(line)
		        where match.Success
		        let typeGuid = match.Groups["TypeGuid"].Value
		        let title = match.Groups["Title"].Value
		        let location = match.Groups["Location"].Value
		        where string.Equals(typeGuid, ProjectGuid, StringComparison.InvariantCultureIgnoreCase)
		        select new CSharpProject(this, title, Path.Combine(Directory, location))).ToArray();

            var solutionSnapshot = new DefaultSolutionSnapshot(Projects.Select(p => p.ProjectContent));
			foreach (var project in Projects) {
				project.Compilation = solutionSnapshot.GetCompilation(project.ProjectContent);
			}

		    AllFiles =
		        from project in Projects
		        from file in project.Files
		        select file;
		}

        public IUnresolvedAssembly LoadAssembly(string assemblyFileName)
		{
			return assemblyDict.GetOrAdd(assemblyFileName, file => new CecilLoader().LoadAssemblyFile(file));
		}
	}
}
