using System.Windows.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace PDF_Editor.Pages
{
    /// <summary>
    /// Interaction logic for EditPage.xaml
    /// </summary>
    public partial class EditPage : Page
    {
        public EditPage()
        {
            InitializeComponent();
            DataContextChanged += EditPage_DataContextChanged;
        }

        private void EditPage_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is string path)
            {
                PathText.Text = $"Editing: {path}";
                // TODO: load the PDF here
            }
        }

    }
}
