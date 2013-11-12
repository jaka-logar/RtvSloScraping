using System;
using System.ServiceProcess;
using System.Threading;
using Castle.Core.Logging;
using RtvSlo.Core.Configuration;
using RtvSlo.Core.Infrastructure.Windsor;
using RtvSlo.Framework;

namespace RtvSlo.WindowsService
{
    public partial class Service : ServiceBase
    {
        #region Constants

        private static TimeSpan THREAD_SLEEP = TimeSpan.FromSeconds(RtvSloConfig.MainThreadSleep);

        #endregion Constants

        #region Fields

        private readonly ILogger _logger;
        private Worker _worker;

        private bool stopping;
        private ManualResetEvent stoppedEvent;

        #endregion Fields

        #region Ctor

        public Service()
        {
            InitializeComponent();

            this._logger = DependencyContainer.Instance.Resolve<ILogger>();
            this._worker = new Worker();
        }

        #endregion Ctor

        #region Methods

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("STARTED");
            this._logger.DebugFormat("RtvSloService -> Started");

            ThreadPool.QueueUserWorkItem(new WaitCallback(ServiceWorkerThread));
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("STOPPED");
            this._logger.DebugFormat("RtvSloService -> Stopped");


            this.stopping = true;
            this.stoppedEvent.WaitOne();
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("CONTINUE");
        }


        /// <summary>
        /// Main functionallity
        /// </summary>
        /// <param name="state"></param>
        private void ServiceWorkerThread(object state)
        {
            this._worker.StartTimers();

            while (!this.stopping)
            {
                this._logger.DebugFormat("RtvSloService -> Running");
                Thread.Sleep(THREAD_SLEEP);
            }

            this.stoppedEvent.Set();
        }

        #endregion Methods
    }
}
