using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace PrintImportTree
{
    internal static class Program
    {
        private enum Kind
        {
            None,
            ImportStart,
            ImportEnd
        }

        internal static void Main(string[] args)
        {
            var file = @"e:\temp\scratch.xml";
            var indent = 0;
            var indentString = "";
            foreach (var line in File.ReadAllLines(file))
            {
                var (kind, name) = ClassifyLine(line);
                switch (kind)
                {
                    case Kind.ImportStart:
                        Console.WriteLine($"{indentString}{name}");
                        indent++;
                        indentString = new string(' ', indent * 2);
                        break;
                    case Kind.ImportEnd:
                        indent--;
                        indentString = new string(' ', indent * 2);
                        Console.WriteLine($"{indentString}{name}");
                        break;
                    default:
                        break;
                }
            }
        }

        private static (Kind, string) ClassifyLine(string line)
        {
            var dirMatch = @"[\w\W\d:-\\/]+";
            var options = RegexOptions.Compiled | RegexOptions.IgnoreCase;
            var endMatch = Regex.Match(line, $@"^\s*<!-- ======== END OF ({dirMatch})+ ======= -->", options);
            if (endMatch.Success)
            {
                return (Kind.ImportEnd, endMatch.Groups[1].Value);
            }

            var startMatch = Regex.Match(line, $@"^\s*<!-- ======== ({dirMatch}) ======= -->", options);
            if (startMatch.Success)
            {
                return (Kind.ImportStart, startMatch.Groups[1].Value);
            }

            return (Kind.None, null);
        }
    }
}
