using PDF_Editor.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
using Wpf.Ui.Controls;

namespace PDF_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string WindowTitle { get; }
        public MainWindow()
        {
            var version = Assembly.GetEntryAssembly()?
            .GetName().Version?.ToString(3) ?? "1.0.0";

            WindowTitle = $"PDF Editor v{version}";
            DataContext = this;
            InitializeComponent();
            RootNavigation.SelectionChanged += new TypedEventHandler<NavigationView, RoutedEventArgs>(RootNavigation_SelectionChanged);

            RootNavigation.Loaded += RootNavigation_Loaded; 
        }

        private void RootNavigation_Loaded(object sender, RoutedEventArgs e)
        {
            RootNavigation.Navigate(typeof(Pages.HomePage));
        }

        public void RootNavigation_SelectionChanged(NavigationView sender, RoutedEventArgs e)
        {
            var item = sender.SelectedItem as NavigationViewItem;
            var tag = item?.Tag as string;

            switch (tag)
            {
                case "Home":
                    sender.Navigate(typeof(Pages.HomePage));
                    break;

                case "Edit":
                    sender.Navigate(typeof(Pages.EditPage));
                    break;

                case "Attachments":
                    sender.Navigate(typeof(Pages.AttachmentsPage));
                    break;

                case "Settings":
                    sender.Navigate(typeof(Pages.SettingsPage));
                    break;
            }
        }
        public void NavigateToEdit(string pdfPath)
        {
            // Navigate directly to EditPage and pass the PDF path
            RootNavigation.Navigate(typeof(Pages.EditPage), pdfPath);
        }
    }
}
