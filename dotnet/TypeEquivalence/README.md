# Type Equivalence

This project demonstrates the ability to share an interface amongst several C# projects without having to reference a common library which defines the interface.  Instead every project defines their own copy of the interface.  This can be used to simplify dependency management 

In this sample every project includes a shared file which defines the interface:

``` csharp
[TypeIdentifier("Global", "Random")]
[ComImport]
[Guid("369f453f-3cb0-4c7b-92b9-3bc11839ca18")]
public interface ITest
{
    void M();
}
```

The consuming type can then freely cast the returned objects to the interface they implement even though it is referring to it's own independent copy.

