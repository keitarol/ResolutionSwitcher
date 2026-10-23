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
- 📋 **多列游戏列表** — 游戏名 / 进程名 / 目标分辨率 / 启用状态一目了然，中英混排不会错位
- ⚙️ **每个游戏独立目标分辨率** — 选中条目后点「应用到选中」，不同游戏可分别设 2K、1080p 等
- 🗑️ **扫描同步清理** — 重新扫描时自动移除已卸载的游戏，手动添加的条目保留
- 🧠 **恢复目标持久化** — 桌面分辨率记在配置里，重启或异常退出后依然能恢复到正确的分辨率，不会误把正在用的 2K 当成"原始分辨率"
- 🖱️ **系统托盘运行** — 右键菜单：设置 / 当前分辨率 / 暂停监控 / 立即恢复 / 设为恢复目标 / 退出
- 🌍 **自动国际化** — 启动时读取系统语言：`zh-*` 显示中文，其余全部显示英文（所有界面文案集中在字典中），并可用 `--lang` 强制指定
- 🚀 **内置开机自启开关** — 设置窗口里勾选即可，无需手动放快捷方式
- 🖥️ **4K 屏清晰渲染** — 声明 DPI 感知，界面不会被系统拉伸模糊
- 🩺 **诊断日志** — 切换/恢复全过程记录到 `resolution.log`，出问题一眼定位


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

## ⚙️ 配置存储

| 项目 | 位置 |
|------|------|
| 监控游戏列表 + 恢复目标 | `%LOCALAPPDATA%\ResolutionSwitcher\monitored_games.txt`（若被安全软件拒绝写入，自动降级到 exe 同级 `data\` 目录，并把旧配置迁移过去） |
| 诊断日志 | `%LOCALAPPDATA%\ResolutionSwitcher\resolution.log`（超过 512 KB 自动轮转为 `.old`） |
| 开机自启 | 注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`（可在设置里开关） |

配置采用原子写入（先写临时文件再替换），断电或崩溃不会写坏；读取时逐行容错，某一行损坏不影响其它条目。

## 🧰 命令行参数

| 参数 / 环境变量 | 作用 |
|------|------|
| `--lang zh` / `--lang en` | 强制界面语言（中文系统也能预览英文界面） |
| `AGD_LANG=en` | 同上，环境变量形式 |
| `AGD_DATA_DIR=D:\MyTools\RS` | 便携模式：配置与日志改存到指定目录，不写用户目录 |

```bat
:: 示例：强制英文界面 + 便携模式
set AGD_LANG=en
set AGD_DATA_DIR=%~dp0data
AutoGameDetect.exe
```

## ⚠️ 已知限制

- 仅切换**主显示器**的分辨率（多屏用户若在主屏之外的游戏窗口里玩，效果不符预期）
- 游戏需要处于**独占全屏**模式；无边框窗口模式下桌面分辨率变化会被游戏忽略
- Windows 会话被锁定或处于远程桌面时，系统会拒绝修改显示模式（此时日志里会记录 `DISP_CHANGE_FAILED`）


## 🔨 构建

需要 .NET Framework 4.x（Windows 自带）和 C# 编译器 `csc`：

```bat
csc /target:winexe /out:AutoGameDetect.exe AutoGameDetect.cs
```

或直接用 Visual Studio 打开源码编译。

## 🧠 工作原理

- 每 3 秒轮询系统进程列表（一次性枚举后做集合比对，不是每个游戏各扫一遍），匹配勾选游戏的进程名（不含 `.exe`）
- 检测到游戏进程启动 → 枚举显示器可用模式，挑出最接近目标分辨率/刷新率的模式，用 `CDS_TEST` 预检后应用（临时模式，不写注册表）
- 游戏进程退出 → 恢复到配置里记录的桌面分辨率；恢复会校验返回值并重试，退出后 20 秒内还会做一次守护
- 游戏名解析自 Steam `appmanifest_*.acf`，进程名自动匹配安装目录下的主 exe（优先名字相近的，其次体积最大的）
- **Steam 路径查找顺序**：设置里手动指定的路径 → 默认安装路径 → 注册表 `InstallPath` / `SteamPath`（非默认安装位置也能找到）
- 轮询跑在 WinForms 定时器上（UI 线程），所以列表读写不存在竞态；托盘菜单与气球通知也在 UI 线程发出

## 📝 更新日志

### v1.3.1

- 新增：**再次运行 exe 即可唤起设置窗口**——托盘图标被 Windows 折叠进"隐藏的图标"里时，双击程序图标一次就能打开界面（不会再弹"已在运行"）
- 新增：**配置目录自动降级**——`%LOCALAPPDATA%` 被安全软件（如腾讯电脑管家）拦截写入时，自动改用 exe 同级 `data\` 目录，并提示实际使用的位置；旧配置会自动迁移过来
- 修复：写盘失败时完全静默，导致"设置怎么都存不下来"却毫无提示

> 如果想让配置回到 `%LOCALAPPDATA%`，把 `AutoGameDetect.exe` 加入安全软件的信任区/白名单即可。

### v1.3.0

- 修复：启动时若桌面正处于 2K，会把 2K 误当成"原始分辨率"（现在恢复目标持久化到配置）
- 修复：枚举失败时用 `dmFields` 未声明的模式调用 `ChangeDisplaySettingsEx` 导致切换失败（`DISP_CHANGE_FAILED`）
- 修复：切换失败时仍提示"已切换"（现在会如实提示失败原因）
- 修复：4K 屏下界面被系统位图拉伸发虚（声明 DPI 感知）
- 修复：托盘图标句柄/字体/窗体未释放导致的 GDI 句柄泄漏
- 修复：配置某一行损坏会导致其后所有游戏都读不出来（改为逐行容错）
- 改进：游戏列表改为多列 ListView，支持多选、每个游戏独立目标分辨率
- 改进：内置开机自启开关（注册表 Run 键，自动清理旧版 `shell:startup` 快捷方式）
- 改进：进程检测一次性枚举，退出/恢复带校验、重试与显示守护
- 新增：`--lang` 语言强制、`AGD_DATA_DIR` 便携模式、日志轮转、全局异常落盘


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
