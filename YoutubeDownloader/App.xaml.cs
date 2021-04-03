using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.Pipes;
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
        public const string URI_NAME = "ytdl";

        private const string NamedPipeName = "{github.com/AvaN0x/YoutubeDownloader NamedPipeName}";
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

                var client = new NamedPipeClientStream(NamedPipeName);
                client.Connect();
                var writer = new StreamWriter(client);

                // Send the arg to the other instance
                if (e.Args.Length > 0)
                {
                    writer.WriteLine(e.Args[0]);
                    writer.Flush();
                }

                this.Shutdown();
                return;
            }

            if (e.Args.Length > 0)
            {
                // arg[0] exemple : "https://youtu.be/dQw4w9WgXcQ;mp3"
                var args = e.Args[0].Split(";");
                string link = args[0].Replace(URI_NAME + ":/", "").Replace(URI_NAME + ":", "");
                Extension? extension = null;

                if (args.Length > 1)
                    try
                    {
                        extension = (Extension?)Enum.Parse(typeof(Extension), args[1], true);
                    }
                    catch (Exception) { }

                Current.Dispatcher.BeginInvoke((Action)(
                    () => ((MainWindow)Current.MainWindow).TryDownloadLink(link, extension)
                ));
            }

            // Create a thread that is waiting for the event
            new Thread(() =>
                {
                    var server = new NamedPipeServerStream(NamedPipeName);

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

                                // Access arg from other instance
                                server.WaitForConnection();
                                var reader = new StreamReader(server);

                                var arg = reader.ReadLine() ?? "";
                                if (arg is not null && arg.Trim().Length > 0)
                                {
                                    var args = arg.Split(";");
                                    string link = args[0].Replace(URI_NAME + ":/", "").Replace(URI_NAME + ":", "");
                                    Extension? extension = null;

                                    if (args.Length > 1)
                                        try
                                        {
                                            extension = (Extension?)Enum.Parse(typeof(Extension), args[1], true);
                                        }
                                        catch (Exception) { }

                                    mainWindow.TryDownloadLink(link, extension);
                                }

                                server.Disconnect();
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