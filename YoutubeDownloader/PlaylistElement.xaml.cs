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
        public PlaylistElement(string title)
        {
            InitializeComponent();
            this.title.Text = title;
        }

        public void AddElement(UIElement e) => videos.Children.Insert(0, e);

        private void extander_Click(object sender, RoutedEventArgs e)
            => videos.Visibility = videos.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }
}