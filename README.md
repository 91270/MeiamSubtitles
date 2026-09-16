# MeiamSubtitles

**MeiamSubtitles** 是一款专为 **Emby** 和 **Jellyfin** 媒体服务器打造的中文字幕下载插件。它集成了 **[Assrt（伪射手网）](https://assrt.net/)** 与 **迅雷影音** 的字幕搜索能力，支持影片名称与视频特征（CID）匹配，帮助您的媒体库便捷地搜索和补全中文字幕。

<p align="left">
  <a href="https://github.com/91270/MeiamSubtitles/releases/latest"><img src="https://img.shields.io/github/v/release/91270/MeiamSubtitles" alt="Release"></a>
  <img src="https://img.shields.io/badge/.NET-Standard%202.1%20%7C%208.0-blueviolet.svg" alt=".NET Status">
  <a href="#插件与兼容性"><img src="https://img.shields.io/badge/Platform-Linux%20%7C%20Windows%20%7C%20macOS-brightgreen.svg" alt="Platform"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-Apache%202.0-blue.svg" alt="License"></a>
  <a href="https://www.592.la/"><img src="https://img.shields.io/badge/博客-Meiam's%20Home-brightgreen.svg" alt="博客"></a>
</p>

当前维护两个字幕源：

- **[Assrt（伪射手网）](https://assrt.net/)**：按影片名称搜索，支持下载并解析 ZIP、RAR 等压缩字幕；需要配置 Assrt API Token。
- **Thunder**：根据视频文件计算 CID，优先匹配对应文件的字幕；无需额外配置。

> Shooter 的旧版接口已很少返回有效字幕，自 v1.0.16.0 起停止编译、维护和发布。

## 📣 广告时间

> **搬瓦工 $99 年付**：建站神器重出江湖，THE PLAN V1 传家宝套餐，18 机房随意切换。
>
> **配置**：2 核 / 2G / 40G SSD / 1TB 流量 / 2.5Gbps，支持 CN2 GIA 等大陆优化线路。
>
> **当前推荐循环优惠码**：`BWHNCXNVXV`（最高约 6.81%，实际优惠以结账页验证结果为准）
>
> **[直达通道（传家宝套餐）](https://bwh88.net/aff.php?aff=117&pid=87)**

## 功能

- 同时支持 Emby 和 Jellyfin。
- 支持 `zho`、`chi`、`zh-CN` 等中文语言代码。
- 使用异步文件读取计算视频特征，适用于本地及网络媒体库。
- 对搜索结果进行格式校验、去重和排序。
- 下载前校验响应内容，避免将空响应或错误页面保存为字幕。
- Assrt 支持从压缩包中选择并提取 `.srt`、`.ass`、`.ssa` 字幕。
- 日志隐藏 Assrt Token，且不记录完整接口响应。

## 插件与兼容性

| 插件 | 平台 | 目标框架 | 字幕源 |
| --- | --- | --- | --- |
| `Emby.MeiamSub.Assrt` | Emby（版本限制见下文） | .NET Standard 2.1 | [Assrt.net](https://assrt.net/) |
| `Emby.MeiamSub.Thunder` | Emby（版本限制见下文） | .NET Standard 2.1 | Thunder XMP |
| `Jellyfin.MeiamSub.Assrt` | Jellyfin（版本限制见下文） | .NET 8.0 | [Assrt.net](https://assrt.net/) |
| `Jellyfin.MeiamSub.Thunder` | Jellyfin（版本限制见下文） | .NET 8.0 | Thunder XMP |

| 平台 | 兼容版本 |
| --- | --- |
| Emby | 4.8.10.0 至 4.10.0.40 |
| Jellyfin | 10.10.7 至 12.1 |

## 安装

### Jellyfin 存储库

推荐通过插件存储库安装，以便接收后续更新。

1. 打开 Jellyfin 控制台的“插件 → 存储库”。
2. 添加存储库名称 `MeiamSub`。
3. 填写以下地址：

   ```text
   https://github.com/91270/MeiamSubtitles.Release/raw/main/Plugin/manifest-stable.json
   ```

4. 在“目录”中分别安装 `MeiamSub.Assrt` 或 `MeiamSub.Thunder`。
5. 重启 Jellyfin。

### 手动安装

从 [Releases](https://github.com/91270/MeiamSubtitles/releases/latest) 下载与平台对应的压缩包。

#### Emby

1. 解压 `Emby_v*.zip`。
2. 将需要的插件 DLL 放入 Emby 的 `plugins` 目录。
3. 不要额外复制 `SharpCompress.dll`；Assrt 复用 Emby 自带版本。
4. 重启 Emby。

#### Jellyfin

1. 解压 `Jellyfin_v*.zip`。
2. 将需要的整个插件目录复制到 Jellyfin 的 `plugins` 目录。
3. Assrt 目录中的 `SharpCompress.dll` 是 Jellyfin 所需依赖，请勿单独删除。
4. 重启 Jellyfin。

### 从旧版本升级

- 升级到 v1.0.16.0 或更高版本后，删除旧的 Shooter DLL 和插件目录；Shooter 已停止维护和发布。
- 同一个插件不要同时保留多个版本，否则服务器可能重复加载或启动失败。
- Emby Assrt 只需保留插件 DLL，不要额外复制 `SharpCompress.dll`。
- Jellyfin Assrt 必须保留发布包中同目录的 `SharpCompress.dll`。

常见插件目录：

| 环境 | 目录示例 |
| --- | --- |
| Jellyfin Windows | `%LOCALAPPDATA%\jellyfin\plugins` |
| Jellyfin Linux | `/var/lib/jellyfin/plugins` |
| Emby Windows | `%APPDATA%\Emby-Server\programdata\plugins` |
| Docker | 容器映射的 `/config/plugins` |

## 配置

### Assrt

1. 从 [Assrt](https://assrt.net/) 获取 API Token。
2. 打开插件配置页，将 Token 填入 `Assrt API Token`。
3. 在媒体库的字幕下载器中启用 `MeiamSub.Assrt`。

未配置 Token 时，Assrt 会跳过搜索并在日志中给出提示。

### Thunder

Thunder 默认使用视频文件计算 CID。Jellyfin 版本还可在插件设置中启用元数据搜索，用影片名称辅助匹配。

## 自动下载说明

建议先通过“搜索字幕”手动确认匹配效果，再决定是否启用媒体库自动下载。

- 同一媒体库的字幕下载语言只应添加一次。例如只保留一个 `zho`，重复添加会让服务器发起多次下载请求。
- Emby 与 Jellyfin 如果共用媒体目录，会采用不同的字幕文件命名方式，因此目录中可能同时出现 `.zh-CN.*` 和 `.zho.*` 文件。
- `SearchAllProviders` 表示同时搜索多个字幕源，不代表下载全部搜索结果。
- 自动下载行为由 Emby/Jellyfin 调度；插件只响应服务器发出的搜索与下载请求。

## 常见问题

### 搜不到字幕

- 确认字幕语言已选择中文，并使用最新版插件。
- Assrt 用户请确认 Token 已填写且有效。
- Thunder 依赖视频文件内容计算 CID，请确认服务器能够读取媒体文件。
- 某些影片或版本可能没有对应资源，可尝试启用元数据搜索或手动选择其他结果。

### 自动生成了多个字幕文件

首先检查媒体库的“字幕下载语言”是否存在重复项。服务器会为每个语言项分别执行一次下载，产生 `.0`、`.1` 等后缀。

手动连续下载不同的搜索结果时，服务器也会为避免覆盖已有字幕而添加数字后缀。这属于 Emby/Jellyfin 的文件保存行为，不表示插件正在循环下载。

### Assrt 提示找不到 SharpCompress

- Emby：仅安装插件 DLL，不要携带其他版本的 `SharpCompress.dll`。
- Jellyfin：确保 Assrt 插件目录中包含发布包附带的 `SharpCompress.dll`。

### 如何反馈问题

请在 [Issues](https://github.com/91270/MeiamSubtitles/issues) 中提供：

- Emby/Jellyfin 版本与操作系统；
- 插件名称和版本；
- 媒体文件名、容器格式及问题复现步骤；
- 对应时间段的服务器日志。

提交日志前请删除 Token、媒体路径和其他隐私信息。

## 支持项目

欢迎提交 Issue、Pull Request，或者为项目点一个 Star。

## License

[Apache License 2.0](LICENSE)
