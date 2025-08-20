using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using PDF_Editor.Services;

namespace PDF_Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; } = null!;
        protected override void OnStartup(StartupEventArgs e)
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IPdfSession, PdfSession>();
                    // services.AddSingleton<MainWindow>(); // if you want to resolve it too
                })
                .Build();
            base.OnStartup(e);

            // Force Dark theme at startup
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);
            ApplicationAccentColorManager.Apply(Color.FromRgb(0xFF, 0x6F, 0x00),
                                             ApplicationTheme.Dark,
                                             false);
            // --- Global brush overrides (affect all ui:Button) ---
            var white = new SolidColorBrush(Colors.White); white.Freeze();
        }
    }
}
