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
        private readonly Equipment? _existingEquipment;
        private readonly bool _isEditMode;

        public Equipment? SavedEquipment { get; private set; }

        // Constructor for Add mode
        public EquipmentDialog(IUnitOfWork unitOfWork, ITagValidationService tagValidationService, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _tagValidationService = tagValidationService;
            _project = project;
            _isEditMode = false;

            // Display project info
            ProjectNameTextBlock.Text = project.ProjectName;
            TaggingModeTextBlock.Text = project.TaggingMode.ToString();

            // Update title based on tagging mode
            Title = $"Add Equipment - {project.TaggingMode} Tagging Mode";

            // Set default status for new equipment
            StatusComboBox.SelectedIndex = 0; // Planned

            // Load equipment list for connectivity dropdown
            Loaded += async (s, e) => await LoadEquipmentListAsync();
        }

        // Constructor for Edit mode
        public EquipmentDialog(IUnitOfWork unitOfWork, ITagValidationService tagValidationService, Project project, Equipment existingEquipment)
            : this(unitOfWork, tagValidationService, project)
        {
            _existingEquipment = existingEquipment;
            _isEditMode = true;

            Title = $"Edit Equipment - {project.TaggingMode} Tagging Mode";

            // Load existing equipment data into form
            LoadEquipmentData(existingEquipment);
        }

        private void LoadEquipmentData(Equipment equipment)
        {
            TagNumberTextBox.Text = equipment.TagNumber;
            EquipmentTypeComboBox.Text = equipment.EquipmentType;
            DescriptionTextBox.Text = equipment.Description;
            ServiceTextBox.Text = equipment.Service;
            AreaTextBox.Text = equipment.Area;

            // Set status
            StatusComboBox.SelectedIndex = equipment.Status switch
            {
                EquipmentStatus.Planned => 0,
                EquipmentStatus.Installed => 1,
                EquipmentStatus.Commissioned => 2,
                EquipmentStatus.Decommissioned => 3,
                _ => 0
            };

            ManufacturerTextBox.Text = equipment.Manufacturer;
            ModelTextBox.Text = equipment.Model;

            // Process parameters
            OperatingPressureTextBox.Text = equipment.OperatingPressure?.ToString();
            OperatingPressureUnitComboBox.Text = equipment.OperatingPressureUnit ?? "bar";
            OperatingTemperatureTextBox.Text = equipment.OperatingTemperature?.ToString();
            OperatingTemperatureUnitComboBox.Text = equipment.OperatingTemperatureUnit ?? "°C";
            FlowRateTextBox.Text = equipment.FlowRate?.ToString();
            FlowRateUnitComboBox.Text = equipment.FlowRateUnit ?? "m³/h";
            DesignPressureTextBox.Text = equipment.DesignPressure?.ToString();
            DesignPressureUnitComboBox.Text = equipment.DesignPressureUnit ?? "bar";
            DesignTemperatureTextBox.Text = equipment.DesignTemperature?.ToString();
            DesignTemperatureUnitComboBox.Text = equipment.DesignTemperatureUnit ?? "°C";
            PowerOrCapacityTextBox.Text = equipment.PowerOrCapacity?.ToString();
            PowerOrCapacityUnitComboBox.Text = equipment.PowerOrCapacityUnit ?? "kW";

            // Connectivity - will be set after equipment list loads
            if (equipment.UpstreamEquipmentId.HasValue)
            {
                UpstreamEquipmentComboBox.SelectedValue = equipment.UpstreamEquipmentId.Value;
            }
            if (equipment.DownstreamEquipmentId.HasValue)
            {
                DownstreamEquipmentComboBox.SelectedValue = equipment.DownstreamEquipmentId.Value;
            }

            // Drawing assignment
            if (equipment.DrawingId.HasValue && SourceDrawingComboBox != null)
            {
                SourceDrawingComboBox.SelectedValue = equipment.DrawingId.Value;
            }
        }

        private async Task LoadEquipmentListAsync()
        {
            var allEquipment = await _unitOfWork.Equipment
                .FindAsync(e => e.ProjectId == _project.ProjectId && e.IsActive);

            UpstreamEquipmentComboBox.ItemsSource = allEquipment;
            DownstreamEquipmentComboBox.ItemsSource = allEquipment;

            // Load drawings for this project
            var allDrawings = await _unitOfWork.Drawings
                .FindAsync(d => d.ProjectId == _project.ProjectId);

            SourceDrawingComboBox.ItemsSource = allDrawings;
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
                // Validate tag format (exclude current equipment ID in edit mode)
                var excludeId = _isEditMode && _existingEquipment != null ? _existingEquipment.EquipmentId : (Guid?)null;
                var validationResult = await _tagValidationService.ValidateTagAsync(_project.ProjectId, TagNumberTextBox.Text.Trim(), excludeId);

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

                Equipment equipment;

                if (_isEditMode && _existingEquipment != null)
                {
                    // Update existing equipment
                    equipment = _existingEquipment;
                    equipment.TagNumber = TagNumberTextBox.Text.Trim();
                    equipment.EquipmentType = EquipmentTypeComboBox.Text;
                    equipment.Description = DescriptionTextBox.Text;
                    equipment.Service = ServiceTextBox.Text;
                    equipment.Area = AreaTextBox.Text;
                    equipment.Status = status;
                    equipment.Manufacturer = ManufacturerTextBox.Text;
                    equipment.Model = ModelTextBox.Text;

                    // Connectivity
                    equipment.UpstreamEquipmentId = (Guid?)UpstreamEquipmentComboBox.SelectedValue;
                    equipment.DownstreamEquipmentId = (Guid?)DownstreamEquipmentComboBox.SelectedValue;

                    // Drawing assignment
                    equipment.DrawingId = (Guid?)SourceDrawingComboBox.SelectedValue;

                    // Process parameters
                    equipment.OperatingPressure = ParseDecimal(OperatingPressureTextBox.Text);
                    equipment.OperatingPressureUnit = OperatingPressureUnitComboBox.Text;
                    equipment.OperatingTemperature = ParseDecimal(OperatingTemperatureTextBox.Text);
                    equipment.OperatingTemperatureUnit = OperatingTemperatureUnitComboBox.Text;
                    equipment.FlowRate = ParseDecimal(FlowRateTextBox.Text);
                    equipment.FlowRateUnit = FlowRateUnitComboBox.Text;
                    equipment.DesignPressure = ParseDecimal(DesignPressureTextBox.Text);
                    equipment.DesignPressureUnit = DesignPressureUnitComboBox.Text;
                    equipment.DesignTemperature = ParseDecimal(DesignTemperatureTextBox.Text);
                    equipment.DesignTemperatureUnit = DesignTemperatureUnitComboBox.Text;
                    equipment.PowerOrCapacity = ParseDecimal(PowerOrCapacityTextBox.Text);
                    equipment.PowerOrCapacityUnit = PowerOrCapacityUnitComboBox.Text;

                    equipment.ModifiedDate = DateTime.UtcNow;

                    await _unitOfWork.Equipment.UpdateAsync(equipment);
                }
                else
                {
                    // Create new equipment
                    equipment = new Equipment
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

                        // Connectivity (optional fields)
                        UpstreamEquipmentId = (Guid?)UpstreamEquipmentComboBox.SelectedValue,
                        DownstreamEquipmentId = (Guid?)DownstreamEquipmentComboBox.SelectedValue,

                        // Drawing assignment (optional)
                        DrawingId = (Guid?)SourceDrawingComboBox.SelectedValue,

                        // Process parameters (optional fields)
                        OperatingPressure = ParseDecimal(OperatingPressureTextBox.Text),
                        OperatingPressureUnit = OperatingPressureUnitComboBox.Text,
                        OperatingTemperature = ParseDecimal(OperatingTemperatureTextBox.Text),
                        OperatingTemperatureUnit = OperatingTemperatureUnitComboBox.Text,
                        FlowRate = ParseDecimal(FlowRateTextBox.Text),
                        FlowRateUnit = FlowRateUnitComboBox.Text,
                        DesignPressure = ParseDecimal(DesignPressureTextBox.Text),
                        DesignPressureUnit = DesignPressureUnitComboBox.Text,
                        DesignTemperature = ParseDecimal(DesignTemperatureTextBox.Text),
                        DesignTemperatureUnit = DesignTemperatureUnitComboBox.Text,
                        PowerOrCapacity = ParseDecimal(PowerOrCapacityTextBox.Text),
                        PowerOrCapacityUnit = PowerOrCapacityUnitComboBox.Text,

                        CreatedDate = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _unitOfWork.Equipment.AddAsync(equipment);
                }

                // Save to database
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

        private decimal? ParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (decimal.TryParse(input, out decimal result))
                return result;

            return null;
        }
    }
}
