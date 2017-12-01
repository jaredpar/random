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
        private bool _isTopStart = false;
        private Stack<string> _filePathStack = new Stack<string>();

        internal void PrintStart(string filePath)
        {
            if (_isTopStart)
            {
                Console.WriteLine($"{CreateIndentString()}+ {_filePathStack.Peek()}");
            }

            _filePathStack.Push(filePath);
            _isTopStart = true;
        }

        internal void PrintEnd()
        {
            var prefix = _isTopStart ? "+-" : "-";
            Console.WriteLine($"{CreateIndentString()}{prefix} {_filePathStack.Pop()}");
            _isTopStart = false;
        }

        private string CreateIndentString() => new string(' ', (_filePathStack.Count - 1) * 2);
    }
}
