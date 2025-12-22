# 🎬 MeiamSubtitles

**MeiamSubtitles** 是一款专为 **Emby** 和 **Jellyfin** 媒体服务器打造的中文字幕下载插件。它集成了 **迅雷影音** 与 **射手网** 的强大搜索能力，支持精准的视频哈希（Hash）匹配，让您的媒体库自动补全高质量字幕。

---

<p align="left">
  <img src="https://img.shields.io/badge/.NET-Standard%202.1%20%7C%209.0-blueviolet.svg" alt=".NET Status">
  <img src="https://img.shields.io/badge/Platform-Linux%20%7C%20Win%20%7C%20OSX-brightgreen.svg" alt="Platform">
  <img src="https://img.shields.io/badge/license-Apache%202-blue" alt="LICENSE">
  <a href="https://github.com/91270/Emby.MeiamSub"><img src="https://img.shields.io/github/stars/91270/Emby.MeiamSub?label=Star%20this%20repo" alt="Star"></a>
  <a href="https://www.592.la/"><img src="https://img.shields.io/badge/博客-Meiam's%20Home-brightgreen.svg" alt="博客"></a>
</p>

## ✨ 核心特性

- **🚀 精准匹配**: 支持迅雷看看 (CID) 和射手网 (Hash) 双重校验逻辑，确保字幕与视频内容完美同步。
- **⚡ 极致性能**: 核心采样算法全面采用**异步 I/O (Async/Await)** 模式，在大规模媒体库扫描时不会阻塞服务器线程。
- **🌐 广泛兼容**: 深度适配 **Jellyfin 10.11+** 及 **Emby v4.9+**，支持 `zho`、`chi` 等多种国际化语言代码映射。
- **🛡️ 稳定可靠**: 针对射手网 API 的老化问题增加了防御性校验，能有效处理乱码返回，保证系统长效稳定。
- **📝 详尽日志**: 记录哈希计算耗时与接口原始响应，让问题排查不再是黑盒。

## 📦 项目组件说明

| 组件名称 | 适用平台 | 目标框架 | 说明 |
| :--- | :--- | :--- | :--- |
| **Emby.MeiamSub.Thunder** | Emby | .NET Standard 2.1 | 迅雷看看字幕插件 |
| **Emby.MeiamSub.Shooter** | Emby | .NET Standard 2.1 | 射手影音字幕插件 |
| **Jellyfin.MeiamSub.Thunder** | Jellyfin | .NET 9.0 | 迅雷看看字幕插件 (现代 DI 架构) |
| **Jellyfin.MeiamSub.Shooter** | Jellyfin | .NET 9.0 | 射手影音字幕插件 (现代 DI 架构) |
| **Emby.MeiamSub.DevTool** | 开发调试 | .NET 8.0 | 哈希算法测试与 API 模拟工具 |

---

## 🚀 快速安装

### 第一步：获取插件
前往 [GitHub Releases](https://github.com/91270/Emby.MeiamSub/releases) 下载最新版本的发布包。

> **🔔 推荐建议**：在媒体库设置中**不勾选**本插件作为默认自动下载器。建议仅在手动“搜索字幕”时使用，以获得更精准的人工筛选体验。

### 第二步：部署插件

#### 🔹 方式 A：Jellyfin 存储库安装 (强烈推荐)
Jellyfin 用户可直接添加官方存储库，实现一键安装与自动更新：
1. 控制台 -> **插件** -> **存储库** -> 点击“添加”。
2. 输入名称 `MeiamSub` 和 URL：  
   `https://github.com/91270/MeiamSubtitles.Release/raw/main/Plugin/manifest-stable.json`
3. 在“目录”中找到插件并安装，重启服务即可。

#### 🔹 方式 B：手动安装 (Emby/通用)
将下载的 `.dll` 文件（Jellyfin 用户请下载 `.zip` 并解压完整目录）放入服务器的 `plugins` 文件夹：

- **Windows**: `AppData\Local\jellyfin\plugins` 或 `Emby-Server\programdata\plugins`
- **Linux/Docker**: `/config/plugins` 或 `/var/lib/emby/plugins`
- **群晖/威联通**: 对应套件安装目录下的 `plugins` 文件夹

---

## ❓ 常见问题排查 (FAQ)

<details>
<summary><b>1. 为什么在 Jellyfin 10.11+ 中搜不到字幕？</b></summary>
新版 Jellyfin 采用了三位字母的语言代码（如 <code>zho</code>）。请确保您已升级至本插件的 <b>v1.0.13.0</b> 或更高版本，该版本已完美解决语言映射兼容性。
</details>

<details>
<summary><b>2. 为什么射手网有时候返回结果为空？</b></summary>
由于射手网 API 维护状态不佳，对于部分冷门资源或 Hash 不匹配的文件，API 可能会返回非法数据。插件目前已增加防御逻辑，会自动忽略这些无效返回以保护服务器稳定。
</details>

<details>
<summary><b>3. 安装本插件后会影响 Open Subtitles 吗？</b></summary>
不会。本插件已将优先级 (Order) 调整为 100（低优先级），并在代码层面优化了并发逻辑，确保官方插件能优先获取请求机会。
</details>

<details>
<summary><b>4. 如何提供有效的错误反馈？</b></summary>
如果确定有字幕但搜不到，请在 Issue 中提供日志里的 <code>Target</code> 文件名、计算出的 <code>FileHash</code> 以及 <code>ResponseBody</code> 内容。
</details>

---

## 🤝 贡献与感谢

欢迎通过提交 Issue 或 Pull Request 来完善本项目。

- **开发守则**: 遵循异步命名规范，所有修改请标注 `修改人: Meiam`。
- **致谢**: 感谢 [Emby.Subtitle.Subscene](https://github.com/nRafinia/Emby.Subtitle.Subscene) 提供的灵感与参考。

---

## ⭐️ 给个星星

如果你喜欢这个项目，请给一个 **Star**！这对我非常重要。

如果你有稳定的 Google Drive 团队盘资源可供媒体库插件测试，欢迎联系：`91270#QQ.COM`

---
*Powered by Meiam*
