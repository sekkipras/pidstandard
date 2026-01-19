using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace PIDStandardization.UI.Views
{
    /// <summary>
    /// Hierarchical view dialog for browsing equipment relationships
    /// </summary>
    public partial class HierarchicalViewDialog : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Project _project;
        private List<Equipment> _allEquipment;
        private List<Line> _allLines;
        private List<Instrument> _allInstruments;
        private List<Drawing> _allDrawings;

        public HierarchicalViewDialog(IUnitOfWork unitOfWork, Project project)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _project = project;
            _allEquipment = new List<Equipment>();
            _allLines = new List<Line>();
            _allInstruments = new List<Instrument>();
            _allDrawings = new List<Drawing>();

            Loaded += HierarchicalViewDialog_Loaded;
        }

        private async void HierarchicalViewDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBlock.Text = "Loading data...";

                // Load all data for the project
                _allEquipment = (await _unitOfWork.Equipment.FindAsync(eq => eq.ProjectId == _project.ProjectId && eq.IsActive))
                    .OrderBy(eq => eq.TagNumber)
                    .ToList();

                _allLines = (await _unitOfWork.Lines.FindAsync(l => l.ProjectId == _project.ProjectId))
                    .OrderBy(l => l.LineNumber)
                    .ToList();

                _allInstruments = (await _unitOfWork.Instruments.FindAsync(i => i.ProjectId == _project.ProjectId))
                    .OrderBy(i => i.TagNumber)
                    .ToList();

                _allDrawings = (await _unitOfWork.Drawings.FindAsync(d => d.ProjectId == _project.ProjectId))
                    .OrderBy(d => d.DrawingNumber)
                    .ToList();

                // Build initial tree view
                BuildTreeView();

                StatusTextBlock.Text = $"Loaded {_allEquipment.Count} equipment, {_allLines.Count} lines, {_allInstruments.Count} instruments";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error loading data";
            }
        }

        private void BuildTreeView()
        {
            HierarchyTreeView.Items.Clear();

            if (ByAreaRadio.IsChecked == true)
            {
                BuildAreaHierarchy();
            }
            else if (ByTypeRadio.IsChecked == true)
            {
                BuildTypeHierarchy();
            }
            else if (ByConnectionRadio.IsChecked == true)
            {
                BuildConnectionHierarchy();
            }
            else if (ByDrawingRadio.IsChecked == true)
            {
                BuildDrawingHierarchy();
            }
        }

        private void BuildAreaHierarchy()
        {
            var areas = _allEquipment
                .GroupBy(eq => string.IsNullOrEmpty(eq.Area) ? "Unassigned" : eq.Area)
                .OrderBy(g => g.Key);

            foreach (var area in areas)
            {
                var areaNode = new HierarchyNode
                {
                    DisplayName = area.Key,
                    Icon = "📍",
                    Count = $"({area.Count()} items)",
                    FontWeight = "Bold"
                };

                var typeGroups = area.GroupBy(eq => string.IsNullOrEmpty(eq.EquipmentType) ? "Unknown" : eq.EquipmentType);
                foreach (var typeGroup in typeGroups.OrderBy(g => g.Key))
                {
                    var typeNode = new HierarchyNode
                    {
                        DisplayName = typeGroup.Key,
                        Icon = "⚙",
                        Count = $"({typeGroup.Count()})",
                        FontWeight = "SemiBold"
                    };

                    foreach (var equipment in typeGroup.OrderBy(eq => eq.TagNumber))
                    {
                        typeNode.Children.Add(new HierarchyNode
                        {
                            DisplayName = equipment.TagNumber,
                            Icon = "•",
                            Tag = equipment,
                            FontWeight = "Normal"
                        });
                    }

                    areaNode.Children.Add(typeNode);
                }

                HierarchyTreeView.Items.Add(areaNode);
            }
        }

        private void BuildTypeHierarchy()
        {
            var types = _allEquipment
                .GroupBy(eq => string.IsNullOrEmpty(eq.EquipmentType) ? "Unknown" : eq.EquipmentType)
                .OrderBy(g => g.Key);

            foreach (var type in types)
            {
                var typeNode = new HierarchyNode
                {
                    DisplayName = type.Key,
                    Icon = "⚙",
                    Count = $"({type.Count()} items)",
                    FontWeight = "Bold"
                };

                foreach (var equipment in type.OrderBy(eq => eq.TagNumber))
                {
                    typeNode.Children.Add(new HierarchyNode
                    {
                        DisplayName = equipment.TagNumber,
                        Icon = "•",
                        Tag = equipment,
                        FontWeight = "Normal"
                    });
                }

                HierarchyTreeView.Items.Add(typeNode);
            }
        }

        private void BuildConnectionHierarchy()
        {
            // Find root equipment (no upstream connections)
            var equipmentWithUpstream = _allEquipment
                .Where(eq => eq.UpstreamEquipmentId.HasValue)
                .Select(eq => eq.EquipmentId)
                .ToHashSet();

            var rootEquipment = _allEquipment
                .Where(eq => !equipmentWithUpstream.Contains(eq.EquipmentId))
                .OrderBy(eq => eq.TagNumber);

            var rootNode = new HierarchyNode
            {
                DisplayName = "Process Flow",
                Icon = "🔄",
                Count = $"({rootEquipment.Count()} root items)",
                FontWeight = "Bold"
            };

            foreach (var equipment in rootEquipment)
            {
                rootNode.Children.Add(BuildConnectionNode(equipment, new HashSet<Guid>()));
            }

            HierarchyTreeView.Items.Add(rootNode);
        }

        private HierarchyNode BuildConnectionNode(Equipment equipment, HashSet<Guid> visited)
        {
            if (visited.Contains(equipment.EquipmentId))
            {
                return new HierarchyNode
                {
                    DisplayName = $"{equipment.TagNumber} (circular reference)",
                    Icon = "⚠",
                    Tag = equipment,
                    FontWeight = "Normal"
                };
            }

            visited.Add(equipment.EquipmentId);

            var node = new HierarchyNode
            {
                DisplayName = equipment.TagNumber,
                Icon = GetEquipmentIcon(equipment.EquipmentType),
                Tag = equipment,
                FontWeight = "Normal"
            };

            // Add downstream connections
            var downstream = _allEquipment.Where(eq => eq.UpstreamEquipmentId == equipment.EquipmentId);
            foreach (var downstreamEq in downstream.OrderBy(eq => eq.TagNumber))
            {
                node.Children.Add(BuildConnectionNode(downstreamEq, new HashSet<Guid>(visited)));
            }

            return node;
        }

        private void BuildDrawingHierarchy()
        {
            var drawingsWithEquipment = _allDrawings
                .Where(d => _allEquipment.Any(eq => eq.DrawingId == d.DrawingId))
                .OrderBy(d => d.DrawingNumber);

            foreach (var drawing in drawingsWithEquipment)
            {
                var drawingNode = new HierarchyNode
                {
                    DisplayName = drawing.DrawingNumber,
                    Icon = "📄",
                    Count = $"({_allEquipment.Count(eq => eq.DrawingId == drawing.DrawingId)} items)",
                    FontWeight = "Bold"
                };

                var equipment = _allEquipment.Where(eq => eq.DrawingId == drawing.DrawingId).OrderBy(eq => eq.TagNumber);
                foreach (var eq in equipment)
                {
                    drawingNode.Children.Add(new HierarchyNode
                    {
                        DisplayName = eq.TagNumber,
                        Icon = GetEquipmentIcon(eq.EquipmentType),
                        Tag = eq,
                        FontWeight = "Normal"
                    });
                }

                HierarchyTreeView.Items.Add(drawingNode);
            }

            // Add unassigned equipment
            var unassignedEquipment = _allEquipment.Where(eq => !eq.DrawingId.HasValue).OrderBy(eq => eq.TagNumber);
            if (unassignedEquipment.Any())
            {
                var unassignedNode = new HierarchyNode
                {
                    DisplayName = "Unassigned",
                    Icon = "❓",
                    Count = $"({unassignedEquipment.Count()} items)",
                    FontWeight = "Bold"
                };

                foreach (var eq in unassignedEquipment)
                {
                    unassignedNode.Children.Add(new HierarchyNode
                    {
                        DisplayName = eq.TagNumber,
                        Icon = GetEquipmentIcon(eq.EquipmentType),
                        Tag = eq,
                        FontWeight = "Normal"
                    });
                }

                HierarchyTreeView.Items.Add(unassignedNode);
            }
        }

        private string GetEquipmentIcon(string? equipmentType)
        {
            if (string.IsNullOrEmpty(equipmentType))
                return "•";

            return equipmentType.ToUpper() switch
            {
                var t when t.Contains("PUMP") => "🔧",
                var t when t.Contains("TANK") => "🛢",
                var t when t.Contains("VESSEL") => "⚗",
                var t when t.Contains("VALVE") => "🔩",
                var t when t.Contains("HEAT") || t.Contains("HX") => "🔥",
                var t when t.Contains("FILTER") => "🔬",
                var t when t.Contains("COMPRESSOR") => "💨",
                var t when t.Contains("SEPARATOR") => "⚡",
                _ => "⚙"
            };
        }

        private void HierarchyTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is HierarchyNode node && node.Tag is Equipment equipment)
            {
                ShowEquipmentDetails(equipment);
            }
            else
            {
                ClearDetails();
            }
        }

        private async void ShowEquipmentDetails(Equipment equipment)
        {
            try
            {
                // Basic details
                DetailTagNumberText.Text = equipment.TagNumber;
                DetailEquipmentTypeText.Text = equipment.EquipmentType ?? "-";
                DetailDescriptionText.Text = equipment.Description ?? "-";
                DetailServiceText.Text = equipment.Service ?? "-";
                DetailAreaText.Text = equipment.Area ?? "-";
                DetailStatusText.Text = equipment.Status.ToString();
                DetailManufacturerText.Text = equipment.Manufacturer ?? "-";
                DetailModelText.Text = equipment.Model ?? "-";

                // Drawing
                if (equipment.DrawingId.HasValue)
                {
                    var drawing = _allDrawings.FirstOrDefault(d => d.DrawingId == equipment.DrawingId);
                    DetailDrawingText.Text = drawing?.DrawingNumber ?? "-";
                }
                else
                {
                    DetailDrawingText.Text = "-";
                }

                // Process parameters
                DetailOpPressureText.Text = equipment.OperatingPressure.HasValue
                    ? $"{equipment.OperatingPressure} {equipment.OperatingPressureUnit}"
                    : "-";

                DetailDesPressureText.Text = equipment.DesignPressure.HasValue
                    ? $"{equipment.DesignPressure} {equipment.DesignPressureUnit}"
                    : "-";

                DetailOpTempText.Text = equipment.OperatingTemperature.HasValue
                    ? $"{equipment.OperatingTemperature} {equipment.OperatingTemperatureUnit}"
                    : "-";

                DetailDesTempText.Text = equipment.DesignTemperature.HasValue
                    ? $"{equipment.DesignTemperature} {equipment.DesignTemperatureUnit}"
                    : "-";

                DetailFlowRateText.Text = equipment.FlowRate.HasValue
                    ? $"{equipment.FlowRate} {equipment.FlowRateUnit}"
                    : "-";

                DetailPowerText.Text = equipment.PowerOrCapacity.HasValue
                    ? $"{equipment.PowerOrCapacity} {equipment.PowerOrCapacityUnit}"
                    : "-";

                // Connected lines
                var connectedLines = _allLines
                    .Where(l => l.FromEquipmentId == equipment.EquipmentId || l.ToEquipmentId == equipment.EquipmentId)
                    .Select(l => new
                    {
                        l.LineNumber,
                        l.Service,
                        l.NominalSize,
                        Direction = l.FromEquipmentId == equipment.EquipmentId ? "Outgoing" : "Incoming"
                    })
                    .ToList();

                ConnectedLinesDataGrid.ItemsSource = connectedLines;

                // Connected equipment
                var connectedEquipment = new List<object>();

                if (equipment.UpstreamEquipmentId.HasValue)
                {
                    var upstream = _allEquipment.FirstOrDefault(eq => eq.EquipmentId == equipment.UpstreamEquipmentId);
                    if (upstream != null)
                    {
                        connectedEquipment.Add(new
                        {
                            upstream.TagNumber,
                            upstream.EquipmentType,
                            upstream.Description,
                            Relationship = "Upstream"
                        });
                    }
                }

                if (equipment.DownstreamEquipmentId.HasValue)
                {
                    var downstream = _allEquipment.FirstOrDefault(eq => eq.EquipmentId == equipment.DownstreamEquipmentId);
                    if (downstream != null)
                    {
                        connectedEquipment.Add(new
                        {
                            downstream.TagNumber,
                            downstream.EquipmentType,
                            downstream.Description,
                            Relationship = "Downstream"
                        });
                    }
                }

                ConnectedEquipmentDataGrid.ItemsSource = connectedEquipment;

                // Instruments
                var instruments = _allInstruments.Where(i => i.ParentEquipmentId == equipment.EquipmentId).ToList();
                InstrumentsDataGrid.ItemsSource = instruments;

                StatusTextBlock.Text = $"Viewing: {equipment.TagNumber}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDetails()
        {
            DetailTagNumberText.Text = "-";
            DetailEquipmentTypeText.Text = "-";
            DetailDescriptionText.Text = "-";
            DetailServiceText.Text = "-";
            DetailAreaText.Text = "-";
            DetailStatusText.Text = "-";
            DetailManufacturerText.Text = "-";
            DetailModelText.Text = "-";
            DetailDrawingText.Text = "-";
            DetailOpPressureText.Text = "-";
            DetailDesPressureText.Text = "-";
            DetailOpTempText.Text = "-";
            DetailDesTempText.Text = "-";
            DetailFlowRateText.Text = "-";
            DetailPowerText.Text = "-";

            ConnectedLinesDataGrid.ItemsSource = null;
            ConnectedEquipmentDataGrid.ItemsSource = null;
            InstrumentsDataGrid.ItemsSource = null;

            StatusTextBlock.Text = "Ready";
        }

        private void ViewOption_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                BuildTreeView();
            }
        }

        private void SearchTextBox_Changed(object sender, TextChangedEventArgs e)
        {
            // Implement search filtering if needed
            // For now, just rebuild the tree
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            BuildTreeView();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Hierarchy tree node for display
    /// </summary>
    public class HierarchyNode
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Count { get; set; } = string.Empty;
        public string FontWeight { get; set; } = "Normal";
        public object? Tag { get; set; }
        public ObservableCollection<HierarchyNode> Children { get; set; } = new ObservableCollection<HierarchyNode>();
    }
}
