using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace YoutubeDownloader
{
    /// <summary>
    /// Logique d'interaction pour PlaylistElement.xaml
    /// </summary>
    public partial class PlaylistElement : UserControl
    {
        public string Link { get; private set; }

        public PlaylistElement(string link, string title)
        {
            InitializeComponent();
            Link = link;
            this.title.Text = title;
        }

        public void AddElement(DownloadElement e) => videos.Children.Insert(0, e);

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            var elements = videos.Children.Cast<DownloadElement>();
            if (elements.Any(e => !e.IsCanceled))
                foreach (var element in elements)
                    element.Cancel();
            else
            {
                try
                {
                    ((StackPanel)this.Parent).Children.Remove(this);
                }
                catch (Exception)
                {
                    closeButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void extander_Click(object sender, RoutedEventArgs e)
        {
            videos.Visibility = videos.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            ((RotateTransform)expandImage.RenderTransform).Angle = videos.Visibility == Visibility.Visible ? 90 : 0;
        }
    }
}