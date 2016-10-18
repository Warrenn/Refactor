using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor
{
    public static class TypeManager
    {
        public static string CamelCase(string name)
        {
            return name.Substring(0, 1).ToLower() + name.Substring(1);
        }

        public static IEnumerable<ResolveResult> GetServices(CSharpFile file, CSharpAstResolver resolver)
        {
            return
                from codetype in file.SyntaxTree.Descendants.OfType<TypeDeclaration>()
                let resolvedType = resolver.Resolve(codetype)
                where
                codetype.Attributes.Any(s => s.Attributes.Any(a => a.Type.ToString() == "ServiceContract"))
                select resolvedType;
        }


        public static string SplitByCase(string value)
        {
            var r = new Regex(@"
                        (?<=[A-Z])(?=[A-Z][a-z]) |
                        (?<=[^A-Z])(?=[A-Z]) |
                        (?<=[A-Za-z])(?=[^A-Za-z])",
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            return r.Replace(value, " ");
        }

        public static IEnumerable<string> GetUsings(IType type)
        {
            var list = new List<string> { type.Namespace };
            list.AddRange(
                from property in type.GetProperties(p => true)
                let returnType = property.ReturnType
                select returnType.Namespace);
            list.AddRange(
                from property in type.GetProperties(p => true)
                let returnType = property.ReturnType
                from typeArgument in returnType.TypeArguments
                select typeArgument.Namespace);
            list.AddRange(
                from method in type.GetMethods(m => true)
                let returnType = method.ReturnType
                select returnType.Namespace);
            list.AddRange(
                from method in type.GetMethods(m => true)
                let returnType = method.ReturnType
                from typeArgument in returnType.TypeArguments
                select typeArgument.Namespace);
            list.AddRange(
                from method in type.GetMethods(m => true)
                from parameter in method.Parameters
                let parameterType = parameter.Type
                select parameterType.Namespace);
            list.AddRange(
                from method in type.GetMethods(m => true)
                from parameter in method.Parameters
                let parameterType = parameter.Type
                from typeArgument in parameterType.TypeArguments
                select typeArgument.Namespace);
            return list.Distinct();
        }

        public static bool IsPage(IParameterizedMember method)
        {
            return
                method.Parameters.Any(p => p.Type.Name == "Request" && p.Type.TypeParameterCount > 0) &&
                method.ReturnType.Name == "Page" &&
                method.ReturnType.TypeParameterCount > 0;
        }

        public static string GetTypeName(IType type)
        {
            var name = type.Name;
            if (!type.IsParameterized) return name;
            var generics = type.TypeArguments.Select(GetTypeName).ToArray();
            return name + "<" + string.Join(",", generics) + ">";
        }

        public static T CreateTypeViewModel<T>(IType serviceDeclarationType)
            where T : TypeViewModel, new()
        {
            var viewModel = new T
            {
                CamelCaseName = CamelCase(serviceDeclarationType.Name),
                Name = serviceDeclarationType.Name,
                Type = serviceDeclarationType,
                Usings = GetUsings(serviceDeclarationType),
                Properties = serviceDeclarationType
                    .GetProperties(p => p.CanGet || p.CanSet, GetMemberOptions.ReturnMemberDefinitions)
                    .Select(p => new TypeViewModel.PropertyModel
                    {
                        CamelCaseName = CamelCase(p.Name),
                        Name = p.Name,
                        Property = p,
                        ReturnTypeName = GetTypeName(p.ReturnType)
                    }),
                Methods = serviceDeclarationType
                    .GetMethods(m => m.IsPublic && !m.IsStatic, GetMemberOptions.ReturnMemberDefinitions)
                    .Select(m => new TypeViewModel.MethodCall
                    {
                        CamelCaseName = CamelCase(m.Name),
                        Name = m.Name,
                        ReturnType = m.ReturnType,
                        Method = m,
                        ReturnTypeName = GetTypeName(m.ReturnType),
                        Parameters = m.Parameters.Select(p => new TypeViewModel.ParameterModel
                        {
                            Parameter = p,
                            Name = p.Name,
                            TypeName = GetTypeName(p.Type)
                        }),
                        ParameterStrings = m.Parameters.Select(p => GetTypeName(p.Type) + " " + p.Name).ToArray()
                    })
            };
            return viewModel;
        }
    }
}
