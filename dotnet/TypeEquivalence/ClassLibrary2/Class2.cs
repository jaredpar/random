using System;
using Shared;

namespace ClassLibrary2
{
    public class Class2 : ITest
    {
        public void M() => Console.WriteLine("Class2.M");
    }

    public class Util2
    {
        public void Use(ITest test) => test.M();
    }
}
