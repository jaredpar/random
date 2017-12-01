using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrintImportTree
{
    internal enum Kind
    {
        ImportStart,
        ImportEnd
    }

    internal struct Import
    {
        internal Kind Kind { get; }
        internal string FilePath { get; }

        internal static Import End { get; } = new Import(Kind.ImportEnd, null);

        private Import(Kind kind, string filePath)
        {
            Kind = kind;
            FilePath = filePath;
        }

        internal static Import CreateStart(string filePath) => new Import(Kind.ImportStart, filePath);
    }

    internal sealed class Parser
    {
        private string[] _lines;
        private int _index;
        private readonly RegexOptions _options = RegexOptions.Compiled | RegexOptions.IgnoreCase;

        internal Parser(string filePath)
        {
            _lines = File.ReadAllLines(filePath);
        }

        internal Import? ParseNext()
        {
            const int headerLength = 5;
            while (_index + headerLength < _lines.Length)
            {
                var import = TryParse();
                if (import != null)
                {
                    _index += headerLength;
                    return import;
                }
                else
                {
                    _index++;
                }
            }

            return null;
        }

        private Import? TryParse()
        {
            Debug.Assert(_index + 5 < _lines.Length);

            var commentStart = _lines[_index];
            if (!Regex.IsMatch(commentStart, @"^\s*<!--\s*$", _options))
            {
                return null;
            }

            var barLine = _lines[_index + 1];
            if (!Regex.IsMatch(barLine, @"^(=)+\s*$", _options))
            {
                return null;
            }

            var importLine = _lines[_index + 2];
            if (Regex.IsMatch(importLine, @"^\s*<Import Project"))
            {
                var filePath = _lines[_index + 4].Trim();
                if (!Path.IsPathRooted(filePath))
                {
                    return null;
                }

                return Import.CreateStart(filePath);
            }

            if (Regex.IsMatch(importLine, @"^\s*</Import"))
            {
                return Import.End;
            }

            return null;
        }
    }
}
