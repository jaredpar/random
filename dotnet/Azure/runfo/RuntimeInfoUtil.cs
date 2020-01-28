using System;
using System.Collections.Generic;
using Mono.Options;

internal static class RuntimeInfoUtil
{
    internal const int ExitSuccess = 0;
    internal const int ExitFailure = 1;

    internal static void ParseAll(OptionSet optionSet, IEnumerable<string> args)
    {
        var extra = optionSet.Parse(args);
        if (extra.Count != 0)
        {
            optionSet.WriteOptionDescriptions(Console.Out);
            var text = string.Join(' ', extra);
            throw new Exception($"Extra arguments: {text}");
        }
    }

}