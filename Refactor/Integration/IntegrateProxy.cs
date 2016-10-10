using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using Refactor.Angular;

namespace Refactor.Integration
{
    public class IntegrateProxy :
        ArgsRefactorFileStrategy<IntegrateProxyOptions>,
        IRefactorProjectStrategy
    {
        private readonly IntegrationModel model;

        public IntegrateProxy(IntegrateProxyOptions options) : base(options)
        {
             
            model = new IntegrationModel();
        }

        public override void RefactorFile(FileEntry entry)
        {
            if (IsServiceReferenceFile(entry))
            {
                model.ServiceReferencePath = Path.GetDirectoryName(entry.CSharpFile.RelativePath);
            }
            //find the contract(s)
            //find the manager
            //find the reference
            //entry.CSharpFile.Project.MsbuildProject.AddItem(Microsoft.Build.Evaluation.ProjectM)
        }

        private static bool IsServiceReferenceFile(FileEntry entry)
        {
            var types = entry
                .CSharpFile
                .SyntaxTree
                .Descendants
                .OfType<TypeDeclaration>()
                .ToArray();

            var serviceContracts =
                from declaration in types
                from section in declaration.Attributes
                from attribute in section.Attributes
                let type = attribute.Type as MemberType
                where type != null && type.MemberName == "ServiceContractAttribute"
                select attribute;

            return
                types.Count() > 1 &&
                serviceContracts.Any();
        }


        public void RefactorProject(CSharpProject project)
        {
            FileManager.AddWcfReferenceToProject(project.MsbuildProject, model.ServiceReferencePath);
            //throw new NotImplementedException();
        }
    }
}
