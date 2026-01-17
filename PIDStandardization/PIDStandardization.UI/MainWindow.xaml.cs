using Microsoft.Extensions.DependencyInjection;
using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Services.TaggingServices;
using PIDStandardization.UI.Views;
using System.Windows;
using System.Windows.Controls;

namespace PIDStandardization.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServiceProvider _serviceProvider;
        private bool _isLoadingEquipmentProjects = false;
        private Project? _lastSelectedProject = null;

        public MainWindow(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _serviceProvider = serviceProvider;
            Loaded += MainWindow_Loaded;
            LoadProjects();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Show welcome screen on first launch
            if (Properties.Settings.Default.ShowWelcomeScreen)
            {
                var welcomeDialog = new WelcomeDialog();
                welcomeDialog.Owner = this;
                welcomeDialog.ShowDialog();
            }
        }

        private async void LoadProjects()
        {
            try
            {
                var projects = await _unitOfWork.Projects.GetAllAsync();
                ProjectsDataGrid.ItemsSource = projects;
                StatusTextBlock.Text = $"Loaded {projects.Count()} project(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading projects";
            }
        }

        private void ProjectsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectsDataGrid.SelectedItem is Project project)
            {
                _lastSelectedProject = project;
            }
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = _serviceProvider.GetRequiredService<NewProjectDialog>();
            if (dialog.ShowDialog() == true)
            {
                LoadProjects();
                StatusTextBlock.Text = $"Created project: {dialog.CreatedProject?.ProjectName}";
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            LoadProjects();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Help Menu Methods
        private void WelcomeScreen_Click(object sender, RoutedEventArgs e)
        {
            var welcomeDialog = new WelcomeDialog();
            welcomeDialog.Owner = this;
            welcomeDialog.ShowDialog();
        }

        private void QuickStartGuide_Click(object sender, RoutedEventArgs e)
        {
            // Show welcome dialog (which contains the quick start guide)
            var welcomeDialog = new WelcomeDialog();
            welcomeDialog.Owner = this;
            welcomeDialog.ShowDialog();
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Documentation will be available in the deployment package.\n\n" +
                "For now, refer to the Welcome Screen or contact the development team.",
                "Documentation",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "P&ID Standardization Application\n" +
                "Version 1.0.0\n\n" +
                "A comprehensive tool for managing P&ID equipment tagging,\n" +
                "supporting both Custom and KKS (DIN 40719) tagging standards.\n\n" +
                "Features:\n" +
                "• Dual tagging mode support (Custom and KKS)\n" +
                "• AutoCAD integration for equipment extraction\n" +
                "• Smart block learning mechanism\n" +
                "• Excel import/export capabilities\n" +
                "• Drawing version control\n\n" +
                "© 2026 - All rights reserved",
                "About P&ID Standardization",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // Equipment Management Methods
        private async void EquipmentProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EquipmentProjectComboBox.SelectedItem is Project project)
            {
                EquipmentProjectTaggingModeTextBlock.Text = $"Tagging Mode: {project.TaggingMode}";
                await LoadEquipmentForProject(project.ProjectId);
            }
        }

        private async Task LoadEquipmentForProject(Guid projectId)
        {
            try
            {
                var equipment = await _unitOfWork.Equipment.FindAsync(e => e.ProjectId == projectId);
                EquipmentDataGrid.ItemsSource = equipment;
                StatusTextBlock.Text = $"Loaded {equipment.Count()} equipment item(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading equipment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading equipment";
            }
        }

        private async void AddEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var tagValidationService = _serviceProvider.GetRequiredService<ITagValidationService>();
            var dialog = new EquipmentDialog(_unitOfWork, tagValidationService, selectedProject);

            if (dialog.ShowDialog() == true)
            {
                await LoadEquipmentForProject(selectedProject.ProjectId);
                StatusTextBlock.Text = $"Added equipment: {dialog.SavedEquipment?.TagNumber}";
            }
        }

        private async void RefreshEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentProjectComboBox.SelectedItem is Project project)
            {
                await LoadEquipmentForProject(project.ProjectId);
            }
        }

        private async void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (MainTabControl.SelectedIndex == 1) // Equipment tab
                {
                    await LoadEquipmentProjectsAsync();
                }
                else if (MainTabControl.SelectedIndex == 2) // Drawings tab
                {
                    await LoadDrawingsProjectsAsync();
                }
            }
        }

        private async Task LoadEquipmentProjectsAsync()
        {
            if (_isLoadingEquipmentProjects)
                return;

            _isLoadingEquipmentProjects = true;
            try
            {
                var projects = await _unitOfWork.Projects.GetAllAsync();
                EquipmentProjectComboBox.ItemsSource = projects;

                // Select the last selected project from Projects tab, or first project if none selected
                if (_lastSelectedProject != null && projects.Any(p => p.ProjectId == _lastSelectedProject.ProjectId))
                {
                    EquipmentProjectComboBox.SelectedItem = projects.First(p => p.ProjectId == _lastSelectedProject.ProjectId);
                }
                else if (projects.Any())
                {
                    EquipmentProjectComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingEquipmentProjects = false;
            }
        }

        // Drawing Management Methods
        private bool _isLoadingDrawingProjects = false;

        private async Task LoadDrawingsProjectsAsync()
        {
            if (_isLoadingDrawingProjects)
                return;

            _isLoadingDrawingProjects = true;
            try
            {
                var projects = await _unitOfWork.Projects.GetAllAsync();
                DrawingsProjectComboBox.ItemsSource = projects;

                // Select the last selected project from Projects tab, or first project if none selected
                if (_lastSelectedProject != null && projects.Any(p => p.ProjectId == _lastSelectedProject.ProjectId))
                {
                    DrawingsProjectComboBox.SelectedItem = projects.First(p => p.ProjectId == _lastSelectedProject.ProjectId);
                }
                else if (projects.Any())
                {
                    DrawingsProjectComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingDrawingProjects = false;
            }
        }

        private async void DrawingsProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DrawingsProjectComboBox.SelectedItem is Project project)
            {
                DrawingsProjectNameTextBlock.Text = $"Project: {project.ProjectName}";
                await LoadDrawingsForProject(project.ProjectId);
            }
        }

        private async Task LoadDrawingsForProject(Guid projectId)
        {
            try
            {
                var drawings = await _unitOfWork.Drawings.FindAsync(d => d.ProjectId == projectId);
                DrawingsDataGrid.ItemsSource = drawings;
                StatusTextBlock.Text = $"Loaded {drawings.Count()} drawing(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading drawings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading drawings";
            }
        }

        private async void ImportDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (DrawingsProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Open file dialog for .dwg or .dxf files
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "AutoCAD Drawings (*.dwg;*.dxf)|*.dwg;*.dxf|All Files (*.*)|*.*",
                Title = "Select P&ID Drawing File",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = openFileDialog.FileName;
                    var fileName = System.IO.Path.GetFileName(filePath);

                    // Calculate MD5 hash for duplicate detection
                    string fileHash;
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    using (var stream = System.IO.File.OpenRead(filePath))
                    {
                        var hash = md5.ComputeHash(stream);
                        fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }

                    // Check for duplicate
                    var existingDrawings = await _unitOfWork.Drawings.FindAsync(d => d.ProjectId == selectedProject.ProjectId);
                    var duplicateDrawing = existingDrawings.FirstOrDefault(d => d.FileHash == fileHash);

                    int versionNumber = 1;
                    if (duplicateDrawing != null)
                    {
                        var result = MessageBox.Show(
                            $"This drawing file already exists (Version {duplicateDrawing.VersionNumber}).\n\n" +
                            "Do you want to import it as a new version?",
                            "Duplicate Drawing Detected",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                            return;

                        versionNumber = duplicateDrawing.VersionNumber + 1;
                    }

                    // Create storage directory
                    var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    var storagePath = System.IO.Path.Combine(programDataPath, "PIDStandardization", "Drawings", selectedProject.ProjectName);
                    System.IO.Directory.CreateDirectory(storagePath);

                    // Copy file to storage
                    var storedFileName = $"{System.IO.Path.GetFileNameWithoutExtension(fileName)}_v{versionNumber}{System.IO.Path.GetExtension(fileName)}";
                    var storedFilePath = System.IO.Path.Combine(storagePath, storedFileName);
                    System.IO.File.Copy(filePath, storedFilePath, overwrite: true);

                    // Get file size
                    var fileInfo = new System.IO.FileInfo(filePath);

                    // Create drawing record
                    var drawing = new Drawing
                    {
                        DrawingId = Guid.NewGuid(),
                        ProjectId = selectedProject.ProjectId,
                        DrawingNumber = System.IO.Path.GetFileNameWithoutExtension(fileName),
                        DrawingTitle = System.IO.Path.GetFileNameWithoutExtension(fileName),
                        FileName = fileName,
                        FilePath = filePath,
                        StoredFilePath = storedFilePath,
                        FileHash = fileHash,
                        FileSizeBytes = fileInfo.Length,
                        VersionNumber = versionNumber,
                        ImportDate = DateTime.UtcNow,
                        ImportedBy = Environment.UserName,
                        Status = "Imported",
                        CreatedDate = DateTime.UtcNow
                    };

                    await _unitOfWork.Drawings.AddAsync(drawing);
                    await _unitOfWork.SaveChangesAsync();

                    await LoadDrawingsForProject(selectedProject.ProjectId);
                    StatusTextBlock.Text = $"Imported drawing: {fileName} (Version {versionNumber})";

                    MessageBox.Show(
                        $"Drawing imported successfully!\n\n" +
                        $"File: {fileName}\n" +
                        $"Version: {versionNumber}\n" +
                        $"Stored at: {storedFilePath}",
                        "Import Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing drawing: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusTextBlock.Text = "Error importing drawing";
                }
            }
        }

        private void ViewDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (DrawingsDataGrid.SelectedItem is not Drawing selectedDrawing)
            {
                MessageBox.Show("Please select a drawing first.", "No Drawing Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(selectedDrawing.StoredFilePath) && System.IO.File.Exists(selectedDrawing.StoredFilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = selectedDrawing.StoredFilePath,
                        UseShellExecute = true
                    });
                    StatusTextBlock.Text = $"Opening drawing: {selectedDrawing.FileName}";
                }
                else
                {
                    MessageBox.Show("Drawing file not found in storage.", "File Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening drawing: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDrawingList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export drawing list to Excel will be implemented in the next phase.",
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (DrawingsDataGrid.SelectedItem is not Drawing selectedDrawing)
            {
                MessageBox.Show("Please select a drawing first.", "No Drawing Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the drawing '{selectedDrawing.DrawingNumber}'?\n\n" +
                "This will also delete the stored file and unlink all associated equipment.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete the stored file
                    if (!string.IsNullOrEmpty(selectedDrawing.StoredFilePath) && System.IO.File.Exists(selectedDrawing.StoredFilePath))
                    {
                        System.IO.File.Delete(selectedDrawing.StoredFilePath);
                    }

                    // Delete from database
                    await _unitOfWork.Drawings.DeleteAsync(selectedDrawing);
                    await _unitOfWork.SaveChangesAsync();

                    await LoadDrawingsForProject(selectedDrawing.ProjectId);
                    StatusTextBlock.Text = $"Deleted drawing: {selectedDrawing.DrawingNumber}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting drawing: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RefreshDrawings_Click(object sender, RoutedEventArgs e)
        {
            if (DrawingsProjectComboBox.SelectedItem is Project project)
            {
                await LoadDrawingsForProject(project.ProjectId);
            }
        }
    }
}
