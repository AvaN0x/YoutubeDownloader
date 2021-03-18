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

        private CancellationTokenSource? cancellationTokenSource { get; set; }

        public DownloadElement(String link)
        {
            InitializeComponent();

            this.Link = link;
            label.Text = Link;
        }

        public async Task StartDownloadAsync(String path)
        {
            var youtube = new YoutubeClient();
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var video = await youtube.Videos.GetAsync(Link);

                // Wait for the information to be downloaded before displaying the grid
                VideoPath = System.IO.Path.Combine(path, RemoveInvalidChars(video.Title) + ".mp3");
                label.Text = video.Title;
                progressbar.IsIndeterminate = false;

                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(Link);
                var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

                if (streamInfo != null)
                {
                    var progress = new Progress<double>(percent =>
                    {
                        progressbar.Value = Math.Round(percent * 100);
                    });

                    await youtube.Videos.Streams.DownloadAsync(streamInfo, VideoPath, progress, cancellationTokenSource.Token);
                    progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#4e88d9");

                    open.Visibility = Visibility.Visible;
                    openFolder.Visibility = Visibility.Visible;
                }
            }
            catch (ArgumentException)
            {
                progressbar.IsIndeterminate = false;
                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");
                progressbar.Value = 100;
            }
            catch (OperationCanceledException)
            {
                progressbar.IsIndeterminate = false;
                progressbar.Foreground = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");

                if (VideoPath != null && File.Exists(VideoPath))
                {
                    File.Delete(VideoPath);
                }
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

        private string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(System.IO.Path.GetInvalidFileNameChars()));
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }
    }
}