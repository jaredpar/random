using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkItemFixer
{
    internal sealed class CSharpWorkItemRewriter : CSharpSyntaxRewriter, IRewriter
    {
        private readonly WorkItemUtil _workItemUtil;
        private readonly string _filePath;

        internal CSharpWorkItemRewriter(WorkItemUtil workItemUtil, string filePath)
        {
            _workItemUtil = workItemUtil;
            _filePath = filePath;
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            int id;
            string description;
            if (!CSharpAttributeUtil.TryGetInformation(node, out id, out description))
            {
                return node;
            }

            var info = _workItemUtil.GetUpdatedWorkItemInfo(_filePath, node.GetLocation().GetMappedLineSpan(), id, description);
            if (info.HasValue)
            {
                node = CSharpAttributeUtil.UpdateAttribute(node, info.Value.Id, info.Value.Description);
            }

            return node;
        }

        public SourceText TryUpdate(SourceText text)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(text);
            var node = syntaxTree.GetRoot();
            var newNode = Visit(node);
            return node != newNode
                ? syntaxTree.WithRootAndOptions(newNode, syntaxTree.Options).GetText()
                : null;
        }
    }

    internal static class CSharpAttributeUtil
    {
        internal static AttributeSyntax UpdateAttribute(AttributeSyntax node, int id, string url)
        {
            var descriptionArg = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(value: url).WithLeadingTrivia(SyntaxFactory.Space)));
            var idArg = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(value: id)));

            var list = SyntaxFactory.SeparatedList(new[] { idArg, descriptionArg });
            return node.WithArgumentList(SyntaxFactory.AttributeArgumentList(list));
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
                list.Add(arg.Expression.ToString().Replace("\"", ""));
            }
            return list;
        }
    }

    /*
    /// <summary>
    /// Update all of our WorkItem attributes that refer to bugs that changed IDs when our database was 
    /// migrated to VSO.
    /// </summary>
    internal sealed class WorkItemMigrator : CSharpSyntaxRewriter
    {
        private readonly WorkItemInfo _workItemInfo;

        internal WorkItemMigrator(WorkItemInfo workItemInfo)
        {
            _workItemInfo = workItemInfo;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var list = TryGetMappedIds(node);
            if (list != null)
            {
                var text = node.Identifier.Text;
                foreach (var pair in list)
                {
                    text = text.Replace(pair.Item1.ToString(), pair.Item2.ToString());
                }

                if (text != node.Identifier.Text)
                {
                    node = node.WithIdentifier(SyntaxFactory.Identifier(
                        node.Identifier.LeadingTrivia,
                        text,
                        node.Identifier.TrailingTrivia));
                }
            }

            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            int oldId;
            int newId;
            if (TryGetMigratedInfo(node, oldId: out oldId, newId: out newId))
            {
                var description = $"https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id={newId}";
                node = AttributeUtil.UpdateAttribute(node, newId, description);
            }

            return base.VisitAttribute(node);
        }

        private List<Tuple<int, int>> TryGetMappedIds(MethodDeclarationSyntax node)
        {
            List<Tuple<int, int>> list = null;
            foreach (var attributeList in node.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    int newId;
                    int oldId;
                    if (TryGetMigratedInfo(attribute, oldId: out oldId, newId: out newId))
                    {
                        list = list ?? new List<Tuple<int, int>>();
                        list.Add(Tuple.Create(oldId, newId));
                    }
                }
            }

            return list;
        }

        private bool TryGetMigratedInfo(AttributeSyntax node, out int oldId, out int newId)
        {
            string description;
            if (!AttributeUtil.TryGetInformation(node, out oldId, out description))
            {
                newId = 0;
                return false;
            }

            if (!StringComparer.OrdinalIgnoreCase.Equals("devdiv", description))
            {
                newId = 0;
                return false;
            }

            return _workItemInfo.TryGetMigratedInfo(oldId, out newId);
        }
    }
    */
}
