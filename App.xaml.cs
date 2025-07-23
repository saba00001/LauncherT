using System;
using System.Windows;
using ModernWpf;

namespace HRP
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set dark theme for Modern WPF
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            ThemeManager.Current.AccentColor = System.Windows.Media.Colors.DeepSkyBlue;

            // Handle unhandled exceptions
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"დაფიქსირდა შეცდომა: {e.Exception.Message}", "შეცდომა", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"კრიტიკული შეცდომა: {((Exception)e.ExceptionObject).Message}", "კრიტიკული შეცდომა", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}