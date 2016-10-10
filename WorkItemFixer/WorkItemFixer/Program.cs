using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
using System.Text.RegularExpressions;
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
            var workItemData = new WorkItemData(tfsWorkItemStore, roslynWorkItemStore);
            var unknownList = new List<string>();
            var workItemUtil = new WorkItemUtil(workItemData, unknownList);

            var dataFile = @"c:\users\jaredpar\data.txt";
            var root = @"e:\dd\roslyn\src";
            var files =
                Directory.EnumerateFiles(root, "*.vb", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories));

            foreach (var filePath in files)
            {
                Console.WriteLine($"{filePath}");
                SourceText text;
                using (var stream = File.OpenRead(filePath))
                {
                    text = SourceText.From(stream);
                }

                var rewriter = Path.GetExtension(filePath) == ".cs"
                    ? (IRewriter)new CSharpWorkItemRewriter(workItemUtil, filePath)
                    : (IRewriter)new BasicWorkItemRewriter(workItemUtil, filePath);

                var newText = rewriter.TryUpdate(text);
                if (newText != null)
                {
                    using (var writer = new StreamWriter(filePath, append: false, encoding: text.Encoding))
                    {
                        newText.Write(writer);
                    }
                }

                if (unknownList.Count > 20)
                {
                    File.AppendAllLines(dataFile, unknownList);
                    unknownList.Clear();
                }
            }

            File.AppendAllLines(dataFile, unknownList.ToArray());
        }
    }

    interface IRewriter
    {
        SourceText TryUpdate(SourceText text);
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

        internal bool IsDevDivTfsBug(int id, bool checkAreaPath = true)
        {
            try
            {
                var workItem = _tfsWorkItemStore.GetWorkItem(id);
                var areaPath = workItem.AreaPath;
                if (checkAreaPath)
                {
                    return areaPath.StartsWith(@"DevDiv\Cloud Platform\Managed Languages");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
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
        private const string UrlVso = "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems/edit/{0}";
        private const string UrlRoslyn = "http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems/edit/{0}";
        private const string UrlDevDiv = "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/{0}";
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

                if (id > 100000 && _workItemData.IsDevDivTfsBug(id, checkAreaPath: false))
                {
                    return new WorkItemInfo(id, string.Format(UrlDevDiv, id));
                }

                _unknownList.Add($"{filePath} line {loc.StartLinePosition.Line} id {id}");
            }

            if (RewriteUrl(id, ref description))
            {
                return new WorkItemInfo(id, description);
            }

            Uri uri;
            if (!Uri.TryCreate(description, UriKind.Absolute, out uri))
            {
                _unknownList.Add($"Bad Url {filePath} {loc.StartLinePosition.Line}");
            }

            return null;
        }

        internal static bool RewriteUrl(int id, ref string description)
        {
            Uri uri;
            if (!Uri.TryCreate(description, UriKind.Absolute, out uri) || string.IsNullOrEmpty(uri.Fragment))
            {
                return false;
            }

            var builder = new UriBuilder(uri);
            builder.Fragment = null;
            builder.Path = $"{uri.PathAndQuery}/edit/{id}";
            description = builder.ToString();
            return true;
        }
    }
}
