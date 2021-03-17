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

            //Trace.WriteLine(Directory.GetCurrentDirectory());

            txtbx_folder.Text = Directory.GetCurrentDirectory();

            //DownloadVideoAsync("https://youtu.be/o-YBDTqX_ZU");
            //Directory.Exists("C:/Users/cleme/Downloads/");
        }

        public async Task DownloadVideoAsync(String link)
        {
            var youtube = new YoutubeClient();

            var video = await youtube.Videos.GetAsync(link);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(link);

            var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

            if (streamInfo != null && video != null)
            {
                await youtube.Videos.Streams.DownloadAsync(streamInfo, $"C:/Users/cleme/Downloads/{video.Title}.mp3");
                Trace.WriteLine("download done");
            }
            else
                Trace.WriteLine("is null");
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

                Trace.WriteLine("Start download");
            }
        }

        private void btn_folderDialog_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new Winforms.FolderBrowserDialog();
            Winforms.DialogResult result = fbd.ShowDialog();

            if (result == Winforms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                txtbx_folder.Text = fbd.SelectedPath;
        }
    }
}
