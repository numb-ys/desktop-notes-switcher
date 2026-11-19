# 桌面便笺 · Desktop Notes Switcher

一个 Windows 虚拟桌面悬浮条。它显示系统当前拥有的桌面编号，并在切换桌面时保留在屏幕的同一位置。

## 安装与使用

从 [Releases](../../releases/latest) 下载名称以 `Setup-win-x64.exe` 结尾的安装包，双击打开安装向导。安装包自带运行时，安装完成后可从开始菜单打开「桌面便笺」。如果便携版已经运行，请先从托盘菜单退出。

系统已有几个虚拟桌面就会显示几个编号，当前桌面会高亮。按 `Win + Ctrl + D` 可新建桌面。

| 操作 | 功能 |
| --- | --- |
| 右键点编号 | 直接切换到对应虚拟桌面 |
| 左键点编号 | 展开该桌面的便笺，再点一次收起 |
| 拖动小便笺图标 | 自由移动悬浮条。松开时若靠近屏幕工作区边缘，会自动吸附 |
| 右键点小便笺图标或托盘图标 | 打开设置、控制自动吸附、定位到主屏右侧或退出 |

在「设置」中可以选择登录 Windows 后自动启动。此选项默认关闭，变更后即时保存。皮肤设置当前提供「默认便笺」。悬浮条和便笺共用所选皮肤。

便笺支持多行纯文本与自动保存，可用 `Ctrl + S` 立即保存，用 `Esc` 收起。每个桌面的便笺单独保存在 `%LOCALAPPDATA%\DesktopNotesSwitcher\notes`，应用不联网，也不读取 Windows 便笺。便笺跟随桌面的内部 ID，因此桌面改名或重排后仍能找到原内容，删除桌面也会保留其文本文件。

可以通过 Windows 的「设置 → 应用 → 已安装的应用」卸载。卸载会移除安装文件、快捷方式和该安装副本的启动项，保留本地便笺与偏好。

## 系统与构建

软件面向 **Windows 11 24H2（内部版本 26100）及更新版本的 x64 系统**，并在 Windows 11 25H2 Insider Preview（26220.9568）上实测。直接跳转和窗口跨桌面固定使用 Windows Shell 的非公开接口，后续系统更新可能需要调整兼容代码。

从源码运行需安装 .NET 10 SDK。

```powershell
dotnet run
```

生成安装包需要 PowerShell 7 和 [Inno Setup 7](https://jrsoftware.org/inno-setup/downloads)。构建脚本会把安装包写入 `artifacts`。

```powershell
pwsh -File installer/Build-Installer.ps1
```

编译器安装在其他位置时，可通过 `-Iscc` 指定 `ISCC.exe` 的路径。项目采用 [MIT 许可证](LICENSE)，虚拟桌面 COM 声明参考了 [Markus Scholtes 的 VirtualDesktop](https://github.com/MScholtes/VirtualDesktop)。
