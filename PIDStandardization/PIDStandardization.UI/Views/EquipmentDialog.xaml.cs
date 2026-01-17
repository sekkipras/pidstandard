using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Enums;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Services.TaggingServices;
using System.Windows;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Interaction logic for EquipmentDialog.xaml
    /// </summary>
    public partial class EquipmentDialog : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITagValidationService _tagValidationService;
        private readonly Project _project;

        public Equipment? SavedEquipment { get; private set; }

        public EquipmentDialog(IUnitOfWork unitOfWork, ITagValidationService tagValidationService, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _tagValidationService = tagValidationService;
            _project = project;

            // Display project info
            ProjectNameTextBlock.Text = project.ProjectName;
            TaggingModeTextBlock.Text = project.TaggingMode.ToString();

            // Update title based on tagging mode
            Title = $"Add Equipment - {project.TaggingMode} Tagging Mode";
        }

        private async void ValidateTag_Click(object sender, RoutedEventArgs e)
        {
            var tagNumber = TagNumberTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(tagNumber))
            {
                MessageBox.Show("Please enter a tag number to validate.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = await _tagValidationService.ValidateTagAsync(_project.ProjectId, tagNumber);

                if (result.IsValid)
                {
                    var message = "Tag number is valid!";
                    if (result.Warnings.Any())
                    {
                        message += "\n\nWarnings:\n" + string.Join("\n", result.Warnings);
                    }
                    MessageBox.Show(message, "Validation Passed",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var message = "Tag number validation failed:\n\n" + string.Join("\n", result.Errors);
                    if (result.Warnings.Any())
                    {
                        message += "\n\nWarnings:\n" + string.Join("\n", result.Warnings);
                    }
                    MessageBox.Show(message, "Validation Failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating tag: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AutoGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var equipmentType = EquipmentTypeComboBox.Text;

                if (string.IsNullOrWhiteSpace(equipmentType))
                {
                    MessageBox.Show("Please select an equipment type first.", "Auto Generate",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EquipmentTypeComboBox.Focus();
                    return;
                }

                var generatedTag = await _tagValidationService.GenerateNextTagAsync(_project.ProjectId, equipmentType);
                TagNumberTextBox.Text = generatedTag;

                MessageBox.Show($"Generated tag number: {generatedTag}", "Auto Generate",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating tag: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TagNumberTextBox.Text))
            {
                MessageBox.Show("Please enter a tag number.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TagNumberTextBox.Focus();
                return;
            }

            try
            {
                // Validate tag format
                var validationResult = await _tagValidationService.ValidateTagAsync(_project.ProjectId, TagNumberTextBox.Text.Trim());

                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join("\n", validationResult.Errors);
                    var result = MessageBox.Show(
                        $"Tag validation failed:\n\n{errorMessage}\n\nDo you want to save anyway?",
                        "Validation Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;
                }

                // Parse status
                var status = StatusComboBox.SelectedIndex switch
                {
                    0 => EquipmentStatus.Planned,
                    1 => EquipmentStatus.Installed,
                    2 => EquipmentStatus.Commissioned,
                    3 => EquipmentStatus.Decommissioned,
                    _ => EquipmentStatus.Planned
                };

                // Create equipment
                var equipment = new Equipment
                {
                    EquipmentId = Guid.NewGuid(),
                    ProjectId = _project.ProjectId,
                    TagNumber = TagNumberTextBox.Text.Trim(),
                    EquipmentType = EquipmentTypeComboBox.Text,
                    Description = DescriptionTextBox.Text,
                    Service = ServiceTextBox.Text,
                    Area = AreaTextBox.Text,
                    Status = status,
                    Manufacturer = ManufacturerTextBox.Text,
                    Model = ModelTextBox.Text,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                // Save to database
                await _unitOfWork.Equipment.AddAsync(equipment);
                await _unitOfWork.SaveChangesAsync();

                SavedEquipment = equipment;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving equipment: {ex.Message}", "Error",
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
