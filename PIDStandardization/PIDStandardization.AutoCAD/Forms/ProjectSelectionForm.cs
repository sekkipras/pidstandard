using System.Windows.Forms;
using PIDStandardization.Core.Entities;
using PIDStandardization.Core.Interfaces;

namespace PIDStandardization.AutoCAD.Forms
{
    /// <summary>
    /// Simple form for selecting a project in AutoCAD
    /// </summary>
    public class ProjectSelectionForm : Form
    {
        private ComboBox projectComboBox;
        private Button okButton;
        private Button cancelButton;
        private Label label;

        public Project? SelectedProject { get; private set; }

        public ProjectSelectionForm(IEnumerable<Project> projects)
        {
            InitializeForm();
            LoadProjects(projects);
        }

        private void InitializeForm()
        {
            this.Text = "Select Project";
            this.Width = 400;
            this.Height = 150;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Label
            label = new Label
            {
                Text = "Select Project:",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };

            // ComboBox
            projectComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(20, 45),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // OK Button
            okButton = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(200, 75),
                Width = 75,
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            // Cancel Button
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(285, 75),
                Width = 75,
                DialogResult = DialogResult.Cancel
            };

            // Add controls
            this.Controls.Add(label);
            this.Controls.Add(projectComboBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadProjects(IEnumerable<Project> projects)
        {
            foreach (var project in projects)
            {
                projectComboBox.Items.Add(new ProjectItem(project));
            }

            if (projectComboBox.Items.Count > 0)
            {
                projectComboBox.SelectedIndex = 0;
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (projectComboBox.SelectedItem is ProjectItem item)
            {
                SelectedProject = item.Project;
            }
        }

        private class ProjectItem
        {
            public Project Project { get; }

            public ProjectItem(Project project)
            {
                Project = project;
            }

            public override string ToString()
            {
                return $"{Project.ProjectName} ({Project.TaggingMode})";
            }
        }
    }
}
