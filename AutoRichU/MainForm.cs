using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Threading.Tasks;

public class MainForm : Form
{
    #region UI ��ɫ�����嶨��
    private readonly Color bgColor = Color.FromArgb(30, 30, 30);
    private readonly Color sidebarColor = Color.FromArgb(45, 45, 48);
    private readonly Color contentColor = Color.FromArgb(37, 37, 40);
    private readonly Color controlBgColor = Color.FromArgb(51, 51, 55);
    private readonly Color controlBorderColor = Color.FromArgb(80, 80, 85);
    private readonly Color textColor = Color.FromArgb(240, 240, 240);
    private readonly Color accentColor = Color.FromArgb(0, 122, 204);
    private readonly Color accentHoverColor = Color.FromArgb(28, 151, 234);
    private readonly Color sidebarSelectedColor = Color.FromArgb(60, 60, 65);
    private readonly Color statusSuccessColor = Color.FromArgb(100, 210, 100);
    private readonly Color statusErrorColor = Color.FromArgb(210, 90, 90);
    private readonly Color statusWarningColor = Color.FromArgb(230, 180, 80);
    private readonly Font mainFont = new Font("Microsoft YaHei", 9F);
    private readonly Font titleFont = new Font("Microsoft YaHei", 22F, FontStyle.Bold);
    private readonly Font buttonFont = new Font("Microsoft YaHei", 11F);
    private readonly Font sidebarFont = new Font("Microsoft YaHei", 11F);
    private readonly Font settingsLabelFont = new Font("Microsoft YaHei", 10.5F);
    private readonly Font settingsControlFont = new Font("Microsoft YaHei", 11F);
    private readonly Font settingsGroupFont = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
    private readonly Font statusFont = new Font("Microsoft YaHei", 13F);
    private readonly Font footerFont = new Font("Microsoft YaHei", 9F);
    #endregion

    #region UI �ؼ�����
    private Button adjustWindowButton;
    private Button restoreWindowButton;
    private Button startBuyingButton;
    private Label statusLabel;
    private Panel sidebarPanel;
    private Button sidebarAdjustWindowButton;
    private Button sidebarAutoBuyButton;
    private Button sidebarSettingsButton;
    private LinkLabel sidebarHomepageLink;
    private LinkLabel sidebarProjectLink;
    private Panel mainContentPanel;
    private Panel adjustWindowPanel;
    private Panel autoBuyPanel;
    private Panel settingsPanel;
    private Button setPriceButton;
    private TextBox priceThresholdTextBox;
    private ComboBox quantityComboBox;
    private TextBox stopAfterPurchasesTextBox;
    private Button setStopPurchasesButton;
    private CheckBox enableTimerCheckBox;
    private TextBox startTimeTextBox;
    private TextBox stopTimeTextBox;
    private Button setTimerButton;
    private Label watermarkLabel;
    private ToolTip startButtonToolTip;
    #endregion

    #region �����߼���״̬
    private readonly WindowAdjuster windowAdjuster;
    private readonly AutoBuyer autoBuyer;
    private AppSettings settings;
    private DateTime? scheduledStartTime;
    private DateTime? scheduledStopTime;
    private bool hasStartedToday = false;
    private bool hasStoppedToday = false;
    private bool isTimerEnabled = false;
    private System.Windows.Forms.Timer timer;
    #endregion

    public MainForm()
    {
        this.Font = mainFont;
        LoadSettings();
        InitializeComponents();
        windowAdjuster = new WindowAdjuster();
        if (settings == null) settings = new AppSettings();
        autoBuyer = new AutoBuyer(settings);
        ApplySettingsToUI();
        InitializeExternalDependencies();
        autoBuyer.Stopped += HandleAutoBuyerStopped;
        timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += Timer_Tick;
        timer.Start();
        if (isTimerEnabled)
        {
            SetScheduledTimes();
        }
        else
        {
            UpdateStatusLabelSafe("��ʱ���ѽ���", textColor);
        }
        this.FormClosing += MainForm_FormClosing;
        Console.WriteLine("MainForm ��ʼ����ɡ�");
    }

    #region ��ʼ���� UI ����
    private void InitializeComponents()
    {
        this.Text = "AUT0RICH Lite";
        this.Size = new Size(1100, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.BackColor = bgColor;

        startButtonToolTip = new ToolTip();

        Label titleLabel = new Label { Text = "AUT0RICH Lite", Font = titleFont, ForeColor = textColor, TextAlign = ContentAlignment.MiddleLeft, Size = new Size(400, 50), Location = new Point(30, 20) };

        sidebarPanel = new Panel { Location = new Point(0, 80), Size = new Size(250, this.ClientSize.Height - 80 - 50), BackColor = sidebarColor, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom };
        sidebarAdjustWindowButton = CreateSidebarButton("��������", new Point(15, 20));
        sidebarAdjustWindowButton.Click += (s, e) => SwitchPanel(adjustWindowPanel, sidebarAdjustWindowButton);
        sidebarAutoBuyButton = CreateSidebarButton("�Զ�����", new Point(15, 80));
        sidebarAutoBuyButton.Click += (s, e) => SwitchPanel(autoBuyPanel, sidebarAutoBuyButton);
        sidebarSettingsButton = CreateSidebarButton("��������", new Point(15, 140));
        sidebarSettingsButton.Click += (s, e) => SwitchPanel(settingsPanel, sidebarSettingsButton);

        sidebarHomepageLink = new LinkLabel { Text = "Bվ��ҳ", Font = sidebarFont, LinkColor = accentColor, ActiveLinkColor = accentHoverColor, VisitedLinkColor = accentColor, Location = new Point(15, 220), Size = new Size(220, 30), TextAlign = ContentAlignment.MiddleLeft };
        sidebarHomepageLink.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo("https://b23.tv/zmdKRcb") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"�޷�������: {ex.Message}"); } };
        sidebarProjectLink = new LinkLabel { Text = "GitHub ��Ŀ", Font = sidebarFont, LinkColor = accentColor, ActiveLinkColor = accentHoverColor, VisitedLinkColor = accentColor, Location = new Point(15, 260), Size = new Size(220, 30), TextAlign = ContentAlignment.MiddleLeft };
        sidebarProjectLink.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo("https://github.com/BDaway/AUT0RICH") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"�޷�������: {ex.Message}"); } };

        sidebarPanel.Controls.AddRange(new Control[] { sidebarAdjustWindowButton, sidebarAutoBuyButton, sidebarSettingsButton, sidebarHomepageLink, sidebarProjectLink });

        mainContentPanel = new Panel { Location = new Point(250, 80), Size = new Size(this.ClientSize.Width - 250, this.ClientSize.Height - 80 - 50), BackColor = contentColor, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom, Padding = new Padding(30) };

        adjustWindowPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        adjustWindowButton = CreateStyledButton("������ǰ����", new Point(40, 40), new Size(200, 60));
        adjustWindowButton.Click += AdjustWindowButton_Click;
        restoreWindowButton = CreateStyledButton("�ָ��ϴδ���", new Point(280, 40), new Size(200, 60));
        restoreWindowButton.Click += RestoreWindowButton_Click;
        ToolTip restoreToolTip = new ToolTip(); restoreToolTip.SetToolTip(restoreWindowButton, "���ϴε������Ĵ��ڻָ���ԭʼ��С��λ�ú���ʽ");
        adjustWindowPanel.Controls.AddRange(new Control[] { adjustWindowButton, restoreWindowButton });

        autoBuyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };
        startBuyingButton = CreateStyledButton("��ʼ���� (�� F12 ֹͣ)", new Point(40, 40), new Size(280, 60));
        startBuyingButton.Tag = "start";
        startBuyingButton.Click += StartBuyingButton_Click;
        statusLabel = new Label { Text = "��ʼ����...", Font = statusFont, ForeColor = textColor, TextAlign = ContentAlignment.MiddleLeft, Size = new Size(mainContentPanel.Width - 60, 40), Location = new Point(40, 120), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        autoBuyPanel.Controls.AddRange(new Control[] { startBuyingButton, statusLabel });

        settingsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false, AutoScroll = true };

        GroupBox purchaseGroup = CreateSettingsGroup("�������", new Point(40, 30), new Size(settingsPanel.Width - 80, 220));
        purchaseGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        Label priceLabel = CreateSettingsLabel("��ߵ���:", new Point(20, 40));
        priceThresholdTextBox = CreateSettingsTextBox("10", new Point(180, 40), new Size(120, 30));
        ToolTip priceToolTip = new ToolTip(); priceToolTip.SetToolTip(priceThresholdTextBox, "���ڻ���ڴ˵���ʱ�ſ��ǹ���");
        setPriceButton = CreateSettingsButton("Ӧ��", new Point(320, 40));
        setPriceButton.Click += SetPriceButton_Click;
        Label quantityLabel = CreateSettingsLabel("��������:", new Point(20, 90));
        quantityComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = settingsControlFont, Location = new Point(180, 90), Size = new Size(220, 30), BackColor = controlBgColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        quantityComboBox.Items.AddRange(new string[] { "ÿ���� 1 ��", "ÿ���� 25 ��", "ÿ���� 100 ��", "ÿ���� 200 ��" });
        quantityComboBox.SelectedIndexChanged += QuantityComboBox_SelectedIndexChanged;
        ToolTip quantityToolTip = new ToolTip(); quantityToolTip.SetToolTip(quantityComboBox, "ѡ��ÿ�ε�����������");
        Label stopLabel = CreateSettingsLabel("�������:", new Point(20, 140));
        stopAfterPurchasesTextBox = CreateSettingsTextBox("0", new Point(180, 140), new Size(120, 30));
        ToolTip stopToolTip = new ToolTip(); stopToolTip.SetToolTip(stopAfterPurchasesTextBox, "�ɹ�������ٴκ��Զ�ֹͣ (0 ��ʾ������)");
        setStopPurchasesButton = CreateSettingsButton("Ӧ��", new Point(320, 140));
        setStopPurchasesButton.Click += SetStopPurchasesButton_Click;
        purchaseGroup.Controls.AddRange(new Control[] { priceLabel, priceThresholdTextBox, setPriceButton, quantityLabel, quantityComboBox, stopLabel, stopAfterPurchasesTextBox, setStopPurchasesButton });

        GroupBox timerGroup = CreateSettingsGroup("��ʱ����", new Point(40, 270), new Size(settingsPanel.Width - 80, 170));
        timerGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        enableTimerCheckBox = new CheckBox { Text = "���ö�ʱ�Զ�����", Font = settingsControlFont, ForeColor = textColor, Location = new Point(20, 40), Size = new Size(220, 30), BackColor = Color.Transparent };
        enableTimerCheckBox.CheckedChanged += EnableTimerCheckBox_CheckedChanged;
        ToolTip enableTimerToolTip = new ToolTip(); enableTimerToolTip.SetToolTip(enableTimerCheckBox, "��ѡ�󣬽���ָ��ʱ���Զ���ʼ��ֹͣ����");
        Label startTimeLabel = CreateSettingsLabel("����ʱ�� (HH:mm:ss):", new Point(20, 90), new Size(180, 30));
        startTimeTextBox = CreateSettingsTextBox("08:00:00", new Point(220, 90), new Size(120, 30));
        ToolTip startTimeToolTip = new ToolTip(); startTimeToolTip.SetToolTip(startTimeTextBox, "�Զ�����Ŀ���ʱ�� (24Сʱ��)");
        Label stopTimeLabel = CreateSettingsLabel("�ر�ʱ�� (HH:mm:ss):", new Point(360, 90), new Size(180, 30));
        stopTimeTextBox = CreateSettingsTextBox("23:00:00", new Point(560, 90), new Size(120, 30));
        ToolTip stopTimeToolTip = new ToolTip(); stopTimeToolTip.SetToolTip(stopTimeTextBox, "�Զ�����Ĺر�ʱ�� (24Сʱ��)");
        setTimerButton = CreateSettingsButton("Ӧ��", new Point(700, 90));
        setTimerButton.Click += SetTimerButton_Click;
        timerGroup.Controls.AddRange(new Control[] { enableTimerCheckBox, startTimeLabel, startTimeTextBox, stopTimeLabel, stopTimeTextBox, setTimerButton });

        settingsPanel.Controls.AddRange(new Control[] { purchaseGroup, timerGroup });
        mainContentPanel.Controls.AddRange(new Control[] { adjustWindowPanel, autoBuyPanel, settingsPanel });
        adjustWindowPanel.Visible = true;

        watermarkLabel = new Label { Text = "��Դ��Ŀ", Font = footerFont, ForeColor = Color.FromArgb(160, 160, 160), TextAlign = ContentAlignment.MiddleCenter, Size = new Size(this.ClientSize.Width, 24), Location = new Point(0, this.ClientSize.Height - 45), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

        this.Controls.AddRange(new Control[] { titleLabel, sidebarPanel, mainContentPanel, watermarkLabel });
        SwitchPanel(adjustWindowPanel, sidebarAdjustWindowButton);
    }

    private Button CreateStyledButton(string text, Point location, Size size)
    {
        Button button = new Button { Text = text, Font = buttonFont, Location = location, Size = size, BackColor = accentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter };
        button.FlatAppearance.BorderSize = 0; button.FlatAppearance.MouseOverBackColor = accentHoverColor; button.FlatAppearance.MouseDownBackColor = accentColor;
        return button;
    }

    private Button CreateSidebarButton(string text, Point location)
    {
        Button button = new Button { Text = "   " + text, Font = sidebarFont, Location = location, Size = new Size(sidebarPanel.Width - 30, 50), BackColor = sidebarColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        button.FlatAppearance.BorderSize = 0; button.FlatAppearance.MouseOverBackColor = sidebarSelectedColor; button.FlatAppearance.MouseDownBackColor = sidebarSelectedColor;
        return button;
    }

    private Label CreateSettingsLabel(string text, Point location, Size? size = null) => new Label { Text = text, Font = settingsLabelFont, ForeColor = textColor, Location = location, Size = size ?? new Size(140, 26), AutoSize = !size.HasValue, TextAlign = ContentAlignment.MiddleLeft };

    private TextBox CreateSettingsTextBox(string defaultText, Point location, Size? size = null) => new TextBox { Text = defaultText, Font = settingsControlFont, Location = location, Size = size ?? new Size(120, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle };

    private Button CreateSettingsButton(string text, Point location)
    {
        Button button = CreateStyledButton(text, location, new Size(90, 35));
        button.BackColor = Color.FromArgb(80, 80, 85);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(100, 100, 105); button.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 85);
        button.Font = new Font("Microsoft YaHei", 9.5F);
        return button;
    }

    private GroupBox CreateSettingsGroup(string title, Point location, Size size)
    {
        GroupBox groupBox = new GroupBox { Text = title, Location = location, Size = size, ForeColor = accentColor, Font = settingsGroupFont };
        return groupBox;
    }

    private void SwitchPanel(Panel panelToShow, Button selectedButton)
    {
        foreach (Control c in mainContentPanel.Controls) if (c is Panel panel) panel.Visible = false;
        panelToShow.Visible = true;
        foreach (Control c in sidebarPanel.Controls)
        {
            if (c is Button btn)
            {
                btn.BackColor = (btn == selectedButton) ? sidebarSelectedColor : sidebarColor;
                btn.ForeColor = textColor;
            }
        }
    }
    #endregion

    #region ���ü���/�����Ӧ��
    private void LoadSettings()
    {
        string configFile = AppSettings.ConfigFilePath;
        if (File.Exists(configFile))
        {
            try
            {
                string json = File.ReadAllText(configFile);
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                Console.WriteLine($"�����Ѵ� {configFile} ���ء�");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"����: ���������ļ� {configFile} ʧ��: {ex.Message}");
                MessageBox.Show($"���������ļ�ʧ��: {ex.Message}\n��ʹ��Ĭ�����á�", "���ô���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                settings = new AppSettings();
            }
        }
        else
        {
            Console.WriteLine("�����ļ������ڣ�ʹ��Ĭ�����á�");
            settings = new AppSettings();
        }
    }

    private void SaveSettings()
    {
        UpdateSettingsFromUI();
        string configFile = AppSettings.ConfigFilePath;
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(configFile, json);
            Console.WriteLine($"�����ѱ��浽 {configFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"����: ���������ļ� {configFile} ʧ��: {ex.Message}");
        }
    }

    private void ApplySettingsToUI()
    {
        if (settings == null) settings = new AppSettings();

        priceThresholdTextBox.Text = settings.PriceThreshold.ToString();
        stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();

        switch (settings.PurchaseQuantity)
        {
            case 1: quantityComboBox.SelectedIndex = 0; break;
            case 25: quantityComboBox.SelectedIndex = 1; break;
            case 100: quantityComboBox.SelectedIndex = 2; break;
            case 200: default: quantityComboBox.SelectedIndex = 3; break;
        }
        if (quantityComboBox.SelectedIndex < 0 && quantityComboBox.Items.Count > 0)
        {
            quantityComboBox.SelectedIndex = 3;
        }

        enableTimerCheckBox.Checked = settings.IsTimerEnabled;
        startTimeTextBox.Text = settings.StartTimeString;
        stopTimeTextBox.Text = settings.StopTimeString;
        isTimerEnabled = settings.IsTimerEnabled;

        ToolTip adjustToolTip = new ToolTip();
        adjustToolTip.SetToolTip(adjustWindowButton, $"�Ƴ���ǰ���㴰�ڵı߿򣬲������� {settings.TargetWindowWidth}x{settings.TargetWindowHeight} @ ({settings.TargetWindowX},{settings.TargetWindowY}) (���ȵ��Ŀ�괰��)");
        startButtonToolTip.SetToolTip(startBuyingButton, "��ʼ�Զ����� (�� F12 ֹͣ)");
    }

    private void UpdateSettingsFromUI()
    {
        if (settings == null) return;

        if (int.TryParse(priceThresholdTextBox.Text, out int price) && price >= 0)
            settings.PriceThreshold = price;
        else
            priceThresholdTextBox.Text = settings.PriceThreshold.ToString();

        if (int.TryParse(stopAfterPurchasesTextBox.Text, out int stopCount) && stopCount >= 0)
            settings.StopAfterPurchases = stopCount;
        else
            stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();

        switch (quantityComboBox.SelectedIndex)
        {
            case 0: settings.PurchaseQuantity = 1; break;
            case 1: settings.PurchaseQuantity = 25; break;
            case 2: settings.PurchaseQuantity = 100; break;
            case 3: default: settings.PurchaseQuantity = 200; break;
        }

        settings.IsTimerEnabled = enableTimerCheckBox.Checked;
        if (TimeSpan.TryParseExact(startTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            settings.StartTimeString = startTimeTextBox.Text;
        else
            startTimeTextBox.Text = settings.StartTimeString;

        if (TimeSpan.TryParseExact(stopTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            settings.StopTimeString = stopTimeTextBox.Text;
        else
            stopTimeTextBox.Text = settings.StopTimeString;
    }

    private void ApplySettingsFromUIToAutoBuyer()
    {
        if (autoBuyer == null) return;
        autoBuyer.SetPriceThreshold(settings.PriceThreshold);
        autoBuyer.SetStopAfterPurchases(settings.StopAfterPurchases);
        autoBuyer.SetPurchaseQuantity(settings.PurchaseQuantity);
    }
    #endregion

    #region �ⲿ������ʼ��
    private void InitializeExternalDependencies()
    {
        bool logitechOk = false;
        bool keyboardHookOk = false;
        string currentStatus = "";

        try
        {
            LogitechMouse.DllPath = settings.LogitechDllPath;
            LogitechMouse.MoveSpeedFactor = settings.MoveSpeedFactor;
            LogitechMouse.RandomRangeNear = settings.RandomRangeNear;
            LogitechMouse.RandomRangeShake = settings.RandomRangeShake;
            LogitechMouse.MaxMoveTimeMs = settings.MaxMoveTimeMs;
            LogitechMouse.Initialize();
            logitechOk = true;
            currentStatus = "�޼����� OK";
            UpdateStatusLabelSafe(currentStatus, statusSuccessColor);
        }
        catch (Exception ex)
        {
            logitechOk = false;
            HandleInitError("�޼�����", ex);
            if (startBuyingButton != null) startBuyingButton.Enabled = false;
            currentStatus = "�޼�����ʧ��!";
        }

        try
        {
            KeyboardHook.StopKey = settings.StopKey;
            KeyboardHook.Initialize();
            keyboardHookOk = true;
            currentStatus += " | ���̹��� OK";
            UpdateStatusLabelSafe(currentStatus, logitechOk ? statusSuccessColor : statusWarningColor);
        }
        catch (Exception ex)
        {
            keyboardHookOk = false;
            HandleInitError("���̹���", ex);
            currentStatus += " | ���̹���ʧ��!";
            UpdateStatusLabelSafe(currentStatus, statusErrorColor);
        }

        if (logitechOk && keyboardHookOk)
        {
            UpdateStatusLabelSafe("��ʼ����ɣ�׼��������", textColor);
        }
        else if (!logitechOk)
        {
            UpdateStatusLabelSafe("�޼�����ʧ��! �����ܲ����á�", statusErrorColor);
            if (startBuyingButton != null) startBuyingButton.Enabled = false;
        }
    }

    private void HandleInitError(string componentName, Exception ex)
    {
        Console.WriteLine($"����: ��ʼ�� {componentName} ʧ��: {ex.Message}");
        string errorMsg = $"{componentName} ��ʼ��ʧ��: {ex.Message}\n\n";
        if (ex is FileNotFoundException fnfEx) errorMsg += $"��ȷ���ļ�����: {fnfEx.FileName}\n";
        else if (ex is Win32Exception w32Ex) errorMsg += $"ϵͳ������: {w32Ex.NativeErrorCode}\n";
        else if (ex is DllNotFoundException) errorMsg += $"�޷��ҵ������ DLL �ļ���\n";
        else if (ex is BadImageFormatException) errorMsg += $"DLL �ļ���ʽ������뵱ǰϵͳ�ܹ� (x86/x64) �����ݡ�\n";

        errorMsg += "����������������á�";
        MessageBox.Show(errorMsg, $"{componentName} ��ʼ������", MessageBoxButtons.OK, MessageBoxIcon.Error);

        string failureText = $"{componentName} ����ʧ��!";
        if (statusLabel != null)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.BeginInvoke(new Action(() => UpdateStatusLabelWithError(failureText)));
            }
            else
            {
                UpdateStatusLabelWithError(failureText);
            }
        }
    }

    private void UpdateStatusLabelWithError(string newError)
    {
        string currentText = statusLabel.Text ?? "";
        Color currentColor = statusLabel.ForeColor;

        if (currentText.Contains("��ʼ�����") || currentText == "��ʼ����...") currentText = "";

        string updatedText;
        if (string.IsNullOrEmpty(currentText) || currentText.Contains("OK"))
        {
            updatedText = newError;
        }
        else if (!currentText.Contains(newError))
        {
            updatedText = currentText + " | newError";
        }
        else
        {
            updatedText = currentText;
        }

        UpdateStatusLabelSafe(updatedText, statusErrorColor);
    }
    #endregion

    #region �¼�������
    private void AdjustWindowButton_Click(object sender, EventArgs e)
    {
        windowAdjuster.TargetWidth = settings.TargetWindowWidth;
        windowAdjuster.TargetHeight = settings.TargetWindowHeight;
        windowAdjuster.TargetX = settings.TargetWindowX;
        windowAdjuster.TargetY = settings.TargetWindowY;
        try
        {
            windowAdjuster.AdjustFocusedWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"��������ʱ��������: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreWindowButton_Click(object sender, EventArgs e)
    {
        try
        {
            windowAdjuster.RestoreOriginalWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"�ָ�����ʱ��������: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StartBuyingButton_Click(object sender, EventArgs e)
    {
        if (!autoBuyer.IsRunning)
        {
            if (!LogitechMouse.IsInitialized)
            {
                MessageBox.Show("�޼�����δ��ʼ�������ʧ�ܣ��޷���ʼ����", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatusLabelSafe("�޼�����ʧ��! �޷�������", statusErrorColor);
                return;
            }

            UpdateSettingsFromUI();
            ApplySettingsFromUIToAutoBuyer();

            try
            {
                autoBuyer.Start();
                UpdateStartButtonState(true);
                UpdateStatusLabelSafe("������...", statusSuccessColor);
            }
            catch (InvalidOperationException opEx)
            {
                MessageBox.Show($"��������ʧ��: {opEx.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStartButtonState(false);
                UpdateStatusLabelSafe("����ʧ��", statusErrorColor);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"��������ʱ����δ֪����: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStartButtonState(false);
                UpdateStatusLabelSafe("�����쳣", statusErrorColor);
            }
        }
        else
        {
            UpdateStatusLabelSafe("����ֹͣ...", statusWarningColor);
            autoBuyer.Stop();
        }
    }

    private void SetPriceButton_Click(object sender, EventArgs e)
    {
        if (int.TryParse(priceThresholdTextBox.Text, out int val) && val >= 0)
        {
            settings.PriceThreshold = val;
            autoBuyer.SetPriceThreshold(val);
            MessageBox.Show($"��߹��򵥼��Ѹ���Ϊ {val}", "���óɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("��������Ч�ķǸ������۸�", "�������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            priceThresholdTextBox.Text = settings.PriceThreshold.ToString();
        }
    }

    private void QuantityComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        int qty = 200;
        switch (quantityComboBox.SelectedIndex)
        {
            case 0: qty = 1; break;
            case 1: qty = 25; break;
            case 2: qty = 100; break;
            case 3: qty = 200; break;
        }
        settings.PurchaseQuantity = qty;
        autoBuyer.SetPurchaseQuantity(qty);
        Console.WriteLine($"��������������Ϊ: {qty}");
    }

    private void SetStopPurchasesButton_Click(object sender, EventArgs e)
    {
        if (int.TryParse(stopAfterPurchasesTextBox.Text, out int val) && val >= 0)
        {
            settings.StopAfterPurchases = val;
            autoBuyer.SetStopAfterPurchases(val);
            MessageBox.Show($"���ڹ��� {val} �κ�ֹͣ (0 ��ʾ����)", "���óɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("��������Ч�ķǸ�����������", "�������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();
        }
    }

    private void EnableTimerCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        isTimerEnabled = enableTimerCheckBox.Checked;
        settings.IsTimerEnabled = isTimerEnabled;
        Console.WriteLine($"��ʱ��״̬: {(isTimerEnabled ? "����" : "����")}");
        if (isTimerEnabled)
        {
            SetScheduledTimes();
        }
        else
        {
            scheduledStartTime = null;
            scheduledStopTime = null;
            UpdateStatusLabelSafe("��ʱ���ѽ���", textColor);
        }
    }

    private void SetTimerButton_Click(object sender, EventArgs e)
    {
        settings.StartTimeString = startTimeTextBox.Text;
        settings.StopTimeString = stopTimeTextBox.Text;

        if (SetScheduledTimes())
        {
            string statusMsg = settings.IsTimerEnabled ? GetTimerStatusString() : "��ʱ���ѽ��� (�빴ѡ���ø�ѡ��)";
            MessageBox.Show($"��ʱ������Ӧ�á�\n{statusMsg}", "��ʱ����", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void HandleAutoBuyerStopped()
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(UpdateUIOnStop));
        }
        else
        {
            UpdateUIOnStop();
        }
    }

    private void UpdateUIOnStop()
    {
        UpdateStartButtonState(false);
        if (isTimerEnabled)
        {
            UpdateStatusLabelSafe(GetTimerStatusString(), textColor);
        }
        else
        {
            UpdateStatusLabelSafe("��ֹͣ", statusErrorColor);
        }
        Console.WriteLine("UI �Ѹ���Ϊֹͣ״̬��");
    }
    #endregion

    #region UI ���¸�������
    private void UpdateStatusLabelSafe(string text, Color color)
    {
        if (statusLabel == null || statusLabel.IsDisposed) return;

        if (statusLabel.InvokeRequired)
        {
            statusLabel.BeginInvoke(new Action(() =>
            {
                statusLabel.Text = text;
                statusLabel.ForeColor = color;
            }));
        }
        else
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
        }
    }

    private void UpdateStartButtonState(bool isRunning)
    {
        if (startBuyingButton == null || startBuyingButton.IsDisposed) return;

        if (startBuyingButton.InvokeRequired)
        {
            startBuyingButton.BeginInvoke(new Action(() => UpdateStartButtonState(isRunning)));
            return;
        }

        if (isRunning)
        {
            startBuyingButton.Text = "ֹͣ���� (�� F12 ֹͣ)";
            startBuyingButton.Tag = "stop";
            startBuyingButton.BackColor = statusErrorColor;
            startBuyingButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 110, 110);
            startBuyingButton.FlatAppearance.MouseDownBackColor = statusErrorColor;
            startButtonToolTip.SetToolTip(startBuyingButton, "ֹͣ�Զ����� (�� F12 ֹͣ)");
        }
        else
        {
            startBuyingButton.Text = "��ʼ���� (�� F12 ֹͣ)";
            startBuyingButton.Tag = "start";
            startBuyingButton.Enabled = LogitechMouse.IsInitialized;
            startBuyingButton.BackColor = accentColor;
            startBuyingButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
            startBuyingButton.FlatAppearance.MouseDownBackColor = accentColor;
            startButtonToolTip.SetToolTip(startBuyingButton, "��ʼ�Զ����� (�� F12 ֹͣ)");
        }
    }
    #endregion

    #region ��ʱ���߼�
    private bool SetScheduledTimes()
    {
        if (!TimeSpan.TryParseExact(startTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out TimeSpan startTimeOfDay))
        {
            MessageBox.Show("��������Ч�Ŀ���ʱ�� (HH:mm:ss ��ʽ)", "�������", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        if (!TimeSpan.TryParseExact(stopTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out TimeSpan stopTimeOfDay))
        {
            MessageBox.Show("��������Ч�Ĺر�ʱ�� (HH:mm:ss ��ʽ)", "�������", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        DateTime now = DateTime.Now;
        DateTime today = now.Date;

        DateTime potentialStartTime = today.Add(startTimeOfDay);
        DateTime potentialStopTime = today.Add(stopTimeOfDay);

        scheduledStartTime = (now < potentialStartTime) ? potentialStartTime : potentialStartTime.AddDays(1);
        DateTime stopBaseDate = scheduledStartTime.Value.Date;
        scheduledStopTime = (stopTimeOfDay <= startTimeOfDay) ? stopBaseDate.AddDays(1).Add(stopTimeOfDay) : stopBaseDate.Add(stopTimeOfDay);

        hasStartedToday = false;
        hasStoppedToday = false;

        Console.WriteLine($"��ʱʱ�������¼���: �´ο��� {scheduledStartTime:G}, �´ιر� {scheduledStopTime:G}");
        if (isTimerEnabled)
        {
            UpdateStatusLabelSafe(GetTimerStatusString(), textColor);
        }

        return true;
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        if (!isTimerEnabled || !scheduledStartTime.HasValue || !scheduledStopTime.HasValue)
        {
            return;
        }

        DateTime now = DateTime.Now;

        if (now >= scheduledStopTime.Value && hasStoppedToday)
        {
            Console.WriteLine($"��⵽�ѹ��ƻ��ر�ʱ�� ({scheduledStopTime.Value:G})���ҽ�����ֹͣ�����¼�����һ����...");
            SetScheduledTimes();
        }

        if (!hasStartedToday && !autoBuyer.IsRunning && now >= scheduledStartTime.Value && now < scheduledStopTime.Value)
        {
            Console.WriteLine($"����Ԥ������ʱ�� {scheduledStartTime.Value:T}��������������...");
            UpdateSettingsFromUI();
            ApplySettingsFromUIToAutoBuyer();
            try
            {
                autoBuyer.Start();
                if (autoBuyer.IsRunning)
                {
                    UpdateStartButtonState(true);
                    UpdateStatusLabelSafe($"������ (��ʱ������ {scheduledStartTime.Value:T})", statusSuccessColor);
                    hasStartedToday = true;
                    hasStoppedToday = false;
                    Console.WriteLine($"��ʱ�����ɹ�����һ���ر�ʱ��: {scheduledStopTime.Value:G}");
                }
                else
                {
                    UpdateStatusLabelSafe($"��ʱ����ʧ�� (�ڲ�����)", statusErrorColor);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��ʱ����ʧ��: {ex.Message}");
                UpdateStatusLabelSafe($"��ʱ����ʧ��: {ex.Message}", statusErrorColor);
                UpdateStartButtonState(false);
            }
        }

        if (autoBuyer.IsRunning && now >= scheduledStopTime.Value && !hasStoppedToday)
        {
            Console.WriteLine($"����Ԥ���ر�ʱ�� {scheduledStopTime.Value:T}������ֹͣ����...");
            try
            {
                autoBuyer.Stop();
                Task.Delay(100).Wait();
                if (!autoBuyer.IsRunning)
                {
                    UpdateStartButtonState(false);
                    UpdateStatusLabelSafe($"��ֹͣ (��ʱ�ر��� {scheduledStopTime.Value:T})", statusWarningColor);
                    hasStoppedToday = true;
                    SetScheduledTimes();
                    Console.WriteLine($"��ʱֹͣ�ɹ�����һ����ʱ��: {scheduledStartTime.Value:G}");
                }
                else
                {
                    UpdateStatusLabelSafe("��ʱֹͣʧ�� (��������)", statusErrorColor);
                    Console.WriteLine("��ʱֹͣ������ɣ��� AutoBuyer �������С�");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��ʱֹͣʱ��������: {ex.Message}");
                UpdateStatusLabelSafe($"��ʱֹͣʱ����: {ex.Message}", statusErrorColor);
                SetScheduledTimes();
            }
        }

        if (isTimerEnabled && !autoBuyer.IsRunning && !hasStartedToday)
        {
            UpdateStatusLabelSafe(GetTimerStatusString(), textColor);
        }
    }

    private string GetTimerStatusString()
    {
        if (!isTimerEnabled) return "��ʱ���ѽ���";
        if (scheduledStartTime.HasValue && scheduledStopTime.HasValue)
        {
            return $"��ʱ������: �´����� {scheduledStartTime.Value:T}, �´�ֹͣ {scheduledStopTime.Value:T}";
        }
        return "��ʱ�����ã��ȴ�����ʱ��...";
    }
    #endregion

    #region ����رպ�����
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        Console.WriteLine("Ӧ�ó���ر���...");

        try { timer?.Stop(); timer?.Dispose(); } catch (Exception ex) { Console.WriteLine($"�ر� Timer ʱ����: {ex.Message}"); }

        try
        {
            if (autoBuyer != null && autoBuyer.IsRunning)
            {
                Console.WriteLine("����ֹͣ AutoBuyer...");
                autoBuyer.Stop();
                Task.Delay(300).Wait();
            }
            autoBuyer?.Dispose();
            Console.WriteLine("AutoBuyer ��ֹͣ���ͷš�");
        }
        catch (Exception ex) { Console.WriteLine($"ֹͣ�� Dispose AutoBuyer ʱ����: {ex.Message}"); }

        try { KeyboardHook.Cleanup(); } catch (Exception ex) { Console.WriteLine($"���� KeyboardHook ʱ����: {ex.Message}"); }
        try { LogitechMouse.Cleanup(); } catch (Exception ex) { Console.WriteLine($"���� LogitechMouse ʱ����: {ex.Message}"); }

        try { SaveSettings(); } catch (Exception ex) { Console.WriteLine($"��������ʱ����: {ex.Message}"); }

        Console.WriteLine("������ɣ������˳���");
    }
    #endregion

    #region ��������ڵ�
    [STAThread]
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("zh-CN");
        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
    #endregion
}