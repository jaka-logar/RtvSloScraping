using System;
using System.Threading;
using RtvSlo.Core.Configuration;
using RtvSlo.Framework;

namespace RtvSlo.ScrapingSimulator
{
    public class Program
    {
        private static TimeSpan THREAD_SLEEP = TimeSpan.FromSeconds(RtvSloConfig.MainThreadSleep);

        public static void Main(string[] args)
        {
            WindsorInstaller.InstallAll();

            Worker worker = new Worker();
            //worker.RunDebug();

            //worker.RunStep1();
            //worker.RunStep2();
            //worker.RunStep3();

            worker.StartTimers();
            while (true)
            {
                Thread.Sleep(THREAD_SLEEP);
            }
        }
    }
}
