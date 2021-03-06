﻿using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;

namespace Refactor.Cacheable
{
    public class CacheableRefactorFile : IRefactorFileStrategy
    {
        public void RefactorFile(FileEntry entry)
        {
            var script = entry.Script;
            var methodDeclarations =
                (from methodDeclaration in entry
                    .CSharpFile
                    .SyntaxTree
                    .Descendants
                    .OfType<MethodDeclaration>()
                 from attributesection in methodDeclaration.Attributes
                 from attribute in attributesection.Attributes
                 where attribute.Type.ToString() == "Cacheable"
                 select methodDeclaration).ToArray();

            if (!methodDeclarations.Any())
            {
                return;
            }

            var astResolver = entry.CSharpFile.CreateResolver();

            foreach (var declaration in methodDeclarations)
            {
                var cachingPolicy = "";
                var propertyKeys = "";
                var cloneDeclaration = (MethodDeclaration)declaration.Clone();
                var methodTemplate = ((MemberResolveResult)astResolver.Resolve(declaration)).Member.FullName;

                var cloneAttribute = (
                    from section in cloneDeclaration.Attributes
                    from attribute in section.Attributes
                    where attribute.Type.ToString() == "Cacheable"
                    select new { section, attribute }).First();

                var cachingPolicies =
                    (from expression in cloneAttribute
                        .attribute
                        .Arguments
                        .OfType<NamedExpression>()
                     where expression.Name == "CachingPolicyName"
                     select expression).ToArray();

                var propertyKeysPrimitive =
                    (from expression in cloneAttribute
                        .attribute
                        .Arguments
                        .OfType<PrimitiveExpression>()
                     select expression).ToArray();

                if (propertyKeysPrimitive.Any())
                {
                    propertyKeys = "," + propertyKeysPrimitive[0].Value;
                    var parts = ((string) propertyKeysPrimitive[0].Value).Split(',');
                    methodTemplate += " (";
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (i > 0)
                        {
                            methodTemplate += ", ";
                        }
                        methodTemplate += parts[i] + ": [{" + i + "}]";
                    }
                    methodTemplate += ")";
                }

                if (cachingPolicies.Any())
                {
                    cachingPolicy = ((PrimitiveExpression)cachingPolicies[0].Expression).Value as string;
                }

                cloneDeclaration.Attributes.Remove(cloneAttribute.section);

                var bodyCode = declaration.Body.ToString();
                var newCode = string.Format(
                    "{{\r\nreturn CacheHelper.FetchFromCache(\"{0}\",() =>\r\n{3}\r\n, \"{1}\"{2});}}\r\n",
                    cachingPolicy, methodTemplate, propertyKeys, bodyCode);

                var newBody = entry.CSharpFile.Parser.ParseStatements(newCode);
                cloneDeclaration.Body.ReplaceWith(newBody.First());
                cloneAttribute.section.Remove();

                script.Replace(declaration, cloneDeclaration);
            }
        }
    }
}
