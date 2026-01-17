using PIDStandardization.Core.Entities;
using System.Windows.Forms;

namespace PIDStandardization.AutoCAD.Forms
{
    /// <summary>
    /// Form for assigning tag numbers to equipment blocks
    /// </summary>
    public partial class TagAssignmentForm : Form
    {
        private ComboBox tagComboBox = null!;
        private TextBox customTagTextBox = null!;
        private RadioButton existingTagRadio = null!;
        private RadioButton customTagRadio = null!;
        private RadioButton autoGenerateRadio = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;
        private Label blockInfoLabel = null!;
        private Label equipmentTypeLabel = null!;

        public string? SelectedTagNumber { get; private set; }
        public bool UseExistingEquipment { get; private set; }
        public Equipment? SelectedEquipment { get; private set; }

        private readonly IEnumerable<Equipment> _availableEquipment;
        private readonly string _blockName;
        private readonly string _suggestedTag;

        public TagAssignmentForm(IEnumerable<Equipment> availableEquipment, string blockName, string suggestedTag)
        {
            _availableEquipment = availableEquipment;
            _blockName = blockName;
            _suggestedTag = suggestedTag;

            InitializeComponent();
            LoadEquipmentList();
        }

        private void InitializeComponent()
        {
            this.Text = "Assign Tag Number";
            this.Size = new System.Drawing.Size(500, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Block info label
            blockInfoLabel = new Label
            {
                Text = $"Block: {_blockName}",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(450, 20),
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };
            this.Controls.Add(blockInfoLabel);

            // Equipment type label
            equipmentTypeLabel = new Label
            {
                Text = $"Suggested Equipment Type: {GetEquipmentType(_blockName)}",
                Location = new System.Drawing.Point(20, 45),
                Size = new System.Drawing.Size(450, 20)
            };
            this.Controls.Add(equipmentTypeLabel);

            // Option 1: Use existing equipment
            existingTagRadio = new RadioButton
            {
                Text = "Use existing equipment from database:",
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(450, 20),
                Checked = true
            };
            existingTagRadio.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(existingTagRadio);

            tagComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(40, 105),
                Size = new System.Drawing.Size(420, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(tagComboBox);

            // Option 2: Auto-generate tag
            autoGenerateRadio = new RadioButton
            {
                Text = $"Auto-generate tag number: {_suggestedTag}",
                Location = new System.Drawing.Point(20, 145),
                Size = new System.Drawing.Size(450, 20)
            };
            autoGenerateRadio.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(autoGenerateRadio);

            // Option 3: Enter custom tag
            customTagRadio = new RadioButton
            {
                Text = "Enter custom tag number:",
                Location = new System.Drawing.Point(20, 185),
                Size = new System.Drawing.Size(450, 20)
            };
            customTagRadio.CheckedChanged += RadioButton_CheckedChanged;
            this.Controls.Add(customTagRadio);

            customTagTextBox = new TextBox
            {
                Location = new System.Drawing.Point(40, 210),
                Size = new System.Drawing.Size(420, 25),
                Enabled = false
            };
            this.Controls.Add(customTagTextBox);

            // Info label
            Label infoLabel = new Label
            {
                Text = "The tag number will be written to the block's TAG attribute\nand saved to the database.",
                Location = new System.Drawing.Point(20, 260),
                Size = new System.Drawing.Size(450, 40),
                ForeColor = System.Drawing.Color.Gray
            };
            this.Controls.Add(infoLabel);

            // OK Button
            okButton = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(280, 320),
                Size = new System.Drawing.Size(90, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            // Cancel Button
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(380, 320),
                Size = new System.Drawing.Size(90, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadEquipmentList()
        {
            tagComboBox.Items.Clear();

            // Group by equipment type for better organization
            var equipmentType = GetEquipmentType(_blockName);
            var matchingEquipment = _availableEquipment
                .Where(e => e.EquipmentType?.Contains(equipmentType, StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(e => e.TagNumber)
                .ToList();

            var otherEquipment = _availableEquipment
                .Where(e => e.EquipmentType == null || !e.EquipmentType.Contains(equipmentType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.TagNumber)
                .ToList();

            if (matchingEquipment.Any())
            {
                tagComboBox.Items.Add("--- Matching Equipment Type ---");
                foreach (var eq in matchingEquipment)
                {
                    tagComboBox.Items.Add($"{eq.TagNumber} - {eq.Description ?? eq.EquipmentType}");
                }
            }

            if (otherEquipment.Any())
            {
                if (matchingEquipment.Any())
                    tagComboBox.Items.Add("--- Other Equipment ---");

                foreach (var eq in otherEquipment)
                {
                    tagComboBox.Items.Add($"{eq.TagNumber} - {eq.Description ?? eq.EquipmentType}");
                }
            }

            if (tagComboBox.Items.Count > 0)
            {
                // Select first non-separator item
                for (int i = 0; i < tagComboBox.Items.Count; i++)
                {
                    if (!tagComboBox.Items[i].ToString()!.StartsWith("---"))
                    {
                        tagComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                // No existing equipment, disable this option
                existingTagRadio.Enabled = false;
                autoGenerateRadio.Checked = true;
            }
        }

        private void RadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            tagComboBox.Enabled = existingTagRadio.Checked;
            customTagTextBox.Enabled = customTagRadio.Checked;

            if (customTagRadio.Checked)
            {
                customTagTextBox.Focus();
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (existingTagRadio.Checked)
            {
                if (tagComboBox.SelectedItem == null || tagComboBox.SelectedItem.ToString()!.StartsWith("---"))
                {
                    MessageBox.Show("Please select an equipment from the list.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                // Extract tag number from combo box selection
                string selected = tagComboBox.SelectedItem.ToString()!;
                SelectedTagNumber = selected.Split('-')[0].Trim();
                UseExistingEquipment = true;

                // Find the equipment object
                SelectedEquipment = _availableEquipment.FirstOrDefault(e => e.TagNumber == SelectedTagNumber);
            }
            else if (autoGenerateRadio.Checked)
            {
                SelectedTagNumber = _suggestedTag;
                UseExistingEquipment = false;
            }
            else if (customTagRadio.Checked)
            {
                if (string.IsNullOrWhiteSpace(customTagTextBox.Text))
                {
                    MessageBox.Show("Please enter a tag number.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                SelectedTagNumber = customTagTextBox.Text.Trim();
                UseExistingEquipment = false;

                // Check for duplicates
                if (_availableEquipment.Any(e => e.TagNumber.Equals(SelectedTagNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    var result = MessageBox.Show(
                        $"Tag number '{SelectedTagNumber}' already exists in the database.\nDo you want to use it anyway?",
                        "Duplicate Tag", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                    {
                        this.DialogResult = DialogResult.None;
                        return;
                    }

                    UseExistingEquipment = true;
                    SelectedEquipment = _availableEquipment.FirstOrDefault(e => e.TagNumber.Equals(SelectedTagNumber, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        private string GetEquipmentType(string blockName)
        {
            // Simple equipment type detection
            string upperBlock = blockName.ToUpper();

            if (upperBlock.Contains("PUMP") || upperBlock.Contains("PMP") || upperBlock.StartsWith("P-"))
                return "Pump";
            if (upperBlock.Contains("VALVE") || upperBlock.Contains("VLV") || upperBlock.StartsWith("V-"))
                return "Valve";
            if (upperBlock.Contains("TANK") || upperBlock.StartsWith("TK") || upperBlock.StartsWith("T-"))
                return "Tank";
            if (upperBlock.Contains("VESSEL") || upperBlock.Contains("VSL") || upperBlock.StartsWith("VS"))
                return "Vessel";
            if (upperBlock.Contains("HX") || upperBlock.Contains("HEAT") || upperBlock.Contains("EXCHANGER"))
                return "Heat Exchanger";
            if (upperBlock.Contains("FILTER") || upperBlock.Contains("FLT") || upperBlock.StartsWith("F-"))
                return "Filter";
            if (upperBlock.Contains("COMPRESSOR") || upperBlock.Contains("COMP") || upperBlock.StartsWith("C-"))
                return "Compressor";
            if (upperBlock.Contains("SEPARATOR") || upperBlock.Contains("SEP") || upperBlock.StartsWith("S-"))
                return "Separator";

            return "Equipment";
        }
    }
}
