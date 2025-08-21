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
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace PDF_Editor.Windows
{
    /// <summary>
    /// Interaction logic for CustomMessageBox.xaml
    public enum CustomMessageBoxResult { Primary, Secondary, Closed }
    public enum CustomMessageBoxIcon { Info, Success, Warning, Error, Question }
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        #nullable enable
        public string TitleText { get; set; } = "Message";
        public string MessageText { get; set; } = "";
        public string PrimaryButtonText { get; set; } = "OK";
        public string? SecondaryButtonText { get; set; } = null;
        public object? ExtraContent { get; set; } = null; // optional extra control/markup
        public CustomMessageBoxIcon MessageIcon { get; set; } = CustomMessageBoxIcon.Info;

        private CustomMessageBoxResult _result = CustomMessageBoxResult.Closed;
        public CustomMessageBox()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += (_, __) => ApplyIcon();
        }
        private void ApplyIcon()
        {
            // Map icon to Fluent Symbol
            // Adjust symbol names if your Wpf.Ui version differs.
            IconElement.Symbol = MessageIcon switch
            {
                CustomMessageBoxIcon.Success => SymbolRegular.CheckmarkCircle24,
                CustomMessageBoxIcon.Warning => SymbolRegular.Warning24,
                CustomMessageBoxIcon.Error => SymbolRegular.ErrorCircle24,
                CustomMessageBoxIcon.Question => SymbolRegular.ChatHelp24,
                _ => SymbolRegular.Info24
            };

            // Show/hide secondary button
            SecondaryButton.Visibility = string.IsNullOrWhiteSpace(SecondaryButtonText)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Primary_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomMessageBoxResult.Primary;
            DialogResult = true; // closes ShowDialog
        }

        private void Secondary_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomMessageBoxResult.Secondary;
            DialogResult = false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomMessageBoxResult.Closed;
            Close();
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        public static CustomMessageBoxResult Show(
            #nullable enable
            Window? owner,
            string title,
            string message,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.Info,
            string primaryText = "OK",
            string? secondaryText = null,
            object? extraContent = null)
        {
            var dlg = new CustomMessageBox
            {
                TitleText = title,
                MessageText = message,
                MessageIcon = icon,
                PrimaryButtonText = primaryText,
                SecondaryButtonText = secondaryText,
                ExtraContent = extraContent
            };

            if (owner != null && owner.IsLoaded)
            {
                dlg.Owner = owner;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }


            _ = dlg.ShowDialog();
            return dlg._result;
        }
    }
}
