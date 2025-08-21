using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BitCraftTimer
{
    [ToolboxItem(true)]
    [DesignerCategory("Form")]
    public partial class SettingsForm : Form
    {
        private double _staminaLeakInterval = 1.0;
        private bool _minimizeToTray = true;
        private bool _showNotifications = true;
        private bool _autoSaveNotes = true;
        private int _autoSaveInterval = 30;

        [Category("Behavior")]
        [Description("Interval in seconds for stamina leak notification")]
        [Browsable(true)]
        [DefaultValue(1.0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public double StaminaLeakInterval { get => _staminaLeakInterval; set => _staminaLeakInterval = value; }

        [Category("Behavior")]
        [Description("Whether to minimize the application to system tray instead of taskbar")]
        [Browsable(true)]
        [DefaultValue(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool MinimizeToTray { get => _minimizeToTray; set => _minimizeToTray = value; }

        [Category("Behavior")]
        [Description("Whether to show desktop notifications")]
        [Browsable(true)]
        [DefaultValue(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowNotifications { get => _showNotifications; set => _showNotifications = value; }

        [Category("Behavior")]
        [Description("Whether to automatically save notes periodically")]
        [Browsable(true)]
        [DefaultValue(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool AutoSaveNotes { get => _autoSaveNotes; set => _autoSaveNotes = value; }

        [Category("Behavior")]
        [Description("Interval in seconds for auto-saving notes")]
        [Browsable(true)]
        [DefaultValue(30)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int AutoSaveInterval { get => _autoSaveInterval; set => _autoSaveInterval = value; }

        private NumericUpDown staminaLeakUpDown = null!;
        private CheckBox minimizeToTrayCheckBox = null!;
        private CheckBox showNotificationsCheckBox = null!;
        private CheckBox autoSaveNotesCheckBox = null!;
        private NumericUpDown autoSaveIntervalUpDown = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "BitCraft Timer Settings";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(32, 34, 42);
            this.ForeColor = Color.FromArgb(220, 221, 222);

            // Stamina leak interval
            var staminaLeakLabel = new Label();
            staminaLeakLabel.Text = "Stamina Leak Interval (seconds):";
            staminaLeakLabel.Location = new Point(20, 20);
            staminaLeakLabel.Size = new Size(200, 20);
            staminaLeakLabel.ForeColor = Color.FromArgb(220, 221, 222);
            this.Controls.Add(staminaLeakLabel);

            staminaLeakUpDown = new NumericUpDown();
            staminaLeakUpDown.Location = new Point(230, 18);
            staminaLeakUpDown.Size = new Size(120, 25);
            staminaLeakUpDown.Minimum = 0.1m;
            staminaLeakUpDown.Maximum = 10.0m;
            staminaLeakUpDown.DecimalPlaces = 1;
            staminaLeakUpDown.Increment = 0.1m;
            staminaLeakUpDown.Value = (decimal)StaminaLeakInterval;
            staminaLeakUpDown.BackColor = Color.FromArgb(40, 43, 52);
            staminaLeakUpDown.ForeColor = Color.FromArgb(220, 221, 222);
            this.Controls.Add(staminaLeakUpDown);

            // Minimize to tray
            minimizeToTrayCheckBox = new CheckBox();
            minimizeToTrayCheckBox.Text = "Minimize to system tray";
            minimizeToTrayCheckBox.Location = new Point(20, 60);
            minimizeToTrayCheckBox.Size = new Size(300, 20);
            minimizeToTrayCheckBox.Checked = MinimizeToTray;
            minimizeToTrayCheckBox.ForeColor = Color.FromArgb(220, 221, 222);
            this.Controls.Add(minimizeToTrayCheckBox);

            // Show notifications
            showNotificationsCheckBox = new CheckBox();
            showNotificationsCheckBox.Text = "Show balloon notifications";
            showNotificationsCheckBox.Location = new Point(20, 90);
            showNotificationsCheckBox.Size = new Size(300, 20);
            showNotificationsCheckBox.Checked = ShowNotifications;
            showNotificationsCheckBox.ForeColor = Color.FromArgb(220, 221, 222);
            this.Controls.Add(showNotificationsCheckBox);

            // Auto-save notes
            autoSaveNotesCheckBox = new CheckBox();
            autoSaveNotesCheckBox.Text = "Auto-save notes";
            autoSaveNotesCheckBox.Location = new Point(20, 120);
            autoSaveNotesCheckBox.Size = new Size(150, 20);
            autoSaveNotesCheckBox.Checked = AutoSaveNotes;
            autoSaveNotesCheckBox.ForeColor = Color.FromArgb(220, 221, 222);
            this.Controls.Add(autoSaveNotesCheckBox);

            // Auto-save interval
            var autoSaveLabel = new Label();
            autoSaveLabel.Text = "Auto-save interval (seconds):";
            autoSaveLabel.Location = new Point(20, 150);
            autoSaveLabel.Size = new Size(200, 20);
            autoSaveLabel.ForeColor = Color.FromArgb(220, 221, 222);
            this.Controls.Add(autoSaveLabel);

            autoSaveIntervalUpDown = new NumericUpDown();
            autoSaveIntervalUpDown.Location = new Point(230, 148);
            autoSaveIntervalUpDown.Size = new Size(120, 25);
            autoSaveIntervalUpDown.Minimum = 10;
            autoSaveIntervalUpDown.Maximum = 300;
            autoSaveIntervalUpDown.Value = AutoSaveInterval;
            autoSaveIntervalUpDown.BackColor = Color.FromArgb(40, 43, 52);
            autoSaveIntervalUpDown.ForeColor = Color.FromArgb(220, 221, 222);
            this.Controls.Add(autoSaveIntervalUpDown);

            // Buttons
            okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(200, 220);
            okButton.Size = new Size(80, 30);
            okButton.BackColor = Color.FromArgb(78, 110, 242);
            okButton.ForeColor = Color.White;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(290, 220);
            cancelButton.Size = new Size(80, 30);
            cancelButton.BackColor = Color.FromArgb(108, 117, 125);
            cancelButton.ForeColor = Color.White;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.Click += CancelButton_Click;
            this.Controls.Add(cancelButton);
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            StaminaLeakInterval = (double)staminaLeakUpDown.Value;
            MinimizeToTray = minimizeToTrayCheckBox.Checked;
            ShowNotifications = showNotificationsCheckBox.Checked;
            AutoSaveNotes = autoSaveNotesCheckBox.Checked;
            AutoSaveInterval = (int)autoSaveIntervalUpDown.Value;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
