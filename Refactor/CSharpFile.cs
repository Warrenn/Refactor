﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
        public string FileName { get; }
        public string OriginalText { get; }
        public SyntaxTree SyntaxTree { get; }
        public CSharpParser Parser { get; }
        public CSharpProject Project { get; }
        public CSharpUnresolvedFile UnresolvedTypeSystemForFile { get; }

        public CSharpFile(CSharpProject project, string fileName)
        {
            Project = project;
            FileName = fileName;

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
