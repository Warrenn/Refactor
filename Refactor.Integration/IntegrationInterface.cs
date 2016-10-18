using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace Refactor.Integration
{
    public class IntegrationInterface :
        ArgsRefactorFileStrategy<IntegrationInterfaceOptions>,
        IRefactorProjectStrategy
    {
        private readonly IntegrationModel model;

        public IntegrationInterface(IntegrationInterfaceOptions options) : base(options)
        {
            options.Agent = string.IsNullOrEmpty(options.Agent) ? "Clients" : options.Agent;

            model = new IntegrationModel
            {
                DataContracts = new List<TypeDeclaration>(),
                ViewModelContracts = new List<TypeViewModel>(),
                Name = options.Agent
            };

        }

        public override void RefactorFile(FileEntry entry)
        {
            var file = entry.CSharpFile;
            var resolver = file.CreateResolver();
            var types = entry
                .CSharpFile
                .SyntaxTree
                .Descendants
                .OfType<TypeDeclaration>()
                .ToArray();

            var serviceDeclaration = GetServiceDeclartion(types);
            if ((serviceDeclaration != null) && (types.Length > 1))
            {
                model.ServiceDeclaration = serviceDeclaration;
                model.ServiceReferencePath = Path.GetDirectoryName(entry.CSharpFile.RelativePath);
                var resolvedServiceType = resolver.Resolve(serviceDeclaration);
                model.ServiceViewModel = TypeManager.CreateTypeViewModel<TypeViewModel>(resolvedServiceType.Type);
                return;
            }

            var typeDeclaration = GetManagerDeclaration(types);
            if (typeDeclaration != null)
            {
                model.ManagerDeclaration = typeDeclaration;
                var resolvedManagerType = resolver.Resolve(typeDeclaration);
                model.ManagerViewModel = TypeManager.CreateTypeViewModel<TypeViewModel>(resolvedManagerType.Type);
                return;
            }

            var contractDeclaration = GetContractDeclaration(types);
            if (contractDeclaration == null)
            {
                return;
            }

            var resolvedContractType = resolver.Resolve(contractDeclaration);
            var contractViewModel = TypeManager.CreateTypeViewModel<TypeViewModel>(resolvedContractType.Type);
            model.ViewModelContracts.Add(contractViewModel);
            model.DataContracts.Add(contractDeclaration);
        }

        static TypeDeclaration GetManagerDeclaration(TypeDeclaration[] types)
        {
            return
            (from type in types
                where
                type.Name.Contains("Manager") &&
                (type.Members.OfType<MethodDeclaration>().Any()) &&
                (type.Members.OfType<ConstructorDeclaration>().Any()) &&
                (!type.Members.OfType<PropertyDeclaration>().Any())
                select type).FirstOrDefault();
        }

        static TypeDeclaration GetContractDeclaration(TypeDeclaration[] types)
        {
            return
            (from type in types
                where
                (!type.Members.OfType<MethodDeclaration>().Any()) &&
                (!type.Members.OfType<ConstructorDeclaration>().Any()) &&
                (type.Members.OfType<PropertyDeclaration>().Any())
                select type).FirstOrDefault();
        }

        static TypeDeclaration GetServiceDeclartion(TypeDeclaration[] types)
        {
            var serviceContracts =
                from declaration in types
                from section in declaration.Attributes
                from attribute in section.Attributes
                let type = attribute.Type as MemberType
                where
                    type != null &&
                    type.MemberName == "ServiceContractAttribute" &&
                    types.Length > 1
                select declaration;

            return serviceContracts.FirstOrDefault();
        }


        public void RefactorProject(CSharpProject project)
        {
            //var cerburusSolution = new Solution(options.CerberusSln);
            //var integrationProject = cerburusSolution.Projects.First(p => p.Title == "AbCap.Cerberus.Integration");


            ////copy the wcf and wsdl files
            //var destinationfolder = integrationProject.Folder + model.ServiceReferencePath;
            //var sourceFolder = project.Folder + model.ServiceReferencePath;
            //if (!Directory.Exists(destinationfolder))
            //{
            //    Directory.CreateDirectory(destinationfolder);
            //}

            //foreach (var file in Directory.GetFiles(sourceFolder))
            //{
            //    var filename = Path.GetFileName(file);
            //    File.Copy(file, Path.Combine(destinationfolder, filename));
            //}
            //FileManager.AddWcfReferenceToProject(integrationProject.MsbuildProject, model.ServiceReferencePath);

            ////generate the agent and add it to the integration project
            //var agentPart = "Agents\\" + model.Name + "\\" + model.ServiceDeclaration.Name + "Agent.cs";
            //var agentPath = Path.Combine(integrationProject.Folder, agentPart);

            //FileManager.CreateFileFromTemplate(agentPath, "integration.agent.cshtml", model);
            //FileManager.AddCompileToProject(integrationProject.MsbuildProject, agentPart);
        }
    }
}
