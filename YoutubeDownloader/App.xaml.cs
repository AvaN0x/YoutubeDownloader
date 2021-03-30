using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace YoutubeDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string UniqueEventName = "{github.com/AvaN0x/YoutubeDownloader UniqueEventName}";
        private const string UniqueMutexName = "{github.com/AvaN0x/YoutubeDownloader UniqueMutexName}";
        private EventWaitHandle? _eventWaitHandle;
        private Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Inspired from https://stackoverflow.com/a/23730146/12183781

            // isNewInstance to check if an instance is already running
            this._mutex = new Mutex(true, UniqueMutexName, out bool isNewInstance);
            // Create an event or get the event of the other instance
            this._eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            // So, R# would not give a warning that this variable is not used.
            GC.KeepAlive(this._mutex);

            // Program is already running
            if (!isNewInstance)
            {
                // Trigger event of other instance
                this._eventWaitHandle.Set();

                this.Shutdown();
                return;
            }

            // Create a thread that is waiting for the event
            new Thread(() =>
                {
                    while (this._eventWaitHandle.WaitOne())
                    {
                        Current.Dispatcher.BeginInvoke((Action)(
                            () =>
                            {
                                // We bring the window to foreground
                                var mainWindow = (MainWindow)Current.MainWindow;
                                if (mainWindow.WindowState == WindowState.Minimized || mainWindow.Visibility == Visibility.Hidden)
                                {
                                    mainWindow.Show();
                                    mainWindow.WindowState = WindowState.Normal;
                                }

                                mainWindow.Activate();
                                var oldTopMost = mainWindow.Topmost;
                                mainWindow.Topmost = true;
                                mainWindow.Topmost = oldTopMost;
                                mainWindow.Focus();
                            }
                        ));
                    }
                })
            {
                IsBackground = true
            }.Start();
        }
    }
}