# 桌面便笺 · Desktop Notes Switcher

一个 Windows 虚拟桌面悬浮条。它显示系统当前拥有的桌面编号，并在切换桌面时保留在屏幕的同一位置。

## 使用

从 [Releases](../../releases/latest) 下载 Windows x64 便携版，解压后运行 `DesktopNotesSwitcher.exe`。系统已有几个虚拟桌面就会显示几个编号，当前桌面会高亮。按 `Win + Ctrl + D` 可新建桌面。

| 操作 | 功能 |
| --- | --- |
| 右键点编号 | 直接切换到对应虚拟桌面 |
| 左键点编号 | 展开该桌面的便笺，再点一次收起 |
| 拖动小便笺图标 | 自由移动悬浮条。松开时若靠近屏幕工作区边缘，会自动吸附 |
| 右键点小便笺图标或托盘图标 | 控制自动吸附、定位到主屏右侧或退出 |

便笺支持多行纯文本与自动保存，可用 `Ctrl + S` 立即保存，用 `Esc` 收起。每个桌面的便笺单独保存在 `%LOCALAPPDATA%\DesktopNotesSwitcher\notes`，应用不联网，也不读取 Windows 便笺。便笺跟随桌面的内部 ID，因此桌面改名或重排后仍能找到原内容，删除桌面也会保留其文本文件。

## 系统与构建

首版面向 **Windows 11 24H2（内部版本 26100）及更新版本的 x64 系统**，并在 Windows 11 25H2 Insider Preview（26220.9568）上实测。直接跳转和窗口跨桌面固定使用 Windows Shell 的非公开接口，后续系统更新可能需要调整兼容代码。

从源码运行需安装 .NET 10 SDK。

```powershell
dotnet run
```

生成包含运行时的便携文件夹。

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o artifacts/publish
```

发行包需保留同目录下的运行时文件。项目采用 [MIT 许可证](LICENSE)，虚拟桌面 COM 声明参考了 [Markus Scholtes 的 VirtualDesktop](https://github.com/MScholtes/VirtualDesktop)。
