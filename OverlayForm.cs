using System;
using System.Drawing;
using System.Windows.Forms;

namespace BitCraftTimer
{
    /// <summary>
    /// A semi-transparent, topmost form used to notify the user about stamina depletion.
    /// </summary>
    public partial class OverlayForm : Form
    {
        public event Action? ResumeRequested;
        private Button resumeButton = null!;
        private Button openGameButton = null!;
        private Label messageLabel = null!;

        public OverlayForm()
        {
            InitializeComponent();
            SetupOverlay();
        }

        private void SetupOverlay()
        {
            this.BackColor = Color.FromArgb(30, 30, 30); // Dark background instead of transparent
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Normal;
            this.Opacity = 0.95; // Semi-transparent effect
            
            // Positioning at top center of 1920x1080 screen
            this.Size = new Size(600, 120);
            this.Location = new Point((1920 - this.Width) / 2, 10); // Fixed for 1920x1080, top position
            
            // Prevent focus stealing
            this.ShowInTaskbar = false;
            this.SetStyle(ControlStyles.Selectable, false);

            // Main container panel
            var containerPanel = new Panel
            {
                Size = new Size(580, 100),
                BackColor = Color.FromArgb(220, 40, 44, 52),
                Location = new Point(10, 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Message label
            messageLabel = new Label
            {
                Text = "STAMINA DEPLETED\nClick Resume to continue when ready",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(350, 60),
                Location = new Point(15, 15),
                BackColor = Color.Transparent
            };

            // Open BitCraft button
            openGameButton = new Button
            {
                Text = "Open BitCraft",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Size = new Size(100, 35),
                Location = new Point(375, 32),
                BackColor = Color.FromArgb(255, 87, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            openGameButton.Click += OpenGameButton_Click;

            // Resume button
            resumeButton = new Button
            {
                Text = "Resume",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(100, 35),
                Location = new Point(485, 32),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            resumeButton.Click += (s, e) => ResumeRequested?.Invoke();
            resumeButton.Click += ResumeButton_Click;

            // Timer info label
            var timerLabel = new Label
            {
                Text = "Таймер приостановлен",
                ForeColor = Color.FromArgb(255, 193, 7),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(200, 20),
                Location = new Point(15, 50)
            };

            containerPanel.Controls.Add(messageLabel);
            containerPanel.Controls.Add(openGameButton);
            containerPanel.Controls.Add(resumeButton);
            containerPanel.Controls.Add(timerLabel);
            this.Controls.Add(containerPanel);
        }

        public void ShowCompletionMessage()
        {
            messageLabel.Text = "🎉 CRAFTING COMPLETED! 🎉\n✨ Click to return to application";
            messageLabel.ForeColor = Color.FromArgb(76, 175, 80);
            resumeButton.Text = "📱 Go to App";
            resumeButton.BackColor = Color.FromArgb(33, 150, 243);
            openGameButton.Visible = false; // Hide open game button for completion message
        }
        
        private void OpenGameButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("BitCraft");
                if (processes.Length > 0)
                {
                    var process = processes[0];
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(process.MainWindowHandle, 9); // SW_RESTORE
                        SetForegroundWindow(process.MainWindowHandle);
                    }
                }
                else
                {
                    // Try to launch BitCraft from common paths
                    string[] possiblePaths = {
                        @"C:\Program Files\BitCraft\BitCraft.exe",
                        @"C:\Program Files (x86)\BitCraft\BitCraft.exe",
                        @"C:\Games\BitCraft\BitCraft.exe",
                        @"D:\Games\BitCraft\BitCraft.exe"
                    };

                    bool gameFound = false;
                    foreach (string path in possiblePaths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            System.Diagnostics.Process.Start(path);
                            gameFound = true;
                            break;
                        }
                    }

                    if (!gameFound)
                    {
                        MessageBox.Show("BitCraft.exe не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии BitCraft: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void ResumeButton_Click(object? sender, EventArgs e)
        {
            ResumeRequested?.Invoke();
            this.Hide();
        }
    }
}
