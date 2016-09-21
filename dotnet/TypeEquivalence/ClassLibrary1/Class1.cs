using System;
using Shared;

namespace ClassLibrary1
{
    public class Class1 : ITest
    {
        public void M() => Console.WriteLine("Class1.M");
    }

    public class Util1
    {
        public void Use(ITest test) => test.M();
    }
}
