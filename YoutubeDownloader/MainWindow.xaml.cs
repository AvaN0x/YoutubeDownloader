using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Winforms = System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

//using System.Windows.Shapes;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Exceptions;
using Microsoft.Win32;
using System.Threading;
using Newtonsoft.Json;

namespace YoutubeDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Config Config { get; set; } = LoadConfig();
        public static YoutubeClient Youtube { get; } = new YoutubeClient();
        private Task? CurrentDownload { get; set; }
        private Queue<DownloadElement> Downloads { get; }
        private Mutex Mutex { get; }

        public MainWindow()
        {
            InitializeComponent();

            Downloads = new();
            Mutex = new();
            CurrentDownload = null;

            txtbx_folder.Text = Config.DownloadPath;
            cb_OnTop.IsChecked = Config.TopMost;

            // We need to remove the event otherwise we would save the config each time we insert an element, and this would cause to lose the user extension config
            cmbobox_extension.SelectionChanged -= cmbobox_extension_SelectionChanged;
            {
                foreach (Extension ext in Enum.GetValues(typeof(Extension)))
                {
                    switch (ext)
                    {
                        case Extension.mp3:
                            cmbobox_extension.Items.Insert((int)ext, "MP3");
                            break;

                        default:
                            // Display the Extension name
                            cmbobox_extension.Items.Insert((int)ext, Enum.GetName(typeof(Extension), ext));
                            break;
                    }
                }
                cmbobox_extension.SelectedIndex = (int)Config.Extension;
            }
            cmbobox_extension.SelectionChanged += cmbobox_extension_SelectionChanged;
        }

        private void cmbobox_extension_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbobox_extension.SelectedIndex != (int)Config.Extension)
            {
                Config.Extension = (Extension)cmbobox_extension.SelectedIndex;
                SaveConfig(Config);
            }
        }

        private void OnTop_Checked(object sender, RoutedEventArgs e)
        {
            Topmost = true;
            if (!Config.TopMost)
            {
                Config.TopMost = true;
                SaveConfig(Config);
            }
        }

        private void OnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            Topmost = false;
            if (Config.TopMost)
            {
                Config.TopMost = false;
                SaveConfig(Config);
            }
        }

        private void btn_folderDialog_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new Winforms.FolderBrowserDialog();
            Winforms.DialogResult result = fbd.ShowDialog();

            if (result == Winforms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                Config.DownloadPath = fbd.SelectedPath;
                SaveConfig(Config);

                txtbx_folder.Text = fbd.SelectedPath;
            }
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            TryDownloadLink(txtbx_input.Text.Trim());
            txtbx_input.Text = "";
        }

        private void StartDownload()
        {
            Mutex.WaitOne();

            if (Downloads.Any() && CurrentDownload is null)
            {
                var dl = Downloads.Dequeue();
                CurrentDownload = dl.StartDownloadAsync().ContinueWith(t =>
                {
                    Mutex.WaitOne();
                    CurrentDownload = null;
                    Mutex.ReleaseMutex();

                    Dispatcher.Invoke(StartDownload);
                });
            }

            Mutex.ReleaseMutex();
        }

        public async void TryDownloadLink(string link)
        {
            if (link.Trim() != string.Empty && !IsLinkAlreadyInDownloads(link))
                // We check to see if it is a playlist, then we create a DownloadElement for each
                // video of the playlist Else we check if it is a video
                try
                {
                    var playlist = await Youtube.Playlists.GetAsync(link);

                    var playlistElement = new PlaylistElement(link, playlist.Title);
                    history.Children.Insert(0, playlistElement);
                    await foreach (var video in Youtube.Playlists.GetVideosAsync(playlist.Id))
                    {
                        if (!IsLinkAlreadyInDownloads(video.Url))
                        {
                            var dl = new DownloadElement(video.Url);
                            playlistElement.AddElement(dl);

                            _ = dl.SetupAsync().ContinueWith((t) =>
                            {
                                Mutex.WaitOne();
                                Downloads.Enqueue(dl);
                                Mutex.ReleaseMutex();

                                Dispatcher.Invoke(StartDownload);
                            });
                        }
                    }
                }
                catch (TransientFailureException)
                {
                    errorsContainer.AddError(ErrorType.YoutubeTransientFailure);
                }
                catch (Exception)
                {
                    var dl = new DownloadElement(link);

                    history.Children.Insert(0, dl);

                    _ = dl.SetupAsync().ContinueWith((t) =>
                    {
                        Mutex.WaitOne();
                        Downloads.Enqueue(dl);
                        Mutex.ReleaseMutex();

                        Dispatcher.Invoke(StartDownload);
                    });
                }
        }

        private bool IsLinkAlreadyInDownloads(string link)
        {
            foreach (object child in history.Children)
            {
                if (child is PlaylistElement playlist)
                {
                    if (playlist.Link == link)
                        return true;
                    foreach (object plChild in playlist.videos.Children)
                        if (plChild is DownloadElement dl)
                            if (dl.Link == link)
                                return true;
                }
                else if (child is DownloadElement dl)
                    if (dl.Link == link)
                        return true;
            }

            return false;
        }

        private void txtbx_input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                TryDownloadLink(txtbx_input.Text.Trim());
                txtbx_input.Text = "";
            }
        }

        public static Config LoadConfig()
        {
            string configDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(configDir, "config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var ser = new JsonSerializer();
                    using var stream = new JsonTextReader(new StreamReader(configPath))
                    {
                        CloseInput = true,
                    };
                    return ser.Deserialize<Config>(stream) ?? new Config();
                }
                catch (Exception e)
                {
                    File.Move(configPath, Path.Combine(configDir, "config_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"));
                    Trace.WriteLine(e.StackTrace);
                }
            }

            return new Config();
        }

        public static void SaveConfig(Config conf)
        {
            string configDir = Directory.GetCurrentDirectory();

            var configPath = Path.Combine(configDir, "config.json");

            if (!File.Exists(configPath))
                Directory.CreateDirectory(configDir);

            var ser = new JsonSerializer
            {
                Formatting = Formatting.Indented
            };
            using var JSONstream = new StreamWriter(configPath);
            ser.Serialize(JSONstream, conf);
            JSONstream.Flush();
        }
    }
}