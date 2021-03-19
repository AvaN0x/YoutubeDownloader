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
        public String? FolderPath { get; private set; }
        private CancellationTokenSource? CancelTokenSource { get; set; }

        public DownloadElement(String link)
        {
            InitializeComponent();

            this.Link = link;
            label.Text = Link;
        }

        public async Task StartDownloadAsync(String path)
        {
            var youtube = new YoutubeClient();
            FolderPath = path;

            // Add back indetermination to progressbar
            progressbar.IsIndeterminate = true;
            redo.Visibility = Visibility.Collapsed;
            progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#179c22");

            try
            {
                var video = await youtube.Videos.GetAsync(Link);

                // Now that the video have loaded, we can display the video title
                VideoPath = System.IO.Path.Combine(FolderPath, Utils.RemoveInvalidChars(video.Title) + ".mp3");
                label.Text = video.Title;
                progressbar.IsIndeterminate = false;
                progressbar.Value = 0;

                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(Link);
                var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

                if (streamInfo != null)
                {
                    CancelTokenSource = new CancellationTokenSource();

                    //if (File.Exists(VideoPath))
                    //{
                    //    // TODO ask the user if we should rewrite or abandon
                    //}

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
            if (FolderPath != null)
                await StartDownloadAsync(FolderPath);
            else
                redo.Visibility = Visibility.Collapsed;
        }
    }
}