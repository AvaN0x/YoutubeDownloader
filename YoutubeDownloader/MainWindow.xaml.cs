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
using System.Windows.Shapes;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Exceptions;
using Microsoft.Win32;

namespace YoutubeDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //txtbx_folder.Text = Directory.GetCurrentDirectory();
            txtbx_folder.Text = (string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", null) ?? Directory.GetCurrentDirectory();
        }

        private void OnTop_Checked(object sender, RoutedEventArgs e)
        {
            Topmost = true;
        }

        private void OnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            Topmost = false;
        }

        private void txtbx_input_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                TryDownloadLink(txtbx_input.Text.Trim());
            }
        }

        private async void TryDownloadLink(string link)
        {
            if (link.Trim() != string.Empty)
                // We check to see if it is a playlist, then we create a DownloadElement for each video of the playlist
                // Else we check if it is a video
                try
                {
                    var youtube = new YoutubeClient();
                    var playlist = await youtube.Playlists.GetAsync(link);

                    txtbx_input.Text = "";
                    await foreach (var video in youtube.Playlists.GetVideosAsync(playlist.Id))
                    {
                        var dl = new DownloadElement(video.Url, txtbx_folder.Text);
                        history.Children.Insert(0, dl);

                        _ = dl.StartDownloadAsync();
                    }
                }
                catch (Exception)
                {
                    var dl = new DownloadElement(link, txtbx_folder.Text);

                    history.Children.Insert(0, dl);
                    txtbx_input.Text = "";

                    await dl.StartDownloadAsync();
                }
        }

        private void btn_folderDialog_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new Winforms.FolderBrowserDialog();
            Winforms.DialogResult result = fbd.ShowDialog();

            if (result == Winforms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                txtbx_folder.Text = fbd.SelectedPath;
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            TryDownloadLink(txtbx_input.Text.Trim());
        }
    }
}