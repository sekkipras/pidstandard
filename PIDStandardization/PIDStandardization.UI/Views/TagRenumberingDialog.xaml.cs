using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Tag Renumbering Dialog for bulk equipment tag renumbering
    /// </summary>
    public partial class TagRenumberingDialog : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Project _project;
        private readonly AuditLogService _auditService;
        private List<Equipment> _allEquipment;
        private ObservableCollection<RenumberingItem> _renumberingItems;

        public TagRenumberingDialog(IUnitOfWork unitOfWork, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _project = project;
            _auditService = new AuditLogService(_unitOfWork);
            _allEquipment = new List<Equipment>();
            _renumberingItems = new ObservableCollection<RenumberingItem>();

            Loaded += TagRenumberingDialog_Loaded;
        }

        private async void TagRenumberingDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load all equipment for the project
                _allEquipment = (await _unitOfWork.Equipment.FindAsync(eq => eq.ProjectId == _project.ProjectId && eq.IsActive))
                    .OrderBy(eq => eq.TagNumber)
                    .ToList();

                // Populate filter dropdowns
                LoadFilters();

                // Initialize grid
                RefreshGrid();

                // Set default pattern based on tagging mode
                if (_project.TaggingMode == Core.Enums.TaggingMode.KKS)
                {
                    NewPatternTextBox.Text = "={AREA}-{TYPE}-{SEQ:000}";
                }
                else
                {
                    NewPatternTextBox.Text = "{TYPE}-{SEQ:001}";
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading equipment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFilters()
        {
            // Equipment types
            var types = _allEquipment
                .Select(eq => eq.EquipmentType)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            EquipmentTypeFilterComboBox.Items.Clear();
            EquipmentTypeFilterComboBox.Items.Add("All");
            foreach (var type in types)
            {
                EquipmentTypeFilterComboBox.Items.Add(type);
            }
            EquipmentTypeFilterComboBox.SelectedIndex = 0;

            // Areas
            var areas = _allEquipment
                .Select(eq => eq.Area)
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            AreaFilterComboBox.Items.Clear();
            AreaFilterComboBox.Items.Add("All");
            foreach (var area in areas)
            {
                AreaFilterComboBox.Items.Add(area);
            }
            AreaFilterComboBox.SelectedIndex = 0;
        }

        private void RefreshGrid()
        {
            var filteredEquipment = FilterEquipment();

            _renumberingItems.Clear();
            foreach (var eq in filteredEquipment)
            {
                _renumberingItems.Add(new RenumberingItem
                {
                    EquipmentId = eq.EquipmentId,
                    CurrentTag = eq.TagNumber,
                    EquipmentType = eq.EquipmentType ?? "Unknown",
                    Description = eq.Description,
                    Area = eq.Area,
                    IsSelected = false,
                    NewTag = string.Empty
                });
            }

            EquipmentDataGrid.ItemsSource = _renumberingItems;
            UpdateSummary();
        }

        private List<Equipment> FilterEquipment()
        {
            var filtered = _allEquipment.AsEnumerable();

            // Equipment type filter
            if (EquipmentTypeFilterComboBox.SelectedItem is string selectedType && selectedType != "All")
            {
                filtered = filtered.Where(eq => eq.EquipmentType == selectedType);
            }

            // Area filter
            if (AreaFilterComboBox.SelectedItem is string selectedArea && selectedArea != "All")
            {
                filtered = filtered.Where(eq => eq.Area == selectedArea);
            }

            // Current pattern filter
            if (!string.IsNullOrWhiteSpace(CurrentPatternTextBox.Text))
            {
                var pattern = CurrentPatternTextBox.Text.Trim();
                var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                filtered = filtered.Where(eq => regex.IsMatch(eq.TagNumber));
            }

            return filtered.ToList();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                RefreshGrid();
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            RefreshGrid();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _renumberingItems)
            {
                item.IsSelected = true;
            }
            UpdateSummary();
        }

        private void NewPattern_Changed(object sender, TextChangedEventArgs e)
        {
            UpdatePatternExample();
        }

        private void UpdatePatternExample()
        {
            try
            {
                var pattern = NewPatternTextBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(pattern))
                {
                    PatternExampleTextBlock.Text = string.Empty;
                    return;
                }

                var example = pattern
                    .Replace("{TYPE}", "PMP")
                    .Replace("{AREA}", "A01")
                    .Replace("{SEQ:000}", "001")
                    .Replace("{SEQ:0000}", "0001")
                    .Replace("{SEQ:001}", "001")
                    .Replace("{SEQ}", "1");

                PatternExampleTextBlock.Text = $"Example: {example}";
            }
            catch
            {
                PatternExampleTextBlock.Text = "Invalid pattern";
            }
        }

        private void PreviewChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pattern = NewPatternTextBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(pattern))
                {
                    MessageBox.Show("Please enter a renumbering pattern.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(StartNumberTextBox.Text, out int startNumber))
                {
                    MessageBox.Show("Please enter a valid start number.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(IncrementTextBox.Text, out int increment) || increment < 1)
                {
                    MessageBox.Show("Please enter a valid increment (minimum 1).", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Generate preview
                int currentNumber = startNumber;
                foreach (var item in _renumberingItems.Where(i => i.IsSelected))
                {
                    item.NewTag = GenerateNewTag(pattern, item, currentNumber);
                    currentNumber += increment;
                }

                StatusTextBlock.Text = "Preview Generated";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating preview: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateNewTag(string pattern, RenumberingItem item, int sequenceNumber)
        {
            var result = pattern
                .Replace("{TYPE}", GetTypeCode(item.EquipmentType))
                .Replace("{AREA}", item.Area ?? "00");

            // Handle sequence number formatting
            var seqMatch = Regex.Match(result, @"\{SEQ:([0#]+)\}");
            if (seqMatch.Success)
            {
                var format = seqMatch.Groups[1].Value;
                var formattedNumber = sequenceNumber.ToString(new string('0', format.Length));
                result = result.Replace(seqMatch.Value, formattedNumber);
            }
            else
            {
                result = result.Replace("{SEQ}", sequenceNumber.ToString());
            }

            return result;
        }

        private string GetTypeCode(string equipmentType)
        {
            // Common equipment type to code mappings
            var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Pump", "P" },
                { "Tank", "T" },
                { "Vessel", "V" },
                { "Heat Exchanger", "HX" },
                { "Valve", "VLV" },
                { "Filter", "F" },
                { "Compressor", "C" },
                { "Separator", "S" }
            };

            if (typeMap.TryGetValue(equipmentType, out var code))
            {
                return code;
            }

            // Default: use first 3 characters uppercase
            return equipmentType.Length >= 3 ? equipmentType.Substring(0, 3).ToUpper() : equipmentType.ToUpper();
        }

        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _renumberingItems.Where(i => i.IsSelected && !string.IsNullOrEmpty(i.NewTag)).ToList();

            if (!selectedItems.Any())
            {
                MessageBox.Show("No equipment selected for renumbering.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate for duplicates
            var newTags = selectedItems.Select(i => i.NewTag).ToList();
            var duplicates = newTags.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicates.Any())
            {
                MessageBox.Show($"Duplicate new tags detected: {string.Join(", ", duplicates)}\n\nPlease adjust the pattern or filters.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check for conflicts with existing tags
            var existingTags = _allEquipment.Select(eq => eq.TagNumber).ToList();
            var conflicts = newTags.Where(nt => existingTags.Contains(nt) &&
                !selectedItems.Select(si => si.CurrentTag).Contains(nt)).ToList();

            if (conflicts.Any())
            {
                var result = MessageBox.Show(
                    $"The following new tags already exist in the database:\n{string.Join(", ", conflicts)}\n\n" +
                    "Do you want to continue anyway? This may cause conflicts.",
                    "Warning: Tag Conflicts",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            // Confirm action
            var confirmResult = MessageBox.Show(
                $"You are about to renumber {selectedItems.Count} equipment tags.\n\n" +
                "This action will:\n" +
                "- Update tag numbers in the database\n" +
                "- Create audit log entries for each change\n" +
                "- Require synchronization with AutoCAD drawings\n\n" +
                "Do you want to proceed?",
                "Confirm Renumbering",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                StatusTextBlock.Text = "Applying changes...";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Orange;

                int successCount = 0;
                int errorCount = 0;
                var errors = new List<string>();

                await _unitOfWork.BeginTransactionAsync();

                foreach (var item in selectedItems)
                {
                    try
                    {
                        var equipment = await _unitOfWork.Equipment.GetByIdAsync(item.EquipmentId);
                        if (equipment != null)
                        {
                            var oldTag = equipment.TagNumber;
                            var oldEquipment = new Equipment
                            {
                                TagNumber = oldTag,
                                EquipmentType = equipment.EquipmentType,
                                Description = equipment.Description
                            };

                            equipment.TagNumber = item.NewTag;
                            equipment.ModifiedDate = DateTime.UtcNow;

                            await _unitOfWork.Equipment.UpdateAsync(equipment);

                            // Log the change
                            await _auditService.LogEquipmentUpdatedAsync(
                                oldEquipment,
                                equipment,
                                Environment.UserName,
                                $"Tag Renumbering Wizard: {Environment.MachineName}"
                            );

                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"{item.CurrentTag}: {ex.Message}");
                    }
                }

                await _unitOfWork.CommitTransactionAsync();
                await _unitOfWork.SaveChangesAsync();

                // Show results
                var message = $"Renumbering Complete!\n\n" +
                             $"Successfully renumbered: {successCount}\n" +
                             $"Errors: {errorCount}";

                if (errors.Any())
                {
                    message += $"\n\nErrors:\n{string.Join("\n", errors.Take(5))}";
                    if (errors.Count > 5)
                    {
                        message += $"\n... and {errors.Count - 5} more";
                    }
                }

                MessageBox.Show(message, "Renumbering Complete",
                    MessageBoxButton.OK,
                    errorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                if (errorCount == 0)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    StatusTextBlock.Text = $"Completed with {errorCount} errors";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                MessageBox.Show($"Error applying renumbering: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                StatusTextBlock.Text = "Error occurred";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateSummary()
        {
            TotalEquipmentTextBlock.Text = _renumberingItems.Count.ToString();
            SelectedCountTextBlock.Text = _renumberingItems.Count(i => i.IsSelected).ToString();
        }
    }

    /// <summary>
    /// Renumbering item for display in the grid
    /// </summary>
    public class RenumberingItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _newTag = string.Empty;

        public Guid EquipmentId { get; set; }
        public string CurrentTag { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Area { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public string NewTag
        {
            get => _newTag;
            set
            {
                _newTag = value;
                OnPropertyChanged(nameof(NewTag));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
