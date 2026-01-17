using System.Windows;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Interaction logic for WelcomeDialog.xaml
    /// </summary>
    public partial class WelcomeDialog : Window
    {
        public WelcomeDialog()
        {
            InitializeComponent();
        }

        private void GetStarted_Click(object sender, RoutedEventArgs e)
        {
            // Save the "Don't show again" preference
            if (DontShowAgainCheckBox.IsChecked == true)
            {
                Properties.Settings.Default.ShowWelcomeScreen = false;
                Properties.Settings.Default.Save();
            }

            DialogResult = true;
            Close();
        }

        private void ViewDocumentation_Click(object sender, RoutedEventArgs e)
        {
            // Save the "Don't show again" preference
            if (DontShowAgainCheckBox.IsChecked == true)
            {
                Properties.Settings.Default.ShowWelcomeScreen = false;
                Properties.Settings.Default.Save();
            }

            // Open documentation (for now, show a message - can be updated to open PDF later)
            MessageBox.Show(
                "Documentation will be available in the deployment package.\n\n" +
                "For now, refer to the Quick Start Guide above or contact the development team.",
                "Documentation",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
