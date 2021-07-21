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
        static void Main(string[] args)
        {
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            WaitToContinue();
        }

        private static void Run(string[] args)
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
                var hf = new HashFile() { FilePath = relativeFilePath, FileName = fileName, FileFullPath = filePath };
                hfs.Add(hf);
            }
            Console.WriteLine();

            ProgressCounter.Start(hfs.Count);

            Parallel.ForEach<HashFile>(hfs,
                new ParallelOptions() { MaxDegreeOfParallelism = threadCount },
                hf =>
                {
                    ComputeHash(hf);
                    ProgressCounter.Increase();
                });

            ProgressCounter.Join();
            Console.WriteLine();
        }

        private static void ComputeHash(HashFile hf)
        {
            var f = new FileInfo(hf.FilePath);
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var reader = new StreamReader(hf.FileFullPath))
                {
                    var bs = md5.ComputeHash(reader.BaseStream);
                    var hash = BitConverter.ToString(bs).Replace("-", "");
                    hf.Hash = hash;
                }
            }
        }

        private static void WaitToContinue()
        {
            Console.WriteLine();
            Console.WriteLine("press [ENTER] to continue..");
            Console.ReadLine();
        }
    }
}
