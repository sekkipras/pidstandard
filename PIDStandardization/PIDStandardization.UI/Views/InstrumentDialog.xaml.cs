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
        private readonly Instrument? _existingInstrument;
        private readonly bool _isEditMode;

        public Instrument? SavedInstrument { get; private set; }

        // Constructor for Add mode
        public InstrumentDialog(IUnitOfWork unitOfWork, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _project = project;
            _isEditMode = false;

            // Display project info
            ProjectNameTextBlock.Text = project.ProjectName;

            // Load equipment and lines list for association dropdowns
            LoadEquipmentAndLinesAsync();
        }

        // Constructor for Edit mode
        public InstrumentDialog(IUnitOfWork unitOfWork, Project project, Instrument existingInstrument)
            : this(unitOfWork, project)
        {
            _existingInstrument = existingInstrument;
            _isEditMode = true;

            Title = "Edit Instrument";

            // Load existing instrument data
            LoadInstrumentData(existingInstrument);
        }

        private void LoadInstrumentData(Instrument instrument)
        {
            TagNumberTextBox.Text = instrument.TagNumber;
            InstrumentTypeComboBox.Text = instrument.InstrumentType;
            MeasurementTypeComboBox.Text = instrument.MeasurementType;
            RangeMinTextBox.Text = instrument.RangeMin?.ToString();
            RangeMaxTextBox.Text = instrument.RangeMax?.ToString();
            UnitsComboBox.Text = instrument.Units;
            AccuracyTextBox.Text = instrument.Accuracy;
            ProcessConnectionComboBox.Text = instrument.ProcessConnection;
            OutputSignalComboBox.Text = instrument.OutputSignal;
            LoopNumberTextBox.Text = instrument.LoopNumber;
            LocationTextBox.Text = instrument.Location;

            // Set association radio buttons and selections
            if (instrument.ParentEquipmentId.HasValue)
            {
                AssociateWithEquipmentRadio.IsChecked = true;
                ParentEquipmentComboBox.SelectedValue = instrument.ParentEquipmentId.Value;
            }
            else if (instrument.LineId.HasValue)
            {
                AssociateWithLineRadio.IsChecked = true;
                LineComboBox.SelectedValue = instrument.LineId.Value;
            }
        }

        private async void LoadEquipmentAndLinesAsync()
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading equipment and lines: {ex.Message}\n\nYou can still add the instrument, but association dropdowns will be empty.",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
                Instrument instrument;

                if (_isEditMode && _existingInstrument != null)
                {
                    // Update existing instrument
                    instrument = _existingInstrument;
                    instrument.TagNumber = TagNumberTextBox.Text.Trim();
                    instrument.InstrumentType = InstrumentTypeComboBox.Text;
                    instrument.MeasurementType = MeasurementTypeComboBox.Text;
                    instrument.RangeMin = ParseDecimal(RangeMinTextBox.Text);
                    instrument.RangeMax = ParseDecimal(RangeMaxTextBox.Text);
                    instrument.Units = UnitsComboBox.Text;
                    instrument.Accuracy = AccuracyTextBox.Text;
                    instrument.ProcessConnection = ProcessConnectionComboBox.Text;
                    instrument.OutputSignal = OutputSignalComboBox.Text;
                    instrument.LoopNumber = LoopNumberTextBox.Text;
                    instrument.Location = LocationTextBox.Text;

                    // Set either ParentEquipmentId OR LineId based on radio button selection
                    instrument.ParentEquipmentId = AssociateWithEquipmentRadio.IsChecked == true
                        ? (Guid?)ParentEquipmentComboBox.SelectedValue
                        : null;
                    instrument.LineId = AssociateWithLineRadio.IsChecked == true
                        ? (Guid?)LineComboBox.SelectedValue
                        : null;

                    await _unitOfWork.Instruments.UpdateAsync(instrument);
                }
                else
                {
                    // Create new instrument
                    instrument = new Instrument
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

                    await _unitOfWork.Instruments.AddAsync(instrument);
                }

                // Save to database
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
