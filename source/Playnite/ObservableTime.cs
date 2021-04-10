﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Playnite.Common;

namespace Playnite
{
    public class ObservableTime : ObservableObject
    {
        private SynchronizationContext context;
        private CancellationTokenSource watcherToken;
        private Task currentTask;

        public string Time
        {
            get
            {
                string show = DateTime.Now.ToString(Common.Constants.TimeUiFormat);
                
                if (UserManager.station != null)
                {
                    TimeSpan timeSpan = DateTime.Now - UserManager.station.ClientStartTime;
                    show += $" {UserManager.station.StationName}已连接: {(int)timeSpan.TotalMinutes}分";
                }
                return show;
            }
        }

        public ObservableTime()
        {
            context = SynchronizationContext.Current;
            if (!DesignerTools.IsInDesignMode)
            {
                StartWatcher();
            }
        }

        public async void StartWatcher()
        {
            watcherToken?.Cancel();
            if (currentTask != null)
            {
                await currentTask;
            }

            watcherToken = new CancellationTokenSource();
            currentTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (watcherToken.IsCancellationRequested)
                    {
                        return;
                    }

                    context.Post((a) => OnPropertyChanged(nameof(Time)), null);
                    await Task.Delay(10000);
                }
            }, watcherToken.Token);
        }

        public async void StopWatcher()
        {
            watcherToken?.Cancel();
            if (currentTask != null)
            {
                await currentTask;
            }
        }

        public void Dispose()
        {
            StopWatcher();
        }
    }
}
