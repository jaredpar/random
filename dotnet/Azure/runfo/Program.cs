using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOps.Util;

public class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;

    internal static async Task<int> Main(string[] args)
    {
        try
        {
            var runtimeInfo = new RuntimeInfo();
            if (args.Length == 0)
            {
                await runtimeInfo.PrintBuildResults();
                return ExitSuccess;
            }

            var command = args[0].ToLower();
            switch (command)
            {
                case "status":
                    await runtimeInfo.PrintBuildResults();
                    return ExitSuccess;
                default:
                    Console.WriteLine($"Error: {command} is not recognized as a valid command");
                    ShowHelp();
                    return ExitFailure;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return ExitFailure;
        }

        static void ShowHelp()
        {
            Console.WriteLine("runfo");
            Console.WriteLine("\tstatus\tPrint build status");
        }
    }
}
