using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Enums;
using PIDStandardization.Core.Interfaces;
using System.Windows;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Interaction logic for NewProjectDialog.xaml
    /// </summary>
    public partial class NewProjectDialog : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        public Project? CreatedProject { get; private set; }

        public NewProjectDialog(IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
            {
                MessageBox.Show("Please enter a project name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return;
            }

            try
            {
                // Determine tagging mode
                var taggingMode = CustomRadioButton.IsChecked == true
                    ? TaggingMode.Custom
                    : TaggingMode.KKS;

                // Create new project
                var project = new Project
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = ProjectNameTextBox.Text.Trim(),
                    ProjectNumber = ProjectNumberTextBox.Text.Trim(),
                    Client = ClientTextBox.Text.Trim(),
                    TaggingMode = taggingMode,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                // Save to database
                await _unitOfWork.Projects.AddAsync(project);
                await _unitOfWork.SaveChangesAsync();

                CreatedProject = project;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating project: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
