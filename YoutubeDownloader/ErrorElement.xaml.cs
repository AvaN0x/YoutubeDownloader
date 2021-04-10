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
                ErrorType.UnauthorizedAccess => "The program require admin permissions.",
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