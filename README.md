# MeiamSubtitles
Emby Jellyfin 中文字幕插件 ，支持 迅雷影音、射手网、 精准匹配，自动下载


[![.NET CORE](https://img.shields.io/badge/.NET%20Core-3.1-d.svg)](#)
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

[直达通道(传家宝套餐)](https://bwh88.net/aff.php?aff=117&pid=147)

&nbsp;

## 功能介绍


- [x] 支持  迅雷看看    字幕下载    Hash匹配
- [x] 支持  射手影音    字幕下载    Hash匹配


## 项目说明

| # | 模块功能                      |  项目文件                    | 说明
|---|-------------------------------|-------------------------------|-------------------------------
| 1 | 开发程序 | Emby.MeiamSub.DevTool | 项目开发测试调试使用
| 2 | 字幕插件 | Emby.MeiamSub.Thunder | 迅雷看看字幕插件 - Emby
| 3 | 字幕插件 | Emby.MeiamSub.Shooter | 射手影音字幕插件 - Emby
| 4 | 字幕插件 | Jellyfin.MeiamSub.Shooter | 迅雷看看字幕插件 - Jellyfin
| 5 | 字幕插件 | Jellyfin.MeiamSub.Thunder | 射手影音字幕插件 - Jellyfin



## 使用插件

首先下载已编译好的插件 [LINK](https://github.com/91270/Emby.MeiamSub/releases)

### WINDOWS
```bash
复制插件文件到   Emby-Server\Programdata\Plugins\
复制插件文件到   Emby-Server\System\Plugins\
重启服务
```

### LINUX
```bash
复制插件文件到  /opt/emby-server/system/plugins
复制插件文件到  /var/lib/emby/plugins
重启服务
```

&nbsp;

### 群晖
```bash
复制插件文件到 /var/packages/EmbyServer/var/plugins
复制插件文件到 /var/packages/EmbyServer/target/system/plugins
重启服务
```

### 威联通
```bash
# 其中`CACHEDEV{num}_DATA`的名称取决于你的qpkg安装位置
复制插件文件到 /share/CACHEDEV1_DATA/.qpkg/EmbyServer/programdata/plugins
复制插件文件到 /share/CACHEDEV1_DATA/.qpkg/EmbyServer/system/plugins
重启服务
```


### Jellyfin 可通过存储库安装、更新插件
```bash
# 通过 控制台 -> 插件 -> 存储库 添加存储库 URL , 即可通过插件目录查看并安装插件
https://github.com/91270/MeiamSubtitles.Release/raw/main/Plugin/manifest-stable.json
```

&nbsp;

## 贡献

贡献的最简单的方法之一就是是参与讨论和讨论问题（issue）。你也可以通过提交的 Pull Request 代码变更作出贡献。

## 致谢

[Emby.Subtitle.Subscene](https://github.com/nRafinia/Emby.Subtitle.Subscene)
