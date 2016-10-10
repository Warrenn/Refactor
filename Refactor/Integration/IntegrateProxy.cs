using System;
using System.Collections.Generic;
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
            //find the contract(s)
            //find the manager
            //find the reference
            //entry.CSharpFile.Project.MsbuildProject.AddItem(Microsoft.Build.Evaluation.ProjectM)
        }

        public void RefactorProject(CSharpProject project)
        {
            //throw new NotImplementedException();
        }
    }
}
