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
        internal static void Main(string[] args)
        {
            var file = args.Length == 1 ? args[0] : @"e:\temp\data.pp";
            var printer = new TreePrinter();
            var parser = new Parser(file);
            do
            {
                if (parser.ParseNext() is Import import)
                {
                    switch (import.Kind)
                    {
                        case Kind.ImportStart:
                            printer.PrintStart(import.FilePath);
                            break;
                        case Kind.ImportEnd:
                            printer.PrintEnd();
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    break;
                }
            } while (true);
        }
    }
}
