using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;

namespace Refactor
{
    public class TryCatchRefactor : IRefactorStrategy
    {
        public void Refactor(FileEntry entry)
        {
            var script = entry.Script;
            var catchStatements =
                from invocation in entry
                    .CSharpFile
                    .SyntaxTree
                    .Descendants
                    .OfType<TryCatchStatement>()
                select invocation;

            foreach (var statement in catchStatements)
            {
                var astResolver = entry.CSharpFile.CreateResolver();
                var expectedType = astResolver.Compilation.FindType(typeof (Exception));
                var catchClause = statement
                    .CatchClauses
                    .FirstOrNullObject(c => astResolver.Resolve(c.Type).Type.Equals(expectedType));

                if (catchClause == null || catchClause.IsNull)
                {
                    continue;
                }

                var identifiers = catchClause.Body.Descendants.OfType<IdentifierExpression>().ToArray();
                if (!string.IsNullOrEmpty(catchClause.VariableName) &&
                    identifiers.Any() &&
                    identifiers.Any(i => i.Identifier == catchClause.VariableName))
                {
                    continue;
                }

                var variableName = string.IsNullOrEmpty(catchClause.VariableName) ? "ex" : catchClause.VariableName;
                var catchClone = (CatchClause) catchClause.Clone();

                catchClone.VariableName = variableName;
                catchClone.Body.Statements.Add(
                    new ExpressionStatement(
                        new InvocationExpression(
                            new MemberReferenceExpression(
                                new MemberReferenceExpression(
                                    new MemberReferenceExpression(
                                        new IdentifierExpression("System")
                                        , "Diagnostics")
                                    , "Trace"),
                                "WriteLine"),
                            new MemberReferenceExpression(
                                new IdentifierExpression(variableName), "Message"))));
                script.Replace(catchClause, catchClone);
            }
        }
    }
}
