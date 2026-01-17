using PIDStandardization.Core.Entities;
using System.Windows.Forms;

namespace PIDStandardization.AutoCAD.Forms
{
    /// <summary>
    /// Form for selecting a drawing from the project
    /// </summary>
    public class DrawingSelectionForm : Form
    {
        private ComboBox drawingComboBox;
        private Button okButton;
        private Button cancelButton;
        private Label label;

        public Drawing SelectedDrawing { get; private set; }

        public DrawingSelectionForm(IEnumerable<Drawing> drawings)
        {
            InitializeComponent();
            LoadDrawings(drawings);
        }

        private void InitializeComponent()
        {
            this.Text = "Select Drawing";
            this.Size = new System.Drawing.Size(450, 180);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            label = new Label
            {
                Text = "Select the source drawing for this equipment extraction:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(400, 30),
                AutoSize = false
            };

            drawingComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(20, 55),
                Size = new System.Drawing.Size(400, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(255, 100),
                Size = new System.Drawing.Size(80, 30)
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(340, 100),
                Size = new System.Drawing.Size(80, 30)
            };

            this.Controls.Add(label);
            this.Controls.Add(drawingComboBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadDrawings(IEnumerable<Drawing> drawings)
        {
            var drawingList = drawings.ToList();

            foreach (var drawing in drawingList)
            {
                string displayText = $"{drawing.DrawingNumber} - {drawing.DrawingTitle} (v{drawing.VersionNumber})";
                drawingComboBox.Items.Add(new DrawingDisplayItem(drawing, displayText));
            }

            if (drawingComboBox.Items.Count > 0)
            {
                drawingComboBox.SelectedIndex = 0;
            }
        }

        private void OkButton_Click(object sender, System.EventArgs e)
        {
            if (drawingComboBox.SelectedItem is DrawingDisplayItem selectedItem)
            {
                SelectedDrawing = selectedItem.Drawing;
            }
        }

        private class DrawingDisplayItem
        {
            public Drawing Drawing { get; }
            public string DisplayText { get; }

            public DrawingDisplayItem(Drawing drawing, string displayText)
            {
                Drawing = drawing;
                DisplayText = displayText;
            }

            public override string ToString() => DisplayText;
        }
    }
}
