using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ConsoleHash
{
    public static class ProgressCounter
    {
        static long CurrentProgress;
        static long TargetProgress;

        static Thread ProgressJob;
        static Stopwatch sw;

        public static void Start(long max)
        {
            if (ProgressJob != null && ProgressJob.IsAlive) Stop();

            if (max <= 0)
                throw new Exception($"{max} less then or equal to 0");

            TargetProgress = max;

            ProgressJob = new Thread(ProgressBar) { IsBackground = true };
            sw = new Stopwatch();
            ProgressJob.Start();
            sw.Start();
        }
        public static void Increase(long step = 1)
        {
            lock (ProgressJob)
            {
                CurrentProgress += step;
            }
        }
        public static void Stop()
        {
            if (ProgressJob.IsAlive)
                ProgressJob.Abort();
            sw.Stop();
        }

        private static void ProgressBar()
        {
            while (true)
            {
                var elapsed = sw.Elapsed.ToString();
                Console.CursorLeft = 0;
                Console.WriteLine($"Total: {TargetProgress:#,##0}, Processed: {CurrentProgress:#,##0}, Elapsed: {elapsed}");

                var totalBlocks = 20;
                var progressTitle = "Progress: ";
                decimal completedRatio = (int)((decimal)CurrentProgress / TargetProgress * 100);
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

                if (CurrentProgress == TargetProgress) break;
                Thread.Sleep(200);
                Console.CursorTop -= 1;
            }
            sw.Stop();
        }

        internal static void Join()
        {
            while (ProgressJob.IsAlive)
                Thread.Sleep(10);
        }
    }
}
