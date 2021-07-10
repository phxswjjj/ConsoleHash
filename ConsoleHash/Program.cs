using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleHash
{
    class Program
    {
        static int TotalFiles;
        static int ProcessedCount;

        static void Main(string[] args)
        {
            var exeFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var exeFileName = Path.GetFileName(exeFilePath);
            var rootDir = Path.GetDirectoryName(exeFilePath);

            var threadCount = 2;

            #region parse startup args
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg.ToUpper())
                {
                    case "-H":
                    case "--HELP":
                        Console.WriteLine("-t, --thread:");
                        Console.CursorLeft = 4;
                        Console.WriteLine($"-t n: 同時處理 n 個檔案, default n={threadCount}");

                        Console.WriteLine();

                        Console.WriteLine("-d, --dir:");
                        Console.CursorLeft = 4;
                        Console.WriteLine($"-d path: 要處理的目錄, default path={rootDir}");

                        WaitToContinue();
                        return;
                    case "-T":
                    case "--THREAD":
                        threadCount = int.Parse(args[++i]);
                        if (threadCount <= 0) throw new Exception("Thread Count must > 0");
                        break;
                    case "-D":
                    case "--DIR":
                        var assignRootDir = args[++i];
                        if (!Directory.Exists(assignRootDir)) throw new Exception($"{assignRootDir} Directory not exists");
                        rootDir = assignRootDir;
                        break;
                }
            }
            #endregion

            Console.WriteLine($"Target Dir: {rootDir}");

            Console.WriteLine("Collecting Files..");
            var hfs = new List<HashFile>();
            foreach (var filePath in Directory.GetFiles(rootDir, "*", SearchOption.AllDirectories))
            {
                var relativeFilePath = Path.GetRelativePath(rootDir, filePath);
                if (exeFileName == relativeFilePath) continue;
                var fileName = Path.GetFileName(filePath);
                var hf = new HashFile() { FilePath = relativeFilePath, FileName = fileName };
                hfs.Add(hf);
            }
            Console.WriteLine();

            TotalFiles = hfs.Count;

            Thread progressJob = new Thread(ProgressBar) { IsBackground = true };
            progressJob.Start();

            Parallel.ForEach<HashFile>(hfs,
                new ParallelOptions() { MaxDegreeOfParallelism = threadCount },
                hf =>
                {
                    var ms = new Random().Next(10);
                    Thread.Sleep(ms);
                    ProcessedCount++;
                });

            progressJob.Join();
            Console.WriteLine();

            WaitToContinue();
        }

        private static void WaitToContinue()
        {
            Console.WriteLine();
            Console.WriteLine("press [ENTER] to continue..");
            Console.ReadLine();
        }

        private static void ProgressBar()
        {
            var sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                var elapsed = sw.Elapsed.ToString();
                Console.CursorLeft = 0;
                Console.WriteLine($"Total: {TotalFiles:#,##0}, Processed: {ProcessedCount:#,##0}, Elapsed: {elapsed}");

                var totalBlocks = 20;
                var progressTitle = "Progress: ";
                decimal completedRatio = (int)((decimal)ProcessedCount / TotalFiles * 100);
                completedRatio /= 100;
                var completedBlocks = (int)(completedRatio * totalBlocks);
                var unCompletedBlocks = totalBlocks - completedBlocks;

                Console.CursorLeft = 0;

                Console.Write(progressTitle);
                Console.CursorLeft += 2;

                Console.Write(new string('#', completedBlocks));
                Console.Write(new string('-', unCompletedBlocks));
                Console.CursorLeft += 2;

                var completedPercentText = completedRatio.ToString("P0");
                Console.Write(completedPercentText);

                if (ProcessedCount == TotalFiles) break;
                Thread.Sleep(200);
                Console.CursorTop -= 1;
            }
            sw.Stop();
        }
    }
}
