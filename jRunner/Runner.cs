using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace jRunner
{
    partial class Runner : ServiceBase
    {
        private bool _stopService = false;
        private ManualResetEvent mrejRunner = new ManualResetEvent(false);
        private TimeSpan tsCheckSettings = new TimeSpan(0, 1, 0);
        
        public Runner()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            Stop();
        }

        public void Start()
        {

            while (!_stopService)
            {

                mrejRunner.Reset();
                mrejRunner.WaitOne(tsCheckSettings);
            }


        }

        public void Stop()
        {


        }

    }
}
