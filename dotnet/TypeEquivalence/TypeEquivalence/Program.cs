using ClassLibrary1;
using ClassLibrary2;
using Shared;

namespace TypeEquivalence
{
    class Program
    {
        static void Main(string[] args)
        {
            var c1 = (object)new Class1();
            var c2 = (object)new Class2();
            var t1 = (ITest)c1;
            t1.M();

            var t2 = (ITest)c2;
            t2.M();
        }
    }
}
