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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("AutoGameDetect")]
[assembly: AssemblyProduct("ResolutionSwitcher")]
[assembly: AssemblyDescription("Auto resolution switcher: 4K desktop, 2K while gaming")]
[assembly: AssemblyCompany("keitarol")]
[assembly: AssemblyCopyright("Copyright (c) 2025 keitarol")]
[assembly: AssemblyVersion("1.3.1.0")]
[assembly: AssemblyFileVersion("1.3.1.0")]

namespace AutoGameDetect
{
    public class AppContext : ApplicationContext
    {
        private const string AppVersion = "1.3.1";

        // ================================================================
        //  Localization
        //  优先级：--lang 参数 > AGD_LANG 环境变量 > 系统 UI 语言
        //  zh-* → 中文；其余所有情况 → 英文
        // ================================================================
        private static string ResolveLanguage()
        {
            string forced = null;
            try
            {
                string env = Environment.GetEnvironmentVariable("AGD_LANG");
                if (!string.IsNullOrEmpty(env)) forced = env;

                string[] args = Environment.GetCommandLineArgs();
                for (int i = 1; i < args.Length; i++)
                {
                    string a = (args[i] ?? "").Trim();
                    if (a.Equals("--lang", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                        forced = args[i + 1];
                    else if (a.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                        forced = a.Substring("--lang=".Length);
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(forced))
            {
                string f = forced.Trim().ToLowerInvariant();
                if (f.StartsWith("zh")) return "zh";
                if (f.StartsWith("en")) return "en";
            }
            return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? "zh" : "en";
        }

        private static readonly string Lang = ResolveLanguage();
        private static readonly bool IsChinese = Lang == "zh";

        // 中文文案（key 统一，便于对照维护）
        private static readonly Dictionary<string, string> Dict_ZH = new Dictionary<string, string>
        {
            { "win_title", "分辨率自动监控 - 设置" },
            { "lbl_info", "选择需要自动切换到 2K 的游戏：" },
            { "btn_scan", "🔄 扫描已安装游戏" },
            { "btn_scanning", "扫描中..." },
            { "col_on", "启用" },
            { "col_name", "游戏" },
            { "col_proc", "进程名" },
            { "col_target", "目标分辨率" },
            { "lbl_target", "目标分辨率:" },
            { "btn_apply", "应用到选中" },
            { "lbl_restore", "恢复目标: {0}×{1}@{2}Hz" },
            { "lbl_proc", "进程名(不含.exe):" },
            { "btn_add", "添加手动" },
            { "btn_del", "删除选中" },
            { "lbl_steam", "Steam 路径(可选):" },
            { "btn_browse", "浏览..." },
            { "dlg_steam_desc", "选择 Steam 安装目录（含 steam.exe 的文件夹）" },
            { "lbl_steam_hint", "留空 = 自动检测（默认路径 / 注册表，支持任意安装位置）" },
            { "chk_autostart", "开机自动启动" },
            { "btn_close", "关闭" },
            { "tray_text", "分辨率自动监控\n{0} | 监控 {1} 个游戏" },
            { "menu_settings", "设置监控游戏..." },
            { "menu_pause", "⏸ 暂停监控" },
            { "menu_resume", "▶ 恢复监控" },
            { "menu_restore", "立即恢复桌面分辨率" },
            { "menu_settarget", "将当前分辨率设为恢复目标" },
            { "menu_current", "当前桌面: {0}×{1}@{2}Hz" },
            { "menu_quit", "退出" },
            { "status_paused", "⏸ 已暂停" },
            { "status_running", "▶ 运行中" },
            { "status_fmt", "状态: {0}  |  监控 {1} 个游戏" },
            { "lbl_now", "当前桌面: {0}×{1}@{2}Hz" },
            { "msg_already", "该游戏已在列表中。" },
            { "title_hint", "提示" },
            { "title_done", "完成" },
            { "scan_done", "扫描完成" },
            { "scan_added", "，新增 {0} 个游戏" },
            { "scan_removed", "，移除 {0} 个已卸载的游戏" },
            { "scan_fresh", "，列表已是最新" },
            { "scan_prompt", "。\n请勾选要自动监控的游戏。" },
            { "title_scan_done", "扫描完成" },
            { "balloon_start_title", "游戏检测" },
            { "balloon_start", "检测到 {0} 已启动，已切换到 {1}×{2}" },
            { "balloon_exit_title", "游戏退出" },
            { "balloon_exit", "已恢复桌面分辨率 {0}×{1}" },
            { "switch_failed_title", "切换失败" },
            { "switch_failed", "无法切换到 {0}×{1}。该分辨率可能不被当前显示器支持，已保持原分辨率。" },
            { "restore_failed_title", "恢复失败" },
            { "restore_failed", "未能恢复桌面分辨率 {0}×{1}，请手动在显示设置中调整。" },
            { "manual_suffix", " (手动)" },
            { "main_already", "分辨率自动监控已在运行！\n请在系统托盘图标上操作。" },
            { "title_already", "已在运行" },
            { "capture_failed_title", "无法读取当前分辨率" },
            { "capture_failed", "为保护显示设置，自动切换已暂停。请在桌面分辨率正常后，右键托盘图标选择【恢复监控】重试。" },
            { "msg_no_selection", "请先在列表中选择游戏。" },
            { "apply_done", "已更新 {0} 个游戏的目标分辨率。" },
            { "target_set", "恢复目标已设为 {0}×{1}@{2}Hz" },
            { "autostart_on", "已开启开机自动启动。" },
            { "autostart_off", "已关闭开机自动启动。" },
            { "autostart_fail", "无法修改开机自启设置：{0}" },
            { "data_dir_title", "配置目录不可写" },
            { "data_dir_fallback", "系统拒绝写入默认配置目录（通常是安全软件拦截）。\n已自动改用：{0}\n设置仍可正常保存。" },
        };

        // 英文文案
        private static readonly Dictionary<string, string> Dict_En = new Dictionary<string, string>
        {
            { "win_title", "Resolution Switcher - Settings" },
            { "lbl_info", "Select games to auto-switch to 2K:" },
            { "btn_scan", "🔄 Scan Installed Games" },
            { "btn_scanning", "Scanning..." },
            { "col_on", "On" },
            { "col_name", "Game" },
            { "col_proc", "Process" },
            { "col_target", "Target" },
            { "lbl_target", "Target resolution:" },
            { "btn_apply", "Apply to Selected" },
            { "lbl_restore", "Restore target: {0}×{1}@{2}Hz" },
            { "lbl_proc", "Process name (no .exe):" },
            { "btn_add", "Add Manually" },
            { "btn_del", "Remove Selected" },
            { "lbl_steam", "Steam path (optional):" },
            { "btn_browse", "Browse..." },
            { "dlg_steam_desc", "Select the Steam installation folder (the one containing steam.exe)" },
            { "lbl_steam_hint", "Leave blank = auto-detect (default path / registry, any install location)" },
            { "chk_autostart", "Start with Windows" },
            { "btn_close", "Close" },
            { "tray_text", "Resolution Switcher\n{0} | Monitoring {1} game(s)" },
            { "menu_settings", "Settings..." },
            { "menu_pause", "⏸ Pause Monitoring" },
            { "menu_resume", "▶ Resume Monitoring" },
            { "menu_restore", "Restore Desktop Resolution" },
            { "menu_settarget", "Set Current as Restore Target" },
            { "menu_current", "Desktop: {0}×{1}@{2}Hz" },
            { "menu_quit", "Exit" },
            { "status_paused", "⏸ Paused" },
            { "status_running", "▶ Running" },
            { "status_fmt", "Status: {0} | Monitoring {1} game(s)" },
            { "lbl_now", "Desktop: {0}×{1}@{2}Hz" },
            { "msg_already", "This game is already in the list." },
            { "title_hint", "Notice" },
            { "title_done", "Done" },
            { "scan_done", "Scan complete" },
            { "scan_added", ", {0} new game(s) added" },
            { "scan_removed", ", {0} uninstalled game(s) removed" },
            { "scan_fresh", ", the list is already up to date" },
            { "scan_prompt", ".\nCheck the games you want to monitor." },
            { "title_scan_done", "Scan Complete" },
            { "balloon_start_title", "Game Detected" },
            { "balloon_start", "{0} started. Switched to {1}×{2}" },
            { "balloon_exit_title", "Game Exited" },
            { "balloon_exit", "Desktop restored to {0}×{1}" },
            { "switch_failed_title", "Switch Failed" },
            { "switch_failed", "Cannot switch to {0}×{1}. The mode may be unsupported by this display; the current resolution was kept." },
            { "restore_failed_title", "Restore Failed" },
            { "restore_failed", "Failed to restore desktop {0}×{1}. Please set it manually in Display Settings." },
            { "manual_suffix", " (manual)" },
            { "main_already", "Resolution Switcher is already running!\nUse the system tray icon." },
            { "title_already", "Already Running" },
            { "capture_failed_title", "Cannot Read Current Resolution" },
            { "capture_failed", "Auto-switching was paused to protect your display settings. Once the desktop resolution is normal, right-click the tray icon and choose Resume Monitoring to retry." },
            { "msg_no_selection", "Please select at least one game in the list." },
            { "apply_done", "Target resolution updated for {0} game(s)." },
            { "target_set", "Restore target set to {0}×{1}@{2}Hz" },
            { "autostart_on", "Start with Windows enabled." },
            { "autostart_off", "Start with Windows disabled." },
            { "autostart_fail", "Cannot change the startup setting: {0}" },
            { "data_dir_title", "Config Folder Not Writable" },
            { "data_dir_fallback", "The default config folder is blocked (usually by security software).\nUsing instead: {0}\nSettings will still be saved." },
        };

        private static readonly Dictionary<string, string> Dict = IsChinese ? Dict_ZH : Dict_En;

        // 统一取词：缺失 key 时原样返回，避免空界面
        private static string T(string key)
        {
            string s;
            return Dict.TryGetValue(key, out s) ? s : key;
        }

        // 自适应宽度：控件宽度 = 文本测量宽度 + 内边距，中英文自动伸缩
        private static int Measure(string text, Font font)
        {
            return TextRenderer.MeasureText(text ?? "", font).Width;
        }

        private static int ButtonWidth(string text, Font font)
        {
            return Measure(text, font) + 26;
        }

        private static int LabelWidth(string text, Font font)
        {
            return Measure(text, font) + 8;
        }

        // ================================================================
        //  Win32 P/Invoke
        // ================================================================
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int EnumDisplaySettings(
            string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int ChangeDisplaySettingsEx(
            string lpszDeviceName, ref DEVMODE lpDevMode,
            IntPtr hwnd, uint dwFlags, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

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
        private const int DM_BITSPERPEL = 0x00040000;
        private const int DM_DISPLAYFREQUENCY = 0x00400000;
        private const int DM_PELSWIDTH = 0x00080000;
        private const int DM_PELSHEIGHT = 0x00100000;

        // ================================================================
        //  Data
        // ================================================================
        private enum GameSource { Steam, Manual }

        private class MonitoredGame
        {
            public string DisplayName = "";
            public string ProcessName = "";   // 不含 .exe
            public int TargetWidth = 2560;
            public int TargetHeight = 1440;
            public bool Enabled = false;
            public GameSource Source = GameSource.Steam;
        }

        // --- state ---
        private DEVMODE _desktopMode;              // 期望的桌面模式（恢复目标），持久化到配置
        private bool _desktopValid = false;
        private List<MonitoredGame> _games = new List<MonitoredGame>();
        private string _configPath = "";
        private static string _logPath = "";
        private string _manualSteamPath = "";      // 手动指定的 Steam 安装路径（留空=自动检测）
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _menuPause, _menuRestore, _menuSetTarget, _menuCurrent;
        private System.Windows.Forms.Timer _pollTimer;
        private System.Windows.Forms.Timer _activateTimer;
        private static volatile bool _activateRequested = false;   // 另一实例请求唤起设置窗口
        private const string ActivateEventName = "ResolutionAutoDetect.ShowSettings";
        private bool _isInGameMode = false;
        private string _currentGameName = "";
        private bool _paused = false;
        private bool _polling = false;             // 重入保护
        private DateTime _lastGameExit = DateTime.MinValue;
        private int _guardTries = 0;
        private IntPtr _iconHandle = IntPtr.Zero;

        // --- UI ---
        private Form _configForm;
        private ListView _list;
        private NumericUpDown _numWidth, _numHeight;
        private Button _btnScan, _btnAdd, _btnApply, _btnDel;
        private TextBox _txtProc, _txtSteamPath;
        private CheckBox _chkAutostart;
        private Label _lblStatus, _lblNow, _lblRestore;
        private bool _suppressListEvents = false;
        private Font _uiFont, _smallFont;

        // ================================================================
        //  Startup
        // ================================================================
        public AppContext()
        {
            string dir = ResolveDataDir();
            _configPath = Path.Combine(dir, "monitored_games.txt");
            _logPath = Path.Combine(dir, "resolution.log");

            Log("=== start v" + AppVersion + " lang=" + Lang + " data=" + dir
                + (_dataDirFallback ? " (fallback)" : "") + " ===");

            // 配置先读：里面可能保存了用户期望的桌面模式（重启/上次异常退出后）
            if (_dataDirFallback) MigrateLegacyConfig(dir);
            LoadConfig();

            ResolveDesktopMode();
            // 立刻持久化恢复目标：即使首次运行后程序崩溃/断电，下次启动也知道该恢复成什么
            SaveConfig();

            BuildTray();

            if (!_desktopValid)
                Balloon(T("capture_failed_title"), T("capture_failed"));

            // 默认配置目录写不进去（常见于安全软件拦截）时明确告知，避免"设置怎么不保存"
            if (_dataDirFallback)
            {
                Log("data dir fallback -> " + dir);
                Balloon(T("data_dir_title"), string.Format(T("data_dir_fallback"), dir));
            }

            UpdateTrayText();
            Log("desktop target = " + ModeStr(_desktopMode) + ", valid=" + _desktopValid
                + ", games=" + _games.Count + ", enabled=" + GetEnabledCount());

            if (_games.Count == 0) ShowConfig();

            // 轮询用 WinForms 定时器：回调天然在 UI 线程执行，
            // 不再需要跨线程封送，也彻底消除 _games 的并发读写
            _pollTimer = new System.Windows.Forms.Timer();
            _pollTimer.Interval = 3000;
            _pollTimer.Tick += CheckGames;
            _pollTimer.Start();

            // 再次运行 exe 时会置位该标志 → 唤起设置窗口。
            // 托盘图标被 Windows 折叠进"隐藏的图标"里时，用户靠这个也能打开界面
            _activateTimer = new System.Windows.Forms.Timer();
            _activateTimer.Interval = 300;
            _activateTimer.Tick += (s, e) =>
            {
                if (!_activateRequested) return;
                _activateRequested = false;
                ShowConfig();
            };
            _activateTimer.Start();

            // 关机/注销时若正处于 2K，先恢复桌面分辨率
            try
            {
                SystemEvents.SessionEnding += (s, e) =>
                {
                    try { if (_isInGameMode) RestoreResolution(); }
                    catch { }
                };
            }
            catch { }
        }

        // 确定恢复目标：
        //  1. 配置里保存的桌面模式（重启后依然正确，不受"启动时恰好是 2K"影响）
        //  2. 当前实际分辨率
        //  3. 注册表记录的模式（枚举失败时的兜底）
        //  4. 可用模式中最高的一个
        private void ResolveDesktopMode()
        {
            DEVMODE cur = GetCurrentDisplayMode();
            bool curValid = IsValidMode(cur);

            int w, h, freq;
            if (TryParseMode(_savedDesktop, out w, out h, out freq))
            {
                DEVMODE target = FindBestMode(w, h, 0, freq);
                if (IsValidMode(target))
                {
                    _desktopMode = target;
                    _desktopValid = true;
                    Log("desktop from config: " + ModeStr(target) + " (current " + ModeStr(cur) + ")");
                    return;
                }
                Log("desktop config " + _savedDesktop + " not available on this display");
            }

            if (curValid)
            {
                _desktopMode = cur;
                _desktopValid = true;
                Log("desktop from current: " + ModeStr(cur));
                return;
            }

            DEVMODE reg = GetRegistryDisplayMode();
            if (IsValidMode(reg))
            {
                _desktopMode = reg;
                _desktopValid = true;
                Log("desktop from registry: " + ModeStr(reg));
                return;
            }

            DEVMODE highest = FindHighestMode();
            if (IsValidMode(highest))
            {
                _desktopMode = highest;
                _desktopValid = true;
                Log("desktop from highest available: " + ModeStr(highest));
                return;
            }

            _desktopValid = false;
            _paused = true;   // 无法确定桌面模式时暂停，避免破坏显示设置
            Log("desktop unresolved - monitoring paused");
        }

        private string _savedDesktop = "";

        // ================================================================
        //  Tray
        // ================================================================
        private void BuildTray()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = CreateTrayIcon(false);
            _trayIcon.Text = TrayText();
            _trayIcon.Visible = true;

            _menu = new ContextMenuStrip();

            ToolStripMenuItem miSettings = new ToolStripMenuItem(T("menu_settings"));
            miSettings.Click += (s, e) => ShowConfig();

            _menuCurrent = new ToolStripMenuItem(T("menu_current")) { Enabled = false };

            _menuPause = new ToolStripMenuItem(T("menu_pause"));
            _menuPause.Click += (s, e) => TogglePause();

            _menuRestore = new ToolStripMenuItem(T("menu_restore"));
            _menuRestore.Click += (s, e) =>
            {
                if (RestoreResolution())
                    Balloon(T("balloon_exit_title"),
                        string.Format(T("balloon_exit"), _desktopMode.dmPelsWidth, _desktopMode.dmPelsHeight));
            };

            _menuSetTarget = new ToolStripMenuItem(T("menu_settarget"));
            _menuSetTarget.Click += (s, e) => SetCurrentAsTarget();

            ToolStripMenuItem miQuit = new ToolStripMenuItem(T("menu_quit"));
            miQuit.Click += (s, e) => Quit();

            _menu.Items.Add(miSettings);
            _menu.Items.Add(_menuCurrent);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_menuPause);
            _menu.Items.Add(_menuRestore);
            _menu.Items.Add(_menuSetTarget);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(miQuit);

            // 每次打开菜单刷新文案与当前分辨率（语言/模式都可能是最新的）
            _menu.Opening += (s, e) => RefreshMenuTexts();

            _trayIcon.ContextMenuStrip = _menu;
            _trayIcon.DoubleClick += (s, e) => ShowConfig();
        }

        private void RefreshMenuTexts()
        {
            try
            {
                DEVMODE cur = GetCurrentDisplayMode();
                _menu.Items[0].Text = T("menu_settings");
                _menuCurrent.Text = string.Format(T("menu_current"),
                    cur.dmPelsWidth, cur.dmPelsHeight, cur.dmDisplayFrequency);
                _menuPause.Text = _paused ? T("menu_resume") : T("menu_pause");
                _menuRestore.Text = T("menu_restore");
                _menuSetTarget.Text = T("menu_settarget");
                _menu.Items[_menu.Items.Count - 1].Text = T("menu_quit");
            }
            catch { }
        }

        private string TrayText()
        {
            string s = string.Format(T("tray_text"),
                _paused ? T("status_paused") : T("status_running"), GetEnabledCount());
            // NotifyIcon.Text 上限 63 字符，超出会抛异常
            if (s.Length > 63) s = s.Substring(0, 63);
            return s;
        }

        private void UpdateTrayText()
        {
            if (_trayIcon == null) return;
            _trayIcon.Text = TrayText();
            UpdateStatus();
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
        private Font UiFont()
        {
            if (_uiFont == null)
            {
                try { _uiFont = new Font("Microsoft YaHei UI", 9f); }
                catch { _uiFont = SystemFonts.MessageBoxFont; }
            }
            return _uiFont;
        }

        private Font SmallFont()
        {
            if (_smallFont == null)
            {
                try { _smallFont = new Font(UiFont().FontFamily, 8f); }
                catch { _smallFont = SystemFonts.DefaultFont; }
            }
            return _smallFont;
        }

        private void ShowConfig()
        {
            if (_configForm != null && !_configForm.IsDisposed)
            {
                _configForm.Activate();
                return;
            }

            Font f = UiFont();
            const int M = 12, GAP = 8, NUD = 72, MIN_INPUT = 150;

            // ---- 先算宽度：每行所需宽度取最大值，控件宽度全部来自文本测量 ----
            int needTarget = M + LabelWidth(T("lbl_target"), f) + GAP + NUD + 2
                             + LabelWidth("×", f) + 2 + NUD + GAP + ButtonWidth(T("btn_apply"), f) + M;
            int needProc = M + LabelWidth(T("lbl_proc"), f) + GAP + MIN_INPUT + GAP
                           + ButtonWidth(T("btn_add"), f) + GAP + ButtonWidth(T("btn_del"), f) + M;
            int needSteam = M + LabelWidth(T("lbl_steam"), f) + GAP + MIN_INPUT + GAP
                            + ButtonWidth(T("btn_browse"), f) + M;
            int needAuto = M + Measure(T("chk_autostart"), f) + 22 + GAP + ButtonWidth(T("btn_close"), f) + M;
            int needInfo = M + Measure(T("lbl_info"), f) + M;
            int needScan = M + ButtonWidth(T("btn_scan"), f) + M;

            int CW = Math.Max(600, Math.Max(
                Math.Max(Math.Max(needTarget, needProc), Math.Max(needSteam, needAuto)),
                Math.Max(needInfo, needScan)) + 24);

            _configForm = new Form();
            _configForm.Text = T("win_title");
            _configForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            _configForm.StartPosition = FormStartPosition.CenterScreen;
            _configForm.MaximizeBox = false;
            _configForm.MinimizeBox = false;
            _configForm.Font = f;
            _configForm.ShowInTaskbar = true;
            _configForm.ClientSize = new Size(CW, 500);

            int y = M;

            _configForm.Controls.Add(new Label
            {
                Text = T("lbl_info"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_info"), f), 20),
                Font = f,
            });
            y += 26;

            _btnScan = new Button
            {
                Text = T("btn_scan"),
                Location = new Point(M, y),
                Size = new Size(ButtonWidth(T("btn_scan"), f), 28),
                Font = f,
            };
            _btnScan.Click += (s, e) => RunScan();
            _configForm.Controls.Add(_btnScan);
            y += 36;

            // ---- 游戏列表：ListView 多列，避免等宽对齐在中英混排下错位 ----
            int listW = CW - 2 * M;
            int wProcCol = 0, wTgtCol = 0, wNameCol = 0;
            foreach (var g in _games)
            {
                wProcCol = Math.Max(wProcCol, Measure(g.ProcessName + ".exe", f));
                wTgtCol = Math.Max(wTgtCol, Measure(g.TargetWidth + "×" + g.TargetHeight, f));
                wNameCol = Math.Max(wNameCol, Measure(DisplayOf(g), f));
            }
            wProcCol = Math.Min(240, wProcCol + 26);
            wTgtCol = Math.Max(Measure("0000×0000", f), wTgtCol) + 26;
            int wOnCol = Math.Max(46, Measure(T("col_on"), f) + 34);
            wNameCol = Math.Max(140, Math.Min(wNameCol + 26, listW - wOnCol - wProcCol - wTgtCol - 8));
            if (wOnCol + wNameCol + wProcCol + wTgtCol < listW)
                wNameCol += listW - (wOnCol + wNameCol + wProcCol + wTgtCol);

            _list = new ListView
            {
                Location = new Point(M, y),
                Size = new Size(listW, 210),
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                MultiSelect = true,
                HideSelection = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = f,
            };
            _list.Columns.Add(T("col_on"), wOnCol);
            _list.Columns.Add(T("col_name"), wNameCol);
            _list.Columns.Add(T("col_proc"), wProcCol);
            _list.Columns.Add(T("col_target"), wTgtCol);
            _list.ItemChecked += (s, e) =>
            {
                if (_suppressListEvents) return;
                MonitoredGame g = e.Item.Tag as MonitoredGame;
                if (g == null || g.Enabled == e.Item.Checked) return;
                g.Enabled = e.Item.Checked;
                SaveConfig();
                UpdateTrayText();
            };
            _list.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressListEvents || _list.SelectedItems.Count == 0) return;
                MonitoredGame g = _list.SelectedItems[0].Tag as MonitoredGame;
                if (g == null) return;
                if (g.TargetWidth >= _numWidth.Minimum && g.TargetWidth <= _numWidth.Maximum)
                    _numWidth.Value = g.TargetWidth;
                if (g.TargetHeight >= _numHeight.Minimum && g.TargetHeight <= _numHeight.Maximum)
                    _numHeight.Value = g.TargetHeight;
            };
            _configForm.Controls.Add(_list);
            y += 218;

            // ---- 目标分辨率 + 应用到选中 ----
            var lblTarget = new Label
            {
                Text = T("lbl_target"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_target"), f), 24),
                Font = f,
            };
            _configForm.Controls.Add(lblTarget);

            int x = M + LabelWidth(T("lbl_target"), f) + GAP;
            _numWidth = new NumericUpDown
            {
                Location = new Point(x, y),
                Width = NUD,
                Minimum = 640,
                Maximum = 7680,
                Value = 2560,
                Font = f,
            };
            _configForm.Controls.Add(_numWidth);

            int xX = x + NUD + 2;
            _configForm.Controls.Add(new Label
            {
                Text = "×",
                Location = new Point(xX, y),
                Size = new Size(LabelWidth("×", f), 24),
                Font = f,
            });

            _numHeight = new NumericUpDown
            {
                Location = new Point(xX + LabelWidth("×", f) + 2, y),
                Width = NUD,
                Minimum = 480,
                Maximum = 4320,
                Value = 1440,
                Font = f,
            };
            _configForm.Controls.Add(_numHeight);

            _btnApply = new Button
            {
                Text = T("btn_apply"),
                Size = new Size(ButtonWidth(T("btn_apply"), f), 26),
                Font = f,
            };
            _btnApply.Location = new Point(CW - M - _btnApply.Width, y - 1);
            _btnApply.Click += (s, e) => ApplyTargetToSelected();
            _configForm.Controls.Add(_btnApply);
            y += 32;

            // ---- 手动添加进程 ----
            var lblProc = new Label
            {
                Text = T("lbl_proc"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_proc"), f), 24),
                Font = f,
            };
            _configForm.Controls.Add(lblProc);

            _btnDel = new Button
            {
                Text = T("btn_del"),
                Size = new Size(ButtonWidth(T("btn_del"), f), 26),
                ForeColor = Color.Firebrick,
                Font = f,
            };
            _btnDel.Location = new Point(CW - M - _btnDel.Width, y);
            _btnDel.Click += (s, e) => RemoveSelected();

            _btnAdd = new Button
            {
                Text = T("btn_add"),
                Size = new Size(ButtonWidth(T("btn_add"), f), 26),
                Font = f,
            };
            _btnAdd.Location = new Point(_btnDel.Left - GAP - _btnAdd.Width, y);
            _btnAdd.Click += (s, e) => AddManual();

            int procX = M + LabelWidth(T("lbl_proc"), f) + GAP;
            _txtProc = new TextBox
            {
                Location = new Point(procX, y + 1),
                Width = Math.Max(80, _btnAdd.Left - GAP - procX),
                Font = f,
            };
            _configForm.Controls.Add(_txtProc);
            _configForm.Controls.Add(_btnAdd);
            _configForm.Controls.Add(_btnDel);
            y += 34;

            // ---- Steam 路径 ----
            var lblSteam = new Label
            {
                Text = T("lbl_steam"),
                Location = new Point(M, y),
                Size = new Size(LabelWidth(T("lbl_steam"), f), 24),
                Font = f,
            };
            _configForm.Controls.Add(lblSteam);

            Button btnBrowse = new Button
            {
                Text = T("btn_browse"),
                Size = new Size(ButtonWidth(T("btn_browse"), f), 26),
                Font = f,
            };
            btnBrowse.Location = new Point(CW - M - btnBrowse.Width, y);
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

            int steamX = M + LabelWidth(T("lbl_steam"), f) + GAP;
            _txtSteamPath = new TextBox
            {
                Location = new Point(steamX, y + 1),
                Width = Math.Max(80, btnBrowse.Left - GAP - steamX),
                Text = _manualSteamPath,
                Font = f,
            };
            _configForm.Controls.Add(_txtSteamPath);
            _configForm.Controls.Add(btnBrowse);

            _configForm.Controls.Add(new Label
            {
                Text = T("lbl_steam_hint"),
                Location = new Point(M, y + 27),
                Size = new Size(CW - 2 * M, 16),
                ForeColor = Color.Gray,
                Font = SmallFont(),
            });
            y += 52;

            // ---- 开机自启 + 关闭 ----
            _chkAutostart = new CheckBox
            {
                Text = T("chk_autostart"),
                Location = new Point(M, y),
                Size = new Size(Measure(T("chk_autostart"), f) + 22, 24),
                Checked = IsAutostartEnabled(),
                Font = f,
            };
            _chkAutostart.CheckedChanged += (s, e) =>
            {
                bool want = _chkAutostart.Checked;
                string err = null;
                if (!SetAutostart(want, out err))
                {
                    _suppressListEvents = true;
                    _chkAutostart.Checked = !want;
                    _suppressListEvents = false;
                    MessageBox.Show(string.Format(T("autostart_fail"), err ?? "?"), T("title_hint"));
                    return;
                }
            };
            _configForm.Controls.Add(_chkAutostart);

            Button btnClose = new Button
            {
                Text = T("btn_close"),
                Size = new Size(ButtonWidth(T("btn_close"), f), 28),
                Font = f,
            };
            btnClose.Location = new Point(CW - M - btnClose.Width, y - 2);
            btnClose.Click += (s, e) => _configForm.Close();
            _configForm.Controls.Add(btnClose);
            y += 34;

            // ---- 状态栏 ----
            _lblStatus = new Label
            {
                Location = new Point(M, y),
                Size = new Size(CW - 2 * M, 18),
                ForeColor = Color.Gray,
                Font = f,
            };
            _configForm.Controls.Add(_lblStatus);

            _lblNow = new Label
            {
                Location = new Point(M, y + 20),
                Size = new Size(CW - 2 * M, 18),
                ForeColor = Color.Gray,
                Font = f,
            };
            _configForm.Controls.Add(_lblNow);

            _lblRestore = new Label
            {
                Location = new Point(M, y + 40),
                Size = new Size(CW - 2 * M, 18),
                ForeColor = Color.Gray,
                Font = f,
            };
            _configForm.Controls.Add(_lblRestore);

            y += 64;
            _configForm.ClientSize = new Size(CW, y + M);

            RefreshList();
            UpdateStatus();

            _configForm.FormClosed += (s, e) =>
            {
                // 保存勾选状态与 Steam 路径（勾选已即时保存，这里兜底）
                if (_txtSteamPath != null) _manualSteamPath = _txtSteamPath.Text.Trim();
                SaveConfig();
                UpdateTrayText();
            };

            try { _configForm.ShowDialog(); }
            finally
            {
                Form f2 = _configForm;
                _configForm = null;
                _list = null;
                _numWidth = _numHeight = null;
                _btnScan = _btnAdd = _btnApply = _btnDel = null;
                _txtProc = _txtSteamPath = null;
                _chkAutostart = null;
                _lblStatus = _lblNow = _lblRestore = null;
                if (f2 != null) f2.Dispose();
            }
        }

        private static string DisplayOf(MonitoredGame g)
        {
            return g.Source == GameSource.Manual ? g.DisplayName + T("manual_suffix") : g.DisplayName;
        }

        // ================================================================
        //  List handling
        // ================================================================
        private void RefreshList()
        {
            if (_list == null) return;
            _suppressListEvents = true;
            try
            {
                _list.BeginUpdate();
                _list.Items.Clear();
                foreach (var g in _games)
                {
                    ListViewItem it = new ListViewItem("");
                    it.SubItems.Add(DisplayOf(g));
                    it.SubItems.Add((g.ProcessName ?? "") + ".exe");
                    it.SubItems.Add(g.TargetWidth + "×" + g.TargetHeight);
                    it.Checked = g.Enabled;
                    it.Tag = g;
                    _list.Items.Add(it);
                }
                _list.EndUpdate();
            }
            finally { _suppressListEvents = false; }
            UpdateStatus();
        }

        // 先取选中项；没有选中时退回勾选项
        private List<MonitoredGame> SelectedOrChecked()
        {
            List<MonitoredGame> res = new List<MonitoredGame>();
            if (_list == null) return res;
            foreach (ListViewItem it in _list.SelectedItems)
            {
                MonitoredGame g = it.Tag as MonitoredGame;
                if (g != null && !res.Contains(g)) res.Add(g);
            }
            if (res.Count == 0)
            {
                foreach (ListViewItem it in _list.CheckedItems)
                {
                    MonitoredGame g = it.Tag as MonitoredGame;
                    if (g != null && !res.Contains(g)) res.Add(g);
                }
            }
            return res;
        }

        private void ApplyTargetToSelected()
        {
            List<MonitoredGame> sel = SelectedOrChecked();
            if (sel.Count == 0) { MessageBox.Show(T("msg_no_selection"), T("title_hint")); return; }
            int w = (int)_numWidth.Value, h = (int)_numHeight.Value;
            foreach (var g in sel)
            {
                g.TargetWidth = w;
                g.TargetHeight = h;
            }
            SaveConfig();
            RefreshList();
            MessageBox.Show(string.Format(T("apply_done"), sel.Count), T("title_done"));
        }

        private void RemoveSelected()
        {
            List<MonitoredGame> sel = SelectedOrChecked();
            if (sel.Count == 0) { MessageBox.Show(T("msg_no_selection"), T("title_hint")); return; }
            foreach (var g in sel) _games.Remove(g);
            SaveConfig();
            RefreshList();
            UpdateTrayText();
        }

        private void AddManual()
        {
            string name = (_txtProc.Text ?? "").Trim();
            if (name.Length == 0) return;
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
                DisplayName = name,
                ProcessName = name,
                TargetWidth = (int)_numWidth.Value,
                TargetHeight = (int)_numHeight.Value,
                Enabled = true,
                Source = GameSource.Manual,
            });
            _txtProc.Clear();
            SaveConfig();
            RefreshList();
            UpdateTrayText();
        }

        private void UpdateStatus()
        {
            string status = _paused ? T("status_paused") : T("status_running");
            if (_lblStatus != null && !_lblStatus.IsDisposed)
                _lblStatus.Text = string.Format(T("status_fmt"), status, GetEnabledCount());

            DEVMODE cur = GetCurrentDisplayMode();
            if (_lblNow != null && !_lblNow.IsDisposed)
                _lblNow.Text = IsValidMode(cur)
                    ? string.Format(T("lbl_now"), cur.dmPelsWidth, cur.dmPelsHeight, cur.dmDisplayFrequency)
                    : "-";

            if (_lblRestore != null && !_lblRestore.IsDisposed)
                _lblRestore.Text = IsValidMode(_desktopMode)
                    ? string.Format(T("lbl_restore"), _desktopMode.dmPelsWidth,
                        _desktopMode.dmPelsHeight, _desktopMode.dmDisplayFrequency)
                    : "-";
        }

        // ================================================================
        //  Scanning
        // ================================================================
        private void RunScan()
        {
            if (_btnScan == null) return;
            string old = _btnScan.Text;
            _btnScan.Enabled = false;
            _btnScan.Text = T("btn_scanning");
            Cursor prevCursor = _configForm != null ? _configForm.Cursor : Cursors.Default;
            if (_configForm != null) _configForm.Cursor = Cursors.WaitCursor;
            try
            {
                ScanAndPopulate();
            }
            catch (Exception ex)
            {
                Log("scan error: " + ex);
                MessageBox.Show(ex.Message, T("title_hint"));
            }
            finally
            {
                if (_configForm != null) _configForm.Cursor = prevCursor;
                if (_btnScan != null && !_btnScan.IsDisposed)
                {
                    _btnScan.Text = old;
                    _btnScan.Enabled = true;
                }
            }
        }

        private void ScanAndPopulate()
        {
            int added = 0, removed = 0;
            List<MonitoredGame> scanned = ScanInstalledGames();

            // 1. 移除已卸载的 Steam 游戏（手动添加的不动）
            for (int i = _games.Count - 1; i >= 0; i--)
            {
                MonitoredGame g = _games[i];
                if (g.Source != GameSource.Steam) continue;

                bool installed = false;
                foreach (var s in scanned)
                {
                    if (g.DisplayName.Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase))
                    { installed = true; break; }
                }
                if (!installed)
                {
                    _games.RemoveAt(i);
                    removed++;
                }
            }

            // 2. 新增游戏 / 刷新已有游戏信息（保留勾选状态）
            foreach (var s in scanned)
            {
                MonitoredGame existing = null;
                foreach (var g in _games)
                {
                    if (g.Source == GameSource.Steam
                        && g.DisplayName.Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase))
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

            SaveConfig();
            RefreshList();
            UpdateTrayText();

            string msg = T("scan_done");
            if (added > 0) msg += string.Format(T("scan_added"), added);
            if (removed > 0) msg += string.Format(T("scan_removed"), removed);
            if (added == 0 && removed == 0) msg += T("scan_fresh");
            msg += T("scan_prompt");
            MessageBox.Show(msg, T("title_scan_done"));
        }

        private List<MonitoredGame> ScanInstalledGames()
        {
            List<MonitoredGame> result = new List<MonitoredGame>();

            string steamPath = FindSteamPath();
            if (steamPath == null) return result;

            List<string> libs = new List<string>();
            libs.Add(steamPath);
            string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                try
                {
                    string vdf = File.ReadAllText(vdfPath, Encoding.UTF8);
                    foreach (Match m in Regex.Matches(vdf, "\"path\"\\s+\"([^\"]+)\""))
                    {
                        string p = m.Groups[1].Value.Replace("\\\\", "\\");
                        bool dup = false;
                        foreach (string l in libs)
                            if (string.Equals(l, p, StringComparison.OrdinalIgnoreCase)) { dup = true; break; }
                        if (!dup && Directory.Exists(p)) libs.Add(p);
                    }
                }
                catch (Exception ex) { Log("vdf read error: " + ex.Message); }
            }

            foreach (string lib in libs)
            {
                string appsDir = Path.Combine(lib, "steamapps");
                if (!Directory.Exists(appsDir)) continue;
                string[] acfs;
                try { acfs = Directory.GetFiles(appsDir, "*.acf"); }
                catch (Exception ex) { Log("acf list error in " + appsDir + ": " + ex.Message); continue; }

                foreach (string acf in acfs)
                {
                    try
                    {
                        string content = File.ReadAllText(acf, Encoding.UTF8);
                        Match m = Regex.Match(content, "\"appid\"\\s+\"(\\d+)\"");
                        if (!m.Success) continue;
                        m = Regex.Match(content, "\"name\"\\s+\"((?:[^\"\\\\]|\\\\.)*)\"");
                        if (!m.Success) continue;
                        string name = m.Groups[1].Value.Replace("\\\"", "\"");
                        m = Regex.Match(content, "\"installdir\"\\s+\"((?:[^\"\\\\]|\\\\.)*)\"");
                        string installDir = m.Success ? m.Groups[1].Value.Replace("\\\"", "\"") : "";

                        string lower = name.ToLowerInvariant();
                        if (lower.Contains("redistributables") || lower.Contains("steamworks")
                            || lower.Contains("runtime") || lower.Contains("proton")) continue;

                        string exeName = FindGameExe(Path.Combine(lib, "steamapps", "common", installDir), name);
                        if (string.IsNullOrEmpty(exeName))
                            exeName = name.Replace(" ", "").Replace(":", "").Replace("'", "");

                        result.Add(new MonitoredGame
                        {
                            DisplayName = "[Steam] " + name,
                            ProcessName = exeName,
                            TargetWidth = 2560,
                            TargetHeight = 1440,
                            Enabled = false,
                            Source = GameSource.Steam,
                        });
                    }
                    catch { }
                }
            }
            return result;
        }

        // 找主程序：优先与游戏名相近的 exe，其次体积最大的 exe
        private string FindGameExe(string dir, string gameName)
        {
            if (!Directory.Exists(dir)) return null;
            try
            {
                string[] exes = Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly);
                string[] bad = { "unins", "install", "setup", "redist", "dx", "vc_redist",
                    "dotnet", "updater", "crash", "steam", "launcher", "eos", "easyanticheat",
                    "battleye", "sdk", "redistributable" };

                string normalizedName = (gameName ?? "").ToLowerInvariant()
                    .Replace(" ", "").Replace(":", "").Replace("'", "").Replace("-", "").Replace(".", "");

                string best = null;
                long bestSize = 0;
                foreach (string exe in exes)
                {
                    string nameOnly = Path.GetFileNameWithoutExtension(exe);
                    string lower = nameOnly.ToLowerInvariant();
                    bool skip = false;
                    foreach (string b in bad) if (lower.Contains(b)) { skip = true; break; }
                    if (skip) continue;

                    try
                    {
                        long size = new FileInfo(exe).Length;
                        if (size < 1024 * 1024) continue;   // < 1MB 基本是工具/更新器

                        // 名字匹配的优先级最高
                        string norm = lower.Replace(" ", "").Replace(":", "").Replace("'", "")
                            .Replace("-", "").Replace(".", "");
                        if (normalizedName.Length > 2
                            && (norm == normalizedName || normalizedName.Contains(norm) || norm.Contains(normalizedName)))
                            return nameOnly;

                        if (size > bestSize) { bestSize = size; best = nameOnly; }
                    }
                    catch { }
                }
                return best;
            }
            catch { return null; }
        }

        private string FindSteamPath()
        {
            // 1. 用户手动指定（绿色版 / 便携版 Steam）
            if (_manualSteamPath.Length > 0 && Directory.Exists(_manualSteamPath)) return _manualSteamPath;
            // 2. 默认安装路径
            string def = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            if (Directory.Exists(def)) return def;
            // 3. 注册表 InstallPath（任意安装位置都会写入）
            string[] subKeys = { @"SOFTWARE\WOW6432Node\Valve\Steam", @"SOFTWARE\Valve\Steam" };
            foreach (string sub in subKeys)
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(sub))
                    {
                        if (key == null) continue;
                        string p = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(p) && Directory.Exists(p)) return p;
                    }
                }
                catch { }
            }
            // 4. 当前用户安装（部分精简版 Steam 只写 HKCU）
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string p = key.GetValue("SteamPath") as string;
                        if (!string.IsNullOrEmpty(p) && Directory.Exists(p)) return p;
                    }
                }
            }
            catch { }
            return null;
        }

        // ================================================================
        //  Monitoring (runs on UI thread via WinForms timer)
        // ================================================================
        private void CheckGames(object sender, EventArgs e)
        {
            if (_paused || _polling) return;
            _polling = true;
            try
            {
                // 一次性枚举进程，避免"每个游戏各扫一遍全量进程"
                HashSet<string> running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Process[] procs;
                try { procs = Process.GetProcesses(); }
                catch { return; }
                foreach (Process p in procs)
                {
                    try { running.Add(p.ProcessName); }
                    catch { }
                    finally { try { p.Dispose(); } catch { } }
                }

                MonitoredGame active = null;
                foreach (var g in _games)
                {
                    if (!g.Enabled || string.IsNullOrEmpty(g.ProcessName)) continue;
                    if (running.Contains(g.ProcessName)) { active = g; break; }
                }

                if (active != null)
                {
                    _guardTries = 0;
                    if (!_isInGameMode)
                    {
                        _isInGameMode = true;
                        _currentGameName = DisplayOf(active);
                        if (SwitchResolution(active.TargetWidth, active.TargetHeight))
                            Balloon(T("balloon_start_title"), string.Format(T("balloon_start"),
                                _currentGameName, active.TargetWidth, active.TargetHeight));
                        else
                            Balloon(T("switch_failed_title"), string.Format(T("switch_failed"),
                                active.TargetWidth, active.TargetHeight));
                    }
                    else if (!SameProcess(active))
                    {
                        // 换了一个被监控的游戏（例如从 A 切到 B）
                        _currentGameName = DisplayOf(active);
                        if (SwitchResolution(active.TargetWidth, active.TargetHeight))
                            Balloon(T("balloon_start_title"), string.Format(T("balloon_start"),
                                _currentGameName, active.TargetWidth, active.TargetHeight));
                    }
                }
                else if (_isInGameMode)
                {
                    _isInGameMode = false;
                    _currentGameName = "";
                    _lastGameExit = DateTime.Now;
                    _guardTries = 0;
                    if (RestoreResolution())
                        Balloon(T("balloon_exit_title"), string.Format(T("balloon_exit"),
                            _desktopMode.dmPelsWidth, _desktopMode.dmPelsHeight));
                    else
                        Balloon(T("restore_failed_title"), string.Format(T("restore_failed"),
                            _desktopMode.dmPelsWidth, _desktopMode.dmPelsHeight));
                }
                else if (_desktopValid && _guardTries < 5
                         && (DateTime.Now - _lastGameExit).TotalSeconds < 20)
                {
                    // 显示守护：游戏退出后若桌面被降级，再恢复一次（有限次数，避免反复折腾）
                    DEVMODE cur = GetCurrentDisplayMode();
                    if (IsValidMode(cur)
                        && (cur.dmPelsWidth < _desktopMode.dmPelsWidth
                            || cur.dmPelsHeight < _desktopMode.dmPelsHeight))
                    {
                        _guardTries++;
                        Log("guard#" + _guardTries + ": desktop degraded to " + ModeStr(cur));
                        RestoreResolution();
                    }
                }

                UpdateStatus();
            }
            catch (Exception ex) { Log("poll error: " + ex.Message); }
            finally { _polling = false; }
        }

        private bool SameProcess(MonitoredGame g)
        {
            foreach (var x in _games)
            {
                if (!x.Enabled || string.IsNullOrEmpty(x.ProcessName)) continue;
                if (DisplayOf(x) == _currentGameName) return true;
            }
            return false;
        }

        // ================================================================
        //  Resolution switching
        // ================================================================
        private bool SwitchResolution(int width, int height)
        {
            try
            {
                if (!_desktopValid && !RefreshDesktopTarget())
                {
                    Log("switch: aborted, no valid desktop target");
                    Balloon(T("capture_failed_title"), T("capture_failed"));
                    return false;
                }

                DEVMODE before = GetCurrentDisplayMode();

                // 若已经是目标分辨率，无需切换
                if (IsValidMode(before)
                    && before.dmPelsWidth == width && before.dmPelsHeight == height)
                {
                    Log("switch: already at " + width + "x" + height);
                    return true;
                }

                // 从可用模式里挑最合适的一个（保留桌面刷新率，其次取最高刷新率）
                DEVMODE target = FindBestMode(width, height,
                    _desktopMode.dmBitsPerPel, _desktopMode.dmDisplayFrequency);
                if (!IsValidMode(target))
                {
                    Log("switch: mode " + width + "x" + height + " not available");
                    return false;
                }
                // 枚举出来的 DEVMODE 的 dmFields 可能为 0，必须确保关键字段被声明，
                // 否则 ChangeDisplaySettingsEx 会返回 DISP_CHANGE_FAILED。
                // 用 |= 保留枚举时驱动给的其它标志（方向/位深等），比整体覆盖更安全
                target.dmFields |= DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL | DM_DISPLAYFREQUENCY;

                int r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
                Log("switch test " + ModeStr(target) + " " + target.dmBitsPerPel + "bpp, fields=0x"
                    + target.dmFields.ToString("X8") + " -> " + r);

                if (r != DISP_CHANGE_SUCCESSFUL)
                {
                    // 回退：从当前模式派生（只改宽高），兼容个别驱动不接受枚举模式的情况
                    DEVMODE derived = GetCurrentDisplayMode();
                    if (IsValidMode(derived))
                    {
                        derived.dmPelsWidth = width;
                        derived.dmPelsHeight = height;
                        derived.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
                        int t2 = ChangeDisplaySettingsEx(null, ref derived, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
                        Log("switch fallback derive " + width + "x" + height + " -> " + t2);
                        if (t2 == DISP_CHANGE_SUCCESSFUL) { target = derived; r = t2; }
                    }
                }

                if (r == DISP_CHANGE_SUCCESSFUL)
                    // flags=0 = 临时模式（不写注册表），避免把游戏分辨率变成系统默认
                    // 注意：CDS_GLOBAL 必须与 CDS_UPDATEREGISTRY 联用，单独用会返回 BADFLAGS
                    r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero, 0, IntPtr.Zero);

                DEVMODE now = GetCurrentDisplayMode();
                bool ok = r == DISP_CHANGE_SUCCESSFUL && IsValidMode(now)
                          && now.dmPelsWidth == width && now.dmPelsHeight == height;

                Log("switch " + width + "x" + height + ": " + ModeStr(before) + " -> "
                    + ModeStr(now) + ", result=" + r + ", ok=" + ok);
                return ok;
            }
            catch (Exception ex)
            {
                Log("switch error: " + ex);
                return false;
            }
        }

        private bool RestoreResolution()
        {
            try
            {
                if (!_desktopValid && !RefreshDesktopTarget())
                {
                    Log("restore: aborted, no valid desktop target");
                    Balloon(T("capture_failed_title"), T("capture_failed"));
                    return false;
                }

                DEVMODE cur = GetCurrentDisplayMode();
                if (IsValidMode(cur)
                    && cur.dmPelsWidth == _desktopMode.dmPelsWidth
                    && cur.dmPelsHeight == _desktopMode.dmPelsHeight
                    && cur.dmDisplayFrequency == _desktopMode.dmDisplayFrequency)
                {
                    Log("restore: already at desktop mode " + ModeStr(cur));
                    return true;
                }

                DEVMODE target = FindBestMode(_desktopMode.dmPelsWidth, _desktopMode.dmPelsHeight,
                    _desktopMode.dmBitsPerPel, _desktopMode.dmDisplayFrequency);

                // 保存的桌面模式已不可用（换显示器/换线）：退回当前可用最高分辨率
                if (!IsValidMode(target))
                {
                    target = FindHighestMode();
                    if (!IsValidMode(target)) { Log("restore: no usable mode at all"); return false; }
                    Log("restore: desktop mode unavailable, fallback to " + ModeStr(target));
                    _desktopMode = target;
                    SaveConfig();
                }

                // 枚举出来的 DEVMODE 的 dmFields 可能为 0，必须确保关键字段被声明
                target.dmFields |= DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL | DM_DISPLAYFREQUENCY;

                int r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
                Log("restore test " + ModeStr(target) + " -> " + r);
                if (r != DISP_CHANGE_SUCCESSFUL)
                {
                    DEVMODE derived = target;
                    derived.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
                    int t2 = ChangeDisplaySettingsEx(null, ref derived, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
                    Log("restore fallback derive -> " + t2);
                    if (t2 == DISP_CHANGE_SUCCESSFUL) { target = derived; r = t2; }
                }
                if (r == DISP_CHANGE_SUCCESSFUL)
                    r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero,
                        CDS_GLOBAL | CDS_UPDATEREGISTRY, IntPtr.Zero);

                // 验证并重试：显卡/显示器重新协商需要一点时间
                for (int i = 0; i < 3; i++)
                {
                    DEVMODE now = GetCurrentDisplayMode();
                    if (IsValidMode(now)
                        && now.dmPelsWidth == target.dmPelsWidth
                        && now.dmPelsHeight == target.dmPelsHeight)
                    {
                        Log("restore: result=" + r + " -> " + ModeStr(now));
                        return true;
                    }
                    System.Threading.Thread.Sleep(300);
                    ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero,
                        CDS_GLOBAL | CDS_UPDATEREGISTRY, IntPtr.Zero);
                }

                DEVMODE final = GetCurrentDisplayMode();
                bool ok = IsValidMode(final)
                          && final.dmPelsWidth == target.dmPelsWidth
                          && final.dmPelsHeight == target.dmPelsHeight;
                Log("restore: result=" + r + " -> " + ModeStr(final) + ", ok=" + ok);
                return ok;
            }
            catch (Exception ex)
            {
                Log("restore error: " + ex);
                return false;
            }
        }

        private void SetCurrentAsTarget()
        {
            DEVMODE cur = GetCurrentDisplayMode();
            if (!IsValidMode(cur)) return;
            _desktopMode = cur;
            _desktopValid = true;
            SaveConfig();
            UpdateStatus();
            Balloon(T("title_done"), string.Format(T("target_set"),
                cur.dmPelsWidth, cur.dmPelsHeight, cur.dmDisplayFrequency));
        }

        private void TogglePause()
        {
            _paused = !_paused;

            if (!_paused && !_desktopValid) RefreshDesktopTarget();

            if (_paused && _isInGameMode)
            {
                RestoreResolution();
                _isInGameMode = false;
                _currentGameName = "";
            }

            _trayIcon.Icon = CreateTrayIcon(_paused);
            UpdateTrayText();
        }

        private bool RefreshDesktopTarget()
        {
            DEVMODE cur = GetCurrentDisplayMode();
            if (IsValidMode(cur))
            {
                _desktopMode = cur; _desktopValid = true;
                SaveConfig();
                Log("desktop target refreshed from current: " + ModeStr(cur));
                return true;
            }
            DEVMODE reg = GetRegistryDisplayMode();
            if (IsValidMode(reg))
            {
                _desktopMode = reg; _desktopValid = true;
                SaveConfig();
                Log("desktop target refreshed from registry: " + ModeStr(reg));
                return true;
            }
            return false;
        }

        // ================================================================
        //  Config persistence
        // ================================================================
        private void LoadConfig()
        {
            if (!File.Exists(_configPath)) return;
            string[] lines;
            try { lines = File.ReadAllLines(_configPath, Encoding.UTF8); }
            catch (Exception ex) { Log("config read error: " + ex.Message); return; }

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                // 单行容错：一行坏数据不影响其余条目
                try
                {
                    if (line.StartsWith("STEAMPATH=", StringComparison.OrdinalIgnoreCase))
                    {
                        _manualSteamPath = line.Substring("STEAMPATH=".Length).Trim();
                        continue;
                    }
                    if (line.StartsWith("DESKTOP=", StringComparison.OrdinalIgnoreCase))
                    {
                        _savedDesktop = line.Substring("DESKTOP=".Length).Trim();
                        continue;
                    }

                    string[] p = line.Split('|');
                    if (p.Length < 5) continue;

                    string name = p[0].Trim();
                    string proc = p[1].Trim();
                    if (name.Length == 0 || proc.Length == 0) continue;

                    GameSource src = GameSource.Steam;
                    if (p.Length >= 6)
                        src = p[5].Trim().Equals("manual", StringComparison.OrdinalIgnoreCase)
                            ? GameSource.Manual : GameSource.Steam;
                    else if (!name.StartsWith("[Steam]", StringComparison.OrdinalIgnoreCase))
                        src = GameSource.Manual;   // 旧版配置没有来源列，靠前缀推断

                    // 旧版把本地化后缀写进了名字，这里清理掉，避免切换语言后出现怪名字
                    foreach (string suffix in new string[] { " (手动)", " (manual)", " (手動)" })
                        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                            name = name.Substring(0, name.Length - suffix.Length).Trim();

                    int w = 2560, h = 1440;
                    int.TryParse(p[2], out w);
                    int.TryParse(p[3], out h);
                    if (w < 640) w = 2560;
                    if (h < 480) h = 1440;

                    _games.Add(new MonitoredGame
                    {
                        DisplayName = name,
                        ProcessName = proc,
                        TargetWidth = w,
                        TargetHeight = h,
                        Enabled = p[4].Trim() == "1",
                        Source = src,
                    });
                }
                catch (Exception ex) { Log("config line skipped (" + line + "): " + ex.Message); }
            }
        }

        // 原子写入：先写临时文件再替换，避免断电/崩溃导致配置损坏
        private void SaveConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# 分辨率自动监控 - 游戏列表 / Resolution Switcher config");
                sb.AppendLine("# 格式: 显示名|进程名|目标宽|目标高|启用(1/0)|来源(steam/manual)");
                sb.AppendLine("# 恢复目标（桌面分辨率），留空表示按当前分辨率自动推断");
                sb.AppendLine("STEAMPATH=" + _manualSteamPath);
                sb.AppendLine("DESKTOP=" + (_desktopValid
                    ? _desktopMode.dmPelsWidth + "x" + _desktopMode.dmPelsHeight
                      + "@" + _desktopMode.dmDisplayFrequency
                    : ""));
                foreach (var g in _games)
                {
                    sb.AppendLine(Sanitize(g.DisplayName) + "|" + Sanitize(g.ProcessName) + "|"
                        + g.TargetWidth + "|" + g.TargetHeight + "|"
                        + (g.Enabled ? "1" : "0") + "|"
                        + (g.Source == GameSource.Manual ? "manual" : "steam"));
                }

                string tmp = _configPath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), Encoding.UTF8);
                if (File.Exists(_configPath))
                {
                    try { File.Replace(tmp, _configPath, null); }
                    catch { File.Copy(tmp, _configPath, true); File.Delete(tmp); }
                }
                else
                {
                    File.Move(tmp, _configPath);
                }
            }
            catch (Exception ex) { Log("config save error: " + ex.Message); }
        }

        private static string Sanitize(string s)
        {
            if (s == null) return "";
            return s.Replace('|', '｜').Replace("\r", " ").Replace("\n", " ");
        }

        // ================================================================
        //  Autostart (HKCU Run + 兼容旧版 shell:startup 快捷方式)
        // ================================================================
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "AutoGameDetect";
        private static readonly string[] LegacyShortcutNames =
        {
            "分辨率自动监控.lnk", "ResolutionSwitcher.lnk", "AutoGameDetect.lnk",
        };

        private static string StartupFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
        }

        private bool IsAutostartEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                {
                    if (k != null && k.GetValue(RunValueName) != null) return true;
                }
            }
            catch { }
            foreach (string n in LegacyShortcutNames)
            {
                try { if (File.Exists(Path.Combine(StartupFolder(), n))) return true; }
                catch { }
            }
            return false;
        }

        private bool SetAutostart(bool enabled, out string error)
        {
            error = null;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (k == null) { error = "registry"; return false; }
                    if (enabled) k.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\"");
                    else k.DeleteValue(RunValueName, false);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            // 迁移/清理：旧版用 shell:startup 快捷方式开机自启，删除以免重复启动
            foreach (string n in LegacyShortcutNames)
            {
                try
                {
                    string p = Path.Combine(StartupFolder(), n);
                    if (File.Exists(p)) File.Delete(p);
                }
                catch { }
            }
            Log("autostart set to " + enabled);
            return true;
        }

        // ================================================================
        //  Helpers
        // ================================================================
        private static DEVMODE NewDevMode()
        {
            DEVMODE m = new DEVMODE();
            m.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            return m;
        }

        private DEVMODE GetCurrentDisplayMode()
        {
            DEVMODE mode = NewDevMode();
            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref mode) != 0)
                return mode;
            return new DEVMODE();
        }

        // 枚举失败时返回全零结构（dmSize=0），绝不能传给 ChangeDisplaySettingsEx
        private static bool IsValidMode(DEVMODE m)
        {
            return m.dmSize != 0 && m.dmPelsWidth >= 640 && m.dmPelsHeight >= 480;
        }

        private static bool ModeFits(DEVMODE m, int w, int h)
        {
            return IsValidMode(m) && m.dmPelsWidth == w && m.dmPelsHeight == h;
        }

        private DEVMODE GetRegistryDisplayMode()
        {
            DEVMODE mode = NewDevMode();
            if (EnumDisplaySettings(null, ENUM_REGISTRY_SETTINGS, ref mode) != 0)
                return mode;
            return new DEVMODE();
        }

        // 在可用模式里挑最合适的：同宽高下优先匹配刷新率，其次取最高刷新率
        private DEVMODE FindBestMode(int width, int height, int bits, int preferredRefresh)
        {
            DEVMODE exact = new DEVMODE();
            DEVMODE best = new DEVMODE();
            int bestRefresh = -1;

            DEVMODE mode = NewDevMode();
            int i = 0;
            while (EnumDisplaySettings(null, i, ref mode) != 0)
            {
                if (mode.dmPelsWidth == width && mode.dmPelsHeight == height)
                {
                    if (preferredRefresh > 0 && mode.dmDisplayFrequency == preferredRefresh
                        && (bits <= 0 || mode.dmBitsPerPel == bits))
                        exact = mode;

                    int r = mode.dmDisplayFrequency;
                    if (bits > 0 && mode.dmBitsPerPel != bits) r -= 10000;  // 位深不符降权
                    if (r > bestRefresh) { bestRefresh = r; best = mode; }
                }
                i++;
                mode = NewDevMode();
            }
            return IsValidMode(exact) ? exact : best;
        }

        // 当前显示器支持的最高分辨率模式（恢复目标失效时的兜底）
        private DEVMODE FindHighestMode()
        {
            DEVMODE best = new DEVMODE();
            long bestPixels = 0;
            DEVMODE mode = NewDevMode();
            int i = 0;
            while (EnumDisplaySettings(null, i, ref mode) != 0)
            {
                long px = (long)mode.dmPelsWidth * mode.dmPelsHeight;
                if (px > bestPixels || (px == bestPixels && mode.dmDisplayFrequency > best.dmDisplayFrequency))
                {
                    bestPixels = px;
                    best = mode;
                }
                i++;
                mode = NewDevMode();
            }
            return best;
        }

        private static bool TryParseMode(string s, out int w, out int h, out int freq)
        {
            w = 0; h = 0; freq = 0;
            if (string.IsNullOrEmpty(s)) return false;
            Match m = Regex.Match(s.Trim(), @"^(\d+)\s*[x×]\s*(\d+)(?:\s*@\s*(\d+))?$");
            if (!m.Success) return false;
            int.TryParse(m.Groups[1].Value, out w);
            int.TryParse(m.Groups[2].Value, out h);
            if (m.Groups[3].Success) int.TryParse(m.Groups[3].Value, out freq);
            return w >= 640 && h >= 480;
        }

        private static string ModeStr(DEVMODE m)
        {
            if (!IsValidMode(m)) return "(invalid)";
            return m.dmPelsWidth + "x" + m.dmPelsHeight + "@" + m.dmDisplayFrequency + "Hz";
        }

        // 轻量日志：记录捕获/切换/恢复结果，便于事后诊断显示异常
        private static readonly object _logLock = new object();

        // 数据目录：优先 %LOCALAPPDATA%\ResolutionSwitcher；
        // 若被安全软件/策略拒绝写入，自动降级到 exe 同级 data\ 或 %APPDATA%，
        // 保证设置能存下来（日志里会写明最终用了哪个目录）
        private static string _dataDir = null;
        private static bool _dataDirFallback = false;
        private static readonly object _dataDirLock = new object();

        private static string StandardDataDir()
        {
            try
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ResolutionSwitcher");
            }
            catch { return ""; }
        }

        // 能否在该目录创建并写入文件（一次真实探测，比权限判断可靠）
        private static bool IsWritableDir(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return false;
            try
            {
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".write-probe-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                File.WriteAllText(probe, "x", Encoding.UTF8);
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        // 降级到 exe 同级目录时，把原来保存在 %LOCALAPPDATA% 里的配置迁移过来，
        // 避免"换了目录后游戏列表突然空了"（读取通常仍被允许，只是写入被拦）
        private static void MigrateLegacyConfig(string targetDir)
        {
            try
            {
                string standard = StandardDataDir();
                if (string.IsNullOrEmpty(standard)) return;
                string src = Path.Combine(standard, "monitored_games.txt");
                string dst = Path.Combine(targetDir, "monitored_games.txt");
                if (File.Exists(dst) || !File.Exists(src)) return;
                string content = File.ReadAllText(src, Encoding.UTF8);
                File.WriteAllText(dst, content, Encoding.UTF8);
                Log("migrated config: " + src + " -> " + dst);
            }
            catch (Exception ex) { Log("migrate config failed: " + ex.Message); }
        }

        private static string ResolveDataDir()
        {
            lock (_dataDirLock)
            {
                if (_dataDir != null) return _dataDir;

                string env = "";
                try { env = (Environment.GetEnvironmentVariable("AGD_DATA_DIR") ?? "").Trim(); }
                catch { }

                string standard = StandardDataDir();
                List<string> candidates = new List<string>();
                if (env.Length > 0) candidates.Add(env);
                candidates.Add(standard);
                try
                {
                    candidates.Add(Path.Combine(
                        Path.GetDirectoryName(Application.ExecutablePath) ?? "", "data"));
                }
                catch { }
                try
                {
                    candidates.Add(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "ResolutionSwitcher"));
                }
                catch { }

                foreach (string dir in candidates)
                {
                    if (!IsWritableDir(dir)) continue;
                    _dataDir = dir;
                    _dataDirFallback = standard.Length > 0
                        && !string.Equals(dir, standard, StringComparison.OrdinalIgnoreCase);
                    return _dataDir;
                }

                // 全都不行：退回临时目录，至少程序能跑
                _dataDir = Path.GetTempPath();
                _dataDirFallback = true;
                return _dataDir;
            }
        }

        // 惰性解析日志路径：构造早期出错时同样能落盘（否则静默退出无从排查）
        private static string LogPath()
        {
            if (_logPath.Length > 0) return _logPath;
            try { _logPath = Path.Combine(ResolveDataDir(), "resolution.log"); }
            catch { _logPath = ""; }
            return _logPath;
        }

        private static void Log(string msg)
        {
            try
            {
                string path = LogPath();
                if (string.IsNullOrEmpty(path)) return;
                lock (_logLock)
                {
                    _logPath = path;
                    string dir = Path.GetDirectoryName(_logPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    // 轮转而不是直接删除，保留上一份便于回溯
                    if (File.Exists(_logPath) && new FileInfo(_logPath).Length > 512 * 1024)
                    {
                        string old = _logPath + ".old";
                        try { if (File.Exists(old)) File.Delete(old); } catch { }
                        try { File.Move(_logPath, old); } catch { }
                    }
                    File.AppendAllText(_logPath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
            catch { }
        }

        private void Balloon(string title, string text)
        {
            try
            {
                if (_trayIcon == null) return;
                _trayIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
            }
            catch { }
        }

        private Icon CreateTrayIcon(bool paused)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                Color fill = paused ? Color.FromArgb(130, 130, 130) : Color.FromArgb(50, 120, 200);
                using (SolidBrush b = new SolidBrush(fill))
                    g.FillEllipse(b, 1, 1, 14, 14);
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    using (Font f = new Font("Arial", 8, FontStyle.Bold))
                        g.DrawString("R", f, white, 3, 2);
                }
            }
            // 旧句柄必须先释放，否则每次切换状态都会泄漏一个 GDI 图标句柄
            if (_iconHandle != IntPtr.Zero)
            {
                try { DestroyIcon(_iconHandle); } catch { }
                _iconHandle = IntPtr.Zero;
            }
            _iconHandle = bmp.GetHicon();
            Icon icon = Icon.FromHandle(_iconHandle);
            bmp.Dispose();
            return icon;
        }

        private void Quit()
        {
            // 先恢复分辨率，再销毁托盘对象，否则恢复过程中的通知会落在已释放的控件上
            if (_isInGameMode)
            {
                RestoreResolution();
                _isInGameMode = false;
            }
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
                _pollTimer = null;
            }
            if (_activateTimer != null)
            {
                _activateTimer.Stop();
                _activateTimer.Dispose();
                _activateTimer = null;
            }
            if (_configForm != null)
            {
                try { _configForm.Close(); } catch { }
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            if (_iconHandle != IntPtr.Zero)
            {
                try { DestroyIcon(_iconHandle); } catch { }
                _iconHandle = IntPtr.Zero;
            }
            Log("=== exit ===");
            Application.Exit();
        }

        // ================================================================
        //  Entry point
        // ================================================================
        [STAThread]
        static void Main()
        {
            try
            {
                Log("main: enter");

                // 4K 屏上若不做 DPI 感知，Windows 会位图拉伸整个界面 → 字体发虚
                try { SetProcessDPIAware(); } catch { }
                Log("main: dpi-aware");

                Application.EnableVisualStyles();
                Log("main: visual styles");
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => Log("UI exception: " + e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => Log("fatal: " + e.ExceptionObject);
                Log("main: handlers");

                bool createdNew;
                using (System.Threading.Mutex mutex = new System.Threading.Mutex(
                           true, "ResolutionAutoDetect", out createdNew))
                {
                    Log("main: mutex createdNew=" + createdNew);
                    if (!createdNew)
                    {
                        // 已有实例在跑：请它把设置窗口弹出来。
                        // 这是托盘图标被系统折叠时的兜底入口——再运行一次 exe 就能看到界面
                        bool signaled = false;
                        try
                        {
                            using (System.Threading.EventWaitHandle ev =
                                       System.Threading.EventWaitHandle.OpenExisting(ActivateEventName))
                            {
                                ev.Set();
                                signaled = true;
                            }
                        }
                        catch { }
                        Log("main: second instance, activateRequested=" + signaled);
                        if (!signaled) MessageBox.Show(T("main_already"), T("title_already"));
                        return;
                    }

                    using (System.Threading.EventWaitHandle ev = new System.Threading.EventWaitHandle(
                               false, System.Threading.EventResetMode.AutoReset, ActivateEventName))
                    {
                        System.Threading.Thread watcher = new System.Threading.Thread(() =>
                        {
                            while (true)
                            {
                                try { ev.WaitOne(); }
                                catch { return; }
                                _activateRequested = true;
                            }
                        });
                        watcher.IsBackground = true;
                        watcher.Start();

                        AppContext ctx = new AppContext();
                        Log("main: context ready");
                        Application.Run(ctx);
                        Log("main: message loop ended");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("main fatal: " + ex);
                throw;
            }
        }
    }
}
