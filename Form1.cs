using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using Newtonsoft.Json;
using BitCraftTimer.Properties;

namespace BitCraftTimer
{
    public partial class Form1 : Form
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _trayMenu;
        private OverlayForm? _overlayForm;
        private System.Windows.Forms.Timer? _mainTimer;

        // UI Controls - initialized in CreateUIComponents()
        private TextBox _txtEffort = null!;
        private TextBox _txtPower = null!;
        private TextBox _txtSpeed = null!;
        private TextBox _txtXpPerTick = null!;
        private TextBox _txtStamina = null!;
        private TextBox _txtNotes = null!;
        private ComboBox _cmbItemTier = null!;
        private Button _btnCalculate = null!;
        private Button _btnStart = null!;
        private Button _btnMarket = null!;
        private Button _btnBrico = null!;
        private Button _btnOpenCalculator = null!;
        private Button _btnOpenGame = null!;
        private Button _btnMinimizeGame = null!;
        private Button _btnSettings = null!;
        private Label _lblTotalTime = null!;
        private Label _lblTicks = null!;
        private Label _lblXp = null!;
        private Label _lblCargo = null!;
        private Label _lblInterrupts = null!;
        private Label _lblTimer = null!;
        private Label _lblNextInterrupt = null!;
        private Label _lblStaminaConsumption = null!;
        private GroupBox _paramsGroup = null!;
        private GroupBox _resultsGroup = null!;
        private GroupBox _notesGroup = null!;
        private GroupBox _linksGroup = null!;
        private GroupBox _settingsGroup = null!;

        // --- State & Constants ---
        private double _totalTicks, _perTickSeconds, _craftingTime, _totalXp, _cargo, _initialStamina, _currentStamina;
        private bool _isRunning, _isUserPaused;
        private double _elapsedSeconds;
        private int _ticksConsumed;
        #region Fields

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private List<(int tick, int sec)> _interruptionList = new();

        private const double STAMINA_PER_TICK = 0.895;
        private const double TIME_MULTIPLIER = 1;
        private const string CONFIG_FILE = "bitcraft_timer_config.json";
        
        // Windows API constants
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        #endregion

        #region Constructor

        public Form1()
        {
            try
            {
                InitializeComponent();
                
                // Set form properties
                this.Text = "BitCraft Timer";
                this.Size = new Size(1000, 700);
                this.MinimumSize = new Size(800, 600);
                this.StartPosition = FormStartPosition.CenterScreen;
                
                // Set the application icon
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BitCraftTimer.ico");
                    if (File.Exists(iconPath))
                    {
                        this.Icon = new Icon(iconPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading icon: {ex.Message}");
                }
                
                // Initialize UI components
                CreateUIComponents();
                ApplyCustomStyles();
                
                // Load settings and initialize app logic
                LoadSettings();
                InitializeAppLogic();
                
                // Set up form closing handler
                this.FormClosing += (s, e) => {
                    SaveSettings();
                    _notifyIcon?.Dispose();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region UI Initialization & Styling

        /// <summary>
        /// Programmatically creates all UI controls, sets their properties, and adds them to the form.
        /// This approach is used to keep the designer file clean and allow for complex, dynamic layouts.
        /// </summary>
        private void CreateUIComponents()
        {
            // Create all controls with initial properties
            _paramsGroup = new GroupBox { Text = "Crafting Parameters" };
            _resultsGroup = new GroupBox { Text = "Crafting Results" };
            _notesGroup = new GroupBox { Text = "Notes" };
            _linksGroup = new GroupBox { Text = "Quick Actions" };
            _settingsGroup = new GroupBox { Text = "Settings" };

            _txtEffort = new TextBox { Text = "10000" };
            _txtPower = new TextBox { Text = "15" };
            _txtSpeed = new TextBox { Text = "1.31" };
            _txtXpPerTick = new TextBox { Text = "19" };
            _txtStamina = new TextBox { Text = "100" };
            _txtNotes = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical };
            
            _cmbItemTier = new ComboBox 
            { 
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            _cmbItemTier.Items.AddRange(new string[] { 
                "Tier 1 (0.75 stamina/tick)", "Tier 2 (0.89 stamina/tick)", "Tier 3 (1.03 stamina/tick)",
                "Tier 4 (1.16 stamina/tick)", "Tier 5 (1.28 stamina/tick)", "Tier 6 (1.41 stamina/tick)",
                "Tier 7 (1.52 stamina/tick)", "Tier 8 (1.64 stamina/tick)", "Tier 9 (1.75 stamina/tick)",
                "Tier 10 (1.86 stamina/tick)" });
            _cmbItemTier.SelectedIndex = 0;

            _btnCalculate = new Button { Text = "Calculate", FlatStyle = FlatStyle.Flat };
            _btnStart = new Button { Text = "Start", FlatStyle = FlatStyle.Flat };
            _btnMarket = new Button { Text = "Market", FlatStyle = FlatStyle.Flat };
            _btnBrico = new Button { Text = "Brico", FlatStyle = FlatStyle.Flat };
            _btnOpenCalculator = new Button { Text = "Calculator", FlatStyle = FlatStyle.Flat };
            _btnSettings = new Button { Text = "Settings", FlatStyle = FlatStyle.Flat };
            _btnOpenGame = new Button { Text = "Open Game", FlatStyle = FlatStyle.Flat };
            _btnMinimizeGame = new Button { Text = "Minimize to Tray", FlatStyle = FlatStyle.Flat };

            _lblTotalTime = new Label { Text = "Total time: -", AutoSize = true };
            _lblTicks = new Label { Text = "Number of ticks: -", AutoSize = true };
            _lblXp = new Label { Text = "Total XP gained: -", AutoSize = true };
            _lblCargo = new Label { Text = "Cargo: -", AutoSize = true };
            _lblInterrupts = new Label { Text = "Stamina interruptions: -", AutoSize = true };
            _lblNextInterrupt = new Label { Text = "Next interrupt: -", AutoSize = true };
            _lblTimer = new Label { Text = "00:00:00", Font = new Font("Segoe UI", 28, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            _lblStaminaConsumption = new Label { Text = "Stamina: -", AutoSize = true };
            
            // Create smaller labels for form fields with emojis
            var lblEffort = new Label { Text = "Effort", Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, AutoSize = true };
            var lblPower = new Label { Text = "Power", Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, AutoSize = true };
            var lblSpeed = new Label { Text = "Speed (s/tick)", Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, AutoSize = true };
            var lblXpPerTick = new Label { Text = "XP per Tick", Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, AutoSize = true };
            var lblItemTier = new Label { Text = "Item Tier", Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, AutoSize = true };
            var lblStamina = new Label { Text = "Max Stamina", Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, AutoSize = true };
            
            // Add controls to their respective group boxes
            _paramsGroup.Controls.AddRange(new Control[] { 
                lblEffort, _txtEffort, lblPower, _txtPower, lblSpeed, _txtSpeed, 
                lblXpPerTick, _txtXpPerTick, lblItemTier, _cmbItemTier, lblStamina, _txtStamina, 
                _btnCalculate, _btnOpenCalculator });
            _resultsGroup.Controls.AddRange(new Control[] { _lblTimer, _lblTotalTime, _lblTicks, _lblXp, _lblCargo, _lblInterrupts, _lblNextInterrupt, _lblStaminaConsumption, _btnStart });
            _notesGroup.Controls.AddRange(new Control[] { _txtNotes });
            _linksGroup.Controls.AddRange(new Control[] { _btnMarket, _btnBrico, _btnOpenGame, _btnMinimizeGame });
            _settingsGroup.Controls.AddRange(new Control[] { _btnSettings });

            // Add group boxes to the form if they're not already added
            if (!this.Controls.Contains(_paramsGroup))
                this.Controls.Add(_paramsGroup);
            if (!this.Controls.Contains(_resultsGroup))
                this.Controls.Add(_resultsGroup);
            if (!this.Controls.Contains(_notesGroup))
                this.Controls.Add(_notesGroup);
            if (!this.Controls.Contains(_linksGroup))
                this.Controls.Add(_linksGroup);
            if (!this.Controls.Contains(_settingsGroup))
                this.Controls.Add(_settingsGroup);
                
            // Initialize layout
            this.SuspendLayout();
            try
            {
                // Set control positions and anchors for responsive layout
                SetupLayout();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }
        
        private bool _isSettingUpLayout = false;

        /// <summary>
        /// Configures the layout, positions, sizes, and anchors for all UI elements.
        /// </summary>
        private void SetupLayout()
        {
            // Prevent re-entrancy
            if (_isSettingUpLayout)
                return;

            bool layoutSuspended = false;
            try
            {
                _isSettingUpLayout = true;

                // Set form properties if not already set
                if (this.WindowState == FormWindowState.Normal && this.Size != new Size(1000, 700))
                {
                    this.SuspendLayout();
                    layoutSuspended = true;
                    this.Size = new Size(1000, 700);
                    this.MinimumSize = new Size(800, 600);
                    this.StartPosition = FormStartPosition.CenterScreen;
                }

                // Suspend layouts for all group boxes
                _paramsGroup.SuspendLayout();
                _resultsGroup.SuspendLayout();
                _notesGroup.SuspendLayout();
                _linksGroup.SuspendLayout();
                _settingsGroup.SuspendLayout();
                
                // Main Panels (Group Boxes)
                _paramsGroup.Location = new Point(12, 12);
                _paramsGroup.Size = new Size(400, 400);
                _paramsGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;

                _resultsGroup.Location = new Point(424, 12);
                _resultsGroup.Size = new Size(504, 400);
                _resultsGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

                _linksGroup.Location = new Point(12, 424);
                _linksGroup.Size = new Size(400, 70);
                _linksGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                
                _settingsGroup.Location = new Point(12, 504);
                _settingsGroup.Size = new Size(400, 60);
                _settingsGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

                _notesGroup.Location = new Point(424, 424);
                _notesGroup.Size = new Size(504, 244);
                _notesGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            
                // Parameters Controls with proper spacing
                var labels = _paramsGroup.Controls.OfType<Label>().ToArray();
                var lblEffort = labels.FirstOrDefault(l => l.Text == "Effort") ?? new Label { Text = "Effort" };
                var lblPower = labels.FirstOrDefault(l => l.Text == "Power") ?? new Label { Text = "Power" };
                var lblSpeed = labels.FirstOrDefault(l => l.Text == "Speed (s/tick)") ?? new Label { Text = "Speed (s/tick)" };
                var lblXpPerTick = labels.FirstOrDefault(l => l.Text == "XP per Tick") ?? new Label { Text = "XP per Tick" };
                var lblItemTier = labels.FirstOrDefault(l => l.Text == "Item Tier") ?? new Label { Text = "Item Tier" };
                var lblStamina = labels.FirstOrDefault(l => l.Text == "Max Stamina") ?? new Label { Text = "Max Stamina" };
            
                // Set control positions and sizes
                lblEffort.Location = new Point(15, 25);
                _txtEffort.Location = new Point(15, 40); 
                _txtEffort.Width = 370;
                
                lblPower.Location = new Point(15, 75);
                _txtPower.Location = new Point(15, 90); 
                _txtPower.Width = 370;
                
                lblSpeed.Location = new Point(15, 125);
                _txtSpeed.Location = new Point(15, 140); 
                _txtSpeed.Width = 370;
                
                lblXpPerTick.Location = new Point(15, 175);
                _txtXpPerTick.Location = new Point(15, 190); 
                _txtXpPerTick.Width = 370;
                
                lblItemTier.Location = new Point(15, 225);
                _cmbItemTier.Location = new Point(15, 240); 
                _cmbItemTier.Width = 370;
                
                lblStamina.Location = new Point(15, 275);
                _txtStamina.Location = new Point(15, 290); 
                _txtStamina.Width = 370;
                
                _btnCalculate.Location = new Point(15, 330); 
                _btnCalculate.Size = new Size(180, 40);
                _btnCalculate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                
                _btnOpenCalculator.Location = new Point(205, 330); 
                _btnOpenCalculator.Size = new Size(180, 40);
                _btnOpenCalculator.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                // Results Controls
                _lblTimer.Location = new Point(15, 30); 
                _lblTimer.Size = new Size(470, 60); 
                _lblTimer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _lblTotalTime.Location = new Point(15, 100);
                _lblTicks.Location = new Point(15, 125);
                _lblXp.Location = new Point(15, 150);
                _lblCargo.Location = new Point(15, 175);
                _lblInterrupts.Location = new Point(15, 200);
                _lblNextInterrupt.Location = new Point(15, 225);
                _btnStart.Location = new Point(15, 270); 
                _btnStart.Size = new Size(200, 45);
                _btnStart.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

                // Links & Notes Controls - Fixed overlapping
                _btnMarket.Location = new Point(15, 20); 
                _btnMarket.Size = new Size(90, 35);
                _btnBrico.Location = new Point(110, 20); 
                _btnBrico.Size = new Size(90, 35);
                _btnOpenGame.Location = new Point(205, 20); 
                _btnOpenGame.Size = new Size(90, 35);
                _btnMinimizeGame.Location = new Point(300, 20); 
                _btnMinimizeGame.Size = new Size(90, 35);

                // Notes Control
                _txtNotes.Location = new Point(15, 30);
                _txtNotes.Size = new Size(474, 200);
                _txtNotes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                // Settings Controls
                _btnSettings.Location = new Point(15, 20);
                _btnSettings.Size = new Size(370, 30);
                _btnSettings.Anchor = AnchorStyles.Left | AnchorStyles.Right;

                // Resume layouts
                _paramsGroup.ResumeLayout(false);
                _resultsGroup.ResumeLayout(false);
                _notesGroup.ResumeLayout(false);
                _linksGroup.ResumeLayout(false);
                _settingsGroup.ResumeLayout(false);
                
                if (layoutSuspended)
                {
                    this.ResumeLayout(true);
                }
            }
            catch (Exception ex)
            {
                // Log the error and show a message to the user
                Console.WriteLine($"Error in SetupLayout: {ex}");
                MessageBox.Show("An error occurred while setting up the layout. Some controls may not be positioned correctly.", 
                    "Layout Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                // Ensure we always reset the flag, even if an exception occurred
                _isSettingUpLayout = false;
            }
            
            // Add controls to their respective group boxes
            _paramsGroup.Controls.Clear();
            _paramsGroup.Controls.AddRange(new Control[] {
                _txtEffort, _txtPower, _txtSpeed, _txtXpPerTick, _cmbItemTier, _txtStamina,
                _btnCalculate, _btnOpenCalculator,
                new Label { Text = "Effort", Location = new Point(15, 25), AutoSize = true },
                new Label { Text = "Power", Location = new Point(15, 75), AutoSize = true },
                new Label { Text = "Speed (s/tick)", Location = new Point(15, 125), AutoSize = true },
                new Label { Text = "XP per Tick", Location = new Point(15, 175), AutoSize = true },
                new Label { Text = "Item Tier", Location = new Point(15, 225), AutoSize = true },
                new Label { Text = "Max Stamina", Location = new Point(15, 275), AutoSize = true }
            });
            
            // Results Group
            _resultsGroup.Controls.Clear();
            _resultsGroup.Controls.AddRange(new Control[] {
                _lblTimer, _lblTotalTime, _lblTicks, _lblXp, _lblCargo, _lblInterrupts, 
                _lblNextInterrupt, _btnStart
            });
            
            // Links Group
            _linksGroup.Controls.Clear();
            _linksGroup.Controls.AddRange(new Control[] {
                _btnMarket, _btnBrico, _btnOpenGame, _btnMinimizeGame
            });
            
            // Notes Group
            _notesGroup.Controls.Clear();
            _notesGroup.Controls.Add(_txtNotes);
            
            // Settings Group
            _settingsGroup.Controls.Clear();
            _settingsGroup.Controls.Add(_btnSettings);
            
            // Add group boxes to the form if not already added
            if (!this.Controls.Contains(_paramsGroup))
                this.Controls.Add(_paramsGroup);
            if (!this.Controls.Contains(_resultsGroup))
                this.Controls.Add(_resultsGroup);
            if (!this.Controls.Contains(_linksGroup))
                this.Controls.Add(_linksGroup);
            if (!this.Controls.Contains(_notesGroup))
                this.Controls.Add(_notesGroup);
            if (!this.Controls.Contains(_settingsGroup))
                this.Controls.Add(_settingsGroup);
            
            // Set anchors for controls inside groups
            foreach (var grp in new[] { _paramsGroup, _resultsGroup, _linksGroup, _notesGroup })
            {
                foreach (Control ctrl in grp.Controls)
                {
                    if (ctrl is TextBox || ctrl is Button)
                    {
                        ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    }
                }
            }
            _txtNotes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        /// <summary>
        /// Applies the dark theme and custom styling to all controls.
        /// </summary>
        private void ApplyCustomStyles()
        {
            this.BackColor = Color.FromArgb(24, 26, 31);
            this.ForeColor = Color.FromArgb(220, 221, 222);
            this.Font = new Font("Segoe UI Variable", 9F);

            var darkBg = Color.FromArgb(30, 33, 40);
            var accentColor = Color.FromArgb(88, 101, 242);
            var accentHoverColor = Color.FromArgb(71, 82, 196);
            var greenColor = Color.FromArgb(87, 242, 135);

            foreach (var grp in new[] { _paramsGroup, _resultsGroup, _notesGroup, _linksGroup })
            {
                grp.BackColor = darkBg;
                grp.ForeColor = this.ForeColor;
                grp.Font = new Font("Segoe UI Variable", 10F, FontStyle.Bold);
            }

            foreach (var txt in new[] { _txtEffort, _txtPower, _txtSpeed, _txtXpPerTick, _txtStamina, _txtNotes })
            {
                txt.BackColor = Color.FromArgb(40, 43, 52);
                txt.ForeColor = this.ForeColor;
                txt.BorderStyle = BorderStyle.FixedSingle;
                if(txt is RoundedTextBox rtb) rtb.BorderColor = Color.FromArgb(60, 64, 72);
            }

            // ComboBox styling
            _cmbItemTier.BackColor = Color.FromArgb(40, 43, 52);
            _cmbItemTier.ForeColor = this.ForeColor;
            _cmbItemTier.Font = new Font("Segoe UI Variable", 10F);

            foreach (var lbl in new[] { _lblTotalTime, _lblTicks, _lblXp, _lblCargo, _lblInterrupts, _lblNextInterrupt })
            {
                lbl.ForeColor = Color.FromArgb(185, 187, 190);
                lbl.BackColor = Color.Transparent;
            }

            _lblTimer.ForeColor = accentColor;
            _lblTimer.BackColor = Color.Transparent;

            // Button Styles
            _btnCalculate.BackColor = Color.FromArgb(63, 81, 181);
            _btnCalculate.FlatAppearance.MouseOverBackColor = Color.FromArgb(57, 73, 171);
            _btnCalculate.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            
            _btnOpenCalculator.BackColor = Color.FromArgb(63, 81, 181);
            _btnOpenCalculator.FlatAppearance.MouseOverBackColor = Color.FromArgb(57, 73, 171);
            _btnOpenCalculator.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            
            _btnStart.BackColor = Color.FromArgb(76, 175, 80);
            _btnStart.FlatAppearance.MouseOverBackColor = Color.FromArgb(67, 160, 71);
            _btnStart.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            
            _btnOpenGame.BackColor = Color.FromArgb(255, 87, 34);
            _btnOpenGame.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 74, 25);
            
            _btnMinimizeGame.BackColor = Color.FromArgb(255, 152, 0);
            _btnMinimizeGame.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 124, 0);

            foreach (var btn in new[] { _btnCalculate, _btnStart, _btnMarket, _btnBrico, _btnOpenCalculator, _btnOpenGame, _btnMinimizeGame })
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                if (btn.Font == null) btn.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                btn.ForeColor = Color.White;
            }
        }

        #endregion

        private void LoadSettings()
        {
            try
            {
                // Load window position and size
                var savedLocation = Properties.Settings.Default.WindowLocation;
                if (savedLocation.X != 0 || savedLocation.Y != 0)
                {
                    this.Location = savedLocation;
                    this.WindowState = Properties.Settings.Default.WindowState;
                }
                
                // Load form controls values
                _txtEffort.Text = Properties.Settings.Default.Effort;
                _txtPower.Text = Properties.Settings.Default.Power;
                _txtSpeed.Text = Properties.Settings.Default.Speed;
                _txtXpPerTick.Text = Properties.Settings.Default.XpPerTick;
                _txtStamina.Text = Properties.Settings.Default.Stamina;
                _txtNotes.Text = Properties.Settings.Default.Notes;
                
                if (Properties.Settings.Default.ItemTier >= 0 && 
                    Properties.Settings.Default.ItemTier < _cmbItemTier.Items.Count)
                {
                    _cmbItemTier.SelectedIndex = Properties.Settings.Default.ItemTier;
                }
            }
            catch (Exception ex)
            {
                // If settings are corrupted, use default values
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Save window position and size
                if (this.WindowState == FormWindowState.Normal)
                {
                    Properties.Settings.Default.WindowLocation = this.Location;
                    Properties.Settings.Default.WindowSize = this.Size;
                }
                else
                {
                    Properties.Settings.Default.WindowLocation = this.RestoreBounds.Location;
                    Properties.Settings.Default.WindowSize = this.RestoreBounds.Size;
                }
                
                Properties.Settings.Default.WindowState = this.WindowState;
                
                // Save form controls values
                Properties.Settings.Default.Effort = _txtEffort.Text;
                Properties.Settings.Default.Power = _txtPower.Text;
                Properties.Settings.Default.Speed = _txtSpeed.Text;
                Properties.Settings.Default.XpPerTick = _txtXpPerTick.Text;
                Properties.Settings.Default.Stamina = _txtStamina.Text;
                Properties.Settings.Default.Notes = _txtNotes.Text;
                Properties.Settings.Default.ItemTier = _cmbItemTier.SelectedIndex;
                
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        #region App Logic & Event Handlers

        /// <summary>
        /// Initializes timers, tray icon, and wires up event handlers.
        /// </summary>
        private void InitializeAppLogic()
        {
            // Main Timer
            _mainTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _mainTimer.Tick += MainTimer_Tick;

            // Tray Icon
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Open", null, (s, e) => ShowWindow());
            _trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            
            // Create the NotifyIcon with the form's icon (which was set in the constructor)
            _notifyIcon = new NotifyIcon
            {
                Icon = this.Icon ?? SystemIcons.Application,
                Text = "BitCraft Timer",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            
            // Handle double-click on the system tray icon to restore the window
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
            
            // Handle form closing to clean up the NotifyIcon
            this.FormClosing += (s, e) => {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            };
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            SetupEventHandlers();
            
            // Improve font rendering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetupHotkeys();
        }

        private void SetupEventHandlers()
        {
            try
            {
                // Button click handlers
                _btnStart.Click += BtnStartStop_Click;
                _btnCalculate.Click += BtnCalculate_Click;
                _btnMarket.Click += (s, e) => Process.Start(new ProcessStartInfo("https://bitjita.com/market") { UseShellExecute = true });
                _btnBrico.Click += (s, e) => Process.Start(new ProcessStartInfo("https://www.brico.app/#/") { UseShellExecute = true });
                _btnOpenCalculator.Click += BtnOpenCalculator_Click;
                _btnOpenGame.Click += BtnOpenGame_Click;
                _btnMinimizeGame.Click += (s, e) => {
                    this.WindowState = FormWindowState.Minimized;
                };
                _btnSettings.Click += BtnSettings_Click;
                
                // Text changed handlers
                _txtNotes.TextChanged += (s, e) => 
                {
                    if (_txtNotes != null)
                        Settings.Default.Notes = _txtNotes.Text;
                };
                
                // Add Enter key functionality for input fields
                foreach (var txt in new[] { _txtEffort, _txtPower, _txtSpeed, _txtXpPerTick, _txtStamina })
                {
                    if (txt != null)
                    {
                        txt.KeyDown += (s, e) => 
                        {
                            if (e.KeyCode == Keys.Enter)
                            {
                                BtnCalculate_Click(s, e);
                                e.Handled = true;
                                e.SuppressKeyPress = true; // Prevent Windows error sound
                            }
                        };
                    }
                }
                
                // Form events
                this.Resize += Form1_Resize;
                this.FormClosing += Form1_FormClosing;
                
                // Make sure the form is properly shown
                this.Shown += (s, e) => 
                {
                    this.Focus();
                    this.BringToFront();
                    this.Activate();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up event handlers: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCalculate_Click(object? sender, EventArgs e)
        {
            try
            {
                double effort = ValidateInput(_txtEffort.Text, "Effort");
                double power = ValidateInput(_txtPower.Text, "Power", 1e-9);
                double speed = ValidateInput(_txtSpeed.Text, "Crafting Speed", 1e-9);
                double xpPerTick = ValidateInput(_txtXpPerTick.Text, "XP per Tick");
                _initialStamina = ValidateInput(_txtStamina.Text, "Stamina");
                
                // Get the selected tier (1-10)
                int itemTier = _cmbItemTier.SelectedIndex + 1;
                if (itemTier < 1 || itemTier > 10)
                {
                    throw new ArgumentException("Please select a valid item tier (1-10)");
                }

                _totalTicks = Math.Ceiling(effort / power);
                
                // Get the stamina consumption rate for the selected tier
                double staminaPerTick = GetTierMultiplier();
                
                // Calculate XP and cargo with tier-based multipliers
                _totalXp = _totalTicks * xpPerTick;
                _perTickSeconds = speed * TIME_MULTIPLIER;
                _craftingTime = _totalTicks * _perTickSeconds;
                _cargo = CalculateOre(effort) * (1 + (itemTier - 1) * 0.1); // 10% more cargo per tier

                ResetTimerState();
                _interruptionList = ComputeInterrupts(_totalTicks, _initialStamina, _perTickSeconds);

                // Show stamina consumption info
                double totalStamina = _totalTicks * staminaPerTick;
                _lblStaminaConsumption.Text = $"Stamina: {_initialStamina:F1} ({staminaPerTick:F2}/tick, {totalStamina:F1} total)";

                UpdateResultLabels();
                SaveSettings();
            }
            catch (Exception ex)
            {
                // Don't show message box, just update the UI with error state
                _lblStaminaConsumption.Text = "Error in calculation";
                _lblTotalTime.Text = "Error";
                _lblTicks.Text = "Error";
                _lblXp.Text = "Error";
                _lblCargo.Text = "Error";
                _lblInterrupts.Text = "Error";
                _lblNextInterrupt.Text = "Error";
                
                Console.WriteLine($"Error in BtnCalculate_Click: {ex.Message}");
            }
        }

        private double GetTierMultiplier()
        {
            // Tier multipliers based on stamina consumption rates
            // These values represent stamina per tick for each tier (1-10)
            double[] staminaPerTick = { 0.75, 0.89, 1.03, 1.16, 1.28, 1.41, 1.52, 1.64, 1.75, 1.86 };
            int selectedTier = _cmbItemTier.SelectedIndex;
            return selectedTier >= 0 && selectedTier < staminaPerTick.Length ? staminaPerTick[selectedTier] : 1.0;
        }

        private void BtnStartStop_Click(object? sender, EventArgs e)
        {
            if (!_isRunning)
            {
                // Start functionality
                BtnCalculate_Click(sender, e);

                if (_craftingTime <= 0)
                {
                    MessageBox.Show("Please calculate a valid crafting time first.", "Cannot Start Timer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _isRunning = true;
                _isUserPaused = false;

                _stopwatch.Restart();
                _mainTimer?.Start();

                _btnStart.Text = "Stop";
                _btnStart.BackColor = Color.FromArgb(244, 67, 54); // Red color for stop

                UpdateNextInterruptLabel();
                _overlayForm?.Hide();
            }
            else
            {
                // Stop functionality
                _isRunning = false;
                _isUserPaused = false;

                _stopwatch.Stop();
                _mainTimer?.Stop();

                _btnStart.Text = "Start";
                _btnStart.BackColor = Color.FromArgb(76, 175, 80); // Green color for start

                ResetTimerState();
            }
        }

        private void BtnOpenGame_Click(object? sender, EventArgs e)
        {
            try
            {
                var processes = Process.GetProcessesByName("BitCraft");
                if (processes.Length > 0)
                {
                    // Игра уже запущена, активируем окно
                    var process = processes[0];
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(process.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(process.MainWindowHandle);
                        // Removed popup notification
                    }
                }
                else
                {
                    // Пытаемся найти и запустить игру
                    string[] possiblePaths = {
                        @"C:\Program Files\BitCraft\BitCraft.exe",
                        @"C:\Program Files (x86)\BitCraft\BitCraft.exe",
                        @"C:\Games\BitCraft\BitCraft.exe",
                        @"D:\Games\BitCraft\BitCraft.exe"
                    };

                    bool gameFound = false;
                    foreach (string path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            Process.Start(path);
                            gameFound = true;
                            MessageBox.Show("BitCraft запускается...", "Игра найдена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        }
                    }

                    if (!gameFound)
                    {
                        MessageBox.Show("BitCraft.exe не найден!\nПожалуйста, запустите игру вручную.", "Игра не найдена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске игры: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BtnMinimizeGame_Click(object? sender, EventArgs e)
        {
            try
            {
                var processes = Process.GetProcessesByName("BitCraft");
                if (processes.Length > 0)
                {
                    var process = processes[0];
                    ShowWindow(process.MainWindowHandle, SW_MINIMIZE);
                    // Removed popup notification
                }
                // No error message if process not found
            }
            catch (Exception ex)
            {
                // Log the error without showing a popup
                Console.WriteLine($"Error minimizing BitCraft: {ex.Message}");
            }
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void MainTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isRunning) return;

            _elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            double remaining = _craftingTime - _elapsedSeconds;

            if (remaining <= 0)
            {
                FinishCrafting();
                return;
            }

            _lblTimer.Text = FormatTimeSpan(TimeSpan.FromSeconds(remaining));

            int newTicks = (int)(_elapsedSeconds / _perTickSeconds);
            if (newTicks > _ticksConsumed)
            {
                // Use the tier-based stamina consumption rate
                double staminaPerTick = GetTierMultiplier();
                _currentStamina -= (newTicks - _ticksConsumed) * staminaPerTick;
                _ticksConsumed = newTicks;
                
                // Update the UI to show current stamina consumption rate
                _lblStaminaConsumption.Text = $"Stamina: {_currentStamina:F1}/{_initialStamina} ({staminaPerTick:F2}/tick)";
            }

            // Update next interrupt display
            UpdateNextInterruptLabel();

            if (_currentStamina <= 0)
            {
                HandleStaminaDepletion();
            }
        }

        #endregion

        #region Core Logic & Helpers

        private void FinishCrafting()
        {
            _isRunning = false;
            _stopwatch.Stop();
            _mainTimer?.Stop();
            _lblTimer.Text = "COMPLETED";
            _btnStart.Text = "Start";
            _btnStart.BackColor = Color.FromArgb(76, 175, 80);
            
            // Show completion overlay
            ShowCompletionOverlay();
        }
        
        private void ShowCompletionOverlay()
        {
            if (_overlayForm == null || _overlayForm.IsDisposed)
            {
                _overlayForm = new OverlayForm();
            }
            
            _overlayForm.ShowCompletionMessage();
            _overlayForm.ResumeRequested += () => {
                _overlayForm.Hide();
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            };
            _overlayForm.Show();
            _overlayForm.BringToFront();
        }

        private void HandleStaminaDepletion()
        {
            _isRunning = false;
            _isUserPaused = true;

            _stopwatch.Stop();
            _mainTimer?.Stop();

            _btnStart.Text = "Start";
            _btnStart.BackColor = Color.FromArgb(76, 175, 80);

            _lblTimer.Text = "STAMINA EMPTY";

            // Always ensure we have a fresh overlay form and event handler
            if (_overlayForm != null)
            {
                _overlayForm.Hide();
                _overlayForm.Dispose();
            }
            
            _overlayForm = new OverlayForm();
            _overlayForm.ResumeRequested += () => {
                _overlayForm.Hide();
                ResumeFromStaminaDepletion();
            };
            
            _overlayForm.Show();
            _overlayForm.BringToFront();
        }

        private void ResumeFromStaminaDepletion()
        {
            if (!_isUserPaused) return;

            _isRunning = true;
            _isUserPaused = false;
            _currentStamina = _initialStamina; // Restore full stamina
            
            // Update the UI to show the restored stamina
            double staminaPerTick = GetTierMultiplier();
            _lblStaminaConsumption.Text = $"Stamina: {_currentStamina:F1}/{_initialStamina} ({staminaPerTick:F2}/tick)";

            _stopwatch.Start();
            _mainTimer?.Start();

            _btnStart.Text = "⏹️ Stop";
            _btnStart.BackColor = Color.FromArgb(244, 67, 54);

            // Recalculate next interrupt since we've restored stamina
            UpdateNextInterruptLabel();
            
            // Ensure the main form is visible and focused
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            this.BringToFront();
        }


        
        private void ResetTimerState()
        {
            _isRunning = false;
            _isUserPaused = false;
            _stopwatch.Reset();
            _mainTimer?.Stop();
            _elapsedSeconds = 0;
            _ticksConsumed = 0;
            _currentStamina = _initialStamina;

            _btnStart.Text = "Start";
            _btnStart.BackColor = Color.FromArgb(76, 175, 80);
            // Pause button removed
            _lblTimer.Text = FormatTimeSpan(TimeSpan.FromSeconds(_craftingTime));
        }

        private void UpdateResultLabels()
        {
            try
            {
                double effort = ValidateInput(_txtEffort.Text, "Effort");
                double betaChance = CalculateBetaChance(effort);
                double staminaPerTick = GetTierMultiplier();
                
                _lblTotalTime.Text = $"Total time: {FormatTimeSpan(TimeSpan.FromSeconds(_craftingTime))} ({_craftingTime:F2}s)";
                _lblTicks.Text = $"Number of ticks: {_totalTicks:N0}";
                _lblXp.Text = $"Total XP gained: {_totalXp:N0}";
                _lblCargo.Text = $"Cargo: {_cargo:F2}";
                _lblInterrupts.Text = $"Stamina interruptions: {_interruptionList.Count}";
                _lblStaminaConsumption.Text = $"Stamina: {_initialStamina:F1} ({staminaPerTick:F2}/tick)";
                UpdateNextInterruptLabel();
            }
            catch
            {
                // If validation fails, show basic info
                _lblTotalTime.Text = $"Total time: {FormatTimeSpan(TimeSpan.FromSeconds(_craftingTime))} ({_craftingTime:F2}s)";
                _lblTicks.Text = $"Number of ticks: {_totalTicks:N0}";
                _lblXp.Text = $"Total XP gained: {_totalXp:N0}";
                _lblCargo.Text = $"Cargo: {_cargo:F2}";
                _lblInterrupts.Text = $"Stamina interruptions: {_interruptionList.Count}";
                _lblStaminaConsumption.Text = "Stamina: -";
                UpdateNextInterruptLabel();
            }
        }

        private void UpdateNextInterruptLabel()
        {
            if (!_isRunning)
            {
                _lblNextInterrupt.Text = "Next interrupt: -";
                return;
            }
            
            // If we're currently in a paused state due to stamina depletion,
            // show that we're waiting for stamina to be restored
            if (_isUserPaused)
            {
                _lblNextInterrupt.Text = "⏳ Waiting for stamina restore...";
                return;
            }
            
            // Find the next interruption that hasn't happened yet
            foreach (var (tick, sec) in _interruptionList)
            {
                if (tick > _ticksConsumed)
                {
                    double remainingSec = sec - _elapsedSeconds;
                    if (remainingSec > 0)
                    {
                        _lblNextInterrupt.Text = $"⏱ Next interrupt: {FormatTimeSpan(TimeSpan.FromSeconds(remainingSec))}";
                        return;
                    }
                }
            }
            _lblNextInterrupt.Text = "Next interrupt: -";
        }

        private double ValidateInput(string text, string name, double minVal = 0)
        {
            if (!double.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                throw new FormatException($"Invalid value for {name}: '{text}'. Please enter a valid number.");
            }
            if (value < minVal)
            {
                throw new ArgumentOutOfRangeException(name, $"{name} must be at least {minVal}.");
            }
            return value;
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            return $"{ts.Minutes:00}:{ts.Seconds:00}";
        }

        private double CalculateOre(double effort)
        {
            // This is a direct C# port of the power law curve_fit model from the Python script.
            // y = a * x^b
            // Pre-calculated parameters from scipy.optimize.curve_fit
            double a = 0.193;
            double b = 0.787;
            double baseCargo = a * Math.Pow(effort, b);
            
            // Apply beta chance for bonus cargo
            double betaChance = CalculateBetaChance(effort);
            double bonusMultiplier = 1.0 + (betaChance * 0.5); // 50% bonus on beta success
            
            return baseCargo * bonusMultiplier;
        }

        private double CalculateBetaChance(double effort)
        {
            // Beta chance calculation based on effort
            // Higher effort = higher chance for bonus cargo
            double baseChance = 0.1; // 10% base chance
            double effortBonus = Math.Min(effort / 50000.0, 0.4); // Up to 40% bonus for high effort
            return Math.Min(baseChance + effortBonus, 0.5); // Max 50% chance
        }

        private List<(int tick, int sec)> ComputeInterrupts(double totalTicks, double stamina, double secPerTick, int offsetTicks = 0)
        {
            var interrupts = new List<(int, int)>();
            if (stamina <= 0 || totalTicks <= 0) return interrupts;

            // Get the stamina consumption rate for the selected tier
            double staminaPerTick = GetTierMultiplier();
            
            // Calculate how many ticks until stamina depletes
            double ticksPerInterrupt = stamina / staminaPerTick;
            if (ticksPerInterrupt <= 0) return interrupts;

            // Calculate number of interruptions needed
            int estCount = (int)Math.Ceiling((totalTicks * staminaPerTick) / stamina);
            
            // Generate interruption points
            for (int k = 1; k <= estCount; k++)
            {
                int tickIdx = (int)Math.Ceiling(k * ticksPerInterrupt);
                if (tickIdx <= offsetTicks) continue;
                if (tickIdx > totalTicks) break;

                int sec = (int)Math.Round(tickIdx * secPerTick);
                interrupts.Add((tickIdx, sec));
                
                // Add a small delay between interruptions to ensure they're processed correctly
                if (k < estCount)
                {
                    tickIdx += 5; // Add 5 ticks of buffer between interruptions
                }
            }
            
            return interrupts;
        }

        #endregion

        #region System & Window Management

        private void SetupHotkeys()
        {
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5)
                {
                    BtnStartStop_Click(s, e);
                }
                if (e.Control && e.KeyCode == Keys.N)
                {
                    _txtNotes.Clear();
                }
                // Space key removed since pause button is removed
                if (e.Control && e.KeyCode == Keys.G)
                {
                    BtnOpenGame_Click(s, e);
                }
                if (e.KeyCode == Keys.F1)
                {
                    BtnCalculate_Click(s, e);
                }
            };
        }

        // LoadSettings and SaveSettings methods are defined earlier in the file

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();
            _notifyIcon?.Dispose();
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            // Only update layout if we're not already doing so
            if (!_isSettingUpLayout)
            {
                try
                {
                    _isSettingUpLayout = true;
                    SetupLayout();
                }
                finally
                {
                    _isSettingUpLayout = false;
                }
            }
            
            // Handle window state changes
            if (this.WindowState == FormWindowState.Minimized)
            {
                // Hide the form and show the system tray icon
                this.Hide();
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = true;
                    _notifyIcon.ShowBalloonTip(1000, "BitCraft Timer", "Running in the background", ToolTipIcon.Info);
                }
            }
            // Restore from system tray when double-clicking the icon is handled by the NotifyIcon's DoubleClick event
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            if (_notifyIcon != null) _notifyIcon.Visible = false;
            this.Activate();
        }

        private void BtnOpenCalculator_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start("calc.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии калькулятора: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Settings Panel\n\nComing Soon!\n\nPlanned Features:\n• Interface Themes\n• Notification Settings\n• Statistics Export\n• Game Settings\n• Additional Parameters", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}
