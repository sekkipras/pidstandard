using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using System.Windows;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Interaction logic for LineDialog.xaml
    /// </summary>
    public partial class LineDialog : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Project _project;
        private readonly Line? _existingLine;
        private readonly bool _isEditMode;

        public Line? SavedLine { get; private set; }

        // Constructor for Add mode
        public LineDialog(IUnitOfWork unitOfWork, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _project = project;
            _isEditMode = false;

            // Display project info
            ProjectNameTextBlock.Text = project.ProjectName;

            // Load equipment list for connectivity dropdown
            LoadEquipmentListAsync();
        }

        // Constructor for Edit mode
        public LineDialog(IUnitOfWork unitOfWork, Project project, Line existingLine)
            : this(unitOfWork, project)
        {
            _existingLine = existingLine;
            _isEditMode = true;

            Title = "Edit Line";

            // Load existing line data
            LoadLineData(existingLine);
        }

        private void LoadLineData(Line line)
        {
            LineNumberTextBox.Text = line.LineNumber;
            ServiceTextBox.Text = line.Service;
            FluidTypeComboBox.Text = line.FluidType;
            NominalSizeComboBox.Text = line.NominalSize;
            MaterialSpecTextBox.Text = line.MaterialSpec;
            PipeScheduleComboBox.Text = line.PipeSchedule;
            DesignPressureTextBox.Text = line.DesignPressure?.ToString();
            DesignPressureUnitComboBox.Text = line.DesignPressureUnit ?? "bar";
            DesignTemperatureTextBox.Text = line.DesignTemperature?.ToString();
            DesignTemperatureUnitComboBox.Text = line.DesignTemperatureUnit ?? "°C";

            if (line.FromEquipmentId.HasValue)
                FromEquipmentComboBox.SelectedValue = line.FromEquipmentId.Value;
            if (line.ToEquipmentId.HasValue)
                ToEquipmentComboBox.SelectedValue = line.ToEquipmentId.Value;

            InsulationRequiredCheckBox.IsChecked = line.InsulationRequired;
            InsulationTypeTextBox.Text = line.InsulationType;
            InsulationThicknessTextBox.Text = line.InsulationThickness?.ToString();
            LengthTextBox.Text = line.Length?.ToString();
        }

        private async void LoadEquipmentListAsync()
        {
            var allEquipment = await _unitOfWork.Equipment
                .FindAsync(e => e.ProjectId == _project.ProjectId && e.IsActive);

            FromEquipmentComboBox.ItemsSource = allEquipment;
            ToEquipmentComboBox.ItemsSource = allEquipment;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(LineNumberTextBox.Text))
            {
                MessageBox.Show("Please enter a line number.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LineNumberTextBox.Focus();
                return;
            }

            try
            {
                Line line;

                if (_isEditMode && _existingLine != null)
                {
                    // Update existing line
                    line = _existingLine;
                    line.LineNumber = LineNumberTextBox.Text.Trim();
                    line.Service = ServiceTextBox.Text;
                    line.FluidType = FluidTypeComboBox.Text;
                    line.NominalSize = NominalSizeComboBox.Text;
                    line.MaterialSpec = MaterialSpecTextBox.Text;
                    line.PipeSchedule = PipeScheduleComboBox.Text;
                    line.DesignPressure = ParseDecimal(DesignPressureTextBox.Text);
                    line.DesignPressureUnit = DesignPressureUnitComboBox.Text;
                    line.DesignTemperature = ParseDecimal(DesignTemperatureTextBox.Text);
                    line.DesignTemperatureUnit = DesignTemperatureUnitComboBox.Text;
                    line.FromEquipmentId = (Guid?)FromEquipmentComboBox.SelectedValue;
                    line.ToEquipmentId = (Guid?)ToEquipmentComboBox.SelectedValue;
                    line.InsulationRequired = InsulationRequiredCheckBox.IsChecked ?? false;
                    line.InsulationType = InsulationTypeTextBox.Text;
                    line.InsulationThickness = ParseDecimal(InsulationThicknessTextBox.Text);
                    line.Length = ParseDecimal(LengthTextBox.Text);

                    await _unitOfWork.Lines.UpdateAsync(line);
                }
                else
                {
                    // Create new line
                    line = new Line
                    {
                        LineId = Guid.NewGuid(),
                        ProjectId = _project.ProjectId,
                        LineNumber = LineNumberTextBox.Text.Trim(),
                        Service = ServiceTextBox.Text,
                        FluidType = FluidTypeComboBox.Text,
                        NominalSize = NominalSizeComboBox.Text,
                        MaterialSpec = MaterialSpecTextBox.Text,
                        PipeSchedule = PipeScheduleComboBox.Text,
                        DesignPressure = ParseDecimal(DesignPressureTextBox.Text),
                        DesignPressureUnit = DesignPressureUnitComboBox.Text,
                        DesignTemperature = ParseDecimal(DesignTemperatureTextBox.Text),
                        DesignTemperatureUnit = DesignTemperatureUnitComboBox.Text,
                        FromEquipmentId = (Guid?)FromEquipmentComboBox.SelectedValue,
                        ToEquipmentId = (Guid?)ToEquipmentComboBox.SelectedValue,
                        InsulationRequired = InsulationRequiredCheckBox.IsChecked ?? false,
                        InsulationType = InsulationTypeTextBox.Text,
                        InsulationThickness = ParseDecimal(InsulationThicknessTextBox.Text),
                        Length = ParseDecimal(LengthTextBox.Text)
                    };

                    await _unitOfWork.Lines.AddAsync(line);
                }

                // Save to database
                await _unitOfWork.SaveChangesAsync();

                SavedLine = line;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving line: {ex.Message}", "Error",
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
