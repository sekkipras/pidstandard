using System.Windows;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Interaction logic for UserGuideDialog.xaml
    /// </summary>
    public partial class UserGuideDialog : Window
    {
        public UserGuideDialog()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
