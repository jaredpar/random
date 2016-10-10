using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetTempPath
{
    internal static class Program
    {
        internal const string NameUserProfile = "USERPROFILE";
        internal const string NameTemp = "TEMP";
        internal const string NameTmp = "TMP";

        internal static void Main(string[] args)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var workingDir = Path.Combine(userProfile, "getTempPathTest");
            var tempDir = Path.Combine(workingDir, "tempDir");
            EnsureFolder(workingDir);

            Test(
                "TEMP full and valid",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: tempDir,
                tmp: tempDir);

            Test(
                "TMP over TEMP",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: tempDir,
                tmp: Path.Combine(workingDir, "other"));

            Test(
                "TMP when TEMP is null",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: null,
                tmp: tempDir);

            Test(
                "TMP and TEMP are null",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: null,
                tmp: null);

            Test(
                "TMP, TEMP and USERPROFILE are null",
                workingDirectory: workingDir,
                userProfile: null,
                temp: null,
                tmp: null);

            EnsureFolder(Path.Combine(workingDir, "test1"));
            Test(
                "TEMP is relative off of working dir",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: "test1",
                tmp: tempDir);
            Directory.Delete(Path.Combine(workingDir, "test1"));

            EnsureFolder(Path.Combine(workingDir, "test1"));
            Test(
                "TEMP and TMP are relative off of working dir",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: "test1",
                tmp: "test1");
            Directory.Delete(Path.Combine(workingDir, "test1"));

            EnsureNotFolder(Path.Combine(workingDir, "test1"));
            Test(
                "TEMP is relative but does not exist on disk",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: "test1",
                tmp: null);

            EnsureNotFolder(Path.Combine(workingDir, "test1"));
            Test(
                "TMP is relative but does not exist on disk",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: "test1",
                tmp: null);

            EnsureNotFolder(Path.Combine(workingDir, "test1"));
            EnsureNotFolder(Path.Combine(workingDir, "test2"));
            Test(
                "TEMP and TMP are relative but does not exist on disk",
                workingDirectory: workingDir,
                userProfile: userProfile,
                temp: "test1",
                tmp: "test2");

            EnsureNotFolder(Path.Combine(workingDir, "test1"));
            EnsureNotFolder(Path.Combine(workingDir, "test2"));
            Test(
                "TEMP and TMP are relative but does not exist on disk",
                workingDirectory: workingDir,
                userProfile: null,
                temp: "test1",
                tmp: "test2");

            // These test fail for specified reasons.
            if (args.Length > 42)
            {
                // SetCurrentDirectory for c: succeeds but doesn't actually change the current
                // directory.  Hence this is a case that can't really happen.
                EnsureNotFolder(Path.Combine(workingDir, "test1"));
                EnsureNotFolder(Path.Combine(workingDir, "test2"));
                Test(
                    "TEMP and TMP and label path",
                    workingDirectory: @"c:",
                    userProfile: null,
                    temp: "test1",
                    tmp: "test2");
            }
        }

        private static void Test(
            string name,
            string workingDirectory,
            string userProfile = null,
            string temp = null,
            string tmp = null)
        {
            Environment.SetEnvironmentVariable(NameUserProfile, userProfile);
            Environment.SetEnvironmentVariable(NameTemp, temp);
            Environment.SetEnvironmentVariable(NameTmp, tmp);
            Directory.SetCurrentDirectory(workingDirectory);

            var expected = Path.GetTempPath();
            var actual = GetTempPath(workingDirectory, userProfile, temp, tmp);

            Console.Write($"{name} ... ");
            if (actual != expected)
            {
                Console.WriteLine("Failed");
                Console.WriteLine($"\tExpected: {expected}");
                Console.WriteLine($"\tActual: {actual}");

                Console.WriteLine("Environment");
                Console.WriteLine($"\t%{NameUserProfile}%: {userProfile}");
                Console.WriteLine($"\t%{NameTemp}%: {temp}");
                Console.WriteLine($"\t%{NameTmp}%: {tmp}");
                Console.WriteLine($"\tCWD: {workingDirectory}");
            }
            else
            {
                Console.WriteLine("Succeeded");
            }
        }

        private static string GetTempPath(
            string workingDirectory,
            string userProfile,
            string temp,
            string tmp)
        {
            var path = GetTempPathCore(workingDirectory, userProfile, temp, tmp);
            if (path != null && path[path.Length - 1] != '\\')
            {
                path = path + "\\";
            }

            return path;
        }

        private static string GetTempPathCore(
            string workingDirectory,
            string userProfile,
            string temp,
            string tmp)
        {
            if (Path.IsPathRooted(tmp))
            {
                return tmp;
            }

            if (Path.IsPathRooted(temp))
            {
                return temp;
            }

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    return Path.Combine(workingDirectory, tmp);
                }

                if (!string.IsNullOrEmpty(temp))
                {
                    return Path.Combine(workingDirectory, temp);
                }
            }

            if (Path.IsPathRooted(userProfile))
            {
                return userProfile;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        }

        private static void EnsureFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch
            {
                // Already exists.
            }
        }

        private static void EnsureNotFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path);
                }
            }
            catch
            {
                // Already exists.
            }
        }

        private static Dictionary<string, string> SaveEnvironment()
        {
            var map = new Dictionary<string, string>(capacity: 4);
            map[NameUserProfile] = Environment.GetEnvironmentVariable(NameUserProfile);
            map[NameTemp] = Environment.GetEnvironmentVariable(NameTemp);
            map[NameTmp] = Environment.GetEnvironmentVariable(NameTmp);
            return map;
        }

    }
}
