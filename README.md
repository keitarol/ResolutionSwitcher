# ResolutionSwitcher — 4K 显示器自动分辨率切换工具

**中文** | [English](README_EN.md)

Windows 托盘工具：玩 3A 游戏时自动把分辨率从 4K 切换到 2K (2560×1440)，游戏退出后自动恢复 4K。

## 💡 为什么做这个工具

显示器买了 4K 的，但电脑性能只能带动 2K —— 日常用 4K 观感细腻，打 3A 大作却卡成幻灯片。手动切分辨率又麻烦（每次开玩前都要去系统设置里改一遍，打完还要改回来）。这个工具就是为了解决这个痛点：**日常 4K，游戏自动 2K，退出自动恢复，全程无感**。

适合场景：
- 🖥️ 4K 显示器 + 性能只能带动 2K 的电脑
- 🎮 3A 大作（GTA V、古墓丽影等）开 2K 换取流畅帧率
- ⚡ LOL 这类不吃配置的游戏保持 4K 满画质，不受影响

## ✨ 特性

- 🎮 **自动扫描 Steam 游戏库** — 读取 `libraryfolders.vdf` 与 `appmanifest_*.acf`，支持多 Steam 库路径，自动识别已安装游戏
- 📍 **非默认路径也能找到** — Steam 装在任意盘（如 D 盘）都能自动检测（注册表）；绿色版/便携版 Steam 可在设置里手动指定安装目录
- ✅ **按需监控** — 勾选想用 2K 玩的游戏（如 GTA V、古墓丽影），其余游戏（如 LOL）完全不受影响，保持 4K 满画质
- 🗑️ **扫描同步清理** — 重新扫描时自动移除已卸载的游戏，手动添加的条目保留
- 🖱️ **系统托盘运行** — 右键菜单：设置监控游戏 / 暂停监控 / 立即恢复 4K / 退出
- 🌍 **自动国际化** — 启动时读取系统语言：`zh-*` 显示中文，其余全部显示英文（所有界面文案集中在字典中）
- 🚀 **开机自启** — 将快捷方式放入 `shell:startup` 即可
- ⚙️ **自定义目标分辨率** — 默认 2560×1440，可在设置窗口修改

## 🚀 使用方法

### 📥 下载

1. 从 [Releases 页面](https://github.com/keitarol/ResolutionSwitcher/releases/latest) 下载最新版本
2. 下载 `AutoGameDetect.exe`（免安装，双击即用）

### 🎮 使用

1. 运行 `AutoGameDetect.exe`
2. 首次运行自动弹出设置窗口，点「🔄 扫描已安装游戏」
3. 勾选要监控的 3A 游戏（比如 GTA V、古墓丽影）
4. 关闭窗口，程序驻留系统托盘
5. 之后打开勾选过的游戏 → 自动切 2K；游戏退出 → 自动恢复 4K

> 💡 LOL 这类不吃配置的游戏**不要勾选**，正常启动即可享受 4K 满画质。

### 📦 安装建议

- **推荐安装到 `C:\Program Files\ResolutionSwitcher\`**（或其他受信任的程序目录），然后为 exe 创建快捷方式。
- 标准 Windows 10/11 从任意路径运行都没有问题；但**部分 OEM 定制系统（品牌笔记本预装镜像）和安全软件会强制「仅受信任目录的程序可注册托盘图标」**。若托盘图标不出现，先尝试把程序放到 `Program Files` 下运行。
- 开机自启：将快捷方式放入 `shell:startup`（建议目标加 `--autostart` 参数，静默启动不弹窗）。

## 🩺 常见问题

| 现象 | 原因与解决 |
|------|-----------|
| **SmartScreen 提示「无法识别的应用」** | 程序未做商业代码签名。点「更多信息 → 仍要运行」即可；每次发布新版本需重新允许一次。彻底消除需购买代码签名证书（OV/EV，约 $200-400/年）或使用 Azure Trusted Signing |
| **托盘区看不到图标** | ① Windows 11 默认把新图标藏在任务栏 `^` 折叠区，拖出来即可；② 部分 OEM/安全软件环境要求程序位于受信任目录（见下方安装建议）；③ 若装了安全软件（如腾讯电脑管家），把本程序加入其信任区/白名单 |
| **只有装在 C 盘（Program Files）才有托盘图标** | 部分 OEM 定制系统（品牌笔记本预装镜像）对**未签名程序**有路径限制：实测同一程序放到 `C:\Program Files\` 可正常注册托盘图标，放在 D 盘等非系统盘路径会被静默拒绝；有正规数字签名的程序不受此限制。解决：① 安装到 `C:\Program Files\ResolutionSwitcher\`（推荐）；② Windows 安全中心 → 应用和浏览器控制 → 基于声誉的保护设置 → 关闭「检查应用和文件」；③ 购买代码签名证书（OV/EV）从根本上消除限制 |
| **Explorer 重启后图标消失** | v1.2.4+ 已内置 TaskbarCreated 自愈，图标会自动重挂；旧版本重启程序即可 |
| **游戏退出后分辨率没恢复** | 检查 `%LOCALAPPDATA%\ResolutionSwitcher\resolution.log`，把日志反馈到 GitHub Issue |

## ⚙️ 配置存储

| 项目 | 位置 |
|------|------|
| 监控游戏列表 | `%LOCALAPPDATA%\ResolutionSwitcher\monitored_games.txt` |
| 开机自启 | `shell:startup` 快捷方式 |

## 🔨 构建

需要 .NET Framework 4.x（Windows 自带）和 C# 编译器 `csc`：

```bat
csc /target:winexe /win32icon:app.ico /out:AutoGameDetect.exe AutoGameDetect.cs
```

或直接用 Visual Studio 打开源码编译。

## 🧠 工作原理

- 每 3 秒轮询系统进程列表，匹配勾选游戏的进程名（不含 `.exe`）
- 检测到游戏进程启动 → 调用 `ChangeDisplaySettingsEx` 切换到目标分辨率
- 游戏进程退出 → 恢复程序启动时记录的原始分辨率（4K）
- 游戏名解析自 Steam `appmanifest_*.acf`，进程名自动匹配安装目录下的主 exe
- **Steam 路径查找顺序**：设置里手动指定的路径 → 默认安装路径 → 注册表 `InstallPath`（非默认安装位置也能找到）

## 📄 License

本项目基于 **MIT 协议**开源，可自由使用、修改、分发，但需保留版权声明。

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

完整文本见 [LICENSE](LICENSE) 文件。
