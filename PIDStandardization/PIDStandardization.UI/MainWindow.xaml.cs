using Microsoft.Extensions.DependencyInjection;
using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Services;
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
        private bool _isLoadingLinesProjects = false;
        private bool _isLoadingInstrumentsProjects = false;
        private bool _isInitializing = false;
        private Project? _lastSelectedProject = null;
        private Project? _selectedProject = null;

        public MainWindow(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _serviceProvider = serviceProvider;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Show project selection dialog at startup
            // Create a new scope to get a fresh UnitOfWork instance
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var projectSelectionDialog = new ProjectSelectionDialog(unitOfWork);
            projectSelectionDialog.Owner = this;

            if (projectSelectionDialog.ShowDialog() == true)
            {
                _selectedProject = projectSelectionDialog.SelectedProject;
                _lastSelectedProject = _selectedProject;

                // Update window title with selected project
                if (_selectedProject != null)
                {
                    Title = $"P&ID Standardization - {_selectedProject.ProjectName}";
                    StatusTextBlock.Text = $"Selected project: {_selectedProject.ProjectName}";

                    // Pre-populate all tabs with selected project
                    await InitializeTabsWithSelectedProject();
                }

                // Show welcome screen on first launch (after project selection)
                if (Properties.Settings.Default.ShowWelcomeScreen)
                {
                    var welcomeDialog = new WelcomeDialog();
                    welcomeDialog.Owner = this;
                    welcomeDialog.ShowDialog();
                }
            }
            else
            {
                // User cancelled project selection - exit application
                Application.Current.Shutdown();
            }
        }

        private async Task InitializeTabsWithSelectedProject()
        {
            if (_selectedProject == null)
                return;

            _isInitializing = true;
            try
            {
                // Load project into all tab ComboBoxes
                var projects = new[] { _selectedProject };

                DashboardProjectComboBox.ItemsSource = projects;
                DashboardProjectComboBox.SelectedIndex = 0;

                EquipmentProjectComboBox.ItemsSource = projects;
                EquipmentProjectComboBox.SelectedIndex = 0;

                LinesProjectComboBox.ItemsSource = projects;
                LinesProjectComboBox.SelectedIndex = 0;

                InstrumentsProjectComboBox.ItemsSource = projects;
                InstrumentsProjectComboBox.SelectedIndex = 0;

                DrawingsProjectComboBox.ItemsSource = projects;
                DrawingsProjectComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing tabs: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = _serviceProvider.GetRequiredService<NewProjectDialog>();
            if (dialog.ShowDialog() == true)
            {
                StatusTextBlock.Text = $"Created project: {dialog.CreatedProject?.ProjectName}";
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            // Show project selection dialog
            // Create a new scope to get a fresh UnitOfWork instance
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var projectSelectionDialog = new ProjectSelectionDialog(unitOfWork);
            projectSelectionDialog.Owner = this;

            if (projectSelectionDialog.ShowDialog() == true && projectSelectionDialog.SelectedProject != null)
            {
                _selectedProject = projectSelectionDialog.SelectedProject;
                _lastSelectedProject = _selectedProject;
                Title = $"P&ID Standardization - {_selectedProject.ProjectName}";
                StatusTextBlock.Text = $"Switched to project: {_selectedProject.ProjectName}";
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Help Menu Methods
        private void UserGuide_Click(object sender, RoutedEventArgs e)
        {
            var userGuideDialog = new UserGuideDialog();
            userGuideDialog.Owner = this;
            userGuideDialog.ShowDialog();
        }

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
            if (_isInitializing) return;

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

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var tagValidationService = scope.ServiceProvider.GetRequiredService<ITagValidationService>();

            var dialog = new EquipmentDialog(unitOfWork, tagValidationService, selectedProject);

            if (dialog.ShowDialog() == true)
            {
                await LoadEquipmentForProject(selectedProject.ProjectId);
                StatusTextBlock.Text = $"Added equipment: {dialog.SavedEquipment?.TagNumber}";
            }
        }

        private async void EditEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is not Equipment selectedEquipment)
            {
                MessageBox.Show("Please select equipment first.", "No Equipment Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EquipmentProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Project not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var tagValidationService = scope.ServiceProvider.GetRequiredService<ITagValidationService>();

            var dialog = new EquipmentDialog(unitOfWork, tagValidationService, selectedProject, selectedEquipment);

            if (dialog.ShowDialog() == true)
            {
                await LoadEquipmentForProject(selectedProject.ProjectId);
                StatusTextBlock.Text = $"Updated equipment: {selectedEquipment.TagNumber}";
            }
        }

        private async void DeleteEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is not Equipment selectedEquipment)
            {
                MessageBox.Show("Please select equipment first.", "No Equipment Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete equipment '{selectedEquipment.TagNumber}'?\n\n" +
                "This will also unlink it from any lines and instruments.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _unitOfWork.Equipment.DeleteAsync(selectedEquipment);
                    await _unitOfWork.SaveChangesAsync();

                    await LoadEquipmentForProject(selectedEquipment.ProjectId);
                    StatusTextBlock.Text = $"Deleted equipment: {selectedEquipment.TagNumber}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting equipment: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Equipment_{selectedProject.ProjectName}_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Export Equipment to Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var equipment = await _unitOfWork.Equipment.FindAsync(e => e.ProjectId == selectedProject.ProjectId);
                    var excelService = new ExcelExportService();
                    excelService.ExportEquipment(equipment, saveFileDialog.FileName, selectedProject.ProjectName);

                    StatusTextBlock.Text = $"Exported {equipment.Count()} equipment items to Excel";
                    MessageBox.Show($"Equipment list exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting equipment: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Import Equipment from Excel"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    StatusTextBlock.Text = "Importing equipment...";

                    // Get existing tag numbers to prevent duplicates
                    var existingEquipment = await _unitOfWork.Equipment.FindAsync(e => e.ProjectId == selectedProject.ProjectId);
                    var existingTags = existingEquipment.Select(e => e.TagNumber).ToList();

                    // Import from Excel
                    var importService = new ExcelImportService();
                    var result = importService.ImportEquipment(openFileDialog.FileName, selectedProject.ProjectId, existingTags);

                    // Show results dialog
                    ShowImportResultDialog("Equipment Import", result);

                    // Refresh the grid
                    await LoadEquipmentForProject(selectedProject.ProjectId);

                    StatusTextBlock.Text = $"Import complete: {result.SuccessCount} added, {result.SkippedCount} skipped, {result.ErrorCount} errors";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing equipment: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DownloadEquipmentTemplate_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Equipment_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Download Equipment Template"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var importService = new ExcelImportService();
                    importService.GenerateEquipmentTemplate(saveFileDialog.FileName);

                    MessageBox.Show($"Template downloaded successfully!\n\nFile: {saveFileDialog.FileName}\n\nFill in the equipment data and use 'Import from Excel' to load it.",
                        "Template Downloaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading template: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                if (MainTabControl.SelectedIndex == 0) // Dashboard tab
                {
                    // Dashboard loads on project selection
                }
                else if (MainTabControl.SelectedIndex == 1) // Equipment tab
                {
                    await LoadEquipmentProjectsAsync();
                }
                else if (MainTabControl.SelectedIndex == 2) // Lines tab
                {
                    await LoadLinesProjectsAsync();
                }
                else if (MainTabControl.SelectedIndex == 3) // Instruments tab
                {
                    await LoadInstrumentsProjectsAsync();
                }
                else if (MainTabControl.SelectedIndex == 4) // Drawings tab
                {
                    await LoadDrawingsProjectsAsync();
                }
                else if (MainTabControl.SelectedIndex == 6) // Audit Log tab (after Validation)
                {
                    await LoadAuditLogProjectsAsync();
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

        // Lines Management Methods
        private async Task LoadLinesProjectsAsync()
        {
            if (_isLoadingLinesProjects)
                return;

            _isLoadingLinesProjects = true;
            try
            {
                var projects = await _unitOfWork.Projects.GetAllAsync();
                LinesProjectComboBox.ItemsSource = projects;

                // Select the last selected project from Projects tab, or first project if none selected
                if (_lastSelectedProject != null && projects.Any(p => p.ProjectId == _lastSelectedProject.ProjectId))
                {
                    LinesProjectComboBox.SelectedItem = projects.First(p => p.ProjectId == _lastSelectedProject.ProjectId);
                }
                else if (projects.Any())
                {
                    LinesProjectComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingLinesProjects = false;
            }
        }

        private async void LinesProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (LinesProjectComboBox.SelectedItem is Project project)
            {
                LinesProjectTaggingModeTextBlock.Text = $"Tagging Mode: {project.TaggingMode}";
                await LoadLinesForProject(project.ProjectId);
            }
        }

        private async Task LoadLinesForProject(Guid projectId)
        {
            try
            {
                var lines = await _unitOfWork.Lines.FindAsync(l => l.ProjectId == projectId);
                LinesDataGrid.ItemsSource = lines;
                StatusTextBlock.Text = $"Loaded {lines.Count()} line(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading lines: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading lines";
            }
        }

        private async void AddLine_Click(object sender, RoutedEventArgs e)
        {
            if (LinesProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dialog = new LineDialog(unitOfWork, selectedProject);

            if (dialog.ShowDialog() == true)
            {
                await LoadLinesForProject(selectedProject.ProjectId);
                StatusTextBlock.Text = $"Added line: {dialog.SavedLine?.LineNumber}";
            }
        }

        private async void EditLine_Click(object sender, RoutedEventArgs e)
        {
            if (LinesDataGrid.SelectedItem is not Line selectedLine)
            {
                MessageBox.Show("Please select a line first.", "No Line Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (LinesProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Project not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dialog = new LineDialog(unitOfWork, selectedProject, selectedLine);

            if (dialog.ShowDialog() == true)
            {
                await LoadLinesForProject(selectedProject.ProjectId);
                StatusTextBlock.Text = $"Updated line: {selectedLine.LineNumber}";
            }
        }

        private async void DeleteLine_Click(object sender, RoutedEventArgs e)
        {
            if (LinesDataGrid.SelectedItem is not Line selectedLine)
            {
                MessageBox.Show("Please select a line first.", "No Line Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the line '{selectedLine.LineNumber}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _unitOfWork.Lines.DeleteAsync(selectedLine);
                    await _unitOfWork.SaveChangesAsync();

                    await LoadLinesForProject(selectedLine.ProjectId);
                    StatusTextBlock.Text = $"Deleted line: {selectedLine.LineNumber}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting line: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportLines_Click(object sender, RoutedEventArgs e)
        {
            if (LinesProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Lines_{selectedProject.ProjectName}_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Export Lines to Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var lines = await _unitOfWork.Lines.FindAsync(l => l.ProjectId == selectedProject.ProjectId);
                    var excelService = new ExcelExportService();
                    excelService.ExportLines(lines, saveFileDialog.FileName, selectedProject.ProjectName);

                    StatusTextBlock.Text = $"Exported {lines.Count()} lines to Excel";
                    MessageBox.Show($"Lines list exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting lines: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportLines_Click(object sender, RoutedEventArgs e)
        {
            if (LinesProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Import Lines from Excel"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    StatusTextBlock.Text = "Importing lines...";

                    // Get existing line numbers
                    var existingLines = await _unitOfWork.Lines.FindAsync(l => l.ProjectId == selectedProject.ProjectId);
                    var existingLineNumbers = existingLines.Select(l => l.LineNumber).ToList();

                    // Get equipment mapping for From/To associations
                    var allEquipment = await _unitOfWork.Equipment.FindAsync(e => e.ProjectId == selectedProject.ProjectId);
                    var equipmentTagMap = allEquipment.ToDictionary(e => e.TagNumber, e => e.EquipmentId);

                    // Import from Excel
                    var importService = new ExcelImportService();
                    var result = importService.ImportLines(openFileDialog.FileName, selectedProject.ProjectId,
                        existingLineNumbers, equipmentTagMap);

                    // Show results dialog
                    ShowImportResultDialog("Lines Import", result);

                    // Refresh the grid
                    await LoadLinesForProject(selectedProject.ProjectId);

                    StatusTextBlock.Text = $"Import complete: {result.SuccessCount} added, {result.SkippedCount} skipped, {result.ErrorCount} errors";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing lines: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DownloadLinesTemplate_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Lines_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Download Lines Template"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var importService = new ExcelImportService();
                    importService.GenerateLinesTemplate(saveFileDialog.FileName);

                    MessageBox.Show($"Template downloaded successfully!\n\nFile: {saveFileDialog.FileName}\n\nFill in the lines data and use 'Import from Excel' to load it.",
                        "Template Downloaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading template: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RefreshLines_Click(object sender, RoutedEventArgs e)
        {
            if (LinesProjectComboBox.SelectedItem is Project project)
            {
                await LoadLinesForProject(project.ProjectId);
            }
        }

        // Instruments Management Methods
        private async Task LoadInstrumentsProjectsAsync()
        {
            if (_isLoadingInstrumentsProjects)
                return;

            _isLoadingInstrumentsProjects = true;
            try
            {
                var projects = await _unitOfWork.Projects.GetAllAsync();
                InstrumentsProjectComboBox.ItemsSource = projects;

                // Select the last selected project from Projects tab, or first project if none selected
                if (_lastSelectedProject != null && projects.Any(p => p.ProjectId == _lastSelectedProject.ProjectId))
                {
                    InstrumentsProjectComboBox.SelectedItem = projects.First(p => p.ProjectId == _lastSelectedProject.ProjectId);
                }
                else if (projects.Any())
                {
                    InstrumentsProjectComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingInstrumentsProjects = false;
            }
        }

        private async void InstrumentsProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (InstrumentsProjectComboBox.SelectedItem is Project project)
            {
                InstrumentsProjectTaggingModeTextBlock.Text = $"Tagging Mode: {project.TaggingMode}";
                await LoadInstrumentsForProject(project.ProjectId);
            }
        }

        private async Task LoadInstrumentsForProject(Guid projectId)
        {
            try
            {
                var instruments = await _unitOfWork.Instruments.FindAsync(i => i.ProjectId == projectId);
                InstrumentsDataGrid.ItemsSource = instruments;
                StatusTextBlock.Text = $"Loaded {instruments.Count()} instrument(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading instruments: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading instruments";
            }
        }

        private async void AddInstrument_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentsProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dialog = new InstrumentDialog(unitOfWork, selectedProject);

            if (dialog.ShowDialog() == true)
            {
                await LoadInstrumentsForProject(selectedProject.ProjectId);
                StatusTextBlock.Text = $"Added instrument: {dialog.SavedInstrument?.TagNumber}";
            }
        }

        private async void EditInstrument_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentsDataGrid.SelectedItem is not Instrument selectedInstrument)
            {
                MessageBox.Show("Please select an instrument first.", "No Instrument Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (InstrumentsProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Project not found.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dialog = new InstrumentDialog(unitOfWork, selectedProject, selectedInstrument);

            if (dialog.ShowDialog() == true)
            {
                await LoadInstrumentsForProject(selectedProject.ProjectId);
                StatusTextBlock.Text = $"Updated instrument: {selectedInstrument.TagNumber}";
            }
        }

        private async void DeleteInstrument_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentsDataGrid.SelectedItem is not Instrument selectedInstrument)
            {
                MessageBox.Show("Please select an instrument first.", "No Instrument Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the instrument '{selectedInstrument.TagNumber}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _unitOfWork.Instruments.DeleteAsync(selectedInstrument);
                    await _unitOfWork.SaveChangesAsync();

                    await LoadInstrumentsForProject(selectedInstrument.ProjectId);
                    StatusTextBlock.Text = $"Deleted instrument: {selectedInstrument.TagNumber}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting instrument: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportInstruments_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentsProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Instruments_{selectedProject.ProjectName}_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Export Instruments to Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var instruments = await _unitOfWork.Instruments.FindAsync(i => i.ProjectId == selectedProject.ProjectId);
                    var excelService = new ExcelExportService();
                    excelService.ExportInstruments(instruments, saveFileDialog.FileName, selectedProject.ProjectName);

                    StatusTextBlock.Text = $"Exported {instruments.Count()} instruments to Excel";
                    MessageBox.Show($"Instruments list exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting instruments: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportInstruments_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentsProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Import Instruments from Excel"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    StatusTextBlock.Text = "Importing instruments...";

                    // Get existing instrument tags
                    var existingInstruments = await _unitOfWork.Instruments.FindAsync(i => i.ProjectId == selectedProject.ProjectId);
                    var existingInstrumentTags = existingInstruments.Select(i => i.TagNumber).ToList();

                    // Get equipment and line mappings
                    var allEquipment = await _unitOfWork.Equipment.FindAsync(e => e.ProjectId == selectedProject.ProjectId);
                    var equipmentTagMap = allEquipment.ToDictionary(e => e.TagNumber, e => e.EquipmentId);

                    var allLines = await _unitOfWork.Lines.FindAsync(l => l.ProjectId == selectedProject.ProjectId);
                    var lineNumberMap = allLines.ToDictionary(l => l.LineNumber, l => l.LineId);

                    // Import from Excel
                    var importService = new ExcelImportService();
                    var result = importService.ImportInstruments(openFileDialog.FileName, selectedProject.ProjectId,
                        existingInstrumentTags, equipmentTagMap, lineNumberMap);

                    // Show results dialog
                    ShowImportResultDialog("Instruments Import", result);

                    // Refresh the grid
                    await LoadInstrumentsForProject(selectedProject.ProjectId);

                    StatusTextBlock.Text = $"Import complete: {result.SuccessCount} added, {result.SkippedCount} skipped, {result.ErrorCount} errors";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing instruments: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DownloadInstrumentsTemplate_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Instruments_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Download Instruments Template"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var importService = new ExcelImportService();
                    importService.GenerateInstrumentsTemplate(saveFileDialog.FileName);

                    MessageBox.Show($"Template downloaded successfully!\n\nFile: {saveFileDialog.FileName}\n\nFill in the instruments data and use 'Import from Excel' to load it.",
                        "Template Downloaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error downloading template: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RefreshInstruments_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentsProjectComboBox.SelectedItem is Project project)
            {
                await LoadInstrumentsForProject(project.ProjectId);
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
            if (_isInitializing) return;

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

        private async void ExportDrawingList_Click(object sender, RoutedEventArgs e)
        {
            if (DrawingsProjectComboBox.SelectedItem is not Project selectedProject)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Drawings_{selectedProject.ProjectName}_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Export Drawings List to Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var drawings = await _unitOfWork.Drawings.FindAsync(d => d.ProjectId == selectedProject.ProjectId);
                    var excelService = new ExcelExportService();
                    excelService.ExportDrawings(drawings, saveFileDialog.FileName, selectedProject.ProjectName);

                    StatusTextBlock.Text = $"Exported {drawings.Count()} drawings to Excel";
                    MessageBox.Show($"Drawings list exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting drawings: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

        // Import result dialog helper
        private void ShowImportResultDialog(string title, ExcelImportService.ImportResult result)
        {
            var message = $"Import Summary:\n\n" +
                         $"✓ Successfully imported: {result.SuccessCount}\n" +
                         $"⊘ Skipped (duplicates): {result.SkippedCount}\n" +
                         $"✗ Errors: {result.ErrorCount}\n";

            if (result.Warnings.Any())
            {
                message += $"\n⚠ Warnings: {result.Warnings.Count}\n";
                message += string.Join("\n", result.Warnings.Take(5));
                if (result.Warnings.Count > 5)
                    message += $"\n... and {result.Warnings.Count - 5} more warnings";
            }

            if (result.Errors.Any())
            {
                message += $"\n\nErrors:\n";
                message += string.Join("\n", result.Errors.Take(5));
                if (result.Errors.Count > 5)
                    message += $"\n... and {result.Errors.Count - 5} more errors";
            }

            if (result.SkippedItems.Any())
            {
                message += $"\n\nSkipped Items:\n";
                message += string.Join("\n", result.SkippedItems.Take(5));
                if (result.SkippedItems.Count > 5)
                    message += $"\n... and {result.SkippedItems.Count - 5} more";
            }

            var icon = result.ErrorCount > 0 ? MessageBoxImage.Warning :
                       result.SuccessCount > 0 ? MessageBoxImage.Information :
                       MessageBoxImage.Exclamation;

            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        #region Dashboard Methods

        private async void DashboardProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DashboardProjectComboBox.SelectedItem is Core.Entities.Project selectedProject)
            {
                DashboardProjectTaggingModeTextBlock.Text = $"Tagging Mode: {selectedProject.TaggingMode}";
                await LoadDashboardData(selectedProject);
            }
        }

        private async void RefreshDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (DashboardProjectComboBox.SelectedItem is Core.Entities.Project selectedProject)
            {
                await LoadDashboardData(selectedProject);
                MessageBox.Show("Dashboard refreshed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a project first.", "No Project Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadDashboardData(Core.Entities.Project project)
        {
            try
            {
                // Get all data for the project
                var equipment = (await _unitOfWork.Equipment.FindAsync(e => e.ProjectId == project.ProjectId && e.IsActive)).ToList();
                var lines = (await _unitOfWork.Lines.FindAsync(l => l.ProjectId == project.ProjectId && l.IsActive)).ToList();
                var instruments = (await _unitOfWork.Instruments.FindAsync(i => i.ProjectId == project.ProjectId && i.IsActive)).ToList();

                // Update statistics cards
                TotalEquipmentTextBlock.Text = equipment.Count.ToString();
                TotalEquipmentSubtitleTextBlock.Text = equipment.Count == 1 ? "item in project" : "items in project";

                // Calculate tagged equipment (equipment with non-empty tag numbers)
                int taggedCount = equipment.Count(e => !string.IsNullOrWhiteSpace(e.TagNumber));
                TaggedEquipmentTextBlock.Text = taggedCount.ToString();
                double taggedPercent = equipment.Count > 0 ? (double)taggedCount / equipment.Count * 100 : 0;
                TaggedEquipmentPercentTextBlock.Text = $"{taggedPercent:F1}% complete";

                TotalLinesTextBlock.Text = lines.Count.ToString();
                TotalLinesSubtitleTextBlock.Text = lines.Count == 1 ? "process line" : "process lines";

                TotalInstrumentsTextBlock.Text = instruments.Count.ToString();
                TotalInstrumentsSubtitleTextBlock.Text = instruments.Count == 1 ? "instrument" : "instruments";

                // Equipment by Type breakdown
                var equipmentByType = equipment
                    .GroupBy(e => string.IsNullOrEmpty(e.EquipmentType) ? "Unspecified" : e.EquipmentType)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        Percentage = equipment.Count > 0 ? $"{(double)g.Count() / equipment.Count * 100:F1}%" : "0%"
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                EquipmentByTypeListView.ItemsSource = equipmentByType;

                // Equipment by Status breakdown
                var equipmentByStatus = equipment
                    .GroupBy(e => e.Status.ToString())
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Percentage = equipment.Count > 0 ? $"{(double)g.Count() / equipment.Count * 100:F1}%" : "0%"
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                EquipmentByStatusListView.ItemsSource = equipmentByStatus;

                // Recent equipment (last 10)
                var recentEquipment = equipment
                    .OrderByDescending(e => e.CreatedDate)
                    .Take(10)
                    .ToList();

                RecentEquipmentListView.ItemsSource = recentEquipment;

                // Project information
                var projectInfo = $"Project: {project.ProjectName}\n" +
                                  $"Description: {project.Description}\n" +
                                  $"Tagging Mode: {project.TaggingMode}\n" +
                                  $"Created: {project.CreatedDate:yyyy-MM-dd}\n" +
                                  $"Modified: {project.ModifiedDate:yyyy-MM-dd}";

                DashboardProjectInfoTextBlock.Text = projectInfo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageDrawings_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Drawings tab
            MainTabControl.SelectedIndex = 4; // Assuming Drawings is the 5th tab (0-indexed)
        }

        private void RunValidation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Validation feature will be implemented in a future update.",
                "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Tag Renumbering Methods

        private void TagRenumberingWizard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProject == null)
            {
                MessageBox.Show("Please select a project first.", "No Project Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var dialog = new Views.TagRenumberingDialog(unitOfWork, _selectedProject);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                StatusTextBlock.Text = "Tag renumbering completed successfully";

                // Refresh equipment grid if on equipment tab
                if (MainTabControl.SelectedIndex == 1 && EquipmentProjectComboBox.SelectedItem is Project project)
                {
                    _ = LoadEquipmentForProject(project.ProjectId);
                }
            }
        }

        #endregion

        #region Audit Log Methods

        private bool _isLoadingAuditLogProjects = false;

        private async Task LoadAuditLogProjectsAsync()
        {
            if (_isLoadingAuditLogProjects)
                return;

            _isLoadingAuditLogProjects = true;
            try
            {
                var projects = await _unitOfWork.Projects.GetAllAsync();
                AuditLogProjectComboBox.ItemsSource = projects;

                // Select the last selected project
                if (_lastSelectedProject != null && projects.Any(p => p.ProjectId == _lastSelectedProject.ProjectId))
                {
                    AuditLogProjectComboBox.SelectedItem = projects.First(p => p.ProjectId == _lastSelectedProject.ProjectId);
                }
                else if (projects.Any())
                {
                    AuditLogProjectComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingAuditLogProjects = false;
            }
        }

        private async void AuditLogProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (AuditLogProjectComboBox.SelectedItem is Project project)
            {
                AuditLogProjectNameTextBlock.Text = $"Project: {project.ProjectName}";
                await LoadAuditLogsForProject(project.ProjectId);
            }
        }

        private async Task LoadAuditLogsForProject(Guid projectId)
        {
            try
            {
                // Get filter values
                string? entityTypeFilter = (EntityTypeFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string? actionFilter = (ActionFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string? timeRangeFilter = (TimeRangeFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                // Calculate date range
                DateTime startDate = DateTime.MinValue;
                if (timeRangeFilter == "24 Hours")
                    startDate = DateTime.UtcNow.AddDays(-1);
                else if (timeRangeFilter == "7 Days")
                    startDate = DateTime.UtcNow.AddDays(-7);
                else if (timeRangeFilter == "30 Days")
                    startDate = DateTime.UtcNow.AddDays(-30);

                // Get audit logs
                IEnumerable<AuditLog> auditLogs;
                if (timeRangeFilter == "All Time")
                {
                    auditLogs = await _unitOfWork.AuditLogs.FindAsync(a => a.ProjectId == projectId);
                }
                else
                {
                    auditLogs = await _unitOfWork.AuditLogs.FindAsync(a =>
                        a.ProjectId == projectId && a.Timestamp >= startDate);
                }

                // Apply entity type filter
                if (entityTypeFilter != "All" && !string.IsNullOrEmpty(entityTypeFilter))
                {
                    auditLogs = auditLogs.Where(a => a.EntityType == entityTypeFilter);
                }

                // Apply action filter
                if (actionFilter != "All" && !string.IsNullOrEmpty(actionFilter))
                {
                    auditLogs = auditLogs.Where(a => a.Action == actionFilter);
                }

                // Sort by timestamp descending (most recent first)
                var sortedLogs = auditLogs.OrderByDescending(a => a.Timestamp).ToList();

                AuditLogDataGrid.ItemsSource = sortedLogs;
                StatusTextBlock.Text = $"Loaded {sortedLogs.Count} audit log entries";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading audit logs: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading audit logs";
            }
        }

        private async void AuditLogFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || AuditLogProjectComboBox.SelectedItem is not Project project)
                return;

            await LoadAuditLogsForProject(project.ProjectId);
        }

        private async void RefreshAuditLog_Click(object sender, RoutedEventArgs e)
        {
            if (AuditLogProjectComboBox.SelectedItem is Project project)
            {
                await LoadAuditLogsForProject(project.ProjectId);
            }
        }

        #endregion
    }
}
