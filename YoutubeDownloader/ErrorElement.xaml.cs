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
    /// Logique d'interaction pour ErrorElement.xaml
    /// </summary>
    public partial class ErrorElement : UserControl
    {
        public ErrorType ErrorType { get; init; }

        public ErrorElement(ErrorType errorType)
        {
            InitializeComponent();
            ErrorType = errorType;

            lbl_warning.Text = ErrorType switch
            {
                ErrorType.YoutubeTransientFailure => "There's a problem on Youtube's side. Please wait some time and try again.",
                ErrorType.UnauthorizedAccess => "The program can't access the specified folder.",
                ErrorType.ConfigReset => "The config file got reset. The old config file got renamed if you want to fix it.",
                ErrorType.ConfigIsNotAccessible => "The config file is not accessible.\n(" + Config.ConfigDir + ")",
                _ => Enum.GetName(ErrorType),
            };
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject ucParent = this.Parent;

            while (!(ucParent is ErrorsContainer))
            {
                ucParent = LogicalTreeHelper.GetParent(ucParent);
            }
            var container = (ErrorsContainer)ucParent;
            container.RemoveError(ErrorType);
        }
    }
}