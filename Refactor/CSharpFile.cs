// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.IO;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace Refactor
{
    public class CSharpFile
    {
        public string FileName { get; private set; }
        public string Folder { get; private set; }
        public string RelativePath { get; private set; }
        public string OriginalText { get; private set; }
        public SyntaxTree SyntaxTree { get; private set; }
        public CSharpParser Parser { get; private set; }
        public CSharpProject Project { get; private set; }
        public CSharpUnresolvedFile UnresolvedTypeSystemForFile { get; private set; }

        public CSharpFile(CSharpProject project, string fileName)
        {
            Project = project;
            FileName = fileName;

            RelativePath = fileName.Substring(project.Folder.Length);
            Folder = Path.GetDirectoryName(FileName);
            Parser = new CSharpParser(project.CompilerSettings);
            OriginalText = File.ReadAllText(fileName);
            SyntaxTree = Parser.Parse(OriginalText, fileName);
            UnresolvedTypeSystemForFile = SyntaxTree.ToTypeSystem();
        }

        public CSharpAstResolver CreateResolver()
        {
            return new CSharpAstResolver(Project.Compilation, SyntaxTree, UnresolvedTypeSystemForFile);
        }
    }
}
