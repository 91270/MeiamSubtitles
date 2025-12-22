# MeiamSubtitles
Emby & Jellyfin 中文字幕插件，支持 **迅雷影音**、**射手网** 字幕自动下载与精准 Hash 匹配。

[![.NET Status](https://img.shields.io/badge/.NET-Standard%202.1%20%7C%209.0-blueviolet.svg)](#)
[![Platform](https://img.shields.io/badge/Platform-Linux%20%7C%20Win%20%7C%20OSX-brightgreen.svg)](#)
[![LICENSE](https://img.shields.io/badge/license-Apache%202-blue)](#)
[![Star](https://img.shields.io/github/stars/91270/Emby.MeiamSub?label=Star%20this%20repo)](https://github.com/91270/Emby.MeiamSub)
[![Fork](https://img.shields.io/github/forks/91270/Emby.MeiamSub?label=Fork%20this%20repo)](https://github.com/91270/Emby.MeiamSub/fork)
[![博客](https://img.shields.io/badge/博客-Meiam's%20Home-brightgreen.svg)](https://www.592.la/)

&nbsp;

## 给个星星! ⭐️

如果你喜欢这个项目或者它帮助你, 请给 Star~（辛苦咯）

如果你能赞助稳定 Google Drive 团队盘用于媒体库插件测试, 请于我联系 91270#QQ.COM 

&nbsp;

## 广告时间 📣

搬瓦工 $99 年付, 建站神器重出江湖，THE PLAN V1 传家宝套餐，18机房随意切换  

循环优惠码：BWHCCNCXVV（6.77%）

[直达通道(传家宝套餐)](https://bwh88.net/aff.php?aff=117&pid=87)

&nbsp;

## 功能介绍

- [x] **迅雷影音**: 支持通过文件 Hash (CID) 精准匹配字幕。
- [x] **射手网**: 支持通过文件 Hash 精准匹配字幕。
- [x] **高性能**: 核心哈希计算采用异步 I/O (Async/Await) 模式，避免阻塞服务器线程。
- [x] **稳定性**: 内置重试机制与异常处理，Jellyfin 版本采用现代化的依赖注入架构。

## 项目说明

| # | 模块功能 | 项目名称 | 说明 |
|---|---|---|---|
| 1 | Emby 插件 | `Emby.MeiamSub.Thunder` | 迅雷看看字幕插件 (.NET Standard 2.1) |
| 2 | Emby 插件 | `Emby.MeiamSub.Shooter` | 射手影音字幕插件 (.NET Standard 2.1) |
| 3 | Jellyfin 插件 | `Jellyfin.MeiamSub.Thunder` | 迅雷看看字幕插件 (.NET 9.0) |
| 4 | Jellyfin 插件 | `Jellyfin.MeiamSub.Shooter` | 射手影音字幕插件 (.NET 9.0) |
| 5 | 开发工具 | `Emby.MeiamSub.DevTool` | 哈希算法测试与调试工具 |

## 使用插件

首先下载已编译好的插件 [Release 下载](https://github.com/91270/Emby.MeiamSub/releases)。

**注意**：建议在媒体库设置中**不勾选**本插件作为默认下载器，仅在手动“编辑字幕”或“搜索字幕”时使用，以获得最佳体验。

### Jellyfin 安装 (推荐)

Jellyfin 用户可以通过添加插件存储库实现一键安装和自动更新：

1. 打开 Jellyfin 控制台 -> **插件** -> **存储库**。
2. 点击添加，输入名称 (如 MeiamSub) 和以下 URL：
   ```
   https://github.com/91270/MeiamSubtitles.Release/raw/main/Plugin/manifest-stable.json
   ```
3. 保存后在插件目录中找到 **MeiamSub.Thunder** 和 **MeiamSub.Shooter** 进行安装。
4. 重启 Jellyfin 服务。

### 手动安装 (Emby / 通用)

将下载的 `.dll` 文件复制到服务器的插件目录，然后重启服务。

#### Windows
```bash
# 路径可能因安装方式不同而异
Emby-Server\Programdata\Plugins\
# 或
Emby-Server\System\Plugins\
```

#### Linux / Docker
```bash
# 常见路径
/opt/emby-server/system/plugins
# 或
/var/lib/emby/plugins
```

#### 群晖 (Synology)

```bash
/var/packages/EmbyServer/var/plugins
# 或
/var/packages/EmbyServer/target/system/plugins
```

#### 威联通 (QNAP)

```bash
# 其中`CACHEDEV{num}_DATA`的名称取决于你的qpkg安装位置
/share/CACHEDEV1_DATA/.qpkg/EmbyServer/programdata/plugins
/share/CACHEDEV1_DATA/.qpkg/EmbyServer/system/plugins
```

&nbsp;

## 贡献

欢迎提交 Issue 反馈问题，或提交 Pull Request 贡献代码。

*   **开发分支**: `master`
*   **代码风格**: 请遵循现有的 C# 代码风格，异步方法请使用 `Async` 后缀。

## 致谢

[Emby.Subtitle.Subscene](https://github.com/nRafinia/Emby.Subtitle.Subscene)