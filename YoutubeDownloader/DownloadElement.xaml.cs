using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader
{
    /// <summary>
    /// Logique d'interaction pour DownloadElement.xaml
    /// </summary>
    public partial class DownloadElement : UserControl
    {
        public String Link { get; }
        public String? VideoPath { get; private set; }
        public String FolderPath { get; private set; }
        private CancellationTokenSource? CancelTokenSource { get; set; }

        public DownloadElement(String link, String path)
        {
            InitializeComponent();

            this.Link = link;
            this.FolderPath = path;

            label.Text = Link;
        }

        public async Task StartDownloadAsync()
        {
            var youtube = new YoutubeClient();

            // Add back indetermination to progressbar
            progressbar.IsIndeterminate = true;
            redo.Visibility = Visibility.Collapsed;
            progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#179c22");
            // Close button is hidden while whe get informations about the video
            close.Visibility = Visibility.Hidden;

            try
            {
                var video = await youtube.Videos.GetAsync(Link);

                // Now that the video have loaded, we can display the video title
                VideoPath = System.IO.Path.Combine(FolderPath, Utils.RemoveInvalidChars(video.Title) + ".mp3");
                label.Text = video.Title;
                progressbar.IsIndeterminate = false;
                progressbar.Value = 1;

                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(Link);
                var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

                if (streamInfo != null)
                {
                    close.Visibility = Visibility.Visible;
                    CancelTokenSource = new CancellationTokenSource();

                    if (File.Exists(VideoPath))
                    {
                        // ask the user if we should overwrite or abandon
                        MessageBoxResult result = MessageBox.Show("This file already exist, do you want overwrite it ?\n" + VideoPath, "File already exist", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                        // Process message box results
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                File.Delete(VideoPath);
                                break;

                            case MessageBoxResult.No:
                            default:
                                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");
                                redo.Visibility = Visibility.Visible;
                                return;
                        }
                    }

                    var progress = new Progress<double>(percent =>
                    {
                        progressbar.Value = Math.Round(percent * 100);
                    });

                    await youtube.Videos.Streams.DownloadAsync(streamInfo, VideoPath, progress, CancelTokenSource.Token);

                    CancelTokenSource = null;
                    progressbar.Value = 100;
                    progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#4e88d9");
                    redo.Visibility = Visibility.Collapsed;
                    open.Visibility = Visibility.Visible;
                    openFolder.Visibility = Visibility.Visible;
                }
            }
            catch (ArgumentException)
            {
                // Link is not a valid youtube link
                close.Visibility = Visibility.Visible;
                progressbar.IsIndeterminate = false;
                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");
                progressbar.Value = 100;
            }
            catch (OperationCanceledException)
            {
                // ConcellationToken event
                progressbar.IsIndeterminate = false;
                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");

                if (VideoPath != null && File.Exists(VideoPath))
                {
                    File.Delete(VideoPath);
                }

                redo.Visibility = Visibility.Visible;
                close.Visibility = Visibility.Visible;
            }
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPath != null)
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo("explorer.exe", VideoPath)
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
        }

        private void openFolder_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPath != null)
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo("explorer.exe", $"/select, \"{VideoPath}\"")
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            if (CancelTokenSource != null)
            {
                CancelTokenSource.Cancel();
            }
            else
            {
                // TODO remove it from the list in MainWindow
            }
        }

        private async void redo_Click(object sender, RoutedEventArgs e)
        {
            await StartDownloadAsync();
        }
    }
}