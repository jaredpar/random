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
        ImportEnd,
        Header,
        Footer,
    }

    internal struct Import
    {
        internal Kind Kind { get; }
        internal string FilePath { get; }

        internal static Import ImportEnd { get; } = new Import(Kind.ImportEnd, null);
        internal static Import Footer { get; } = new Import(Kind.Footer, null);

        private Import(Kind kind, string filePath)
        {
            Kind = kind;
            FilePath = filePath;
        }

        internal static Import CreateStart(string filePath) => new Import(Kind.ImportStart, filePath);
        internal static Import CreateHeader(string filePath) => new Import(Kind.Header, filePath);
    }

    internal sealed class Parser
    {
        private string[] _lines;
        private int _index;
        private string _headerFilePath;
        private readonly RegexOptions _options = RegexOptions.Compiled | RegexOptions.IgnoreCase;

        internal Parser(string filePath)
        {
            _lines = File.ReadAllLines(filePath);
        }

        internal Import? ParseNext()
        {
            const int headerLength = 6;
            while (_index + headerLength < _lines.Length)
            {
                if (TryParse() is Import import)
                {
                    _index += import.Kind == Kind.ImportStart ? headerLength : 1;
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

            if (_headerFilePath == null)
            {
                var line = _lines[_index + 2].Trim();
                if (!Path.IsPathRooted(line))
                {
                    return null;
                }

                _headerFilePath = line;
                return Import.CreateHeader(line);
            }

            var importLine = _lines[_index + 2];
            if (Regex.IsMatch(importLine, @"^\s*<Import Project=""Sdk.props"""))
            {
                var filePath = _lines[_index + 5].Trim();
                if (!Path.IsPathRooted(filePath))
                {
                    return null;
                }

                return Import.CreateStart(filePath);
            }
            else if (Regex.IsMatch(importLine, @"^\s*<Import Project"))
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
                var footerLine = _lines[_index + 4].Trim();
                return StringComparer.OrdinalIgnoreCase.Equals(_headerFilePath, footerLine)
                    ? Import.Footer
                    : Import.ImportEnd;
            }

            return null;
        }
    }
}
