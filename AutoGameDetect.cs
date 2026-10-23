// AutoGameDetect — 4K 显示器自动分辨率切换工具
// Copyright (c) 2025 keitarol
//
// Licensed under the MIT License.
// You may obtain a copy of the License at
//     https://opensource.org/licenses/MIT
//
// 本项目基于 MIT 协议开源，可自由使用、修改、分发，
// 但需保留上述版权声明。详见仓库 LICENSE 文件。

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace AutoGameDetect
{
    public class AppContext : ApplicationContext
    {
        // ================================================================
        //  Localization: 跟随系统语言，zh-* 显示中文，其余全部显示英文
        // ================================================================
        private static readonly bool IsChinese =
            System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("zh",
                StringComparison.OrdinalIgnoreCase);

        // 中文文案（key 统一，便于对照维护）
        private const string AppVersion = "1.2.3";
        private static readonly Dictionary<string, string> Dict_ZH = new Dictionary<string, string>
        {
            { "win_title", "分辨率自动监控 - 设置" },
            { "lbl_info", "选择需要自动切换到 2K 的游戏：" },
            { "btn_scan", "🔄 扫描已安装游戏" },
            { "btn_scanning", "扫描中..." },
            { "lbl_target", "目标分辨率:" },
            { "lbl_current", "当前: {0}×{1}" },
            { "lbl_proc", "进程名(不含.exe):" },
            { "btn_add", "添加手动" },
            { "btn_del", "删除选中" },
            { "lbl_steam", "Steam 路径(可选):" },
            { "btn_browse", "浏览..." },
            { "dlg_steam_desc", "选择 Steam 安装目录（含 steam.exe 的文件夹）" },
            { "lbl_steam_hint", "留空 = 自动检测（默认路径 / 注册表，支持任意安装位置）" },
            { "btn_close", "关闭" },
            { "tray_text", "分辨率自动监控\n{0} | 监控 {1} 个游戏" },
            { "menu_settings", "设置监控游戏" },
            { "menu_pause", "⏸ 暂停监控" },
            { "menu_resume", "▶ 恢复监控" },
            { "menu_restore", "立即恢复 4K" },
            { "menu_quit", "退出" },
            { "status_paused", "⏸ 已暂停" },
            { "status_running", "▶ 运行中" },
            { "status_fmt", "状态: {0}  |  监控 {1} 个游戏" },
            { "msg_already", "该游戏已在列表中。" },
            { "title_hint", "提示" },
            { "scan_done", "扫描完成" },
            { "scan_added", "，新增 {0} 个游戏" },
            { "scan_removed", "，移除 {0} 个已卸载的游戏" },
            { "scan_fresh", "，列表已是最新" },
            { "scan_prompt", "。\n请勾选要自动监控的游戏。" },
            { "title_scan_done", "扫描完成" },
            { "balloon_start_title", "游戏检测" },
            { "balloon_start", "检测到 {0} 已启动，已切换到 {1}×{2}" },
            { "balloon_exit_title", "游戏退出" },
            { "balloon_exit", "已恢复原始分辨率 {0}×{1}" },
            { "manual_suffix", " (手动)" },
            { "main_already", "分辨率自动监控已在运行！\n请在系统托盘图标上操作。" },
            { "title_already", "已在运行" },
            { "capture_failed_title", "无法读取当前分辨率" },
            { "capture_failed", "为保护显示设置，自动切换已暂停。请在桌面分辨率正常后，右键托盘图标选择【恢复监控】重试。" },
        };

        // 英文文案
        private static readonly Dictionary<string, string> Dict_En = new Dictionary<string, string>
        {
            { "win_title", "Resolution Switcher - Settings" },
            { "lbl_info", "Select games to auto-switch to 2K:" },
            { "btn_scan", "🔄 Scan Installed Games" },
            { "btn_scanning", "Scanning..." },
            { "lbl_target", "Target resolution:" },
            { "lbl_current", "Current: {0}×{1}" },
            { "lbl_proc", "Process name (no .exe):" },
            { "btn_add", "Add Manually" },
            { "btn_del", "Remove Selected" },
            { "lbl_steam", "Steam path (optional):" },
            { "btn_browse", "Browse..." },
            { "dlg_steam_desc", "Select the Steam installation folder (the one containing steam.exe)" },
            { "lbl_steam_hint", "Leave blank = auto-detect (default path / registry, any install location)" },
            { "btn_close", "Close" },
            { "tray_text", "Resolution Switcher\n{0} | Monitoring {1} game(s)" },
            { "menu_settings", "Settings" },
            { "menu_pause", "⏸ Pause Monitoring" },
            { "menu_resume", "▶ Resume Monitoring" },
            { "menu_restore", "Restore 4K Now" },
            { "menu_quit", "Exit" },
            { "status_paused", "⏸ Paused" },
            { "status_running", "▶ Running" },
            { "status_fmt", "Status: {0} | Monitoring {1} game(s)" },
            { "msg_already", "This game is already in the list." },
            { "title_hint", "Notice" },
            { "scan_done", "Scan complete" },
            { "scan_added", ", {0} new game(s) added" },
            { "scan_removed", ", {0} uninstalled game(s) removed" },
            { "scan_fresh", ", the list is already up to date" },
            { "scan_prompt", ".\nCheck the games you want to monitor." },
            { "title_scan_done", "Scan Complete" },
            { "balloon_start_title", "Game Detected" },
            { "balloon_start", "{0} has started. Switched to {1}×{2}" },
            { "balloon_exit_title", "Game Exited" },
            { "balloon_exit", "Restored original resolution {0}×{1}" },
            { "manual_suffix", " (manual)" },
            { "main_already", "Resolution Switcher is already running!\nUse the system tray icon." },
            { "title_already", "Already Running" },
            { "capture_failed_title", "Cannot Read Current Resolution" },
            { "capture_failed", "Auto-switching was paused to protect your display settings. Once the desktop resolution is normal, right-click the tray icon and choose Resume Monitoring to retry." },
        };

        private static readonly Dictionary<string, string> Dict = IsChinese ? Dict_ZH : Dict_En;

        // 统一取词：缺失 key 时原样返回，避免空界面
        private static string T(string key)
        {
            string s;
            return Dict.TryGetValue(key, out s) ? s : key;
        }

        // 自适应宽度：控件宽度 = 文本测量宽度 + 内边距，中英文自动伸缩
        private static int ButtonWidth(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font).Width + 24;
        }

        private static int LabelWidth(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font).Width + 6;
        }

        // --- Win32 P/Invoke ---
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int EnumDisplaySettings(
            string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettingsEx(
            string lpszDeviceName, ref DEVMODE lpDevMode,
            IntPtr hwnd, uint dwFlags, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettingsEx(
            string lpszDeviceName, IntPtr lpDevMode,
            IntPtr hwnd, uint dwFlags, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DEVMODE
        {
            public const int CCHDEVICENAME = 32;
            public const int CCHFORMNAME = 32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int ENUM_REGISTRY_SETTINGS = -2;
        private const int CDS_TEST = 0x02;
        private const int CDS_GLOBAL = 0x08;
        private const int CDS_UPDATEREGISTRY = 0x01;
        private const int DISP_CHANGE_SUCCESSFUL = 0;
        private const int DM_DISPLAYFREQUENCY = 0x00400000;

        // --- Data ---
        class MonitoredGame
        {
            public string DisplayName { get; set; }
            public string ProcessName { get; set; }  // Without .exe
            public int TargetWidth { get; set; }
            public int TargetHeight { get; set; }
            public bool Enabled { get; set; }
        }

        private DEVMODE _originalMode;
        private List<MonitoredGame> _games = new List<MonitoredGame>();
        private string _configPath;
        private string _manualSteamPath = "";   // 手动指定的 Steam 安装路径（留空=自动检测）
        private NotifyIcon _trayIcon;
        private System.Threading.Timer _pollTimer;
        private bool _isInGameMode = false;
        private string _currentGameProcess = "";
        private bool _paused = false;
        private bool _origValid = false;                       // _originalMode 是否为有效模式
        private DateTime _lastGameExit = DateTime.MinValue;    // 游戏退出时间，用于显示模式守护
        private string _logPath;
        private SynchronizationContext _ui;                        // UI 线程上下文，用于跨线程唤起设置窗口
        private EventWaitHandle _showEvent;                        // 第二实例发信号 → 唤起设置窗口
        private Bitmap _trayBmp;                                   // 托盘图标位图：保持引用防止 GC 回收导致图标消失
        private bool _autostart = false;                           // 开机自启时不弹设置窗口

        // --- UI components ---
        private Form _configForm;
        private CheckedListBox _chkList;
        private NumericUpDown _numWidth;
        private NumericUpDown _numHeight;
        private Button _btnAddManual;
        private Button _btnScan;
        private TextBox _txtSteamPath;
        private Label _lblStatus;

        public AppContext(bool autostart)
        {
            _autostart = autostart;
            _ui = SynchronizationContext.Current;
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ResolutionSwitcher", "monitored_games.txt");
            _logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ResolutionSwitcher", "resolution.log");

            // 捕获原始分辨率：必须先校验，绝不让无效（全零）DEVMODE 流入后续的
            // ChangeDisplaySettingsEx 调用 —— 那会把桌面直接掉到驱动默认低分辨率
            _originalMode = GetCurrentDisplayMode();
            _origValid = IsValidMode(_originalMode);
            if (!_origValid)
            {
                // 回退：使用注册表中记录的桌面模式
                DEVMODE reg = GetRegistryDisplayMode();
                if (IsValidMode(reg)) { _originalMode = reg; _origValid = true; }
            }
            Log("startup: original=" + ModeStr(_originalMode) + ", valid=" + _origValid);
            if (!_origValid) _paused = true;   // 无法确定原始分辨率时暂停监控，避免破坏显示设置

            // 第二实例唤起窗口的事件（必须先于任何第二实例创建）
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "ResolutionAutoDetectShowEvent");

            // Tray icon
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = CreateTrayIcon();
            _trayIcon.Text = string.Format(T("tray_text"), T("status_running"), GetEnabledCount());
            _trayIcon.Visible = true;

            var menu = new ContextMenuStrip();
            menu.Items.Add(T("menu_settings"), null, (s, e) => ShowConfig());
            menu.Items.Add(T("menu_pause"), null, (s, e) => TogglePause());
            menu.Items[1].Name = "menuPause";
            menu.Items.Add(T("menu_restore"), null, (s, e) => RestoreResolution());
            menu.Items.Add("-");
            menu.Items.Add(T("menu_quit"), null, (s, e) => Quit());
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, e) => ShowConfig();

            if (!_origValid)
                ShowBalloon(T("capture_failed_title"), T("capture_failed"));

            LoadConfig();
            UpdateTrayText();

            // 无游戏时首次运行弹窗；手动双击启动时也总是弹窗（开机自启除外）
            if (_games.Count == 0 || !_autostart)
            {
                ShowConfig();
            }

            // Start polling (every 3 seconds)
            _pollTimer = new System.Threading.Timer(CheckGames, null, 5000, 3000);
        }

        private int GetEnabledCount()
        {
            int n = 0;
            foreach (var g in _games) if (g.Enabled) n++;
            return n;
        }

        // ================================================================
        //  Config window
        // ================================================================
        private void ShowConfig()
        {
            if (_configForm != null && !_configForm.IsDisposed)
            {
                _configForm.Activate();
                return;
            }

            _configForm = new Form();
            _configForm.Text = T("win_title") + " v" + AppVersion;
            _configForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            _configForm.StartPosition = FormStartPosition.CenterScreen;
            _configForm.MaximizeBox = false;
            Font uiFont = new Font("微软雅黑", 9);
            _configForm.Font = uiFont;
            _configForm.ClientSize = new Size(560, 460);
            _configForm.ShowInTaskbar = true;

            // 布局常量：控件宽度由文本测量结果计算，自适应中/英文
            const int M = 12;                 // 左右边距
            const int CTRL_GAP = 8;           // 控件间距
            const int NUD_WIDTH = 72;         // 数字输入框（纯数字，与语言无关）
            int CW = _configForm.ClientSize.Width;
            int y = 12;

            // Info
            var lblInfo = new Label
            {
                Text = T("lbl_info"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_info"), uiFont), 20),
                Font = uiFont,
            };
            _configForm.Controls.Add(lblInfo);
            y += 26;

            // Scan button
            _btnScan = new Button
            {
                Text = T("btn_scan"),
                Location = new Point(M, y),
                Size = new Size(ButtonWidth(T("btn_scan"), uiFont), 28),
                Font = uiFont,
            };
            _btnScan.Click += (s, e) => ScanAndPopulate();
            _configForm.Controls.Add(_btnScan);
            y += 36;

            // Checked list
            _chkList = new CheckedListBox
            {
                Location = new Point(M, y),
                Size = new Size(CW - 2 * M, 200),
                CheckOnClick = true,
                Font = new Font("Consolas", 9),
            };
            _configForm.Controls.Add(_chkList);
            y += 208;

            // Game resolution target
            var lblTarget = new Label
            {
                Text = T("lbl_target"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_target"), uiFont), 24),
                Font = uiFont,
            };
            _configForm.Controls.Add(lblTarget);

            int x = M + LabelWidth(T("lbl_target"), uiFont) + CTRL_GAP;
            _numWidth = new NumericUpDown
            {
                Location = new Point(x, y),
                Width = NUD_WIDTH,
                Minimum = 640,
                Maximum = 7680,
                Value = 2560,
                Font = uiFont,
            };
            _configForm.Controls.Add(_numWidth);

            var lblX = new Label
            {
                Text = "×",
                Location = new Point(x + NUD_WIDTH + 2, y),
                Size = new Size(LabelWidth("×", uiFont), 24),
                Font = uiFont,
            };
            _configForm.Controls.Add(lblX);

            _numHeight = new NumericUpDown
            {
                Location = new Point(x + NUD_WIDTH + 2 + LabelWidth("×", uiFont) + 2, y),
                Width = NUD_WIDTH,
                Minimum = 480,
                Maximum = 4320,
                Value = 1440,
                Font = uiFont,
            };
            _configForm.Controls.Add(_numHeight);

            // Current res display
            string curText = string.Format(T("lbl_current"),
                _originalMode.dmPelsWidth, _originalMode.dmPelsHeight);
            var lblCur = new Label
            {
                Text = curText,
                Location = new Point(x + NUD_WIDTH + 2 + LabelWidth("×", uiFont) + 2 + NUD_WIDTH + CTRL_GAP, y + 2),
                Size = new Size(LabelWidth(curText, uiFont), 20),
                ForeColor = Color.Gray,
                Font = uiFont,
            };
            _configForm.Controls.Add(lblCur);
            y += 32;

            // Add manual game
            var lblProc = new Label
            {
                Text = T("lbl_proc"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_proc"), uiFont), 24),
                Font = uiFont,
            };
            _configForm.Controls.Add(lblProc);

            _btnAddManual = new Button
            {
                Text = T("btn_add"),
                Size = new Size(ButtonWidth(T("btn_add"), uiFont), 26),
                Font = uiFont,
            };
            Button btnDel = new Button
            {
                Text = T("btn_del"),
                Size = new Size(ButtonWidth(T("btn_del"), uiFont), 26),
                ForeColor = Color.Red,
                Font = uiFont,
            };

            // 右对齐：删除按钮 → 添加按钮 → 输入框（宽度随语言自适应）
            btnDel.Location = new Point(CW - M - btnDel.Width, y);
            _btnAddManual.Location = new Point(btnDel.Left - CTRL_GAP - _btnAddManual.Width, y);
            int procX = M + LabelWidth(T("lbl_proc"), uiFont) + CTRL_GAP;
            TextBox txtProc = new TextBox
            {
                Location = new Point(procX, y),
                Width = _btnAddManual.Left - CTRL_GAP - procX,
                Font = uiFont,
            };
            _configForm.Controls.Add(txtProc);

            _btnAddManual.Click += (s, e) =>
            {
                string name = txtProc.Text.Trim();
                if (name.Length > 0)
                {
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        name = name.Substring(0, name.Length - 4);
                    foreach (var g in _games)
                    {
                        if (g.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show(T("msg_already"), T("title_hint"));
                            return;
                        }
                    }
                    _games.Add(new MonitoredGame
                    {
                        DisplayName = name + T("manual_suffix"),
                        ProcessName = name,
                        TargetWidth = (int)_numWidth.Value,
                        TargetHeight = (int)_numHeight.Value,
                        Enabled = true,
                    });
                    RefreshChecklist();
                    txtProc.Clear();
                    SaveConfig();
                    UpdateTrayText();
                }
            };
            _configForm.Controls.Add(_btnAddManual);

            // Delete selected
            btnDel.Click += (s, e) =>
            {
                for (int i = _chkList.CheckedIndices.Count - 1; i >= 0; i--)
                {
                    int idx = _chkList.CheckedIndices[i];
                    if (idx >= 0 && idx < _games.Count)
                        _games.RemoveAt(idx);
                }
                RefreshChecklist();
                SaveConfig();
                UpdateTrayText();
            };
            _configForm.Controls.Add(btnDel);
            y += 34;

            // Steam path (optional) - 支持非默认安装路径 / 绿色版 Steam
            var lblSteam = new Label
            {
                Text = T("lbl_steam"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_steam"), uiFont), 24),
                Font = uiFont,
            };
            _configForm.Controls.Add(lblSteam);

            Button btnBrowse = new Button
            {
                Text = T("btn_browse"),
                Size = new Size(ButtonWidth(T("btn_browse"), uiFont), 26),
                Font = uiFont,
            };
            btnBrowse.Location = new Point(CW - M - btnBrowse.Width, y);
            int steamX = M + LabelWidth(T("lbl_steam"), uiFont) + CTRL_GAP;
            _txtSteamPath = new TextBox
            {
                Location = new Point(steamX, y),
                Width = btnBrowse.Left - CTRL_GAP - steamX,
                Text = _manualSteamPath,
                Font = uiFont,
            };
            _configForm.Controls.Add(_txtSteamPath);
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = T("dlg_steam_desc");
                    dlg.ShowNewFolderButton = false;
                    if (dlg.ShowDialog() == DialogResult.OK)
                        _txtSteamPath.Text = dlg.SelectedPath;
                }
            };
            _configForm.Controls.Add(btnBrowse);
            var lblSteamHint = new Label
            {
                Text = T("lbl_steam_hint"),
                Location = new Point(M, y + 26),
                Size = new Size(LabelWidth(T("lbl_steam_hint"), uiFont), 16),
                ForeColor = Color.Gray,
                Font = new Font("微软雅黑", 8),
            };
            _configForm.Controls.Add(lblSteamHint);
            y += 50;

            // Status & close
            Button btnClose = new Button
            {
                Text = T("btn_close"),
                Size = new Size(ButtonWidth(T("btn_close"), uiFont), 28),
                Font = uiFont,
            };
            btnClose.Location = new Point(CW - M - btnClose.Width, y - 4);

            _lblStatus = new Label
            {
                Text = string.Format(T("status_fmt"),
                    _paused ? T("status_paused") : T("status_running"), GetEnabledCount()),
                Location = new Point(M, y),
                Size = new Size(btnClose.Left - M - CTRL_GAP, 20),
                ForeColor = Color.Gray,
                Font = uiFont,
            };
            _configForm.Controls.Add(_lblStatus);

            btnClose.Click += (s, e) =>
            {
                // Save checked state from UI
                for (int i = 0; i < _games.Count && i < _chkList.Items.Count; i++)
                    _games[i].Enabled = _chkList.GetItemChecked(i);
                _manualSteamPath = _txtSteamPath.Text.Trim();
                SaveConfig();
                UpdateTrayText();
                _configForm.Close();
            };
            _configForm.Controls.Add(btnClose);

            PopulateChecklist();
            _configForm.FormClosed += (s, e) => _configForm = null;

            _configForm.ShowDialog();
        }

        private void PopulateChecklist()
        {
            _chkList.Items.Clear();
            foreach (var g in _games)
            {
                string text = g.DisplayName.PadRight(36) + "  " + g.ProcessName.ToLower() + ".exe"
                    + "  →  " + g.TargetWidth + "×" + g.TargetHeight;
                int idx = _chkList.Items.Add(text);
                _chkList.SetItemChecked(idx, g.Enabled);
            }
        }

        private void RefreshChecklist()
        {
            PopulateChecklist();
        }

        private void ScanAndPopulate()
        {
            _btnScan.Enabled = false;
            _btnScan.Text = T("btn_scanning");
            Application.DoEvents();

            int added = 0;
            int removed = 0;
            List<MonitoredGame> scanned = ScanInstalledGames();

            // 1. 移除本次扫描中已不存在的 [Steam] 游戏（手动添加的不动）
            for (int i = _games.Count - 1; i >= 0; i--)
            {
                MonitoredGame g = _games[i];
                if (!g.DisplayName.StartsWith("[Steam]")) continue;

                bool stillInstalled = false;
                foreach (var s in scanned)
                {
                    if (g.DisplayName.Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase))
                    { stillInstalled = true; break; }
                }
                if (!stillInstalled)
                {
                    _games.RemoveAt(i);
                    removed++;
                }
            }

            // 2. 新增新游戏 / 刷新已有游戏信息（保留勾选状态）
            foreach (var s in scanned)
            {
                MonitoredGame existing = null;
                foreach (var g in _games)
                {
                    if (g.DisplayName.Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase))
                    { existing = g; break; }
                }
                if (existing == null)
                {
                    _games.Add(s);
                    added++;
                }
                else
                {
                    existing.ProcessName = s.ProcessName;
                    existing.TargetWidth = s.TargetWidth;
                    existing.TargetHeight = s.TargetHeight;
                }
            }

            RefreshChecklist();
            SaveConfig();
            UpdateTrayText();

            _btnScan.Enabled = true;
            _btnScan.Text = T("btn_scan");

            string msg = T("scan_done");
            if (added > 0) msg += string.Format(T("scan_added"), added);
            if (removed > 0) msg += string.Format(T("scan_removed"), removed);
            if (added == 0 && removed == 0) msg += T("scan_fresh");
            msg += T("scan_prompt");
            MessageBox.Show(msg, T("title_scan_done"));
        }

        // ================================================================
        //  Game scanning (reuse from ResolutionSwitcher)
        // ================================================================
        private List<MonitoredGame> ScanInstalledGames()
        {
            List<MonitoredGame> result = new List<MonitoredGame>();

            // Scan Steam
            string steamPath = FindSteamPath();
            if (steamPath != null)
            {
                List<string> libs = new List<string> { steamPath };
                string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    try
                    {
                        string vdf = File.ReadAllText(vdfPath, Encoding.UTF8);
                        foreach (Match m in Regex.Matches(vdf, "\"path\"\\s+\"([^\"]+)\""))
                        {
                            string p = m.Groups[1].Value.Replace("\\\\", "\\");
                            if (!libs.Contains(p) && Directory.Exists(p)) libs.Add(p);
                        }
                    }
                    catch { }
                }

                foreach (string lib in libs)
                {
                    string appsDir = Path.Combine(lib, "steamapps");
                    if (!Directory.Exists(appsDir)) continue;
                    try
                    {
                        foreach (string acf in Directory.GetFiles(appsDir, "*.acf"))
                        {
                            try
                            {
                                string content = File.ReadAllText(acf, Encoding.UTF8);
                                Match m = Regex.Match(content, "\"appid\"\\s+\"(\\d+)\"");
                                if (!m.Success) continue;
                                string appId = m.Groups[1].Value;
                                m = Regex.Match(content, "\"name\"\\s+\"((?:[^\"\\\\]|\\\\.)*)\"");
                                if (!m.Success) continue;
                                string name = m.Groups[1].Value.Replace("\\\"", "\"");
                                m = Regex.Match(content, "\"installdir\"\\s+\"((?:[^\"\\\\]|\\\\.)*)\"");
                                string installDir = m.Success ? m.Groups[1].Value.Replace("\\\"", "\"") : "";

                                // Skip known non-games
                                string nameLower = name.ToLowerInvariant();
                                if (nameLower.Contains("redistributables") ||
                                    nameLower.Contains("steamworks") ||
                                    nameLower.Contains("runtime") ||
                                    nameLower.Contains("proton")) continue;

                                // Try to find exe in install dir
                                string exeName = FindGameExe(Path.Combine(lib, "steamapps", "common", installDir));
                                if (string.IsNullOrEmpty(exeName))
                                    exeName = name.Replace(" ", "").Replace(":", "").Replace("'", "");

                                result.Add(new MonitoredGame
                                {
                                    DisplayName = "[Steam] " + name,
                                    ProcessName = exeName,
                                    TargetWidth = 2560,
                                    TargetHeight = 1440,
                                    Enabled = false,
                                });
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }

            return result;
        }

        private string FindGameExe(string dir)
        {
            if (!Directory.Exists(dir)) return null;
            try
            {
                string[] exes = Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly);
                string best = null;
                long bestSize = 0;
                string[] bad = { "unins", "install", "setup", "redist", "dx", "vc_redist",
                    "dotnet", "updater", "crash", "steam", "launcher", "eos", "easyanticheat",
                    "battleye", "sdk", "redistributable" };

                foreach (string exe in exes)
                {
                    string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                    bool skip = false;
                    foreach (string b in bad) if (name.Contains(b)) { skip = true; break; }
                    if (skip) continue;

                    try
                    {
                        long size = new FileInfo(exe).Length;
                        if (size < 1024 * 1024) continue; // < 1MB skip
                        if (size > bestSize)
                        {
                            bestSize = size;
                            best = Path.GetFileNameWithoutExtension(exe);
                        }
                    }
                    catch { }
                }
                return best;
            }
            catch { return null; }
        }

        private string FindSteamPath()
        {
            // 1. 用户手动指定的路径优先（绿色版/便携版 Steam）
            if (_manualSteamPath.Length > 0 && Directory.Exists(_manualSteamPath)) return _manualSteamPath;
            // 2. 默认安装路径
            string def = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            if (Directory.Exists(def)) return def;
            // 3. 注册表 InstallPath（Steam 装在任何位置都会写入，非默认安装也能找到）
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string p = key.GetValue("InstallPath") as string;
                        if (p != null && Directory.Exists(p)) return p;
                    }
                }
            }
            catch { }
            return null;
        }

        // ================================================================
        //  Game monitoring
        // ================================================================
        private void CheckGames(object state)
        {
            try
            {
                // 自愈：开机自启时任务栏可能尚未就绪导致托盘图标丢失，定期重挂
                if (_trayIcon != null && !_trayIcon.Visible)
                    _trayIcon.Visible = true;

                // 第二实例双击 → 唤起本实例的设置窗口（切回 UI 线程执行）
                if (_showEvent != null && _showEvent.WaitOne(0))
                {
                    if (_ui != null) _ui.Post(delegate { ShowConfig(); }, null);
                }
            }
            catch { }

            if (_paused) return;

            try
            {
                bool foundGame = false;
                string foundProc = "";
                int foundWidth = 0, foundHeight = 0;

                foreach (var g in _games)
                {
                    if (!g.Enabled) continue;

                    Process[] procs = Process.GetProcessesByName(g.ProcessName);
                    if (procs.Length > 0)
                    {
                        foundGame = true;
                        foundProc = g.ProcessName;
                        foundWidth = g.TargetWidth;
                        foundHeight = g.TargetHeight;
                        foreach (Process p in procs) p.Dispose();
                        break;
                    }

                    foreach (Process p in procs) p.Dispose();
                }

                if (foundGame && !_isInGameMode)
                {
                    // Game just started
                    _currentGameProcess = foundProc;
                    _isInGameMode = true;
                    SwitchResolution(foundWidth, foundHeight);
                    ShowBalloon(T("balloon_start_title"),
                        string.Format(T("balloon_start"), foundProc, foundWidth, foundHeight));
                }
                else if (!foundGame && _isInGameMode)
                {
                    // Game just exited
                    _isInGameMode = false;
                    _currentGameProcess = "";
                    RestoreResolution();
                    _lastGameExit = DateTime.Now;
                    ShowBalloon(T("balloon_exit_title"),
                        string.Format(T("balloon_exit"), _originalMode.dmPelsWidth, _originalMode.dmPelsHeight));
                }
                else if (!foundGame && !_isInGameMode && _origValid
                         && (DateTime.Now - _lastGameExit).TotalSeconds < 15)
                {
                    // 显示守护：游戏退出后短时间内，若桌面被降级到比原始分辨率更低的模式，自动再恢复一次
                    DEVMODE cur = GetCurrentDisplayMode();
                    if (IsValidMode(cur)
                        && (cur.dmPelsWidth < _originalMode.dmPelsWidth || cur.dmPelsHeight < _originalMode.dmPelsHeight))
                    {
                        Log("guard: desktop degraded to " + ModeStr(cur) + " - restoring again");
                        RestoreResolution();
                    }
                }
            }
            catch { }
        }

        private void SwitchResolution(int width, int height)
        {
            try
            {
                // 原始模式必须有效，否则绝不动显示设置
                if (!_origValid && !RefreshOriginalMode())
                {
                    Log("switch: aborted, no valid original mode");
                    ShowBalloon(T("capture_failed_title"), T("capture_failed"));
                    return;
                }

                DEVMODE before = GetCurrentDisplayMode();

                DEVMODE target = _originalMode;
                target.dmPelsWidth = width;
                target.dmPelsHeight = height;
                target.dmFields = 0x00080000 | 0x00100000;

                DEVMODE exact = FindMatchingMode(width, height,
                    _originalMode.dmBitsPerPel, _originalMode.dmDisplayFrequency);
                if (exact.dmSize != 0) target = exact;

                int r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
                Log("switch target: " + ModeStr(target) + ", dmSize=" + target.dmSize
                    + ", dmFields=0x" + target.dmFields.ToString("X8") + ", dev='" + target.dmDeviceName
                    + "', test=" + r);
                if (r == DISP_CHANGE_SUCCESSFUL)
                    // flags=0 = 临时模式（仅当前会话生效，不写注册表）：
                    // 避免把游戏分辨率污染成系统默认桌面模式；
                    // 注意 CDS_GLOBAL 必须与 CDS_UPDATEREGISTRY 联用，单独使用会返回 BADFLAGS
                    r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero, 0, IntPtr.Zero);

                Log("switch " + width + "x" + height + ": " + ModeStr(before) + " -> "
                    + ModeStr(GetCurrentDisplayMode()) + ", result=" + r);
            }
            catch (Exception ex) { Log("switch error: " + ex.Message); }
        }

        private void RestoreResolution()
        {
            try
            {
                if (!IsValidMode(_originalMode) && !RefreshOriginalMode())
                {
                    Log("restore: aborted, no valid original mode");
                    ShowBalloon(T("capture_failed_title"), T("capture_failed"));
                    return;
                }

                DEVMODE cur = GetCurrentDisplayMode();
                if (IsValidMode(cur)
                    && cur.dmPelsWidth == _originalMode.dmPelsWidth
                    && cur.dmPelsHeight == _originalMode.dmPelsHeight
                    && cur.dmDisplayFrequency == _originalMode.dmDisplayFrequency)
                {
                    Log("restore: already at original mode " + ModeStr(cur));
                    return;
                }

                // 优先使用当前可枚举到的匹配模式（原刷新率可能暂时不可用）
                DEVMODE target = FindMatchingMode(_originalMode.dmPelsWidth, _originalMode.dmPelsHeight,
                    _originalMode.dmBitsPerPel, _originalMode.dmDisplayFrequency);
                if (target.dmSize == 0) target = _originalMode;

                int r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
                if (r == DISP_CHANGE_SUCCESSFUL)
                    r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero,
                        CDS_GLOBAL | CDS_UPDATEREGISTRY, IntPtr.Zero);

                // 验证结果并重试：显卡/显示器重新协商有时需要一点时间
                for (int i = 0; i < 3; i++)
                {
                    DEVMODE now = GetCurrentDisplayMode();
                    if (IsValidMode(now)
                        && now.dmPelsWidth == _originalMode.dmPelsWidth
                        && now.dmPelsHeight == _originalMode.dmPelsHeight)
                        break;
                    System.Threading.Thread.Sleep(300);
                    ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero,
                        CDS_GLOBAL | CDS_UPDATEREGISTRY, IntPtr.Zero);
                }

                Log("restore: result=" + r + " -> " + ModeStr(GetCurrentDisplayMode()));
            }
            catch (Exception ex) { Log("restore error: " + ex.Message); }
        }

        private void ShowBalloon(string title, string text)
        {
            try { _trayIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info); }
            catch { }
        }

        private void TogglePause()
        {
            _paused = !_paused;
            var menu = _trayIcon.ContextMenuStrip;
            menu.Items[1].Text = _paused ? T("menu_resume") : T("menu_pause");

            // 恢复监控时重新校准原始分辨率（例如启动时枚举失败的场景）
            if (!_paused && !_origValid)
                RefreshOriginalMode();

            if (_paused && _isInGameMode)
            {
                RestoreResolution();
                _isInGameMode = false;
                _currentGameProcess = "";
            }

            UpdateTrayText();
        }

        private void UpdateTrayText()
        {
            string status = _paused ? T("status_paused") : T("status_running");
            _trayIcon.Text = string.Format(T("tray_text"), status, GetEnabledCount());
            if (_lblStatus != null && !_lblStatus.IsDisposed)
                _lblStatus.Text = string.Format(T("status_fmt"), status, GetEnabledCount());
        }

        // ================================================================
        //  Config persistence
        // ================================================================
        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath)) return;
                string[] lines = File.ReadAllLines(_configPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                    if (trimmed.StartsWith("STEAMPATH="))
                    {
                        _manualSteamPath = trimmed.Substring("STEAMPATH=".Length).Trim();
                        continue;
                    }
                    string[] parts = trimmed.Split('|');
                    if (parts.Length >= 5)
                    {
                        _games.Add(new MonitoredGame
                        {
                            DisplayName = parts[0],
                            ProcessName = parts[1],
                            TargetWidth = int.Parse(parts[2]),
                            TargetHeight = int.Parse(parts[3]),
                            Enabled = parts[4] == "1",
                        });
                    }
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using (StreamWriter w = new StreamWriter(_configPath, false, Encoding.UTF8))
                {
                    w.WriteLine("# 分辨率自动监控 - 游戏列表");
                    w.WriteLine("# 格式: 显示名|进程名|目标宽|目标高|启用(1/0)");
                    w.WriteLine("# Steam 安装路径（可选，留空自动检测）");
                    w.WriteLine("STEAMPATH=" + _manualSteamPath);
                    foreach (var g in _games)
                    {
                        w.WriteLine(g.DisplayName + "|" + g.ProcessName + "|"
                            + g.TargetWidth + "|" + g.TargetHeight + "|"
                            + (g.Enabled ? "1" : "0"));
                    }
                }
            }
            catch { }
        }

        // ================================================================
        //  Helpers
        // ================================================================
        private DEVMODE GetCurrentDisplayMode()
        {
            DEVMODE mode = new DEVMODE();
            mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref mode) != 0)
                return mode;
            return new DEVMODE();
        }

        // 无效模式判定：枚举失败时返回全零结构（dmSize=0），
        // 绝不能把这种结构传给 ChangeDisplaySettingsEx
        private static bool IsValidMode(DEVMODE m)
        {
            return m.dmSize != 0 && m.dmPelsWidth >= 640 && m.dmPelsHeight >= 480;
        }

        // 注册表中记录的桌面模式（枚举当前模式失败时的可靠回退）
        private DEVMODE GetRegistryDisplayMode()
        {
            DEVMODE mode = new DEVMODE();
            mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            if (EnumDisplaySettings(null, ENUM_REGISTRY_SETTINGS, ref mode) != 0)
                return mode;
            return new DEVMODE();
        }

        // 仅在原始模式无效时调用：重新校准为当前有效模式，或回退注册表模式
        private bool RefreshOriginalMode()
        {
            DEVMODE cur = GetCurrentDisplayMode();
            if (IsValidMode(cur)) { _originalMode = cur; _origValid = true; Log("original refreshed from current: " + ModeStr(cur)); return true; }
            DEVMODE reg = GetRegistryDisplayMode();
            if (IsValidMode(reg)) { _originalMode = reg; _origValid = true; Log("original refreshed from registry: " + ModeStr(reg)); return true; }
            return false;
        }

        private string ModeStr(DEVMODE m)
        {
            if (!IsValidMode(m)) return "(invalid)";
            return m.dmPelsWidth + "x" + m.dmPelsHeight + "@" + m.dmDisplayFrequency + "Hz " + m.dmBitsPerPel + "bpp";
        }

        // 轻量日志：记录捕获/切换/恢复结果，便于事后诊断显示异常
        private void Log(string msg)
        {
            try
            {
                if (_logPath == null) return;
                string dir = Path.GetDirectoryName(_logPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(_logPath) && new FileInfo(_logPath).Length > 256 * 1024)
                    File.Delete(_logPath);
                File.AppendAllText(_logPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }

        private DEVMODE FindMatchingMode(int width, int height, int bits, int refresh)
        {
            DEVMODE mode = new DEVMODE();
            mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            int i = 0;
            while (EnumDisplaySettings(null, i, ref mode) != 0)
            {
                if (mode.dmPelsWidth == width && mode.dmPelsHeight == height
                    && mode.dmBitsPerPel == bits && mode.dmDisplayFrequency == refresh)
                    return mode;
                i++; mode = new DEVMODE(); mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            }
            mode = new DEVMODE(); mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE)); i = 0;
            while (EnumDisplaySettings(null, i, ref mode) != 0)
            {
                if (mode.dmPelsWidth == width && mode.dmPelsHeight == height) return mode;
                i++; mode = new DEVMODE(); mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            }
            return new DEVMODE();
        }

        private Icon CreateTrayIcon()
        {
            // Create a simple 16x16 icon programmatically
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, 120, 200)))
                {
                    g.FillEllipse(brush, 1, 1, 14, 14);
                }
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    Font f = new Font("Arial", 8, FontStyle.Bold);
                    g.DrawString("R", f, brush, 3, 2);
                    f.Dispose();
                }
            }
            IntPtr hIcon = bmp.GetHicon();
            _trayBmp = bmp;   // 保活：Bitmap 被 GC 回收会销毁 HICON，托盘图标随之消失
            return Icon.FromHandle(hIcon);
        }

        private void Quit()
        {
            if (_pollTimer != null) _pollTimer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            if (_isInGameMode) RestoreResolution();
            Application.Exit();
        }

        [STAThread]
        static void Main(string[] args)
        {
            bool autostart = false;
            foreach (string a in args)
            {
                if (a != null && a.Trim().Equals("--autostart", StringComparison.OrdinalIgnoreCase))
                { autostart = true; break; }
            }

            // Ensure single instance
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "ResolutionAutoDetect", out createdNew))
            {
                if (!createdNew)
                {
                    // 已有实例在运行：发信号唤起其设置窗口，然后退出
                    try
                    {
                        EventWaitHandle ev = EventWaitHandle.OpenExisting("ResolutionAutoDetectShowEvent");
                        ev.Set();
                        ev.Dispose();
                    }
                    catch { }
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                Application.Run(new AppContext(autostart));
            }
        }
    }
}
