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

        public Line? SavedLine { get; private set; }

        public LineDialog(IUnitOfWork unitOfWork, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _project = project;

            // Display project info
            ProjectNameTextBlock.Text = project.ProjectName;

            // Load equipment list for connectivity dropdown
            LoadEquipmentListAsync();
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
                // Create line
                var line = new Line
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
                    DesignTemperature = ParseDecimal(DesignTemperatureTextBox.Text),
                    FromEquipmentId = (Guid?)FromEquipmentComboBox.SelectedValue,
                    ToEquipmentId = (Guid?)ToEquipmentComboBox.SelectedValue,
                    InsulationRequired = InsulationRequiredCheckBox.IsChecked ?? false,
                    InsulationType = InsulationTypeTextBox.Text,
                    InsulationThickness = ParseDecimal(InsulationThicknessTextBox.Text),
                    Length = ParseDecimal(LengthTextBox.Text)
                };

                // Save to database
                await _unitOfWork.Lines.AddAsync(line);
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
