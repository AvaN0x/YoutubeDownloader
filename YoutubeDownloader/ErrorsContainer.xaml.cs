using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public enum ErrorType
    {
        YoutubeTransientFailure
    }

    /// <summary>
    /// Logique d'interaction pour ErrorsContainer.xaml
    /// There can only be one error of each type
    /// </summary>
    public partial class ErrorsContainer : UserControl
    {
        public ErrorsContainer()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public void AddError(ErrorType errorType)
        {
            if (!errors.Children.OfType<ErrorElement>().Any(elt => elt.ErrorType == errorType))
                errors.Children.Insert(0, new ErrorElement(errorType));
            DisplayIfNeeded();
        }

        public void RemoveError(ErrorType errorType)
        {
            errors.Children.Remove(errors.Children.OfType<ErrorElement>().Where(elt => elt.ErrorType == errorType).First());
            // TODO only display some of the errors ?
            DisplayIfNeeded();
        }

        private void DisplayIfNeeded() => this.Visibility = errors.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}