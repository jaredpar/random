using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintImportTree
{
    sealed class TreePrinter
    {
        private int _indent;
        private string _indentString;
        private string _currentFilePath;
        private StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

        internal void PrintStart(string filePath)
        {
            if (_currentFilePath != null)
            {
                Console.WriteLine($"{_indentString}+ {_currentFilePath}");
                _indent++;
                _indentString = new string(' ', _indent * 2);
            }

            _currentFilePath = filePath;
        }

        internal void PrintEnd(string filePath)
        {
            if (_comparer.Equals(filePath, _currentFilePath))
            {
                Console.WriteLine($"{_indentString}+- {_currentFilePath}");
                _currentFilePath = null;
            }
            else
            {
                Debug.Assert(_currentFilePath == null);
                _indent--;
                _indentString = new string(' ', _indent * 2);
                Console.WriteLine($"{_indentString}- {filePath}");
            }
        }
    }
}
