using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WorkItemFixer
{
    internal sealed class BasicWorkItemRewriter : VisualBasicSyntaxRewriter, IRewriter
    {
        private readonly WorkItemUtil _workItemUtil;
        private readonly string _filePath;

        internal BasicWorkItemRewriter(WorkItemUtil workItemUtil, string filePath)
        {
            _workItemUtil = workItemUtil;
            _filePath = filePath;
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            int id;
            string description;
            if (!BasicAttributeUtil.TryGetInformation(node, out id, out description))
            {
                return node;
            }

            var info = _workItemUtil.GetUpdatedWorkItemInfo(_filePath, node.GetLocation().GetMappedLineSpan(), id, description);
            if (info.HasValue)
            {
                node = BasicAttributeUtil.UpdateAttribute(node, info.Value.Id, info.Value.Description);
            }

            return node;
        }

        public SourceText TryUpdate(SourceText text)
        {
            try
            {
                var syntaxTree = VisualBasicSyntaxTree.ParseText(text);
                var node = syntaxTree.GetRoot();
                var newNode = Visit(node);
                return node != newNode
                    ? syntaxTree.WithRootAndOptions(newNode, syntaxTree.Options).GetText()
                    : null;
            }
            catch (InsufficientExecutionStackException)
            {
                return null;
            }
        }
    }

    internal static class BasicAttributeUtil
    {
        internal static AttributeSyntax UpdateAttribute(AttributeSyntax node, int id, string url)
        {
            var descriptionArg = SyntaxFactory.SimpleArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(value: url).WithLeadingTrivia(SyntaxFactory.Space)));
            var idArg = SyntaxFactory.SimpleArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(value: id)));

            var list = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new[] { idArg, descriptionArg }));
            return node.WithArgumentList(list);
        }

        internal static bool TryGetInformation(AttributeSyntax node, out int id, out string description)
        {
            id = 0;
            description = null;

            var name = node.Name as IdentifierNameSyntax;
            if (name?.Identifier.Text != "WorkItem")
            {
                return false;
            }

            var args = GetArgs(node);
            if (args.Count == 1 && int.TryParse(args[0], out id))
            {
                return true;
            }

            if (args.Count == 2 && int.TryParse(args[0], out id))
            {
                description = args[1].ToString();
                return true;
            }

            return false;
        }

        private static List<string> GetArgs(AttributeSyntax node)
        {
            var list = new List<string>();
            foreach (var arg in node.ArgumentList.Arguments)
            {
                list.Add(arg.GetExpression().ToString().Replace("\"", ""));
            }
            return list;
        }
    }
}
