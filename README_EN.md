# ResolutionSwitcher — Automatic Resolution Switching for 4K Displays

[中文](README.md) | **English**

A lightweight Windows tray utility that automatically switches your display from 4K to 2K (2560×1440) when a monitored game launches, and restores the original resolution when the game exits.

## 💡 Why This Tool Exists

You bought a 4K monitor, but your hardware can only comfortably drive 2K in demanding titles. Switching resolutions manually — before and after every gaming session — quickly becomes tedious. ResolutionSwitcher solves this with a fully hands-off workflow: **4K for everyday use, automatic 2K for games, automatic restore on exit.**

Ideal for:

- 🖥️ 4K monitors paired with GPUs that struggle at 4K in AAA titles
- 🎮 Games like GTA V, Tomb Raider, or other demanding titles that run much smoother at 2K
- ⚡ Lightweight games (e.g., MOBAs like League of Legends) that stay at full 4K quality — they are simply never monitored

## ✨ Features

- 🎮 **Automatic Steam library scanning** — reads `libraryfolders.vdf` and `appmanifest_*.acf`, supports multiple Steam library folders, and detects installed games automatically
- 📍 **Non-default Steam installs supported** — Steam installed on any drive is detected via the Windows registry; portable/green Steam builds can be added manually in Settings
- ✅ **Per-game opt-in monitoring** — only the games you check are monitored; everything else (including LOL) runs untouched at 4K
- 📋 **Multi-column game list** — game / process / target resolution / enabled state at a glance, no misalignment with mixed CJK and Latin text
- ⚙️ **Independent target resolution per game** — select rows and click "Apply to Selected" to run different titles at 2K, 1080p, etc.
- 🗑️ **Self-cleaning rescan** — re-scanning removes uninstalled games while preserving manually added entries
- 🧠 **Persistent restore target** — the desktop mode is stored in the config, so a restart or crash never mistakes the 2K mode you were gaming in for your "original" resolution
- 🖱️ **System tray operation** — right-click menu: Settings / current resolution / Pause / Restore now / Set current as restore target / Exit
- 🌍 **Automatic localization** — detects the system UI language at startup: `zh-*` shows Chinese, everything else shows English (all UI text lives in a centralized dictionary), and `--lang` forces a choice
- 🚀 **Built-in autostart toggle** — one checkbox in Settings, no shortcut juggling
- 🖥️ **Crisp on 4K displays** — declares DPI awareness, so the UI is not bitmap-stretched and blurry
- 🩺 **Diagnostic log** — every switch/restore is written to `resolution.log` for one-glance troubleshooting


## 🚀 Getting Started

### 📥 Download

1. Download the latest release from the [Releases page](https://github.com/keitarol/ResolutionSwitcher/releases/latest)
2. Grab `AutoGameDetect.exe` — no installation required, just double-click to run

### 🎮 Usage

1. Run `AutoGameDetect.exe`
2. On first launch, the settings window opens automatically — click **"🔄 Scan Installed Games"**
3. Check the games you want to run at 2K (e.g., GTA V, Tomb Raider)
4. Close the window; the program keeps running in the system tray
5. From now on: launch a monitored game → auto-switch to 2K; exit the game → auto-restore 4K

> 💡 **Tip:** Do **not** check lightweight games like League of Legends — launch them normally to enjoy full 4K quality.

### 🖱️ Tray Menu

| Menu Item | Action |
|-----------|--------|
| Settings... | Open the game monitoring configuration window |
| Desktop: W×H@Hz | Read-only, shows the current display mode |
| Pause Monitoring / Resume | Temporarily disable or re-enable auto-switching |
| Restore Desktop Resolution | Immediately restore the target desktop resolution |
| Set Current as Restore Target | Use the current display mode as the restore target |
| Exit | Quit the program |

## ⚙️ Configuration

| Item | Location |
|------|----------|
| Monitored games + restore target | `%LOCALAPPDATA%\ResolutionSwitcher\monitored_games.txt` |
| Diagnostic log | `%LOCALAPPDATA%\ResolutionSwitcher\resolution.log` (rotated to `.old` at 512 KB) |
| Autostart | Registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (toggle in Settings) |

Config writes are atomic (temp file + replace), and loading is per-line fault tolerant: one corrupted line never hides the rest of your games.

## 🧰 Command Line

| Argument / variable | Effect |
|------|------|
| `--lang zh` / `--lang en` | Force the UI language (handy to preview English on a Chinese system) |
| `AGD_LANG=en` | Same, as an environment variable |
| `AGD_DATA_DIR=D:\MyTools\RS` | Portable mode: keep config and log in the given folder instead of `%LOCALAPPDATA%` |

```bat
:: Example: forced English UI in portable mode
set AGD_LANG=en
set AGD_DATA_DIR=%~dp0data
AutoGameDetect.exe
```

## ⚠️ Known Limitations

- Only the **primary display** is switched (multi-monitor setups where the game runs on a secondary screen will not behave as expected)
- The game must run in **exclusive fullscreen**; borderless-windowed games ignore desktop resolution changes
- When the Windows session is locked or running over Remote Desktop, the OS refuses display-mode changes (the log records `DISP_CHANGE_FAILED`)

## 🌍 Localization

The interface language follows the system UI culture at startup:

- `zh-*` (Chinese) → Chinese UI
- Anything else → English UI

All user-visible strings (window title, buttons, labels, tray menu, balloon notifications, dialog messages) are centralized in the `Dict_ZH` / `Dict_En` dictionaries in the source. Control widths are measured from the rendered text, so layouts adapt automatically to either language.

## 🔨 Building from Source

Requires .NET Framework 4.x (bundled with Windows) and the C# compiler `csc`:

```bat
csc /target:winexe /out:AutoGameDetect.exe AutoGameDetect.cs
```

Or open the source in Visual Studio and build.

## 🧠 How It Works

- Polls running processes every 3 seconds — one enumeration per tick, compared against a set, rather than one scan per game — matching monitored games by process name (without `.exe`)
- On game start → enumerates the display's available modes, picks the best match for the target resolution/refresh rate, validates it with `CDS_TEST`, then applies it as a temporary mode (nothing is written to the registry)
- On game exit → restores the desktop mode stored in the config, verifying the return value and retrying; a short guard window keeps re-restoring if the desktop gets downgraded
- Game names are parsed from Steam `appmanifest_*.acf` files; process names are auto-matched to the primary executable in the install directory (name similarity first, then largest binary)
- **Steam path lookup order**: manually specified path → default install path → registry `InstallPath` / `SteamPath` (covers non-default installations)
- Polling runs on a WinForms timer, i.e. on the UI thread, so the game list has no cross-thread races; tray menu and balloon notifications are raised on the UI thread too

## 📝 Changelog

### v1.3.0

- Fixed: starting the app while the desktop was already at 2K made 2K the "original" resolution (restore target is now persisted in the config)
- Fixed: applying an enumerated `DEVMODE` without declaring `dmFields` made `ChangeDisplaySettingsEx` fail with `DISP_CHANGE_FAILED`
- Fixed: a failed switch still reported "switched" (failures are now reported as failures)
- Fixed: blurry UI on 4K displays caused by Windows bitmap-stretching a DPI-unaware window
- Fixed: GDI handle leaks (tray icon handle, fonts, form not disposed)
- Fixed: a single corrupted config line used to hide every game after it (now per-line fault tolerant)
- Improved: multi-column ListView, multi-select, independent per-game target resolution
- Improved: built-in autostart toggle (registry Run key, cleans up the legacy `shell:startup` shortcut)
- Improved: single-pass process enumeration; restore/switch now validate, retry, and guard the desktop mode
- Added: `--lang` override, `AGD_DATA_DIR` portable mode, log rotation, global exception logging


## 📄 License

This project is open source under the **MIT License** — free to use, modify, and distribute, provided the copyright notice is preserved.

```
MIT License

Copyright (c) 2025 keitarol

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

Full text in the [LICENSE](LICENSE) file.
