# MeiamSubtitles 项目文档

## 项目概览

**MeiamSubtitles** 是一套为 **Emby** 和 **Jellyfin** 媒体服务器开发的 C# 字幕插件。它支持从 **迅雷影音** 和 **射手网** 自动下载中文字幕，并利用文件哈希匹配技术确保字幕的精确度。

## 项目结构

解决方案 `MeiamSubtitles.sln` 包含以下核心项目：

*   **Emby 插件:**
    *   `Emby.MeiamSub.Shooter`: 射手网字幕提供程序 (目标框架: `netstandard2.1`)。
    *   `Emby.MeiamSub.Thunder`: 迅雷看看字幕提供程序 (目标框架: `netstandard2.1`)。
*   **Jellyfin 插件:**
    *   `Jellyfin.MeiamSub.Shooter`: 射手网字幕提供程序 (目标框架: `net9.0`)。
    *   `Jellyfin.MeiamSub.Thunder`: 迅雷看看字幕提供程序 (目标框架: `net9.0`)。
    *   *注: Jellyfin 插件遵循现代 .NET 架构，通过 `PluginServiceRegistrator` 使用依赖注入来管理服务 (如 `IHttpClientFactory`)。*
*   **开发工具:**
    *   `Emby.MeiamSub.DevTool`: 控制台应用程序，用于开发调试，特别是验证各平台字幕匹配所需的哈希计算逻辑。

## 编译与开发

### 环境要求
*   .NET 9 SDK (用于编译 Jellyfin 插件)
*   .NET Framework / .NET Core (支持 .NET Standard 2.1)

### 编译命令
在根目录下执行以下命令编译整个解决方案：
```bash
dotnet build MeiamSubtitles.sln
```

### 编译产出
项目配置了 `PostBuild` 事件，编译后的 DLL 文件会自动复制到：
1.  各项目目录下的 `..\Release`。
2.  解决方案根目录下的 `Release` 或 `Debug` 文件夹。

### 开发调试工具
可以使用 `Emby.MeiamSub.DevTool` 在本地测试哈希算法：
```bash
cd Emby.MeiamSub.DevTool
dotnet run
```
*注：测试时需在 `Program.cs` 中修改对应的视频文件路径。*

## 安装指南

### 手动安装 (Emby/通用)
1.  编译项目或下载已发布的版本。
2.  将生成的 `.dll` 文件复制到服务器的插件目录：
    *   **Windows:** `Emby-Server\Programdata\Plugins\` 或 `Emby-Server\System\Plugins\`
    *   **Linux:** `/opt/emby-server/system/plugins` 或 `/var/lib/emby/plugins`
    *   **群晖 (Synology):** `/var/packages/EmbyServer/var/plugins`
3.  重启 Emby 服务。

### Jellyfin 安装
Jellyfin 支持通过插件存储库安装：
*   **存储库 URL:** `https://github.com/91270/MeiamSubtitles.Release/raw/main/Plugin/manifest-stable.json`
*   在 **控制台 -> 插件 -> 存储库** 中添加此链接。

## 核心技术
*   **C# / .NET:** 核心开发语言 (Jellyfin: .NET 9, Emby: .NET Standard 2.1)。
*   **依赖注入 (Jellyfin)**: 使用 `IHttpClientFactory` 管理 HTTP 请求，符合 Jellyfin 现代插件架构。
*   **异步编程**: 核心 I/O 操作 (文件哈希) 采用异步模式 (`ReadExactlyAsync`)，防止阻塞服务器线程。
*   **依赖库版本**:
    *   **Emby**: `MediaBrowser.Server.Core` (v4.9.1.90)
    *   **Jellyfin**: `Jellyfin.Controller` (v10.11.5)
*   **哈希算法:** 实现了射手网和迅雷特定的 MD5/SHA1 哈希计算逻辑，用于视频内容的精确匹配。
