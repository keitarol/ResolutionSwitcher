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
- 🗑️ **Self-cleaning rescan** — re-scanning removes uninstalled games while preserving manually added entries
- 🖱️ **System tray operation** — right-click menu: Settings / Pause Monitoring / Restore 4K Now / Exit
- 🌍 **Automatic localization** — detects the system UI language at startup: `zh-*` shows Chinese, everything else shows English (all UI text lives in a centralized dictionary)
- 🚀 **Auto-start support** — place a shortcut in `shell:startup` to launch on boot
- ⚙️ **Custom target resolution** — defaults to 2560×1440, adjustable in Settings

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
| Settings | Open the game monitoring configuration window |
| Pause Monitoring / Resume | Temporarily disable or re-enable auto-switching |
| Restore 4K Now | Immediately restore the original resolution |
| Exit | Quit the program |

## ⚙️ Configuration

| Item | Location |
|------|----------|
| Monitored games list | `%LOCALAPPDATA%\ResolutionSwitcher\monitored_games.txt` |
| Auto-start | Shortcut in `shell:startup` |

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

- Polls running processes every 3 seconds, matching monitored games by process name (without `.exe`)
- On game start → calls `ChangeDisplaySettingsEx` to switch to the target resolution
- On game exit → restores the original resolution captured at program startup
- Game names are parsed from Steam `appmanifest_*.acf` files; process names are auto-matched to the primary executable in the install directory
- **Steam path lookup order**: manually specified path → default install path → registry `InstallPath` (covers non-default installations)

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
