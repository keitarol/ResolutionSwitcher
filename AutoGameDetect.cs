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

        // --- UI components ---
        private Form _configForm;
        private CheckedListBox _chkList;
        private NumericUpDown _numWidth;
        private NumericUpDown _numHeight;
        private Button _btnAddManual;
        private Button _btnScan;
        private TextBox _txtSteamPath;
        private Label _lblStatus;

        public AppContext()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ResolutionSwitcher", "monitored_games.txt");

            _originalMode = GetCurrentDisplayMode();

            // Tray icon
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = CreateTrayIcon();
            _trayIcon.Text = "分辨率自动监控\n正在监控 " + GetEnabledCount() + " 个游戏";
            _trayIcon.Visible = true;

            var menu = new ContextMenuStrip();
            menu.Items.Add("设置监控游戏", null, (s, e) => ShowConfig());
            menu.Items.Add("暂停监控", null, (s, e) => TogglePause());
            menu.Items[1].Name = "menuPause";
            menu.Items.Add("立即恢复 4K", null, (s, e) => RestoreResolution());
            menu.Items.Add("-");
            menu.Items.Add("退出", null, (s, e) => Quit());
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, e) => ShowConfig();

            LoadConfig();

            // If no games configured, show config on first run
            if (_games.Count == 0)
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
            _configForm.Text = "分辨率自动监控 - 设置";
            _configForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            _configForm.StartPosition = FormStartPosition.CenterScreen;
            _configForm.MaximizeBox = false;
            _configForm.Font = new Font("微软雅黑", 9);
            _configForm.ClientSize = new Size(520, 460);
            _configForm.ShowInTaskbar = true;

            int y = 12;

            // Info
            var lblInfo = new Label
            {
                Text = "选择需要自动切换到 2K 的游戏：",
                Location = new Point(12, y),
                Size = new Size(480, 20),
            };
            _configForm.Controls.Add(lblInfo);
            y += 26;

            // Scan button
            _btnScan = new Button
            {
                Text = "🔄 扫描已安装游戏",
                Location = new Point(12, y),
                Size = new Size(140, 28),
            };
            _btnScan.Click += (s, e) => ScanAndPopulate();
            _configForm.Controls.Add(_btnScan);
            y += 36;

            // Checked list
            _chkList = new CheckedListBox
            {
                Location = new Point(12, y),
                Size = new Size(496, 200),
                CheckOnClick = true,
                Font = new Font("Consolas", 9),
            };
            _configForm.Controls.Add(_chkList);
            y += 208;

            // Game resolution target
            var lblTarget = new Label
            {
                Text = "目标分辨率:",
                Location = new Point(12, y),
                Size = new Size(90, 24),
            };
            _configForm.Controls.Add(lblTarget);

            _numWidth = new NumericUpDown
            {
                Location = new Point(102, y),
                Width = 70,
                Minimum = 640,
                Maximum = 7680,
                Value = 2560,
            };
            _configForm.Controls.Add(_numWidth);

            var lblX = new Label { Text = "×", Location = new Point(176, y), Size = new Size(20, 24) };
            _configForm.Controls.Add(lblX);

            _numHeight = new NumericUpDown
            {
                Location = new Point(196, y),
                Width = 70,
                Minimum = 480,
                Maximum = 4320,
                Value = 1440,
            };
            _configForm.Controls.Add(_numHeight);

            // Current res display
            var lblCur = new Label
            {
                Text = "当前: " + _originalMode.dmPelsWidth + "×" + _originalMode.dmPelsHeight,
                Location = new Point(290, y + 2),
                Size = new Size(200, 20),
                ForeColor = Color.Gray,
            };
            _configForm.Controls.Add(lblCur);
            y += 32;

            // Add manual game
            var lblProc = new Label { Text = "进程名(不含.exe):", Location = new Point(12, y), Size = new Size(120, 24) };
            _configForm.Controls.Add(lblProc);
            TextBox txtProc = new TextBox { Location = new Point(132, y), Width = 160 };
            _configForm.Controls.Add(txtProc);
            _btnAddManual = new Button
            {
                Text = "添加手动",
                Location = new Point(298, y),
                Size = new Size(80, 26),
            };
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
                            MessageBox.Show("该游戏已在列表中。", "提示");
                            return;
                        }
                    }
                    _games.Add(new MonitoredGame
                    {
                        DisplayName = name + " (手动)",
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
            Button btnDel = new Button
            {
                Text = "删除选中",
                Location = new Point(384, y),
                Size = new Size(80, 26),
                ForeColor = Color.Red,
            };
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
            var lblSteam = new Label { Text = "Steam 路径(可选):", Location = new Point(12, y), Size = new Size(110, 24) };
            _configForm.Controls.Add(lblSteam);
            _txtSteamPath = new TextBox { Location = new Point(122, y), Width = 240, Text = _manualSteamPath };
            _configForm.Controls.Add(_txtSteamPath);
            Button btnBrowse = new Button
            {
                Text = "浏览...",
                Location = new Point(368, y),
                Size = new Size(70, 26),
            };
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "选择 Steam 安装目录（含 steam.exe 的文件夹）";
                    dlg.ShowNewFolderButton = false;
                    if (dlg.ShowDialog() == DialogResult.OK)
                        _txtSteamPath.Text = dlg.SelectedPath;
                }
            };
            _configForm.Controls.Add(btnBrowse);
            var lblSteamHint = new Label
            {
                Text = "留空 = 自动检测（默认路径 / 注册表，支持任意安装位置）",
                Location = new Point(12, y + 26),
                Size = new Size(480, 16),
                ForeColor = Color.Gray,
                Font = new Font("微软雅黑", 8),
            };
            _configForm.Controls.Add(lblSteamHint);
            y += 50;

            // Status & close
            _lblStatus = new Label
            {
                Text = "状态: " + (_paused ? "⏸ 已暂停" : "▶ 运行中") + "  |  监控 " + GetEnabledCount() + " 个游戏",
                Location = new Point(12, y),
                Size = new Size(350, 20),
                ForeColor = Color.Gray,
            };
            _configForm.Controls.Add(_lblStatus);

            Button btnClose = new Button
            {
                Text = "关闭",
                Location = new Point(400, y - 4),
                Size = new Size(80, 28),
            };
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
            _btnScan.Text = "扫描中...";
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
            _btnScan.Text = "🔄 扫描已安装游戏";

            string msg = "扫描完成";
            if (added > 0) msg += "，新增 " + added + " 个游戏";
            if (removed > 0) msg += "，移除 " + removed + " 个已卸载的游戏";
            if (added == 0 && removed == 0) msg += "，列表已是最新";
            msg += "。\n请勾选要自动监控的游戏。";
            MessageBox.Show(msg, "扫描完成");
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
                    ShowBalloon("游戏检测", "检测到 " + foundProc + " 已启动，已切换到 " + foundWidth + "×" + foundHeight);
                }
                else if (!foundGame && _isInGameMode)
                {
                    // Game just exited
                    _isInGameMode = false;
                    _currentGameProcess = "";
                    RestoreResolution();
                    ShowBalloon("游戏退出", "已恢复原始分辨率 " + _originalMode.dmPelsWidth + "×" + _originalMode.dmPelsHeight);
                }
            }
            catch { }
        }

        private void SwitchResolution(int width, int height)
        {
            try
            {
                DEVMODE target = _originalMode;
                target.dmPelsWidth = width;
                target.dmPelsHeight = height;
                target.dmFields = 0x00080000 | 0x00100000;

                DEVMODE exact = FindMatchingMode(width, height,
                    _originalMode.dmBitsPerPel, _originalMode.dmDisplayFrequency);
                if (exact.dmSize != 0) target = exact;

                int r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
                if (r == DISP_CHANGE_SUCCESSFUL)
                    r = ChangeDisplaySettingsEx(null, ref target, IntPtr.Zero,
                        CDS_GLOBAL | CDS_UPDATEREGISTRY, IntPtr.Zero);
            }
            catch { }
        }

        private void RestoreResolution()
        {
            try
            {
                ChangeDisplaySettingsEx(null, ref _originalMode, IntPtr.Zero,
                    CDS_GLOBAL | CDS_UPDATEREGISTRY, IntPtr.Zero);
            }
            catch { }
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
            menu.Items[1].Text = _paused ? "▶ 恢复监控" : "⏸ 暂停监控";

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
            string status = _paused ? "⏸ 已暂停" : "▶ 运行中";
            _trayIcon.Text = "分辨率自动监控\n" + status + " | 监控 " + GetEnabledCount() + " 个游戏";
            if (_lblStatus != null && !_lblStatus.IsDisposed)
                _lblStatus.Text = "状态: " + status + "  |  监控 " + GetEnabledCount() + " 个游戏";
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
        static void Main()
        {
            // Ensure single instance
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "ResolutionAutoDetect", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("分辨率自动监控已在运行！\n请在系统托盘图标上操作。", "已在运行");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new AppContext());
            }
        }
    }
}
