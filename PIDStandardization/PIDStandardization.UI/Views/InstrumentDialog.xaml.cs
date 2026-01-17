using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using System.Windows;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Interaction logic for InstrumentDialog.xaml
    /// </summary>
    public partial class InstrumentDialog : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Project _project;

        public Instrument? SavedInstrument { get; private set; }

        public InstrumentDialog(IUnitOfWork unitOfWork, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _project = project;

            // Display project info
            ProjectNameTextBlock.Text = project.ProjectName;

            // Load equipment and lines list for association dropdowns
            LoadEquipmentAndLinesAsync();
        }

        private async void LoadEquipmentAndLinesAsync()
        {
            // Load equipment
            var allEquipment = await _unitOfWork.Equipment
                .FindAsync(e => e.ProjectId == _project.ProjectId && e.IsActive);
            ParentEquipmentComboBox.ItemsSource = allEquipment;

            // Load lines
            var allLines = await _unitOfWork.Lines
                .FindAsync(l => l.ProjectId == _project.ProjectId);
            LineComboBox.ItemsSource = allLines;
        }

        private void AssociationRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Enable/disable combo boxes based on radio button selection
            if (AssociateWithEquipmentRadio.IsChecked == true)
            {
                ParentEquipmentComboBox.IsEnabled = true;
                LineComboBox.IsEnabled = false;
                LineComboBox.SelectedIndex = -1; // Clear line selection
            }
            else if (AssociateWithLineRadio.IsChecked == true)
            {
                ParentEquipmentComboBox.IsEnabled = false;
                ParentEquipmentComboBox.SelectedIndex = -1; // Clear equipment selection
                LineComboBox.IsEnabled = true;
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

            if (string.IsNullOrWhiteSpace(InstrumentTypeComboBox.Text))
            {
                MessageBox.Show("Please select an instrument type.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InstrumentTypeComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(MeasurementTypeComboBox.Text))
            {
                MessageBox.Show("Please select a measurement type.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MeasurementTypeComboBox.Focus();
                return;
            }

            // Validate association - must have either equipment or line selected
            if (AssociateWithEquipmentRadio.IsChecked == true && ParentEquipmentComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select an equipment to associate with this instrument.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ParentEquipmentComboBox.Focus();
                return;
            }

            if (AssociateWithLineRadio.IsChecked == true && LineComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a line to associate with this instrument.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LineComboBox.Focus();
                return;
            }

            try
            {
                // Create instrument
                var instrument = new Instrument
                {
                    InstrumentId = Guid.NewGuid(),
                    ProjectId = _project.ProjectId,
                    TagNumber = TagNumberTextBox.Text.Trim(),
                    InstrumentType = InstrumentTypeComboBox.Text,
                    MeasurementType = MeasurementTypeComboBox.Text,
                    RangeMin = ParseDecimal(RangeMinTextBox.Text),
                    RangeMax = ParseDecimal(RangeMaxTextBox.Text),
                    Units = UnitsComboBox.Text,
                    Accuracy = AccuracyTextBox.Text,
                    ProcessConnection = ProcessConnectionComboBox.Text,
                    OutputSignal = OutputSignalComboBox.Text,
                    LoopNumber = LoopNumberTextBox.Text,
                    Location = LocationTextBox.Text,

                    // Set either ParentEquipmentId OR LineId based on radio button selection
                    ParentEquipmentId = AssociateWithEquipmentRadio.IsChecked == true
                        ? (Guid?)ParentEquipmentComboBox.SelectedValue
                        : null,
                    LineId = AssociateWithLineRadio.IsChecked == true
                        ? (Guid?)LineComboBox.SelectedValue
                        : null
                };

                // Save to database
                await _unitOfWork.Instruments.AddAsync(instrument);
                await _unitOfWork.SaveChangesAsync();

                SavedInstrument = instrument;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving instrument: {ex.Message}", "Error",
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
