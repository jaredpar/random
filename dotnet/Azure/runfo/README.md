# runfo

This is an abbreviation for "runtime info." It's a tool that provides quick 
summary status of builds from the dotnet/runtime repository.

## Authentication
In order to use the `tests` command you will need to provide a personal access
token to the tool. These can be obtained by visitting the following site:

- https://dnceng.visualstudio.com/_usersSettings/tokens

The token can be passed to the tool in two ways:

1. By using the `-token` command line argument
2. Using the `%RUNFO_AZURE_TOKEN%` environment variable



