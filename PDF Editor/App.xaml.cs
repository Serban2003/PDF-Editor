using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PDF_Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Force Dark theme at startup
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);
            ApplicationAccentColorManager.Apply(Color.FromRgb(0xFF, 0x80, 0x00),
                                             ApplicationTheme.Dark,
                                             false);
        }
    }
}
