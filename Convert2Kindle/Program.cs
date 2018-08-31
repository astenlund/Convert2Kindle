using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Convert2Kindle
{
    public static class Program
    {
        public static void Main()
        {
            try
            {
                var (success, convertedCount) = WalkDirectory(Environment.CurrentDirectory, new List<string>());

                if (success)
                {
                    if (convertedCount > 1)
                    {
                        SayWithColor($"\nAll done, {convertedCount} books were converted\n", ConsoleColor.Green);
                    }
                    else if (convertedCount == 1)
                    {
                        SayWithColor("\nAll done, one book was converted\n", ConsoleColor.Green);
                    }
                    else
                    {
                        SayWithColor("\nThere were no books to convert\n", ConsoleColor.Green);
                    }
                }
                else
                {
                    SayWithColor("\nThere were errors converting one or more books\n", ConsoleColor.Red);
                    Environment.ExitCode = 1;
                }
            }
            catch (Exception e)
            {
                SayWithColor($"\nException caught: {e}\n", ConsoleColor.Red);
                Environment.ExitCode = 1;
            }

            Console.Write("Press any key to continue . . . ");
            Console.ReadKey();
        }

        private static (bool Success, int ConvertedCount) WalkDirectory(string dir, List<string> visitedDirs)
        {
            var success = true;
            var convertedCount = 0;

            if (visitedDirs.Contains(dir))
            {
                Console.WriteLine("Recursive path found: \"{0}\", returning...", dir);
                return (Success: true, ConvertedCount: 0);
            }

            visitedDirs.Add(dir);

            foreach (var input in Directory.GetFiles(dir).Where(file => Path.GetExtension(file) == ".epub"))
            {
                var output = Path.ChangeExtension(input, ".mobi");

                if (File.Exists(output))
                {
                    Console.WriteLine("Book already converted: \"{0}\", skipping...", Path.GetFileNameWithoutExtension(input));
                    continue;
                }

                if (Convert(input, output))
                {
                    ++convertedCount;
                } else
                {
                    success = false;
                    Console.Write("Press any key to continue . . . ");
                    Console.ReadKey();
                }
            }

            foreach (var subdir in Directory.GetDirectories(dir))
            {
                var result = WalkDirectory(subdir, visitedDirs);
                success &= result.Success;
                convertedCount += result.ConvertedCount;
            }

            return (success, convertedCount);
        }

        private static bool Convert(string input, string output)
        {
            output = Path.GetFileName(output);

            var startInfo = new ProcessStartInfo
            {
                FileName = "kindlegen.exe",
                Arguments = $"\"{input}\" -o \"{output}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var proc = new Process
            {
                StartInfo = startInfo
            };

            Console.WriteLine("Converting with command: {0} {1}", startInfo.FileName, startInfo.Arguments);

            proc.Start();

            var stdOut = proc.StandardOutput.ReadToEnd();
            
            proc.WaitForExit();

            var bookName = Path.GetFileNameWithoutExtension(input) ?? Path.GetFileName(input) ?? input;
            var result = true;
            
            switch (proc.ExitCode)
            {
                case 0:
                    SayWithColor($"\nConversion of \"{bookName}\" finished successfully\n", ConsoleColor.Green);
                    break;
                case 1:
                    SayWithColor($"\nConversion of \"{bookName}\" finished with warnings\n", ConsoleColor.Yellow);
                    break;
                case 2:
                default:
                    SayWithColor($"\nConversion of \"{bookName}\" failed\n", ConsoleColor.Red);
                    WriteLog(Path.ChangeExtension(input, ".log"), stdOut);
                    result = false;
                    break;
            }

            return result;
        }

        private static void SayWithColor(string message, ConsoleColor color)
        {
            var consoleForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = consoleForegroundColor;
        }

        private static void WriteLog(string path, string contents)
        {
            if (contents.Length > 0)
            {
                File.WriteAllText(path, contents);
            }
        }
    }
}
