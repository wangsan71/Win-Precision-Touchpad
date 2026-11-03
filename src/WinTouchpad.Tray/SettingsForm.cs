using WinTouchpad.Core.Settings;

namespace WinTouchpad.Tray;

/// <summary>
/// Settings window (zh-HK): grouped sections, modernized WinForms layout, live apply + persist.
/// Shown with <see cref="Form.Show"/> (not ShowDialog) — Close must use Click → Close().
/// </summary>
internal sealed class SettingsForm : Form
{
    private static readonly Color PageBack = Color.FromArgb(243, 243, 243);
    private static readonly Color CardBack = Color.White;
    private static readonly Color AccentText = Color.FromArgb(50, 49, 48);
    private static readonly Color MutedText = Color.FromArgb(96, 94, 92);
    private static readonly Color BorderLine = Color.FromArgb(225, 223, 221);

    private readonly SettingsStore _store;
    private readonly Action<AppSettings> _applyLive;
    private readonly Func<string> _getExePath;

    private readonly CheckBox _enableDrag;
    private readonly TrackBar _sensitivity;
    private readonly Label _sensitivityValue;
    private readonly NumericUpDown _startDistance;
    private readonly CheckBox _startWithWindows;
    private readonly Label _pathHint;
    private bool _suppressEvents;

    public SettingsForm(SettingsStore store, AppSettings current, Action<AppSettings> applyLive, Func<string> getExePath)
    {
        _store = store;
        _applyLive = applyLive;
        _getExePath = getExePath;

        Text = "Win Precision Touchpad — 設定";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(480, 580);
        ShowInTaskbar = true;
        BackColor = PageBack;
        Font = new Font("Microsoft JhengHei UI", 9f);
        Padding = new Padding(16);

        var title = new Label
        {
            Text = "設定",
            AutoSize = true,
            Font = new Font("Microsoft JhengHei UI", 14f, FontStyle.Bold),
            ForeColor = AccentText,
            Location = new Point(20, 16)
        };

        var subtitle = new Label
        {
            Text = "三指拖曳會即時生效；按「儲存」先寫入檔案同開機自啟。",
            AutoSize = false,
            Width = 440,
            Height = 22,
            ForeColor = MutedText,
            Location = new Point(20, 46)
        };

        var dragGroup = CreateCardGroup("拖曳", new Point(16, 78), new Size(448, 268));
        var startupGroup = CreateCardGroup("啟動", new Point(16, 356), new Size(448, 88));
        var aboutGroup = CreateCardGroup("關於／路徑", new Point(16, 454), new Size(448, 56));

        // --- 拖曳 ---
        _enableDrag = new CheckBox
        {
            Text = "啟用三指拖曳",
            AutoSize = true,
            Location = new Point(16, 28),
            ForeColor = AccentText
        };

        var enableHelp = MakeHelpLabel(
            "請喺 Windows「觸控板」設定將三指手勢設為「無／Nothing」，否則系統手勢會同本 App 搶輸入。",
            new Point(16, 52),
            408,
            36);

        var sensLabel = new Label
        {
            Text = "敏感度",
            AutoSize = true,
            Location = new Point(16, 96),
            ForeColor = AccentText,
            Font = new Font("Microsoft JhengHei UI", 9f, FontStyle.Bold)
        };

        _sensitivity = new TrackBar
        {
            Minimum = 20,
            Maximum = 200,
            TickFrequency = 20,
            SmallChange = 5,
            LargeChange = 20,
            TickStyle = TickStyle.None,
            Width = 300,
            Height = 45,
            Location = new Point(16, 118),
            BackColor = CardBack
        };

        _sensitivityValue = new Label
        {
            AutoSize = true,
            Location = new Point(330, 128),
            ForeColor = AccentText,
            Font = new Font("Microsoft JhengHei UI", 10f, FontStyle.Bold)
        };

        var sensHelp = MakeHelpLabel(
            "HID 位移 × 此倍率（0.20–2.00）。預設 0.60；數值愈大拖曳愈快，唔跟系統游標速度。",
            new Point(16, 160),
            408,
            34);

        var distLabel = new Label
        {
            Text = "開始距離閾值（HID）",
            AutoSize = true,
            Location = new Point(16, 200),
            ForeColor = AccentText,
            Font = new Font("Microsoft JhengHei UI", 9f, FontStyle.Bold)
        };

        _startDistance = new NumericUpDown
        {
            DecimalPlaces = 0,
            Minimum = 0,
            Maximum = 200,
            Width = 80,
            Location = new Point(16, 222)
        };

        var distHelp = MakeHelpLabel(
            "三指按下後要滑過幾多 HID 單位先開始按住左鍵。愈大愈唔易誤觸，但開始拖會稍慢。",
            new Point(110, 222),
            314,
            36);

        dragGroup.Controls.AddRange(new Control[]
        {
            _enableDrag, enableHelp,
            sensLabel, _sensitivity, _sensitivityValue, sensHelp,
            distLabel, _startDistance, distHelp
        });

        // --- 啟動 ---
        _startWithWindows = new CheckBox
        {
            Text = "開機時自動啟動",
            AutoSize = true,
            Location = new Point(16, 28),
            ForeColor = AccentText
        };

        var startHelp = MakeHelpLabel(
            "寫入 HKCU\\…\\Run（只限目前使用者，唔使系統管理員）。按「儲存」先更新機碼。",
            new Point(16, 52),
            408,
            28);

        startupGroup.Controls.AddRange(new Control[] { _startWithWindows, startHelp });

        // --- 關於／路徑 ---
        _pathHint = new Label
        {
            AutoSize = false,
            Width = 416,
            Height = 28,
            Location = new Point(16, 24),
            ForeColor = MutedText,
            Font = new Font("Microsoft JhengHei UI", 8.25f)
        };
        aboutGroup.Controls.Add(_pathHint);

        // --- Buttons (own row; never under labels) ---
        var saveButton = new Button
        {
            Text = "儲存",
            DialogResult = DialogResult.None,
            Width = 100,
            Height = 32,
            Location = new Point(252, 528),
            FlatStyle = FlatStyle.System
        };
        saveButton.Click += (_, _) => Save();

        var closeButton = new Button
        {
            Text = "關閉",
            DialogResult = DialogResult.Cancel,
            Width = 100,
            Height = 32,
            Location = new Point(364, 528),
            FlatStyle = FlatStyle.System
        };
        // Form is shown with Show() (not ShowDialog), so DialogResult alone will not close.
        closeButton.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            title, subtitle,
            dragGroup, startupGroup, aboutGroup,
            saveButton, closeButton
        });

        AcceptButton = saveButton;
        CancelButton = closeButton;

        _enableDrag.CheckedChanged += (_, _) => OnEdited();
        _sensitivity.ValueChanged += (_, _) =>
        {
            UpdateSensitivityLabel();
            OnEdited();
        };
        _startDistance.ValueChanged += (_, _) => OnEdited();
        _startWithWindows.CheckedChanged += (_, _) => OnEdited();

        LoadFrom(current);
        _pathHint.Text = $"設定檔：{_store.FilePath}";
    }

    private static GroupBox CreateCardGroup(string title, Point location, Size size)
    {
        return new GroupBox
        {
            Text = title,
            Location = location,
            Size = size,
            BackColor = CardBack,
            ForeColor = AccentText,
            FlatStyle = FlatStyle.System,
            Padding = new Padding(12, 8, 12, 8)
        };
    }

    private static Label MakeHelpLabel(string text, Point location, int width, int height)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Width = width,
            Height = height,
            Location = location,
            ForeColor = MutedText,
            Font = new Font("Microsoft JhengHei UI", 8.25f)
        };
    }

    private void LoadFrom(AppSettings s)
    {
        _suppressEvents = true;
        try
        {
            s.Clamp();
            _enableDrag.Checked = s.DragEnabled;
            var sensHundredths = (int)Math.Round(s.Sensitivity * 100f);
            _sensitivity.Value = Math.Clamp(sensHundredths, _sensitivity.Minimum, _sensitivity.Maximum);
            _startDistance.Value = (decimal)Math.Clamp(s.StartDistanceThreshold, 0f, 200f);
            _startWithWindows.Checked = s.StartWithWindows;
            UpdateSensitivityLabel();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private AppSettings ReadUi()
    {
        var s = new AppSettings
        {
            SettingsVersion = AppSettings.CurrentVersion,
            DragEnabled = _enableDrag.Checked,
            Sensitivity = _sensitivity.Value / 100f,
            StartDistanceThreshold = (float)_startDistance.Value,
            StartWithWindows = _startWithWindows.Checked
        };
        s.Clamp();
        return s;
    }

    private void UpdateSensitivityLabel()
    {
        _sensitivityValue.Text = $"{_sensitivity.Value / 100.0:0.00}";
    }

    private void OnEdited()
    {
        if (_suppressEvents) return;
        _applyLive(ReadUi());
    }

    private void Save()
    {
        var settings = ReadUi();
        _applyLive(settings);
        _store.Save(settings);

        try
        {
            AutostartHelper.SetEnabled(settings.StartWithWindows, _getExePath());
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"無法更新開機自啟：{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(this, "已儲存設定。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Refresh controls from an external settings snapshot (e.g. tray toggle).</summary>
    public void SyncFrom(AppSettings settings) => LoadFrom(settings);
}

