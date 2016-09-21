using System;
using System.Runtime.InteropServices;

namespace Shared
{
    [TypeIdentifier("Global", "Random")]
    [ComImport]
    [Guid("369f453f-3cb0-4c7b-92b9-3bc11839ca18")]
    public interface ITest
    {
        void M();
    }
}
