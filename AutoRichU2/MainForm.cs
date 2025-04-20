// MainForm.cs
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Threading; // 确保 System.Threading 已 using

public class MainForm : Form
{
    #region UI 颜色和字体定义
    // ... (保持不变) ...
    private readonly Color bgColor = Color.FromArgb(30, 30, 30);
    private readonly Color sidebarColor = Color.FromArgb(45, 45, 48);
    private readonly Color contentColor = Color.FromArgb(37, 37, 40);
    private readonly Color controlBgColor = Color.FromArgb(51, 51, 55);
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
    private readonly Font statusFont = new Font("Microsoft YaHei", 13F);
    private readonly Font footerFont = new Font("Microsoft YaHei", 9F);
    #endregion

    #region UI 控件声明
    // ... (保持不变) ...
    private System.Windows.Forms.Button adjustWindowButton;
    private System.Windows.Forms.Button restoreWindowButton;
    private System.Windows.Forms.Button startBuyingButton;
    private System.Windows.Forms.Label statusLabel;
    private System.Windows.Forms.Panel sidebarPanel;
    private System.Windows.Forms.Button sidebarAdjustWindowButton;
    private System.Windows.Forms.Button sidebarAutoBuyButton;
    private System.Windows.Forms.Button sidebarSettingsButton;
    private System.Windows.Forms.LinkLabel sidebarHomepageLink;
    private System.Windows.Forms.LinkLabel sidebarProjectLink;
    private System.Windows.Forms.Panel mainContentPanel;
    private System.Windows.Forms.Panel adjustWindowPanel;
    private System.Windows.Forms.Panel autoBuyPanel;
    private System.Windows.Forms.Panel settingsPanel;
    private System.Windows.Forms.Button setPriceButton;
    private System.Windows.Forms.TextBox priceThresholdTextBox;
    private System.Windows.Forms.Label priceThresholdLabel;
    private System.Windows.Forms.ComboBox quantityComboBox;
    private System.Windows.Forms.Label quantityLabel;
    private System.Windows.Forms.TextBox stopAfterPurchasesTextBox;
    private System.Windows.Forms.Label stopAfterPurchasesLabel;
    private System.Windows.Forms.Button setStopPurchasesButton;
    private System.Windows.Forms.CheckBox enableTimerCheckBox;
    private System.Windows.Forms.TextBox startTimeTextBox;
    private System.Windows.Forms.Label startTimeLabel;
    private System.Windows.Forms.TextBox stopTimeTextBox;
    private System.Windows.Forms.Label stopTimeLabel;
    private System.Windows.Forms.Button setTimerButton;
    private System.Windows.Forms.Label watermarkLabel;
    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.TextBox actionDelayMinTextBox;
    private System.Windows.Forms.Label actionDelayMinLabel;
    private System.Windows.Forms.TextBox actionDelayMaxTextBox;
    private System.Windows.Forms.Label actionDelayMaxLabel;
    private System.Windows.Forms.TextBox ocrRetryDelayMinTextBox;
    private System.Windows.Forms.Label ocrRetryDelayMinLabel;
    private System.Windows.Forms.TextBox ocrRetryDelayMaxTextBox;
    private System.Windows.Forms.Label ocrRetryDelayMaxLabel;
    private System.Windows.Forms.TextBox notificationDelayMinTextBox;
    private System.Windows.Forms.Label notificationDelayMinLabel;
    private System.Windows.Forms.TextBox notificationDelayMaxTextBox;
    private System.Windows.Forms.Label notificationDelayMaxLabel;
    private System.Windows.Forms.TextBox ocrCheckDelayMinTextBox;
    private System.Windows.Forms.Label ocrCheckDelayMinLabel;
    private System.Windows.Forms.TextBox ocrCheckDelayMaxTextBox;
    private System.Windows.Forms.Label ocrCheckDelayMaxLabel;
    private System.Windows.Forms.Button setDelaysButton;
    #endregion

    #region 核心逻辑和状态
    private readonly WindowAdjuster windowAdjuster;
    private readonly AutoBuyer autoBuyer;
    private AppSettings settings;
    private DateTime? scheduledStartTime;
    private DateTime? scheduledStopTime;
    private bool hasStartedToday = false;
    private bool hasStoppedToday = false;
    private bool isTimerEnabled = false;
    // private volatile bool manualStop = false; // <-- 已移除
    private System.Windows.Forms.Timer timer;
    private readonly object stateLock = new object(); // 用于同步状态访问
    private string lastStatusText = string.Empty; // 缓存上一次的状态文本
    #endregion

    public MainForm()
    {
        this.Font = mainFont;
        LoadSettings();
        InitializeComponents();
        windowAdjuster = new WindowAdjuster();
        if (settings == null) settings = new AppSettings();
        autoBuyer = new AutoBuyer(settings); // AutoBuyer 内部会订阅 F12 事件
        ApplySettingsToUI();
        Console.WriteLine("ApplySettingsToUI 完成，检查控件状态...");
        Console.WriteLine($"startTimeTextBox.Text: {startTimeTextBox?.Text}, stopTimeTextBox.Text: {stopTimeTextBox?.Text}");
        InitializeExternalDependencies(); // 初始化键盘钩子等
        autoBuyer.Stopped += HandleAutoBuyerStopped; // 订阅 AutoBuyer 停止事件
        timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += Timer_Tick;
        timer.Start();
        if (isTimerEnabled)
        {
            ResetTimerState();
        }
        else
        {
            UpdateStatusLabelSafe("定时器已禁用", textColor);
        }
        this.FormClosing += MainForm_FormClosing;
        Console.WriteLine("MainForm 初始化完成。");
    }

    #region 初始化和 UI 设置
    // ... (InitializeComponents, CreateSidebarButton, SwitchPanel, LoadSettings, SaveSettings, ApplySettingsToUI, UpdateSettingsFromUI, ApplySettingsFromUIToAutoBuyer 基本保持不变) ...
    private void InitializeComponents()
    {
        this.Text = "AUT0RICH Lite";
        this.Size = new Size(1100, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.BackColor = bgColor;
        toolTip = new System.Windows.Forms.ToolTip();

        System.Windows.Forms.Label titleLabel = new System.Windows.Forms.Label { Text = "AUT0RICH Lite", Font = titleFont, ForeColor = textColor, TextAlign = ContentAlignment.MiddleLeft, Size = new Size(400, 50), Location = new Point(30, 20) };
        sidebarPanel = new System.Windows.Forms.Panel { Location = new Point(0, 80), Size = new Size(250, this.ClientSize.Height - 80 - 50), BackColor = sidebarColor, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom };
        sidebarAdjustWindowButton = CreateSidebarButton("调整窗口", new Point(15, 20));
        sidebarAdjustWindowButton.Click += (s, e) => SwitchPanel(adjustWindowPanel, sidebarAdjustWindowButton);
        toolTip.SetToolTip(sidebarAdjustWindowButton, "调整目标窗口到指定大小和位置");
        sidebarAutoBuyButton = CreateSidebarButton("自动购买", new Point(15, 80));
        sidebarAutoBuyButton.Click += (s, e) => SwitchPanel(autoBuyPanel, sidebarAutoBuyButton);
        toolTip.SetToolTip(sidebarAutoBuyButton, "配置并运行自动购买功能");
        sidebarSettingsButton = CreateSidebarButton("程序设置", new Point(15, 140));
        sidebarSettingsButton.Click += (s, e) => SwitchPanel(settingsPanel, sidebarSettingsButton);
        toolTip.SetToolTip(sidebarSettingsButton, "调整定时和延迟等程序设置");
        sidebarHomepageLink = new System.Windows.Forms.LinkLabel { Text = "B站主页", Font = sidebarFont, LinkColor = accentColor, ActiveLinkColor = accentHoverColor, VisitedLinkColor = accentColor, Location = new Point(15, 220), Size = new Size(220, 30), TextAlign = ContentAlignment.MiddleLeft };
        sidebarHomepageLink.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo("https://b23.tv/zmdKRcb") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"无法打开链接: {ex.Message}"); } };
        toolTip.SetToolTip(sidebarHomepageLink, "访问作者的 B 站主页");
        sidebarProjectLink = new System.Windows.Forms.LinkLabel { Text = "项目地址", Font = sidebarFont, LinkColor = accentColor, ActiveLinkColor = accentHoverColor, VisitedLinkColor = accentColor, Location = new Point(15, 260), Size = new Size(220, 30), TextAlign = ContentAlignment.MiddleLeft };
        sidebarProjectLink.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo("https://github.com/BDaway/AUT0RICH") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"无法打开链接: {ex.Message}"); } };
        toolTip.SetToolTip(sidebarProjectLink, "查看项目源代码和文档");

        mainContentPanel = new System.Windows.Forms.Panel { Location = new Point(250, 80), Size = new Size(this.ClientSize.Width - 250, this.ClientSize.Height - 80 - 50), BackColor = contentColor, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };

        adjustWindowPanel = new System.Windows.Forms.Panel { Size = mainContentPanel.Size, BackColor = contentColor, Visible = false };
        autoBuyPanel = new System.Windows.Forms.Panel { Size = mainContentPanel.Size, BackColor = contentColor, Visible = true };
        settingsPanel = new System.Windows.Forms.Panel { Size = mainContentPanel.Size, BackColor = contentColor, Visible = false };

        adjustWindowButton = new System.Windows.Forms.Button { Text = "调整窗口", Font = buttonFont, Location = new Point(30, 30), Size = new Size(150, 40), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        adjustWindowButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        adjustWindowButton.Click += AdjustWindowButton_Click;
        toolTip.SetToolTip(adjustWindowButton, "将当前活动窗口调整到预设大小和位置 (1920x1080, 0,0)");
        restoreWindowButton = new System.Windows.Forms.Button { Text = "恢复窗口", Font = buttonFont, Location = new Point(200, 30), Size = new Size(150, 40), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        restoreWindowButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        restoreWindowButton.Click += RestoreWindowButton_Click;
        toolTip.SetToolTip(restoreWindowButton, "恢复调整前的窗口大小和位置");

        startBuyingButton = new System.Windows.Forms.Button { Text = "开始购买 (按 F12 停止)", Font = buttonFont, Location = new Point(30, 30), Size = new Size(200, 50), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        startBuyingButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        startBuyingButton.Click += StartBuyingButton_Click;
        toolTip.SetToolTip(startBuyingButton, "启动或停止自动购买功能，按 F12 也可停止");

        priceThresholdTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 100), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(priceThresholdTextBox, "设置最高购买单价，超过此价格将跳过 (默认: 10)");
        priceThresholdLabel = new System.Windows.Forms.Label { Text = "最高购买价格: 10", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 105), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setPriceButton = new System.Windows.Forms.Button { Text = "设置价格", Font = buttonFont, Location = new Point(320, 100), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setPriceButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setPriceButton.Click += SetPriceButton_Click;
        toolTip.SetToolTip(setPriceButton, "确认最高购买单价设置");

        quantityComboBox = new System.Windows.Forms.ComboBox { Location = new Point(230, 150), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, Font = settingsControlFont, DropDownStyle = ComboBoxStyle.DropDownList };
        quantityComboBox.Items.AddRange(new[] { "1", "25", "100", "200" });
        quantityComboBox.SelectedIndex = 3;
        quantityComboBox.SelectedIndexChanged += QuantityComboBox_SelectedIndexChanged;
        toolTip.SetToolTip(quantityComboBox, "选择每次购买的数量 (默认: 200)");
        quantityLabel = new System.Windows.Forms.Label { Text = "购买数量: 200", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 155), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };

        stopAfterPurchasesTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 200), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(stopAfterPurchasesTextBox, "设置购买成功多少次后停止，0 表示无限 (默认: 0)");
        stopAfterPurchasesLabel = new System.Windows.Forms.Label { Text = "停止购买次数: 0", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 205), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setStopPurchasesButton = new System.Windows.Forms.Button { Text = "设置次数", Font = buttonFont, Location = new Point(320, 200), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setStopPurchasesButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setStopPurchasesButton.Click += SetStopPurchasesButton_Click;
        toolTip.SetToolTip(setStopPurchasesButton, "确认购买次数限制");

        enableTimerCheckBox = new System.Windows.Forms.CheckBox { Text = "启用定时", Font = settingsLabelFont, Location = new Point(30, 30), Size = new Size(100, 30), ForeColor = textColor, BackColor = contentColor };
        enableTimerCheckBox.CheckedChanged += EnableTimerCheckBox_CheckedChanged;
        toolTip.SetToolTip(enableTimerCheckBox, "启用后按设置时间自动启动和停止购买");
        startTimeTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 70), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(startTimeTextBox, "设置每天开始购买的时间 (格式: HH:mm:ss，默认: 08:00:00)");
        startTimeLabel = new System.Windows.Forms.Label { Text = "开始时间: 08:00:00", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 75), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        stopTimeTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 110), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(stopTimeTextBox, "设置每天停止购买的时间 (格式: HH:mm:ss，默认: 23:00:00)");
        stopTimeLabel = new System.Windows.Forms.Label { Text = "停止时间: 23:00:00", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 115), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setTimerButton = new System.Windows.Forms.Button { Text = "设置定时", Font = buttonFont, Location = new Point(320, 110), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setTimerButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setTimerButton.Click += SetTimerButton_Click;
        toolTip.SetToolTip(setTimerButton, "确认定时设置");

        System.Windows.Forms.Label delayLabel = new System.Windows.Forms.Label { Text = "延迟设置 (毫秒):", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 150), Size = new Size(200, 20) };
        actionDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 180), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(actionDelayMinTextBox, "操作间隔最小值，如点击后的短暂等待 (默认: 15ms)");
        actionDelayMinLabel = new System.Windows.Forms.Label { Text = "操作间隔最小: 15", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 185), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        actionDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 180), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(actionDelayMaxTextBox, "操作间隔最大值 (默认: 30ms)");
        actionDelayMaxLabel = new System.Windows.Forms.Label { Text = "操作间隔最大: 30", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 185), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrRetryDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 220), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrRetryDelayMinTextBox, "OCR 重试间隔最小值，如价格或通知检查失败后的等待 (默认: 15ms)");
        ocrRetryDelayMinLabel = new System.Windows.Forms.Label { Text = "OCR 重试最小: 15", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 225), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrRetryDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 220), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrRetryDelayMaxTextBox, "OCR 重试间隔最大值 (默认: 20ms)");
        ocrRetryDelayMaxLabel = new System.Windows.Forms.Label { Text = "OCR 重试最大: 20", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 225), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        notificationDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 260), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(notificationDelayMinTextBox, "购买后检查通知的最小等待时间 (默认: 500ms)");
        notificationDelayMinLabel = new System.Windows.Forms.Label { Text = "通知等待最小: 500", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 265), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        notificationDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 260), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(notificationDelayMaxTextBox, "购买后检查通知的最大等待时间 (默认: 510ms)");
        notificationDelayMaxLabel = new System.Windows.Forms.Label { Text = "通知等待最大: 510", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 265), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrCheckDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 300), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrCheckDelayMinTextBox, "OCR 检查价格的最小等待时间 (默认: 5ms)");
        ocrCheckDelayMinLabel = new System.Windows.Forms.Label { Text = "OCR 检查最小: 5", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 305), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrCheckDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 300), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrCheckDelayMaxTextBox, "OCR 检查价格的最大等待时间 (默认: 10ms)");
        ocrCheckDelayMaxLabel = new System.Windows.Forms.Label { Text = "OCR 检查最大: 10", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 305), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setDelaysButton = new System.Windows.Forms.Button { Text = "设置延迟", Font = buttonFont, Location = new Point(350, 340), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setDelaysButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setDelaysButton.Click += SetDelaysButton_Click;
        toolTip.SetToolTip(setDelaysButton, "确认所有延迟设置");

        statusLabel = new System.Windows.Forms.Label { Text = "初始化中...", Font = statusFont, ForeColor = textColor, Location = new Point(10, this.ClientSize.Height - 40), Size = new Size(this.ClientSize.Width - 20, 30), TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        watermarkLabel = new System.Windows.Forms.Label { Text = "AUT0RICH Lite - Powered by xAI", Font = footerFont, ForeColor = Color.FromArgb(150, 150, 150), Location = new Point(this.ClientSize.Width - 300, this.ClientSize.Height - 30), Size = new Size(290, 20), TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

        sidebarPanel.Controls.AddRange(new Control[] { sidebarAdjustWindowButton, sidebarAutoBuyButton, sidebarSettingsButton, sidebarHomepageLink, sidebarProjectLink });
        adjustWindowPanel.Controls.AddRange(new Control[] { adjustWindowButton, restoreWindowButton });
        autoBuyPanel.Controls.AddRange(new Control[] { startBuyingButton, priceThresholdLabel, priceThresholdTextBox, setPriceButton, quantityLabel, quantityComboBox, stopAfterPurchasesLabel, stopAfterPurchasesTextBox, setStopPurchasesButton });
        settingsPanel.Controls.AddRange(new Control[] { enableTimerCheckBox, startTimeLabel, startTimeTextBox, stopTimeLabel, stopTimeTextBox, setTimerButton, delayLabel, actionDelayMinLabel, actionDelayMinTextBox, actionDelayMaxLabel, actionDelayMaxTextBox, ocrRetryDelayMinLabel, ocrRetryDelayMinTextBox, ocrRetryDelayMaxLabel, ocrRetryDelayMaxTextBox, notificationDelayMinLabel, notificationDelayMinTextBox, notificationDelayMaxLabel, notificationDelayMaxTextBox, ocrCheckDelayMinLabel, ocrCheckDelayMinTextBox, ocrCheckDelayMaxLabel, ocrCheckDelayMaxTextBox, setDelaysButton });
        mainContentPanel.Controls.AddRange(new Control[] { adjustWindowPanel, autoBuyPanel, settingsPanel });
        this.Controls.AddRange(new Control[] { titleLabel, sidebarPanel, mainContentPanel, statusLabel, watermarkLabel });

        SwitchPanel(autoBuyPanel, sidebarAutoBuyButton);
    }

    private System.Windows.Forms.Button CreateSidebarButton(string text, Point location)
    {
        System.Windows.Forms.Button button = new System.Windows.Forms.Button { Text = "   " + text, Font = sidebarFont, Location = location, Size = new Size(sidebarPanel.Width - 30, 50), BackColor = sidebarColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        button.FlatAppearance.BorderSize = 0; button.FlatAppearance.MouseOverBackColor = sidebarSelectedColor; button.FlatAppearance.MouseDownBackColor = sidebarSelectedColor;
        return button;
    }

    private void SwitchPanel(System.Windows.Forms.Panel panelToShow, System.Windows.Forms.Button selectedButton)
    {
        foreach (Control c in mainContentPanel.Controls) if (c is System.Windows.Forms.Panel panel) panel.Visible = false;
        panelToShow.Visible = true;
        foreach (Control c in sidebarPanel.Controls)
        {
            if (c is System.Windows.Forms.Button btn) { btn.BackColor = (btn == selectedButton) ? sidebarSelectedColor : sidebarColor; btn.ForeColor = textColor; }
        }
    }

    private void LoadSettings()
    {
        string configFile = AppSettings.ConfigFilePath;
        if (File.Exists(configFile))
        {
            try
            {
                string json = File.ReadAllText(configFile);
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                // 验证时间格式
                if (!TimeSpan.TryParseExact(settings.StartTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
                {
                    Console.WriteLine($"配置文件中的 StartTimeString 无效: {settings.StartTimeString}，使用默认值 08:00:00");
                    settings.StartTimeString = "08:00:00";
                }
                if (!TimeSpan.TryParseExact(settings.StopTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
                {
                    Console.WriteLine($"配置文件中的 StopTimeString 无效: {settings.StopTimeString}，使用默认值 23:00:00");
                    settings.StopTimeString = "23:00:00";
                }
                Console.WriteLine($"配置已从 {configFile} 加载。StartTimeString: {settings.StartTimeString}, StopTimeString: {settings.StopTimeString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: 加载配置文件失败: {ex.Message}");
                MessageBox.Show($"加载配置文件失败: {ex.Message}\n将使用默认设置。", "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                settings = new AppSettings();
            }
        }
        else
        {
            Console.WriteLine("配置文件不存在，使用默认设置。");
            settings = new AppSettings();
        }
    }

    private void SaveSettings()
    {
        lock (stateLock)
        {
            UpdateSettingsFromUI(); // 确保保存的是最新的UI设置
            string configFile = AppSettings.ConfigFilePath;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(configFile, json);
                Console.WriteLine($"配置已保存到 {configFile}");
            }
            catch (Exception ex) { Console.WriteLine($"错误: 保存配置文件失败: {ex.Message}"); }
        }
    }

    private void ApplySettingsToUI()
    {
        lock (stateLock)
        {
            if (settings == null) settings = new AppSettings();
            priceThresholdTextBox.Text = settings.PriceThreshold.ToString();
            priceThresholdLabel.Text = $"最高购买价格: {settings.PriceThreshold}";
            stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();
            stopAfterPurchasesLabel.Text = $"停止购买次数: {settings.StopAfterPurchases}";
            switch (settings.PurchaseQuantity)
            {
                case 1: quantityComboBox.SelectedIndex = 0; break;
                case 25: quantityComboBox.SelectedIndex = 1; break;
                case 100: quantityComboBox.SelectedIndex = 2; break;
                case 200: default: quantityComboBox.SelectedIndex = 3; break;
            }
            quantityLabel.Text = $"购买数量: {settings.PurchaseQuantity}";
            enableTimerCheckBox.Checked = settings.IsTimerEnabled;
            // 确保时间格式有效
            if (!TimeSpan.TryParseExact(settings.StartTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            {
                settings.StartTimeString = "08:00:00";
            }
            if (!TimeSpan.TryParseExact(settings.StopTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            {
                settings.StopTimeString = "23:00:00";
            }
            // 确认控件已初始化
            if (startTimeTextBox == null || stopTimeTextBox == null)
            {
                Console.WriteLine("错误: startTimeTextBox 或 stopTimeTextBox 未初始化！");
                return;
            }
            startTimeTextBox.Text = settings.StartTimeString;
            startTimeLabel.Text = $"开始时间: {settings.StartTimeString}";
            stopTimeTextBox.Text = settings.StopTimeString;
            stopTimeLabel.Text = $"停止时间: {settings.StopTimeString}";
            Console.WriteLine($"ApplySettingsToUI: startTimeTextBox.Text 设置为 {startTimeTextBox.Text}, stopTimeTextBox.Text 设置为 {stopTimeTextBox.Text}");
            isTimerEnabled = settings.IsTimerEnabled;
            actionDelayMinTextBox.Text = settings.ActionDelayMin.ToString();
            actionDelayMinLabel.Text = $"操作间隔最小: {settings.ActionDelayMin}";
            actionDelayMaxTextBox.Text = settings.ActionDelayMax.ToString();
            actionDelayMaxLabel.Text = $"操作间隔最大: {settings.ActionDelayMax}";
            ocrRetryDelayMinTextBox.Text = settings.OcrRetryDelayMin.ToString();
            ocrRetryDelayMinLabel.Text = $"OCR 重试最小: {settings.OcrRetryDelayMin}";
            ocrRetryDelayMaxTextBox.Text = settings.OcrRetryDelayMax.ToString();
            ocrRetryDelayMaxLabel.Text = $"OCR 重试最大: {settings.OcrRetryDelayMax}";
            notificationDelayMinTextBox.Text = settings.NotificationDelayMin.ToString();
            notificationDelayMinLabel.Text = $"通知等待最小: {settings.NotificationDelayMin}";
            notificationDelayMaxTextBox.Text = settings.NotificationDelayMax.ToString();
            notificationDelayMaxLabel.Text = $"通知等待最大: {settings.NotificationDelayMax}";
            ocrCheckDelayMinTextBox.Text = settings.OcrCheckDelayMin.ToString();
            ocrCheckDelayMinLabel.Text = $"OCR 检查最小: {settings.OcrCheckDelayMin}";
            ocrCheckDelayMaxTextBox.Text = settings.OcrCheckDelayMax.ToString();
            ocrCheckDelayMaxLabel.Text = $"OCR 检查最大: {settings.OcrCheckDelayMax}";
        }
    }

    private void UpdateSettingsFromUI()
    {
        lock (stateLock)
        {
            if (settings == null) return;
            if (int.TryParse(priceThresholdTextBox.Text, out int price) && price >= 0) { settings.PriceThreshold = price; priceThresholdLabel.Text = $"最高购买价格: {price}"; } else priceThresholdTextBox.Text = settings.PriceThreshold.ToString();
            if (int.TryParse(stopAfterPurchasesTextBox.Text, out int stopCount) && stopCount >= 0) { settings.StopAfterPurchases = stopCount; stopAfterPurchasesLabel.Text = $"停止购买次数: {stopCount}"; } else stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();
            switch (quantityComboBox.SelectedIndex)
            {
                case 0: settings.PurchaseQuantity = 1; break;
                case 1: settings.PurchaseQuantity = 25; break;
                case 2: settings.PurchaseQuantity = 100; break;
                case 3: default: settings.PurchaseQuantity = 200; break;
            }
            quantityLabel.Text = $"购买数量: {settings.PurchaseQuantity}";
            settings.IsTimerEnabled = enableTimerCheckBox.Checked;
            if (TimeSpan.TryParseExact(startTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _)) { settings.StartTimeString = startTimeTextBox.Text; startTimeLabel.Text = $"开始时间: {settings.StartTimeString}"; } else startTimeTextBox.Text = settings.StartTimeString;
            if (TimeSpan.TryParseExact(stopTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _)) { settings.StopTimeString = stopTimeTextBox.Text; stopTimeLabel.Text = $"停止时间: {settings.StopTimeString}"; } else stopTimeTextBox.Text = settings.StopTimeString;
            if (int.TryParse(actionDelayMinTextBox.Text, out int actionMin) && actionMin >= 0) { settings.ActionDelayMin = actionMin; actionDelayMinLabel.Text = $"操作间隔最小: {actionMin}"; } else actionDelayMinTextBox.Text = settings.ActionDelayMin.ToString();
            if (int.TryParse(actionDelayMaxTextBox.Text, out int actionMax) && actionMax >= settings.ActionDelayMin) { settings.ActionDelayMax = actionMax; actionDelayMaxLabel.Text = $"操作间隔最大: {actionMax}"; } else actionDelayMaxTextBox.Text = settings.ActionDelayMax.ToString();
            if (int.TryParse(ocrRetryDelayMinTextBox.Text, out int retryMin) && retryMin >= 0) { settings.OcrRetryDelayMin = retryMin; ocrRetryDelayMinLabel.Text = $"OCR 重试最小: {retryMin}"; } else ocrRetryDelayMinTextBox.Text = settings.OcrRetryDelayMin.ToString();
            if (int.TryParse(ocrRetryDelayMaxTextBox.Text, out int retryMax) && retryMax >= settings.OcrRetryDelayMin) { settings.OcrRetryDelayMax = retryMax; ocrRetryDelayMaxLabel.Text = $"OCR 重试最大: {retryMax}"; } else ocrRetryDelayMaxTextBox.Text = settings.OcrRetryDelayMax.ToString();
            if (int.TryParse(notificationDelayMinTextBox.Text, out int notifyMin) && notifyMin >= 0) { settings.NotificationDelayMin = notifyMin; notificationDelayMinLabel.Text = $"通知等待最小: {notifyMin}"; } else notificationDelayMinTextBox.Text = settings.NotificationDelayMin.ToString();
            if (int.TryParse(notificationDelayMaxTextBox.Text, out int notifyMax) && notifyMax >= settings.NotificationDelayMin) { settings.NotificationDelayMax = notifyMax; notificationDelayMaxLabel.Text = $"通知等待最大: {notifyMax}"; } else notificationDelayMaxTextBox.Text = settings.NotificationDelayMax.ToString();
            if (int.TryParse(ocrCheckDelayMinTextBox.Text, out int ocrMin) && ocrMin >= 0) { settings.OcrCheckDelayMin = ocrMin; ocrCheckDelayMinLabel.Text = $"OCR 检查最小: {ocrMin}"; } else ocrCheckDelayMinTextBox.Text = settings.OcrCheckDelayMin.ToString();
            if (int.TryParse(ocrCheckDelayMaxTextBox.Text, out int ocrMax) && ocrMax >= settings.OcrCheckDelayMin) { settings.OcrCheckDelayMax = ocrMax; ocrCheckDelayMaxLabel.Text = $"OCR 检查最大: {ocrMax}"; } else ocrCheckDelayMaxTextBox.Text = settings.OcrCheckDelayMax.ToString();
        }
    }

    private void ApplySettingsFromUIToAutoBuyer()
    {
        lock (stateLock)
        {
            if (autoBuyer == null) return;
            autoBuyer.SetPriceThreshold(settings.PriceThreshold);
            autoBuyer.SetStopAfterPurchases(settings.StopAfterPurchases);
            autoBuyer.SetPurchaseQuantity(settings.PurchaseQuantity);
        }
    }

    // InitializeExternalDependencies - 移除了 MainForm 对 F12 的订阅
    private void InitializeExternalDependencies()
    {
        try
        {
            KeyboardHook.StopKey = settings.StopKey;
            // MainForm 不再直接订阅 StopAutoBuyer 事件来设置 manualStop 或调用 Stop
            // KeyboardHook.StopAutoBuyer += HandleKeyboardStopRequest; // <-- 已移除
            KeyboardHook.Initialize(); // 仍然需要初始化钩子，让 AutoBuyer 内部可以订阅
            UpdateStatusLabelSafe("键盘钩子 OK", statusSuccessColor);
        }
        catch (Exception ex)
        {
            HandleInitError("键盘钩子", ex);
            UpdateStatusLabelSafe("键盘钩子失败!", statusErrorColor);
        }

        MouseSimulator.RandomRangeNear = settings.RandomRangeNear;
        MouseSimulator.RandomRangeShake = settings.RandomRangeShake;

        UpdateStatusLabelSafe("初始化完成，准备就绪。", textColor);
    }

    private void HandleInitError(string componentName, Exception ex)
    {
        Console.WriteLine($"错误: 初始化 {componentName} 失败: {ex.Message}");
        MessageBox.Show($"{componentName} 初始化失败: {ex.Message}", $"{componentName} 初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    #endregion

    #region 事件处理
    // ... (AdjustWindowButton_Click, RestoreWindowButton_Click, SetPriceButton_Click, QuantityComboBox_SelectedIndexChanged, SetStopPurchasesButton_Click, EnableTimerCheckBox_CheckedChanged, SetTimerButton_Click, SetDelaysButton_Click 保持不变) ...
    private void AdjustWindowButton_Click(object sender, EventArgs e)
    {
        windowAdjuster.TargetWidth = settings.TargetWindowWidth;
        windowAdjuster.TargetHeight = settings.TargetWindowHeight;
        windowAdjuster.TargetX = settings.TargetWindowX;
        windowAdjuster.TargetY = settings.TargetWindowY;
        try { windowAdjuster.AdjustFocusedWindow(); UpdateStatusLabelSafe("窗口调整成功", statusSuccessColor); }
        catch (Exception ex) { MessageBox.Show($"调整窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RestoreWindowButton_Click(object sender, EventArgs e)
    {
        try { windowAdjuster.RestoreOriginalWindow(); UpdateStatusLabelSafe("窗口恢复成功", statusSuccessColor); }
        catch (Exception ex) { MessageBox.Show($"恢复窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SetPriceButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            if (int.TryParse(priceThresholdTextBox.Text, out int val) && val >= 0)
            {
                settings.PriceThreshold = val;
                priceThresholdLabel.Text = $"最高购买价格: {val}";
                autoBuyer.SetPriceThreshold(val);
                MessageBox.Show($"最高购买单价已更新为 {val}", "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("请输入有效的非负整数价格！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                priceThresholdTextBox.Text = settings.PriceThreshold.ToString();
            }
        }
    }

    private void QuantityComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        lock (stateLock)
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
            quantityLabel.Text = $"购买数量: {qty}";
            autoBuyer.SetPurchaseQuantity(qty);
            Console.WriteLine($"购买数量已设置为: {qty}");
        }
    }

    private void SetStopPurchasesButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            if (int.TryParse(stopAfterPurchasesTextBox.Text, out int val) && val >= 0)
            {
                settings.StopAfterPurchases = val;
                stopAfterPurchasesLabel.Text = $"停止购买次数: {val}";
                autoBuyer.SetStopAfterPurchases(val);
                MessageBox.Show($"将在购买 {val} 次后停止 (0 表示无限)", "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("请输入有效的非负整数次数！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();
            }
        }
    }

    private void EnableTimerCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            isTimerEnabled = enableTimerCheckBox.Checked;
            settings.IsTimerEnabled = isTimerEnabled;
            Console.WriteLine($"定时器状态: {(isTimerEnabled ? "启用" : "禁用")}");
            if (isTimerEnabled)
            {
                if (!autoBuyer.IsRunning)
                {
                    ResetTimerState();
                }
                else
                {
                    UpdateStatusLabelSafe(GetTimerStatusString(), statusSuccessColor);
                }
            }
            else
            {
                ClearTimerState();
                if (autoBuyer.IsRunning)
                {
                    UpdateStatusLabelSafe("运行中 (定时器已禁用)", statusSuccessColor);
                }
                else
                {
                    UpdateStatusLabelSafe("定时器已禁用", textColor);
                }
            }
        }
    }

    private void SetTimerButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            bool timeChanged = false;
            if (TimeSpan.TryParseExact(startTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            {
                if (settings.StartTimeString != startTimeTextBox.Text)
                {
                    settings.StartTimeString = startTimeTextBox.Text;
                    startTimeLabel.Text = $"开始时间: {settings.StartTimeString}";
                    timeChanged = true;
                }
            }
            else
            {
                startTimeTextBox.Text = settings.StartTimeString;
                MessageBox.Show("开始时间格式无效 (应为 HH:mm:ss)", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (TimeSpan.TryParseExact(stopTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            {
                if (settings.StopTimeString != stopTimeTextBox.Text)
                {
                    settings.StopTimeString = stopTimeTextBox.Text;
                    stopTimeLabel.Text = $"停止时间: {settings.StopTimeString}";
                    timeChanged = true;
                }
            }
            else
            {
                stopTimeTextBox.Text = settings.StopTimeString;
                MessageBox.Show("停止时间格式无效 (应为 HH:mm:ss)", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (timeChanged && isTimerEnabled)
            {
                Console.WriteLine("定时时间更改，重置定时器状态...");
                ResetTimerState();
            }
            else if (timeChanged)
            {
                Console.WriteLine("定时时间已更新，但定时器未启用。");
            }

            string statusMsg = isTimerEnabled ? GetTimerStatusString() : "定时器已禁用";
            MessageBox.Show($"定时设置已应用。\n{statusMsg}", "定时设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SetDelaysButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            UpdateSettingsFromUI();
            MessageBox.Show($"延迟设置已更新:\n操作间隔: {settings.ActionDelayMin}-{settings.ActionDelayMax}ms\nOCR 重试间隔: {settings.OcrRetryDelayMin}-{settings.OcrRetryDelayMax}ms\n通知等待: {settings.NotificationDelayMin}-{settings.NotificationDelayMax}ms\nOCR 检查: {settings.OcrCheckDelayMin}-{settings.OcrCheckDelayMax}ms", "设置成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }


    // Start/Stop 按钮逻辑 - 移除 manualStop
    private void StartBuyingButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            if (!autoBuyer.IsRunning)
            {
                UpdateSettingsFromUI();
                ApplySettingsFromUIToAutoBuyer();
                try
                {
                    autoBuyer.Start();
                    UpdateStartButtonState(true);
                    UpdateStatusLabelSafe("运行中...", statusSuccessColor);
                    Console.WriteLine("手动启动购买");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"启动购买失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStartButtonState(false);
                    UpdateStatusLabelSafe("启动异常", statusErrorColor);
                }
            }
            else // 如果正在运行
            {
                UpdateStatusLabelSafe("正在停止 (按钮)...", statusWarningColor); // 可选即时反馈
                autoBuyer.Stop(); // 直接调用 Stop
                Console.WriteLine("手动停止购买 (按钮)");
                // 等待 AutoBuyer.Stopped 事件触发 UpdateUIOnStop
            }
        }
    }

    // HandleKeyboardStopRequest 方法已移除

    // 处理 AutoBuyer 停止事件
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
        Console.WriteLine("HandleAutoBuyerStopped: 已调度 UpdateUIOnStop。");
    }

    // UpdateUIOnStop 方法 - 移除了 manualStop
    private void UpdateUIOnStop()
    {
        lock (stateLock)
        {
            UpdateStartButtonState(false); // 统一设置按钮为 "开始" 状态

            Console.WriteLine($"UpdateUIOnStop 调用: isTimerEnabled={isTimerEnabled}, IsRunning={autoBuyer.IsRunning}");

            // --- 统一操作 ---
            // 1. 更新状态标签为统一的 "已停止"
            UpdateStatusLabelSafe("已停止", statusWarningColor); // 或使用 textColor
            Console.WriteLine("状态更新为: 已停止");

            // 2. 重置定时器状态（准备下一个周期，如果定时器启用）
            Console.WriteLine("重置定时器状态...");
            ResetTimerState(); // 无论停止原因，都调用 ResetTimerState

            // 3. 不再需要重置 manualStop 标志
            Console.WriteLine("UpdateUIOnStop: 停止UI更新完成。");
            // --- 统一操作结束 ---
        }
    }


    private void UpdateStatusLabelSafe(string text, Color color)
    {
        if (statusLabel == null || statusLabel.IsDisposed) return;

        // 只有当状态文本或颜色发生变化时才更新
        if (text == lastStatusText && statusLabel.ForeColor == color) return;
        lastStatusText = text;

        if (statusLabel.InvokeRequired)
        {
            statusLabel.BeginInvoke(new Action(() =>
            {
                if (!statusLabel.IsDisposed)
                {
                    statusLabel.Text = text;
                    statusLabel.ForeColor = color;
                    Console.WriteLine($"状态更新 (异步): {text}");
                }
            }));
        }
        else
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
            Console.WriteLine($"状态更新 (同步): {text}");
        }
    }

    private void UpdateStartButtonState(bool isRunning)
    {
        if (startBuyingButton == null || startBuyingButton.IsDisposed) return;
        if (startBuyingButton.InvokeRequired) { startBuyingButton.BeginInvoke(new Action(() => UpdateStartButtonState(isRunning))); return; }
        if (isRunning)
        {
            startBuyingButton.Text = "停止购买 (按 F12 停止)"; startBuyingButton.Tag = "stop"; startBuyingButton.BackColor = statusErrorColor;
            startBuyingButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 110, 110); startBuyingButton.FlatAppearance.MouseDownBackColor = statusErrorColor;
            toolTip.SetToolTip(startBuyingButton, "停止自动购买 (按 F12 停止)");
        }
        else
        {
            startBuyingButton.Text = "开始购买 (按 F12 停止)"; startBuyingButton.Tag = "start"; startBuyingButton.Enabled = true; startBuyingButton.BackColor = accentColor;
            startBuyingButton.FlatAppearance.MouseOverBackColor = accentHoverColor; startBuyingButton.FlatAppearance.MouseDownBackColor = accentColor;
            toolTip.SetToolTip(startBuyingButton, "开始自动购买 (按 F12 停止)");
        }
    }

    // ResetTimerState 和 ClearTimerState 保持不变
    private bool ResetTimerState()
    {
        lock (stateLock)
        {
            TimeSpan startTimeOfDay, stopTimeOfDay;
            string startTimeToParse = string.IsNullOrWhiteSpace(startTimeTextBox.Text) ? settings.StartTimeString : startTimeTextBox.Text;
            string stopTimeToParse = string.IsNullOrWhiteSpace(stopTimeTextBox.Text) ? settings.StopTimeString : stopTimeTextBox.Text;

            Console.WriteLine($"ResetTimerState: 尝试解析 startTimeToParse={startTimeToParse}, stopTimeToParse={stopTimeToParse}");

            if (!TimeSpan.TryParseExact(startTimeToParse, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out startTimeOfDay))
            {
                Console.WriteLine($"解析开启时间失败: {startTimeToParse}，使用默认值 08:00:00");
                startTimeTextBox.Text = "08:00:00";
                startTimeLabel.Text = "开始时间: 08:00:00";
                settings.StartTimeString = "08:00:00";
                startTimeOfDay = TimeSpan.ParseExact("08:00:00", "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            }
            if (!TimeSpan.TryParseExact(stopTimeToParse, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out stopTimeOfDay))
            {
                Console.WriteLine($"解析关闭时间失败: {stopTimeToParse}，使用默认值 23:00:00");
                stopTimeTextBox.Text = "23:00:00";
                stopTimeLabel.Text = "停止时间: 23:00:00";
                settings.StopTimeString = "23:00:00";
                stopTimeOfDay = TimeSpan.ParseExact("23:00:00", "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            }

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime potentialStartTime = today.Add(startTimeOfDay);

            // 计算下次启动时间
            if (now < potentialStartTime)
            {
                scheduledStartTime = potentialStartTime;
            }
            else
            {
                scheduledStartTime = potentialStartTime.AddDays(1);
            }

            // 计算基于下次启动时间的停止时间
            DateTime stopBaseDate = scheduledStartTime.Value.Date;
            if (stopTimeOfDay <= startTimeOfDay)
            {
                scheduledStopTime = stopBaseDate.AddDays(1).Add(stopTimeOfDay);
            }
            else
            {
                scheduledStopTime = stopBaseDate.Add(stopTimeOfDay);
            }

            hasStartedToday = false;
            hasStoppedToday = false;
            Console.WriteLine($"定时器重置: 下次启动 {scheduledStartTime.Value:G}, 下次停止 {scheduledStopTime.Value:G}");

            // 更新状态标签，但要避免覆盖 "运行中" 或 "已停止"
            if (isTimerEnabled && !autoBuyer.IsRunning && statusLabel.Text != "已停止")
            {
                UpdateStatusLabelSafe(GetTimerStatusString(), textColor);
            }
            else if (!isTimerEnabled && !autoBuyer.IsRunning && statusLabel.Text != "已停止")
            {
                UpdateStatusLabelSafe("定时器已禁用", textColor);
            }
            return true;
        }
    }

    private void ClearTimerState()
    {
        lock (stateLock)
        {
            scheduledStartTime = null;
            scheduledStopTime = null;
            hasStartedToday = false;
            hasStoppedToday = false;
            Console.WriteLine("定时器状态已清除");
            if (!autoBuyer.IsRunning && statusLabel.Text != "已停止")
            {
                UpdateStatusLabelSafe("定时器已禁用", textColor);
            }
        }
    }


    // Timer_Tick - 移除了 manualStop
    private void Timer_Tick(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            // 移除 manualStop 检查
            if (!isTimerEnabled || !scheduledStartTime.HasValue || !scheduledStopTime.HasValue)
            {
                return;
            }

            DateTime now = DateTime.Now;

            // 检查是否到达启动时间
            if (!hasStartedToday && !autoBuyer.IsRunning && now >= scheduledStartTime.Value && now < scheduledStopTime.Value)
            {
                Console.WriteLine($"到达开启时间 {scheduledStartTime.Value:G}，启动购买...");
                UpdateSettingsFromUI();
                ApplySettingsFromUIToAutoBuyer();
                try
                {
                    // 移除 manualStop 检查和设置
                    autoBuyer.Start();
                    if (autoBuyer.IsRunning)
                    {
                        UpdateStartButtonState(true);
                        UpdateStatusLabelSafe($"运行中 (定时开启于 {scheduledStartTime.Value:G})", statusSuccessColor);
                        hasStartedToday = true;
                        hasStoppedToday = false;
                        Console.WriteLine("定时启动成功");
                    }
                    else
                    {
                        Console.WriteLine($"定时启动后检测到 AutoBuyer 未运行，可能启动失败。");
                        UpdateStatusLabelSafe($"定时启动失败", statusErrorColor);
                        UpdateStartButtonState(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"定时启动异常: {ex.Message}");
                    UpdateStatusLabelSafe($"定时启动异常: {ex.Message}", statusErrorColor);
                    UpdateStartButtonState(false);
                }
            }
            // 检查是否到达停止时间
            else if (autoBuyer.IsRunning && now >= scheduledStopTime.Value)
            {
                Console.WriteLine($"到达关闭时间 {scheduledStopTime.Value:G}，停止购买...");
                try
                {
                    autoBuyer.Stop(); // 直接调用 Stop
                    // 等待 AutoBuyer.Stopped 事件触发 UpdateUIOnStop
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"定时停止异常: {ex.Message}");
                    UpdateStatusLabelSafe($"定时停止异常: {ex.Message}", statusErrorColor);
                }
            }
            // 检查是否跨天
            else if (scheduledStartTime.HasValue && now.Date > scheduledStartTime.Value.Date && (hasStartedToday || hasStoppedToday))
            {
                Console.WriteLine($"检测到跨天，重置定时器状态到下一周期...");
                ResetTimerState();
            }
        }
    }


    private string GetTimerStatusString()
    {
        lock (stateLock)
        {
            if (!isTimerEnabled) return "定时器已禁用";
            if (scheduledStartTime.HasValue && scheduledStopTime.HasValue)
            {
                // 避免覆盖 "运行中" 或 "已停止"
                if (autoBuyer.IsRunning) return statusLabel.Text;
                if (statusLabel.Text == "已停止") return statusLabel.Text;
                return $"定时器启用: 下次启动 {scheduledStartTime.Value:G}";
            }
            return "定时器启用，等待设置时间...";
        }
    }

    // MainForm_FormClosing - 移除了 manualStop
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        Console.WriteLine("应用程序关闭中...");
        try { timer?.Stop(); timer?.Dispose(); } catch (Exception ex) { Console.WriteLine($"关闭 Timer 出错: {ex.Message}"); }
        try
        {
            if (autoBuyer != null)
            {
                if (autoBuyer.IsRunning)
                {
                    Console.WriteLine("正在停止 AutoBuyer...");
                    // 移除 manualStop = true;
                    autoBuyer.Stop();
                    System.Threading.Thread.Sleep(500);
                }
                autoBuyer.Stopped -= HandleAutoBuyerStopped;
                autoBuyer.Dispose();
            }
        }
        catch (Exception ex) { Console.WriteLine($"停止/释放 AutoBuyer 出错: {ex.Message}"); }

        // 不再需要取消订阅 MainForm 的 HandleKeyboardStopRequest，因为它已被移除
        // KeyboardHook.StopAutoBuyer -= HandleKeyboardStopRequest; // <-- 已移除

        try { KeyboardHook.Cleanup(); } catch (Exception ex) { Console.WriteLine($"清理 KeyboardHook 出错: {ex.Message}"); }
        try { SaveSettings(); } catch (Exception ex) { Console.WriteLine($"保存设置出错: {ex.Message}"); }
        Console.WriteLine("清理完成，程序退出。");
    }
    #endregion

    // Main 方法修正了区域性设置
    [STAThread]
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        try
        {
            // 先获取当前线程的实例
            System.Threading.Thread uiThread = System.Threading.Thread.CurrentThread;

            // 在获取到的实例上设置区域性
            uiThread.CurrentCulture = new CultureInfo("zh-CN");
            uiThread.CurrentUICulture = new CultureInfo("zh-CN");

            Console.WriteLine($"Current Culture set to: {System.Threading.Thread.CurrentThread.CurrentCulture.Name}");
            Console.WriteLine($"Current UI Culture set to: {System.Threading.Thread.CurrentThread.CurrentUICulture.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置区域性失败: {ex.Message}");
        }
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(new MainForm());
    }
}