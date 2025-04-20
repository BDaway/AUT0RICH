// MainForm.cs
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Threading; // ȷ�� System.Threading �� using

public class MainForm : Form
{
    #region UI ��ɫ�����嶨��
    // ... (���ֲ���) ...
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

    #region UI �ؼ�����
    // ... (���ֲ���) ...
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

    #region �����߼���״̬
    private readonly WindowAdjuster windowAdjuster;
    private readonly AutoBuyer autoBuyer;
    private AppSettings settings;
    private DateTime? scheduledStartTime;
    private DateTime? scheduledStopTime;
    private bool hasStartedToday = false;
    private bool hasStoppedToday = false;
    private bool isTimerEnabled = false;
    // private volatile bool manualStop = false; // <-- ���Ƴ�
    private System.Windows.Forms.Timer timer;
    private readonly object stateLock = new object(); // ����ͬ��״̬����
    private string lastStatusText = string.Empty; // ������һ�ε�״̬�ı�
    #endregion

    public MainForm()
    {
        this.Font = mainFont;
        LoadSettings();
        InitializeComponents();
        windowAdjuster = new WindowAdjuster();
        if (settings == null) settings = new AppSettings();
        autoBuyer = new AutoBuyer(settings); // AutoBuyer �ڲ��ᶩ�� F12 �¼�
        ApplySettingsToUI();
        Console.WriteLine("ApplySettingsToUI ��ɣ����ؼ�״̬...");
        Console.WriteLine($"startTimeTextBox.Text: {startTimeTextBox?.Text}, stopTimeTextBox.Text: {stopTimeTextBox?.Text}");
        InitializeExternalDependencies(); // ��ʼ�����̹��ӵ�
        autoBuyer.Stopped += HandleAutoBuyerStopped; // ���� AutoBuyer ֹͣ�¼�
        timer = new System.Windows.Forms.Timer { Interval = 1000 };
        timer.Tick += Timer_Tick;
        timer.Start();
        if (isTimerEnabled)
        {
            ResetTimerState();
        }
        else
        {
            UpdateStatusLabelSafe("��ʱ���ѽ���", textColor);
        }
        this.FormClosing += MainForm_FormClosing;
        Console.WriteLine("MainForm ��ʼ����ɡ�");
    }

    #region ��ʼ���� UI ����
    // ... (InitializeComponents, CreateSidebarButton, SwitchPanel, LoadSettings, SaveSettings, ApplySettingsToUI, UpdateSettingsFromUI, ApplySettingsFromUIToAutoBuyer �������ֲ���) ...
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
        sidebarAdjustWindowButton = CreateSidebarButton("��������", new Point(15, 20));
        sidebarAdjustWindowButton.Click += (s, e) => SwitchPanel(adjustWindowPanel, sidebarAdjustWindowButton);
        toolTip.SetToolTip(sidebarAdjustWindowButton, "����Ŀ�괰�ڵ�ָ����С��λ��");
        sidebarAutoBuyButton = CreateSidebarButton("�Զ�����", new Point(15, 80));
        sidebarAutoBuyButton.Click += (s, e) => SwitchPanel(autoBuyPanel, sidebarAutoBuyButton);
        toolTip.SetToolTip(sidebarAutoBuyButton, "���ò������Զ�������");
        sidebarSettingsButton = CreateSidebarButton("��������", new Point(15, 140));
        sidebarSettingsButton.Click += (s, e) => SwitchPanel(settingsPanel, sidebarSettingsButton);
        toolTip.SetToolTip(sidebarSettingsButton, "������ʱ���ӳٵȳ�������");
        sidebarHomepageLink = new System.Windows.Forms.LinkLabel { Text = "Bվ��ҳ", Font = sidebarFont, LinkColor = accentColor, ActiveLinkColor = accentHoverColor, VisitedLinkColor = accentColor, Location = new Point(15, 220), Size = new Size(220, 30), TextAlign = ContentAlignment.MiddleLeft };
        sidebarHomepageLink.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo("https://b23.tv/zmdKRcb") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"�޷�������: {ex.Message}"); } };
        toolTip.SetToolTip(sidebarHomepageLink, "�������ߵ� B վ��ҳ");
        sidebarProjectLink = new System.Windows.Forms.LinkLabel { Text = "��Ŀ��ַ", Font = sidebarFont, LinkColor = accentColor, ActiveLinkColor = accentHoverColor, VisitedLinkColor = accentColor, Location = new Point(15, 260), Size = new Size(220, 30), TextAlign = ContentAlignment.MiddleLeft };
        sidebarProjectLink.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo("https://github.com/BDaway/AUT0RICH") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show($"�޷�������: {ex.Message}"); } };
        toolTip.SetToolTip(sidebarProjectLink, "�鿴��ĿԴ������ĵ�");

        mainContentPanel = new System.Windows.Forms.Panel { Location = new Point(250, 80), Size = new Size(this.ClientSize.Width - 250, this.ClientSize.Height - 80 - 50), BackColor = contentColor, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };

        adjustWindowPanel = new System.Windows.Forms.Panel { Size = mainContentPanel.Size, BackColor = contentColor, Visible = false };
        autoBuyPanel = new System.Windows.Forms.Panel { Size = mainContentPanel.Size, BackColor = contentColor, Visible = true };
        settingsPanel = new System.Windows.Forms.Panel { Size = mainContentPanel.Size, BackColor = contentColor, Visible = false };

        adjustWindowButton = new System.Windows.Forms.Button { Text = "��������", Font = buttonFont, Location = new Point(30, 30), Size = new Size(150, 40), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        adjustWindowButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        adjustWindowButton.Click += AdjustWindowButton_Click;
        toolTip.SetToolTip(adjustWindowButton, "����ǰ����ڵ�����Ԥ���С��λ�� (1920x1080, 0,0)");
        restoreWindowButton = new System.Windows.Forms.Button { Text = "�ָ�����", Font = buttonFont, Location = new Point(200, 30), Size = new Size(150, 40), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        restoreWindowButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        restoreWindowButton.Click += RestoreWindowButton_Click;
        toolTip.SetToolTip(restoreWindowButton, "�ָ�����ǰ�Ĵ��ڴ�С��λ��");

        startBuyingButton = new System.Windows.Forms.Button { Text = "��ʼ���� (�� F12 ֹͣ)", Font = buttonFont, Location = new Point(30, 30), Size = new Size(200, 50), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        startBuyingButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        startBuyingButton.Click += StartBuyingButton_Click;
        toolTip.SetToolTip(startBuyingButton, "������ֹͣ�Զ������ܣ��� F12 Ҳ��ֹͣ");

        priceThresholdTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 100), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(priceThresholdTextBox, "������߹��򵥼ۣ������˼۸����� (Ĭ��: 10)");
        priceThresholdLabel = new System.Windows.Forms.Label { Text = "��߹���۸�: 10", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 105), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setPriceButton = new System.Windows.Forms.Button { Text = "���ü۸�", Font = buttonFont, Location = new Point(320, 100), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setPriceButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setPriceButton.Click += SetPriceButton_Click;
        toolTip.SetToolTip(setPriceButton, "ȷ����߹��򵥼�����");

        quantityComboBox = new System.Windows.Forms.ComboBox { Location = new Point(230, 150), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, Font = settingsControlFont, DropDownStyle = ComboBoxStyle.DropDownList };
        quantityComboBox.Items.AddRange(new[] { "1", "25", "100", "200" });
        quantityComboBox.SelectedIndex = 3;
        quantityComboBox.SelectedIndexChanged += QuantityComboBox_SelectedIndexChanged;
        toolTip.SetToolTip(quantityComboBox, "ѡ��ÿ�ι�������� (Ĭ��: 200)");
        quantityLabel = new System.Windows.Forms.Label { Text = "��������: 200", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 155), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };

        stopAfterPurchasesTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 200), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(stopAfterPurchasesTextBox, "���ù���ɹ����ٴκ�ֹͣ��0 ��ʾ���� (Ĭ��: 0)");
        stopAfterPurchasesLabel = new System.Windows.Forms.Label { Text = "ֹͣ�������: 0", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 205), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setStopPurchasesButton = new System.Windows.Forms.Button { Text = "���ô���", Font = buttonFont, Location = new Point(320, 200), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setStopPurchasesButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setStopPurchasesButton.Click += SetStopPurchasesButton_Click;
        toolTip.SetToolTip(setStopPurchasesButton, "ȷ�Ϲ����������");

        enableTimerCheckBox = new System.Windows.Forms.CheckBox { Text = "���ö�ʱ", Font = settingsLabelFont, Location = new Point(30, 30), Size = new Size(100, 30), ForeColor = textColor, BackColor = contentColor };
        enableTimerCheckBox.CheckedChanged += EnableTimerCheckBox_CheckedChanged;
        toolTip.SetToolTip(enableTimerCheckBox, "���ú�����ʱ���Զ�������ֹͣ����");
        startTimeTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 70), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(startTimeTextBox, "����ÿ�쿪ʼ�����ʱ�� (��ʽ: HH:mm:ss��Ĭ��: 08:00:00)");
        startTimeLabel = new System.Windows.Forms.Label { Text = "��ʼʱ��: 08:00:00", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 75), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        stopTimeTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 110), Size = new Size(80, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(stopTimeTextBox, "����ÿ��ֹͣ�����ʱ�� (��ʽ: HH:mm:ss��Ĭ��: 23:00:00)");
        stopTimeLabel = new System.Windows.Forms.Label { Text = "ֹͣʱ��: 23:00:00", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 115), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setTimerButton = new System.Windows.Forms.Button { Text = "���ö�ʱ", Font = buttonFont, Location = new Point(320, 110), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setTimerButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setTimerButton.Click += SetTimerButton_Click;
        toolTip.SetToolTip(setTimerButton, "ȷ�϶�ʱ����");

        System.Windows.Forms.Label delayLabel = new System.Windows.Forms.Label { Text = "�ӳ����� (����):", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 150), Size = new Size(200, 20) };
        actionDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 180), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(actionDelayMinTextBox, "���������Сֵ��������Ķ��ݵȴ� (Ĭ��: 15ms)");
        actionDelayMinLabel = new System.Windows.Forms.Label { Text = "���������С: 15", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 185), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        actionDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 180), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(actionDelayMaxTextBox, "����������ֵ (Ĭ��: 30ms)");
        actionDelayMaxLabel = new System.Windows.Forms.Label { Text = "����������: 30", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 185), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrRetryDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 220), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrRetryDelayMinTextBox, "OCR ���Լ����Сֵ����۸��֪ͨ���ʧ�ܺ�ĵȴ� (Ĭ��: 15ms)");
        ocrRetryDelayMinLabel = new System.Windows.Forms.Label { Text = "OCR ������С: 15", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 225), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrRetryDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 220), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrRetryDelayMaxTextBox, "OCR ���Լ�����ֵ (Ĭ��: 20ms)");
        ocrRetryDelayMaxLabel = new System.Windows.Forms.Label { Text = "OCR �������: 20", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 225), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        notificationDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 260), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(notificationDelayMinTextBox, "�������֪ͨ����С�ȴ�ʱ�� (Ĭ��: 500ms)");
        notificationDelayMinLabel = new System.Windows.Forms.Label { Text = "֪ͨ�ȴ���С: 500", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 265), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        notificationDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 260), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(notificationDelayMaxTextBox, "�������֪ͨ�����ȴ�ʱ�� (Ĭ��: 510ms)");
        notificationDelayMaxLabel = new System.Windows.Forms.Label { Text = "֪ͨ�ȴ����: 510", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 265), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrCheckDelayMinTextBox = new System.Windows.Forms.TextBox { Location = new Point(230, 300), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrCheckDelayMinTextBox, "OCR ���۸����С�ȴ�ʱ�� (Ĭ��: 5ms)");
        ocrCheckDelayMinLabel = new System.Windows.Forms.Label { Text = "OCR �����С: 5", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(30, 305), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        ocrCheckDelayMaxTextBox = new System.Windows.Forms.TextBox { Location = new Point(290, 300), Size = new Size(50, 30), BackColor = controlBgColor, ForeColor = textColor, BorderStyle = BorderStyle.FixedSingle, Font = settingsControlFont };
        toolTip.SetToolTip(ocrCheckDelayMaxTextBox, "OCR ���۸�����ȴ�ʱ�� (Ĭ��: 10ms)");
        ocrCheckDelayMaxLabel = new System.Windows.Forms.Label { Text = "OCR ������: 10", Font = settingsLabelFont, ForeColor = textColor, Location = new Point(350, 305), Size = new Size(190, 20), TextAlign = ContentAlignment.MiddleLeft };
        setDelaysButton = new System.Windows.Forms.Button { Text = "�����ӳ�", Font = buttonFont, Location = new Point(350, 340), Size = new Size(100, 30), BackColor = accentColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
        setDelaysButton.FlatAppearance.MouseOverBackColor = accentHoverColor;
        setDelaysButton.Click += SetDelaysButton_Click;
        toolTip.SetToolTip(setDelaysButton, "ȷ�������ӳ�����");

        statusLabel = new System.Windows.Forms.Label { Text = "��ʼ����...", Font = statusFont, ForeColor = textColor, Location = new Point(10, this.ClientSize.Height - 40), Size = new Size(this.ClientSize.Width - 20, 30), TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
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
                // ��֤ʱ���ʽ
                if (!TimeSpan.TryParseExact(settings.StartTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
                {
                    Console.WriteLine($"�����ļ��е� StartTimeString ��Ч: {settings.StartTimeString}��ʹ��Ĭ��ֵ 08:00:00");
                    settings.StartTimeString = "08:00:00";
                }
                if (!TimeSpan.TryParseExact(settings.StopTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
                {
                    Console.WriteLine($"�����ļ��е� StopTimeString ��Ч: {settings.StopTimeString}��ʹ��Ĭ��ֵ 23:00:00");
                    settings.StopTimeString = "23:00:00";
                }
                Console.WriteLine($"�����Ѵ� {configFile} ���ء�StartTimeString: {settings.StartTimeString}, StopTimeString: {settings.StopTimeString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"����: ���������ļ�ʧ��: {ex.Message}");
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
        lock (stateLock)
        {
            UpdateSettingsFromUI(); // ȷ������������µ�UI����
            string configFile = AppSettings.ConfigFilePath;
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(configFile, json);
                Console.WriteLine($"�����ѱ��浽 {configFile}");
            }
            catch (Exception ex) { Console.WriteLine($"����: ���������ļ�ʧ��: {ex.Message}"); }
        }
    }

    private void ApplySettingsToUI()
    {
        lock (stateLock)
        {
            if (settings == null) settings = new AppSettings();
            priceThresholdTextBox.Text = settings.PriceThreshold.ToString();
            priceThresholdLabel.Text = $"��߹���۸�: {settings.PriceThreshold}";
            stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();
            stopAfterPurchasesLabel.Text = $"ֹͣ�������: {settings.StopAfterPurchases}";
            switch (settings.PurchaseQuantity)
            {
                case 1: quantityComboBox.SelectedIndex = 0; break;
                case 25: quantityComboBox.SelectedIndex = 1; break;
                case 100: quantityComboBox.SelectedIndex = 2; break;
                case 200: default: quantityComboBox.SelectedIndex = 3; break;
            }
            quantityLabel.Text = $"��������: {settings.PurchaseQuantity}";
            enableTimerCheckBox.Checked = settings.IsTimerEnabled;
            // ȷ��ʱ���ʽ��Ч
            if (!TimeSpan.TryParseExact(settings.StartTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            {
                settings.StartTimeString = "08:00:00";
            }
            if (!TimeSpan.TryParseExact(settings.StopTimeString, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            {
                settings.StopTimeString = "23:00:00";
            }
            // ȷ�Ͽؼ��ѳ�ʼ��
            if (startTimeTextBox == null || stopTimeTextBox == null)
            {
                Console.WriteLine("����: startTimeTextBox �� stopTimeTextBox δ��ʼ����");
                return;
            }
            startTimeTextBox.Text = settings.StartTimeString;
            startTimeLabel.Text = $"��ʼʱ��: {settings.StartTimeString}";
            stopTimeTextBox.Text = settings.StopTimeString;
            stopTimeLabel.Text = $"ֹͣʱ��: {settings.StopTimeString}";
            Console.WriteLine($"ApplySettingsToUI: startTimeTextBox.Text ����Ϊ {startTimeTextBox.Text}, stopTimeTextBox.Text ����Ϊ {stopTimeTextBox.Text}");
            isTimerEnabled = settings.IsTimerEnabled;
            actionDelayMinTextBox.Text = settings.ActionDelayMin.ToString();
            actionDelayMinLabel.Text = $"���������С: {settings.ActionDelayMin}";
            actionDelayMaxTextBox.Text = settings.ActionDelayMax.ToString();
            actionDelayMaxLabel.Text = $"����������: {settings.ActionDelayMax}";
            ocrRetryDelayMinTextBox.Text = settings.OcrRetryDelayMin.ToString();
            ocrRetryDelayMinLabel.Text = $"OCR ������С: {settings.OcrRetryDelayMin}";
            ocrRetryDelayMaxTextBox.Text = settings.OcrRetryDelayMax.ToString();
            ocrRetryDelayMaxLabel.Text = $"OCR �������: {settings.OcrRetryDelayMax}";
            notificationDelayMinTextBox.Text = settings.NotificationDelayMin.ToString();
            notificationDelayMinLabel.Text = $"֪ͨ�ȴ���С: {settings.NotificationDelayMin}";
            notificationDelayMaxTextBox.Text = settings.NotificationDelayMax.ToString();
            notificationDelayMaxLabel.Text = $"֪ͨ�ȴ����: {settings.NotificationDelayMax}";
            ocrCheckDelayMinTextBox.Text = settings.OcrCheckDelayMin.ToString();
            ocrCheckDelayMinLabel.Text = $"OCR �����С: {settings.OcrCheckDelayMin}";
            ocrCheckDelayMaxTextBox.Text = settings.OcrCheckDelayMax.ToString();
            ocrCheckDelayMaxLabel.Text = $"OCR ������: {settings.OcrCheckDelayMax}";
        }
    }

    private void UpdateSettingsFromUI()
    {
        lock (stateLock)
        {
            if (settings == null) return;
            if (int.TryParse(priceThresholdTextBox.Text, out int price) && price >= 0) { settings.PriceThreshold = price; priceThresholdLabel.Text = $"��߹���۸�: {price}"; } else priceThresholdTextBox.Text = settings.PriceThreshold.ToString();
            if (int.TryParse(stopAfterPurchasesTextBox.Text, out int stopCount) && stopCount >= 0) { settings.StopAfterPurchases = stopCount; stopAfterPurchasesLabel.Text = $"ֹͣ�������: {stopCount}"; } else stopAfterPurchasesTextBox.Text = settings.StopAfterPurchases.ToString();
            switch (quantityComboBox.SelectedIndex)
            {
                case 0: settings.PurchaseQuantity = 1; break;
                case 1: settings.PurchaseQuantity = 25; break;
                case 2: settings.PurchaseQuantity = 100; break;
                case 3: default: settings.PurchaseQuantity = 200; break;
            }
            quantityLabel.Text = $"��������: {settings.PurchaseQuantity}";
            settings.IsTimerEnabled = enableTimerCheckBox.Checked;
            if (TimeSpan.TryParseExact(startTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _)) { settings.StartTimeString = startTimeTextBox.Text; startTimeLabel.Text = $"��ʼʱ��: {settings.StartTimeString}"; } else startTimeTextBox.Text = settings.StartTimeString;
            if (TimeSpan.TryParseExact(stopTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _)) { settings.StopTimeString = stopTimeTextBox.Text; stopTimeLabel.Text = $"ֹͣʱ��: {settings.StopTimeString}"; } else stopTimeTextBox.Text = settings.StopTimeString;
            if (int.TryParse(actionDelayMinTextBox.Text, out int actionMin) && actionMin >= 0) { settings.ActionDelayMin = actionMin; actionDelayMinLabel.Text = $"���������С: {actionMin}"; } else actionDelayMinTextBox.Text = settings.ActionDelayMin.ToString();
            if (int.TryParse(actionDelayMaxTextBox.Text, out int actionMax) && actionMax >= settings.ActionDelayMin) { settings.ActionDelayMax = actionMax; actionDelayMaxLabel.Text = $"����������: {actionMax}"; } else actionDelayMaxTextBox.Text = settings.ActionDelayMax.ToString();
            if (int.TryParse(ocrRetryDelayMinTextBox.Text, out int retryMin) && retryMin >= 0) { settings.OcrRetryDelayMin = retryMin; ocrRetryDelayMinLabel.Text = $"OCR ������С: {retryMin}"; } else ocrRetryDelayMinTextBox.Text = settings.OcrRetryDelayMin.ToString();
            if (int.TryParse(ocrRetryDelayMaxTextBox.Text, out int retryMax) && retryMax >= settings.OcrRetryDelayMin) { settings.OcrRetryDelayMax = retryMax; ocrRetryDelayMaxLabel.Text = $"OCR �������: {retryMax}"; } else ocrRetryDelayMaxTextBox.Text = settings.OcrRetryDelayMax.ToString();
            if (int.TryParse(notificationDelayMinTextBox.Text, out int notifyMin) && notifyMin >= 0) { settings.NotificationDelayMin = notifyMin; notificationDelayMinLabel.Text = $"֪ͨ�ȴ���С: {notifyMin}"; } else notificationDelayMinTextBox.Text = settings.NotificationDelayMin.ToString();
            if (int.TryParse(notificationDelayMaxTextBox.Text, out int notifyMax) && notifyMax >= settings.NotificationDelayMin) { settings.NotificationDelayMax = notifyMax; notificationDelayMaxLabel.Text = $"֪ͨ�ȴ����: {notifyMax}"; } else notificationDelayMaxTextBox.Text = settings.NotificationDelayMax.ToString();
            if (int.TryParse(ocrCheckDelayMinTextBox.Text, out int ocrMin) && ocrMin >= 0) { settings.OcrCheckDelayMin = ocrMin; ocrCheckDelayMinLabel.Text = $"OCR �����С: {ocrMin}"; } else ocrCheckDelayMinTextBox.Text = settings.OcrCheckDelayMin.ToString();
            if (int.TryParse(ocrCheckDelayMaxTextBox.Text, out int ocrMax) && ocrMax >= settings.OcrCheckDelayMin) { settings.OcrCheckDelayMax = ocrMax; ocrCheckDelayMaxLabel.Text = $"OCR ������: {ocrMax}"; } else ocrCheckDelayMaxTextBox.Text = settings.OcrCheckDelayMax.ToString();
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

    // InitializeExternalDependencies - �Ƴ��� MainForm �� F12 �Ķ���
    private void InitializeExternalDependencies()
    {
        try
        {
            KeyboardHook.StopKey = settings.StopKey;
            // MainForm ����ֱ�Ӷ��� StopAutoBuyer �¼������� manualStop ����� Stop
            // KeyboardHook.StopAutoBuyer += HandleKeyboardStopRequest; // <-- ���Ƴ�
            KeyboardHook.Initialize(); // ��Ȼ��Ҫ��ʼ�����ӣ��� AutoBuyer �ڲ����Զ���
            UpdateStatusLabelSafe("���̹��� OK", statusSuccessColor);
        }
        catch (Exception ex)
        {
            HandleInitError("���̹���", ex);
            UpdateStatusLabelSafe("���̹���ʧ��!", statusErrorColor);
        }

        MouseSimulator.RandomRangeNear = settings.RandomRangeNear;
        MouseSimulator.RandomRangeShake = settings.RandomRangeShake;

        UpdateStatusLabelSafe("��ʼ����ɣ�׼��������", textColor);
    }

    private void HandleInitError(string componentName, Exception ex)
    {
        Console.WriteLine($"����: ��ʼ�� {componentName} ʧ��: {ex.Message}");
        MessageBox.Show($"{componentName} ��ʼ��ʧ��: {ex.Message}", $"{componentName} ��ʼ������", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    #endregion

    #region �¼�����
    // ... (AdjustWindowButton_Click, RestoreWindowButton_Click, SetPriceButton_Click, QuantityComboBox_SelectedIndexChanged, SetStopPurchasesButton_Click, EnableTimerCheckBox_CheckedChanged, SetTimerButton_Click, SetDelaysButton_Click ���ֲ���) ...
    private void AdjustWindowButton_Click(object sender, EventArgs e)
    {
        windowAdjuster.TargetWidth = settings.TargetWindowWidth;
        windowAdjuster.TargetHeight = settings.TargetWindowHeight;
        windowAdjuster.TargetX = settings.TargetWindowX;
        windowAdjuster.TargetY = settings.TargetWindowY;
        try { windowAdjuster.AdjustFocusedWindow(); UpdateStatusLabelSafe("���ڵ����ɹ�", statusSuccessColor); }
        catch (Exception ex) { MessageBox.Show($"��������ʧ��: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RestoreWindowButton_Click(object sender, EventArgs e)
    {
        try { windowAdjuster.RestoreOriginalWindow(); UpdateStatusLabelSafe("���ڻָ��ɹ�", statusSuccessColor); }
        catch (Exception ex) { MessageBox.Show($"�ָ�����ʧ��: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SetPriceButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            if (int.TryParse(priceThresholdTextBox.Text, out int val) && val >= 0)
            {
                settings.PriceThreshold = val;
                priceThresholdLabel.Text = $"��߹���۸�: {val}";
                autoBuyer.SetPriceThreshold(val);
                MessageBox.Show($"��߹��򵥼��Ѹ���Ϊ {val}", "���óɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("��������Ч�ķǸ������۸�", "�������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            quantityLabel.Text = $"��������: {qty}";
            autoBuyer.SetPurchaseQuantity(qty);
            Console.WriteLine($"��������������Ϊ: {qty}");
        }
    }

    private void SetStopPurchasesButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            if (int.TryParse(stopAfterPurchasesTextBox.Text, out int val) && val >= 0)
            {
                settings.StopAfterPurchases = val;
                stopAfterPurchasesLabel.Text = $"ֹͣ�������: {val}";
                autoBuyer.SetStopAfterPurchases(val);
                MessageBox.Show($"���ڹ��� {val} �κ�ֹͣ (0 ��ʾ����)", "���óɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("��������Ч�ķǸ�����������", "�������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            Console.WriteLine($"��ʱ��״̬: {(isTimerEnabled ? "����" : "����")}");
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
                    UpdateStatusLabelSafe("������ (��ʱ���ѽ���)", statusSuccessColor);
                }
                else
                {
                    UpdateStatusLabelSafe("��ʱ���ѽ���", textColor);
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
                    startTimeLabel.Text = $"��ʼʱ��: {settings.StartTimeString}";
                    timeChanged = true;
                }
            }
            else
            {
                startTimeTextBox.Text = settings.StartTimeString;
                MessageBox.Show("��ʼʱ���ʽ��Ч (ӦΪ HH:mm:ss)", "�������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (TimeSpan.TryParseExact(stopTimeTextBox.Text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out _))
            {
                if (settings.StopTimeString != stopTimeTextBox.Text)
                {
                    settings.StopTimeString = stopTimeTextBox.Text;
                    stopTimeLabel.Text = $"ֹͣʱ��: {settings.StopTimeString}";
                    timeChanged = true;
                }
            }
            else
            {
                stopTimeTextBox.Text = settings.StopTimeString;
                MessageBox.Show("ֹͣʱ���ʽ��Ч (ӦΪ HH:mm:ss)", "�������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (timeChanged && isTimerEnabled)
            {
                Console.WriteLine("��ʱʱ����ģ����ö�ʱ��״̬...");
                ResetTimerState();
            }
            else if (timeChanged)
            {
                Console.WriteLine("��ʱʱ���Ѹ��£�����ʱ��δ���á�");
            }

            string statusMsg = isTimerEnabled ? GetTimerStatusString() : "��ʱ���ѽ���";
            MessageBox.Show($"��ʱ������Ӧ�á�\n{statusMsg}", "��ʱ����", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SetDelaysButton_Click(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            UpdateSettingsFromUI();
            MessageBox.Show($"�ӳ������Ѹ���:\n�������: {settings.ActionDelayMin}-{settings.ActionDelayMax}ms\nOCR ���Լ��: {settings.OcrRetryDelayMin}-{settings.OcrRetryDelayMax}ms\n֪ͨ�ȴ�: {settings.NotificationDelayMin}-{settings.NotificationDelayMax}ms\nOCR ���: {settings.OcrCheckDelayMin}-{settings.OcrCheckDelayMax}ms", "���óɹ�", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }


    // Start/Stop ��ť�߼� - �Ƴ� manualStop
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
                    UpdateStatusLabelSafe("������...", statusSuccessColor);
                    Console.WriteLine("�ֶ���������");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"��������ʧ��: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStartButtonState(false);
                    UpdateStatusLabelSafe("�����쳣", statusErrorColor);
                }
            }
            else // �����������
            {
                UpdateStatusLabelSafe("����ֹͣ (��ť)...", statusWarningColor); // ��ѡ��ʱ����
                autoBuyer.Stop(); // ֱ�ӵ��� Stop
                Console.WriteLine("�ֶ�ֹͣ���� (��ť)");
                // �ȴ� AutoBuyer.Stopped �¼����� UpdateUIOnStop
            }
        }
    }

    // HandleKeyboardStopRequest �������Ƴ�

    // ���� AutoBuyer ֹͣ�¼�
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
        Console.WriteLine("HandleAutoBuyerStopped: �ѵ��� UpdateUIOnStop��");
    }

    // UpdateUIOnStop ���� - �Ƴ��� manualStop
    private void UpdateUIOnStop()
    {
        lock (stateLock)
        {
            UpdateStartButtonState(false); // ͳһ���ð�ťΪ "��ʼ" ״̬

            Console.WriteLine($"UpdateUIOnStop ����: isTimerEnabled={isTimerEnabled}, IsRunning={autoBuyer.IsRunning}");

            // --- ͳһ���� ---
            // 1. ����״̬��ǩΪͳһ�� "��ֹͣ"
            UpdateStatusLabelSafe("��ֹͣ", statusWarningColor); // ��ʹ�� textColor
            Console.WriteLine("״̬����Ϊ: ��ֹͣ");

            // 2. ���ö�ʱ��״̬��׼����һ�����ڣ������ʱ�����ã�
            Console.WriteLine("���ö�ʱ��״̬...");
            ResetTimerState(); // ����ֹͣԭ�򣬶����� ResetTimerState

            // 3. ������Ҫ���� manualStop ��־
            Console.WriteLine("UpdateUIOnStop: ֹͣUI������ɡ�");
            // --- ͳһ�������� ---
        }
    }


    private void UpdateStatusLabelSafe(string text, Color color)
    {
        if (statusLabel == null || statusLabel.IsDisposed) return;

        // ֻ�е�״̬�ı�����ɫ�����仯ʱ�Ÿ���
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
                    Console.WriteLine($"״̬���� (�첽): {text}");
                }
            }));
        }
        else
        {
            statusLabel.Text = text;
            statusLabel.ForeColor = color;
            Console.WriteLine($"״̬���� (ͬ��): {text}");
        }
    }

    private void UpdateStartButtonState(bool isRunning)
    {
        if (startBuyingButton == null || startBuyingButton.IsDisposed) return;
        if (startBuyingButton.InvokeRequired) { startBuyingButton.BeginInvoke(new Action(() => UpdateStartButtonState(isRunning))); return; }
        if (isRunning)
        {
            startBuyingButton.Text = "ֹͣ���� (�� F12 ֹͣ)"; startBuyingButton.Tag = "stop"; startBuyingButton.BackColor = statusErrorColor;
            startBuyingButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 110, 110); startBuyingButton.FlatAppearance.MouseDownBackColor = statusErrorColor;
            toolTip.SetToolTip(startBuyingButton, "ֹͣ�Զ����� (�� F12 ֹͣ)");
        }
        else
        {
            startBuyingButton.Text = "��ʼ���� (�� F12 ֹͣ)"; startBuyingButton.Tag = "start"; startBuyingButton.Enabled = true; startBuyingButton.BackColor = accentColor;
            startBuyingButton.FlatAppearance.MouseOverBackColor = accentHoverColor; startBuyingButton.FlatAppearance.MouseDownBackColor = accentColor;
            toolTip.SetToolTip(startBuyingButton, "��ʼ�Զ����� (�� F12 ֹͣ)");
        }
    }

    // ResetTimerState �� ClearTimerState ���ֲ���
    private bool ResetTimerState()
    {
        lock (stateLock)
        {
            TimeSpan startTimeOfDay, stopTimeOfDay;
            string startTimeToParse = string.IsNullOrWhiteSpace(startTimeTextBox.Text) ? settings.StartTimeString : startTimeTextBox.Text;
            string stopTimeToParse = string.IsNullOrWhiteSpace(stopTimeTextBox.Text) ? settings.StopTimeString : stopTimeTextBox.Text;

            Console.WriteLine($"ResetTimerState: ���Խ��� startTimeToParse={startTimeToParse}, stopTimeToParse={stopTimeToParse}");

            if (!TimeSpan.TryParseExact(startTimeToParse, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out startTimeOfDay))
            {
                Console.WriteLine($"��������ʱ��ʧ��: {startTimeToParse}��ʹ��Ĭ��ֵ 08:00:00");
                startTimeTextBox.Text = "08:00:00";
                startTimeLabel.Text = "��ʼʱ��: 08:00:00";
                settings.StartTimeString = "08:00:00";
                startTimeOfDay = TimeSpan.ParseExact("08:00:00", "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            }
            if (!TimeSpan.TryParseExact(stopTimeToParse, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out stopTimeOfDay))
            {
                Console.WriteLine($"�����ر�ʱ��ʧ��: {stopTimeToParse}��ʹ��Ĭ��ֵ 23:00:00");
                stopTimeTextBox.Text = "23:00:00";
                stopTimeLabel.Text = "ֹͣʱ��: 23:00:00";
                settings.StopTimeString = "23:00:00";
                stopTimeOfDay = TimeSpan.ParseExact("23:00:00", "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            }

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime potentialStartTime = today.Add(startTimeOfDay);

            // �����´�����ʱ��
            if (now < potentialStartTime)
            {
                scheduledStartTime = potentialStartTime;
            }
            else
            {
                scheduledStartTime = potentialStartTime.AddDays(1);
            }

            // ��������´�����ʱ���ֹͣʱ��
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
            Console.WriteLine($"��ʱ������: �´����� {scheduledStartTime.Value:G}, �´�ֹͣ {scheduledStopTime.Value:G}");

            // ����״̬��ǩ����Ҫ���⸲�� "������" �� "��ֹͣ"
            if (isTimerEnabled && !autoBuyer.IsRunning && statusLabel.Text != "��ֹͣ")
            {
                UpdateStatusLabelSafe(GetTimerStatusString(), textColor);
            }
            else if (!isTimerEnabled && !autoBuyer.IsRunning && statusLabel.Text != "��ֹͣ")
            {
                UpdateStatusLabelSafe("��ʱ���ѽ���", textColor);
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
            Console.WriteLine("��ʱ��״̬�����");
            if (!autoBuyer.IsRunning && statusLabel.Text != "��ֹͣ")
            {
                UpdateStatusLabelSafe("��ʱ���ѽ���", textColor);
            }
        }
    }


    // Timer_Tick - �Ƴ��� manualStop
    private void Timer_Tick(object sender, EventArgs e)
    {
        lock (stateLock)
        {
            // �Ƴ� manualStop ���
            if (!isTimerEnabled || !scheduledStartTime.HasValue || !scheduledStopTime.HasValue)
            {
                return;
            }

            DateTime now = DateTime.Now;

            // ����Ƿ񵽴�����ʱ��
            if (!hasStartedToday && !autoBuyer.IsRunning && now >= scheduledStartTime.Value && now < scheduledStopTime.Value)
            {
                Console.WriteLine($"���￪��ʱ�� {scheduledStartTime.Value:G}����������...");
                UpdateSettingsFromUI();
                ApplySettingsFromUIToAutoBuyer();
                try
                {
                    // �Ƴ� manualStop ��������
                    autoBuyer.Start();
                    if (autoBuyer.IsRunning)
                    {
                        UpdateStartButtonState(true);
                        UpdateStatusLabelSafe($"������ (��ʱ������ {scheduledStartTime.Value:G})", statusSuccessColor);
                        hasStartedToday = true;
                        hasStoppedToday = false;
                        Console.WriteLine("��ʱ�����ɹ�");
                    }
                    else
                    {
                        Console.WriteLine($"��ʱ�������⵽ AutoBuyer δ���У���������ʧ�ܡ�");
                        UpdateStatusLabelSafe($"��ʱ����ʧ��", statusErrorColor);
                        UpdateStartButtonState(false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"��ʱ�����쳣: {ex.Message}");
                    UpdateStatusLabelSafe($"��ʱ�����쳣: {ex.Message}", statusErrorColor);
                    UpdateStartButtonState(false);
                }
            }
            // ����Ƿ񵽴�ֹͣʱ��
            else if (autoBuyer.IsRunning && now >= scheduledStopTime.Value)
            {
                Console.WriteLine($"����ر�ʱ�� {scheduledStopTime.Value:G}��ֹͣ����...");
                try
                {
                    autoBuyer.Stop(); // ֱ�ӵ��� Stop
                    // �ȴ� AutoBuyer.Stopped �¼����� UpdateUIOnStop
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"��ʱֹͣ�쳣: {ex.Message}");
                    UpdateStatusLabelSafe($"��ʱֹͣ�쳣: {ex.Message}", statusErrorColor);
                }
            }
            // ����Ƿ����
            else if (scheduledStartTime.HasValue && now.Date > scheduledStartTime.Value.Date && (hasStartedToday || hasStoppedToday))
            {
                Console.WriteLine($"��⵽���죬���ö�ʱ��״̬����һ����...");
                ResetTimerState();
            }
        }
    }


    private string GetTimerStatusString()
    {
        lock (stateLock)
        {
            if (!isTimerEnabled) return "��ʱ���ѽ���";
            if (scheduledStartTime.HasValue && scheduledStopTime.HasValue)
            {
                // ���⸲�� "������" �� "��ֹͣ"
                if (autoBuyer.IsRunning) return statusLabel.Text;
                if (statusLabel.Text == "��ֹͣ") return statusLabel.Text;
                return $"��ʱ������: �´����� {scheduledStartTime.Value:G}";
            }
            return "��ʱ�����ã��ȴ�����ʱ��...";
        }
    }

    // MainForm_FormClosing - �Ƴ��� manualStop
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        Console.WriteLine("Ӧ�ó���ر���...");
        try { timer?.Stop(); timer?.Dispose(); } catch (Exception ex) { Console.WriteLine($"�ر� Timer ����: {ex.Message}"); }
        try
        {
            if (autoBuyer != null)
            {
                if (autoBuyer.IsRunning)
                {
                    Console.WriteLine("����ֹͣ AutoBuyer...");
                    // �Ƴ� manualStop = true;
                    autoBuyer.Stop();
                    System.Threading.Thread.Sleep(500);
                }
                autoBuyer.Stopped -= HandleAutoBuyerStopped;
                autoBuyer.Dispose();
            }
        }
        catch (Exception ex) { Console.WriteLine($"ֹͣ/�ͷ� AutoBuyer ����: {ex.Message}"); }

        // ������Ҫȡ������ MainForm �� HandleKeyboardStopRequest����Ϊ���ѱ��Ƴ�
        // KeyboardHook.StopAutoBuyer -= HandleKeyboardStopRequest; // <-- ���Ƴ�

        try { KeyboardHook.Cleanup(); } catch (Exception ex) { Console.WriteLine($"���� KeyboardHook ����: {ex.Message}"); }
        try { SaveSettings(); } catch (Exception ex) { Console.WriteLine($"�������ó���: {ex.Message}"); }
        Console.WriteLine("������ɣ������˳���");
    }
    #endregion

    // Main ��������������������
    [STAThread]
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        try
        {
            // �Ȼ�ȡ��ǰ�̵߳�ʵ��
            System.Threading.Thread uiThread = System.Threading.Thread.CurrentThread;

            // �ڻ�ȡ����ʵ��������������
            uiThread.CurrentCulture = new CultureInfo("zh-CN");
            uiThread.CurrentUICulture = new CultureInfo("zh-CN");

            Console.WriteLine($"Current Culture set to: {System.Threading.Thread.CurrentThread.CurrentCulture.Name}");
            Console.WriteLine($"Current UI Culture set to: {System.Threading.Thread.CurrentThread.CurrentUICulture.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"����������ʧ��: {ex.Message}");
        }
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(new MainForm());
    }
}