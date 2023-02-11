Embed Files in C#
----

# Summary
The compiler will introduce a new directive named `#embed` that enables embedding file and directory content into the application at compile time. For example:

```csharp
// app.cs
#embed data.txt
string text;
Console.WriteLine(text);
```

When consumed it would produce the following:

```cmd
> echo "Hello World" | out-file data.txt
> dotnet run 
Hello World
```

# Details
## embed directive
The `#embed` directive can be applied to a field or local in a type level program file. The content of the file can be expressed by the types `string`, `Stream`, or `byte[]`. The type of a member annotated with `#embed` can be any of these types. 

```csharp
#embed content1.txt
string content1;
#embed content2.txt
byte[] content2;
#embed content3.txt
Stream content3;
```

The type can also be a tuple where the first element is typed to `string` and the second element is one of the above mentioned types. In that form the first element of the tuple will be replaced with the full path of the file. 

```csharp
#embed content.txt
(string FilePath, string Text) content;
Console.WriteLine($"{content.FilePath}: {content.Text}");
```

Leads to 

```cmd
c:\code> echo "present" | out-file content.txt
c:\code> dotnet run
c:\code\content.txt: present
```

The `#embed` directive can also refer to multiple files by pointing to a directory or using a [wildcard](#wildcards). Such directives must annotate a member with the type `IEnumerable<T>` where `T` is one of the above allowed types.

```csharp
#embed *.txt
IEnumerable<(string FilePath, byte[] Content)> textFiles;
```

The file path returned from this member will be subject to `/pathMap` encoding. Specifically it will return the mapped path, not the original one.

The `#embed name` directive will result in a compilation error in any of the following circumstances:

- _name_ refers to a file and the member type is `IEnumerable<...>` 
- _name_ refers to a directory and the member type is not `IEnumerable<..>`
- _name_ contains a wild card and the member type is not `IEnumerable<...>`
- _name_ is not present on disk at compile time and it does not use a wildcard
- The member contains more than one declaration

The member annotated with `#embed` will be considered initialized. For fields they will be considered non-null. For locals in top level programs they will be considered definitively assigned. Any initializer applied to the member will result in compilation error. The compiler will [insert code](#member-init) to ensure the content is available before the member is accessed.

## Member Initialization
<a name="member-init"></a>
The logic of loading resources at execution time will be generated into an unspeakable type at build time. For the purpose of this document that type will be called `EmbedUtil`. Every `#embed` directive will have a corresponding `static` member on that type which loads the content from the resources embedded into the application and serves as an initializer. 

In the case of locals the compiler will silently insert a call to the initializer at the declaration point. 

```csharp
#embed content.txt
string content;
// Emits 
string content = <>Embed.LoadContent();
```

In the case of fields the compiler will _effectively_ call the initializer at the field declaration point. 

```csharp
class TestResources {
    #embed data.json
    string Data;
}

// Emits

class TestResources {
    string Data = <>Embed.LoadDataJson();
}
```

## Resources
The compiler will create a resource entry for every file discovered at build time. The name of the resource will be _Microsoft.CodeAnalysis.Embed.{sha}_ where `{sha}` will be replaced with a checksum of the file contents. The checksum algorithm will be the default for the compiler (presently SHA-256). Any user provided resources with the same name will result in a compile time error.

## MSBuild
This feature causes the compiler to read files on disk that are not listed as inputs to the `<Csc>` task in MSBuild. To keep correct build behavior, specifically changing a 

## Wildcards
<a name="wildcards"></a>
**Cover all the wildcard cases like #embed *.cs **

## Embed loading type
**cover how the embed loading type will look**

# Open Issues


# Considerations
## Initializing to null
The reason for having `#embed` annotated members considered definitely assigned without an explicit assignment is that it seems to be the only realistic option. Forcing an initializer means a suitable value must be picked. In many cases the only acceptable default is `null`. These types are non-null as that is the most sensible option so forcing initializers would lead to awkard code like: 

```csharp
#embed content.txt
Stream stream = null!;
```

That seems like a very poor default experience for this feature. Even in cases where a suitable non-null value could be found the compiler would never use it. Hence it's developers writing code that is never used by the compiler. 

This is the logic for omitting initializers entirely here.

## Resource naming
The compiler could be smarter in how it chooses the resource name to avoid build time errors. For instance it could append a counter to avoid collisions. This seems excessive though as the namespace Microsoft.CodeAnalysis is generally considered reserved. This is the first time we've enforced that at the resource level but it seems a reasonable extension of current approaches.





