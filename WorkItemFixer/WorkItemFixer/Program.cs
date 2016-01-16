using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WorkItemFixer
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var teamFoundationServer = new TfsTeamProjectCollection(new Uri("http://vstfdevdiv:8080/DevDiv2"));
            var tfsWorkItemStore = new WorkItemStore(new TfsTeamProjectCollection(new Uri("http://vstfdevdiv:8080/DevDiv2")));
            var roslynWorkItemStore = new WorkItemStore(new TfsTeamProjectCollection(new Uri("http://vstfdevdiv:8080/DevDiv_Projects")));
            var workItemInfo = new WorkItemData(tfsWorkItemStore, roslynWorkItemStore);
            var files = Directory.EnumerateFiles(@"e:\dd\roslyn\src", "*.cs", SearchOption.AllDirectories);
            var unknownList = new List<string>();
            foreach (var filePath in files)
            {
                Console.WriteLine($"{filePath}");
                SourceText text;
                using (var stream = File.OpenRead(filePath))
                {
                    text = SourceText.From(stream);
                }

                var fixer = new WorkItemRewriter(workItemInfo, filePath, unknownList);
                var syntaxTree = CSharpSyntaxTree.ParseText(text);
                var node = syntaxTree.GetRoot();
                var newNode = fixer.Visit(node);
                if (node != newNode)
                {
                    /* 
                    var newSyntaxTree = syntaxTree.WithRootAndOptions(newNode, syntaxTree.Options);
                    using (var writer = new StreamWriter(filePath, append: false, encoding: text.Encoding))
                    {
                        newSyntaxTree.GetText().Write(writer);
                    }
                    */
                }
            }

            File.WriteAllLines(@"c:\users\jaredpar\data.txt", unknownList.ToArray());
        }
    }

    internal struct WorkItemInfo
    {
        internal readonly int Id;
        internal readonly int OldId;
        internal readonly string Description;

        internal WorkItemInfo(int id, string description, int? oldId = null)
        {
            Id = id;
            Description = description;
            OldId = oldId ?? id;
        }
    }

    internal sealed class WorkItemData
    {
        private readonly WorkItemStore _tfsWorkItemStore;
        private readonly WorkItemStore _roslynWorkItemStore;
        private readonly Dictionary<int, int?> _idMap = new Dictionary<int, int?>();
        private readonly Dictionary<int, bool> _githubMap = new Dictionary<int, bool>();

        internal WorkItemData(WorkItemStore tfsWorkItemStore, WorkItemStore roslynWorkItemStore)
        {
            _tfsWorkItemStore = tfsWorkItemStore;
            _roslynWorkItemStore = roslynWorkItemStore;
        }

        internal bool TryGetMigratedInfo(int id, out int newId)
        {
            int? value;
            if (!_idMap.TryGetValue(id, out value))
            {
                value = GetMigratedInfoCore(id);
                _idMap[id] = value;
            }

            newId = value ?? 0;
            return value.HasValue;
        }

        /// <summary>
        /// Is this a bug from the old Roslyn database? 
        /// </summary>
        internal bool IsRoslynBug(int id)
        {
            try
            {
                var workItem = _roslynWorkItemStore.GetWorkItem(id);
                return workItem.AreaPath.Contains("Roslyn");
            }
            catch
            {
                return false;
            }
        }

        internal bool IsGithubBug(int id)
        {
            if (id > 9000)
            {
                return false;
            }

            bool value;
            if (!_githubMap.TryGetValue(id, out value))
            {
                value = IsGithubBugCore(id);
                _githubMap[id] = value;
            }

            return value;
        }

        private bool IsGithubBugCore(int id)
        {
            int count = 5;
            while (count > 0)
            {
                try
                {
                    var url = $"https://github.com/dotnet/roslyn/issues/{id}";
                    var request = WebRequest.CreateHttp(url);
                    var response = (HttpWebResponse)request.GetResponse();
                    return response.StatusCode == HttpStatusCode.OK;
                }
                catch
                {
                }
                count--;
            }

            return false;
        }

        internal bool IsDevDivTfsBug(int id)
        {
            try
            {
                var workItem = _tfsWorkItemStore.GetWorkItem(id);
                var areaPath = workItem.AreaPath;
                if (areaPath.StartsWith(@"DevDiv\Cloud Platform\Managed Languages"))
                {
                    return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        private int? GetMigratedInfoCore(int id)
        {
            try
            {
                var workItem = _tfsWorkItemStore.GetWorkItem(id);
                var resolutionField = workItem.Fields["Resolution"];
                if (resolutionField?.Value?.ToString() == "Migrated to VSO")
                {
                    var mirrorField = workItem.Fields["Mirrored TFS ID"];
                    if (mirrorField?.Value != null)
                    {
                        return int.Parse(mirrorField.Value.ToString());
                    }
                }
            }
            catch (Exception)
            {

            }

            return null;
        }
    }

    internal sealed class WorkItemUtil
    {
        private const string UrlVso = "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id={0}";
        private const string UrlRoslyn = "http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems#id={0}&_a=edit";
        private const string UrlDevDiv = "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems#id={0}&_a=edit";
        private const string UrlGithub = "https://github.com/dotnet/roslyn/issues/{0}";

        private readonly WorkItemData _workItemData;
        private readonly List<string> _unknownList;

        internal WorkItemUtil(WorkItemData workItemData, List<string> unknownList)
        {
            _workItemData = workItemData;
            _unknownList = unknownList;
        }

        internal WorkItemInfo? GetUpdatedWorkItemInfo(string filePath, FileLinePositionSpan loc, int id, string description)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals("devdiv", description))
            {
                if (_workItemData.IsDevDivTfsBug(id))
                {
                    return new WorkItemInfo(id, string.Format(UrlDevDiv, id));
                }
            }

            if (string.IsNullOrEmpty(description))
            {
                if (_workItemData.IsDevDivTfsBug(id))
                {
                    return new WorkItemInfo(id, string.Format(UrlDevDiv, id));
                }

                if (_workItemData.IsGithubBug(id) && !_workItemData.IsRoslynBug(id))
                {
                    return new WorkItemInfo(id, string.Format(UrlGithub, id));
                }

                if (!_workItemData.IsGithubBug(id) && _workItemData.IsRoslynBug(id))
                {
                    return new WorkItemInfo(id, string.Format(UrlRoslyn, id));
                }

                _unknownList.Add($"{filePath} line {loc.StartLinePosition.Line} id {id}");
            }

            return null;
        }
    }
}
