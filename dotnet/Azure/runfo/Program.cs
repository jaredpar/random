using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOps.Util;

public class Program
{

    internal static async Task Main(string[] args)
    {
        var runtimeInfo = new RuntimeInfo();
        await runtimeInfo.PrintBuildResults();
    }

}
