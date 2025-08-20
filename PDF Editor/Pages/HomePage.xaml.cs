using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PDF_Editor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PDF_Editor.Pages
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        private readonly IPdfSession _pdf;
        public HomePage() : this(App.AppHost.Services.GetRequiredService<IPdfSession>()){}
        public HomePage(IPdfSession pdf)
        {
            _pdf = pdf;
            InitializeComponent();

            DataContext = _pdf;
        }

        private async void OpenPdf_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Select a PDF file"
            };

            if (dlg.ShowDialog() == true)
            {
                // Right now, just show which file was chosen
                SelectedFileText.Text = $"{Path.GetFileName(dlg.FileName)}";

                // Start loading UI
                OpenBtn.IsHitTestVisible = false;
                OpenIcon.Visibility = Visibility.Collapsed;
                OpenSpinner.Visibility = Visibility.Visible;
                OpenBtnText.Text = "Opening…";
                
                await Task.Delay(1500);

                try
                {
                    _pdf.Set(dlg.FileName);
                    (Application.Current.MainWindow as MainWindow)?.NavigateToEdit(dlg.FileName);
                }
                catch
                {
                    // If navigation fails, reset button UI
                    OpenSpinner.Visibility = Visibility.Collapsed;
                    OpenIcon.Visibility = Visibility.Visible;
                    OpenBtnText.Text = "Open PDF";
                    OpenBtn.IsHitTestVisible = true;
                    throw;
                }
            }
        }
    }
}
