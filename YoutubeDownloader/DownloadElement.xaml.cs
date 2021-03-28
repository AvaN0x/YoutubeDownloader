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
        public static readonly Brush BRUSH_BLUE = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#4e88d9");
        public static readonly Brush BRUSH_RED = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#b8200f");
        public static readonly Brush BRUSH_GREEN = (Brush)(new System.Windows.Media.BrushConverter()).ConvertFromString("#179c22");

        public string FolderPath { get; private set; }
        public bool IsCanceled => CancelTokenSource.IsCancellationRequested;
        public string Link { get; }
        public string? VideoPath { get; private set; }
        private CancellationTokenSource CancelTokenSource { get; set; }
        private IStreamInfo? StreamInfo { get; set; }

        public DownloadElement(string link)
        {
            InitializeComponent();

            this.Link = link;
            this.FolderPath = MainWindow.Config.DownloadPath;
            CancelTokenSource = new CancellationTokenSource();

            label.Text = Link;
        }

        public void Cancel()
        {
            CancelTokenSource.Cancel();
            progressbar.IsIndeterminate = false;
            progressbar.Foreground = BRUSH_RED;
            progressbar.Value = 100;
        }

        public async Task SetupAsync()
        {
            // Add back indetermination to progressbar
            progressbar.IsIndeterminate = true;
            redo.Visibility = Visibility.Collapsed;
            progressbar.Foreground = BRUSH_GREEN;

            try
            {
                var video = await MainWindow.Youtube.Videos.GetAsync(Link);
                if (CancelTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();

                // Now that the video have loaded, we can display the video title
                label.Text = video.Title;

                var streamManifest = await MainWindow.Youtube.Videos.Streams.GetManifestAsync(Link);

                switch (MainWindow.Config.Extension)
                {
                    case Extension.mp3:
                        VideoPath = System.IO.Path.Combine(FolderPath, Utils.RemoveInvalidChars(video.Title) + ".mp3");
                        StreamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();
                        break;

                    case Extension.mp4:
                    default:
                        StreamInfo = streamManifest.GetMuxed().WithHighestVideoQuality();
                        VideoPath = null;
                        break;
                }

                if (CancelTokenSource.IsCancellationRequested)
                    throw new OperationCanceledException();

                if (StreamInfo is not null)
                {
                    if (VideoPath is null)
                        VideoPath = System.IO.Path.Combine(FolderPath, Utils.RemoveInvalidChars(video.Title) + "." + StreamInfo.Container);

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
                                throw new OperationCanceledException();
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                // Link is not a valid youtube link
                Cancel();
            }
            catch (OperationCanceledException)
            {
                // ConcellationToken event
                StreamInfo = null;
                Cancel();

                redo.Visibility = Visibility.Visible;
            }
        }

        public async Task StartDownloadAsync()
        {
            try
            {
                if (StreamInfo is not null && VideoPath is not null)
                {
                    if (CancelTokenSource.IsCancellationRequested)

                        throw new OperationCanceledException();
                    var progress = new Progress<double>(percent =>
                    {
                        progressbar.Value = Math.Round(percent * 100);
                    });

                    progressbar.IsIndeterminate = false;
                    progressbar.Value = 1;

                    await MainWindow.Youtube.Videos.Streams.DownloadAsync(StreamInfo, VideoPath, progress, CancelTokenSource.Token);
                    if (CancelTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException();

                    progressbar.Value = 100;
                    progressbar.Foreground = BRUSH_BLUE;
                    redo.Visibility = Visibility.Collapsed;
                    open.Visibility = Visibility.Visible;
                    openFolder.Visibility = Visibility.Visible;
                    CancelTokenSource.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
                // ConcellationToken event
                Cancel();

                if (VideoPath != null && File.Exists(VideoPath))
                {
                    File.Delete(VideoPath);
                }

                redo.Visibility = Visibility.Visible;
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            if (!CancelTokenSource.IsCancellationRequested)
            {
                CancelTokenSource.Cancel();
                progressbar.IsIndeterminate = false;
                progressbar.Value = 100;
                progressbar.Foreground = BRUSH_RED;
            }
            else
            {
                try
                {
                    ((StackPanel)this.Parent).Children.Remove(this);
                }
                catch (Exception)
                {
                    close.Visibility = Visibility.Collapsed;
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

        private async void redo_Click(object sender, RoutedEventArgs e)
        {
            CancelTokenSource = new CancellationTokenSource();
            await SetupAsync();
            await StartDownloadAsync();
        }
    }
}