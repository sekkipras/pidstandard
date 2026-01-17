using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Enums;
using PIDStandardization.Core.Interfaces;
using System.Windows;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Interaction logic for ProjectSelectionDialog.xaml
    /// </summary>
    public partial class ProjectSelectionDialog : Window
    {
        private readonly IUnitOfWork _unitOfWork;

        public Project? SelectedProject { get; private set; }

        public ProjectSelectionDialog(IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            LoadProjects();
        }

        private async void LoadProjects()
        {
            try
            {
                var projects = await _unitOfWork.Projects
                    .FindAsync(p => p.IsActive);

                // Order by created date descending
                var orderedProjects = projects.OrderByDescending(p => p.CreatedDate).ToList();

                ProjectsDataGrid.ItemsSource = orderedProjects;

                // Auto-select first project if available
                if (projects.Any())
                {
                    ProjectsDataGrid.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectsDataGrid.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project from the list.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedProject = selectedProject;
            DialogResult = true;
            Close();
        }

        private void ProjectsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Double-click to select project
            if (ProjectsDataGrid.SelectedItem is Project)
            {
                SelectProject_Click(sender, e);
            }
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            var newProjectDialog = new NewProjectDialog(_unitOfWork);

            if (newProjectDialog.ShowDialog() == true)
            {
                // Reload projects list
                LoadProjects();

                // Auto-select the newly created project
                if (newProjectDialog.CreatedProject != null)
                {
                    SelectedProject = newProjectDialog.CreatedProject;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
