# ResolutionSwitcher — 4K 显示器自动分辨率切换工具

Windows 托盘工具：玩 3A 游戏时自动把分辨率从 4K 切换到 2K (2560×1440)，游戏退出后自动恢复 4K。适合"4K 日常使用，特定游戏想用 2K 换取更高帧率"的场景。

## ✨ 特性

- 🎮 **自动扫描 Steam 游戏库** — 读取 `libraryfolders.vdf` 与 `appmanifest_*.acf`，支持多 Steam 库路径，自动识别已安装游戏
- ✅ **按需监控** — 勾选想用 2K 玩的游戏（如 GTA V、古墓丽影），其余游戏（如 LOL）完全不受影响，保持 4K 满画质
- 🗑️ **扫描同步清理** — 重新扫描时自动移除已卸载的游戏，手动添加的条目保留
- 🖱️ **系统托盘运行** — 右键菜单：设置监控游戏 / 暂停监控 / 立即恢复 4K / 退出
- 🚀 **开机自启** — 将快捷方式放入 `shell:startup` 即可
- ⚙️ **自定义目标分辨率** — 默认 2560×1440，可在设置窗口修改

## 🚀 使用方法

1. 运行 `AutoGameDetect.exe`
2. 首次运行自动弹出设置窗口，点「🔄 扫描已安装游戏」
3. 勾选要监控的 3A 游戏（比如 GTA V、古墓丽影）
4. 关闭窗口，程序驻留系统托盘
5. 之后打开勾选过的游戏 → 自动切 2K；游戏退出 → 自动恢复 4K

> 💡 LOL 这类不吃配置的游戏**不要勾选**，正常启动即可享受 4K 满画质。

## ⚙️ 配置存储

| 项目 | 位置 |
|------|------|
| 监控游戏列表 | `%LOCALAPPDATA%\ResolutionSwitcher\monitored_games.txt` |
| 开机自启 | `shell:startup` 快捷方式 |

## 🔨 构建

需要 .NET Framework 4.x（Windows 自带）和 C# 编译器 `csc`：

```bat
csc /target:winexe /out:AutoGameDetect.exe AutoGameDetect.cs
```

或直接用 Visual Studio 打开源码编译。

## 🧠 工作原理

- 每 3 秒轮询系统进程列表，匹配勾选游戏的进程名（不含 `.exe`）
- 检测到游戏进程启动 → 调用 `ChangeDisplaySettingsEx` 切换到目标分辨率
- 游戏进程退出 → 恢复程序启动时记录的原始分辨率（4K）
- 游戏名解析自 Steam `appmanifest_*.acf`，进程名自动匹配安装目录下的主 exe

## 📄 License

[MIT](LICENSE)
