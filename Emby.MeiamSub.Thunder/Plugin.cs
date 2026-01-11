﻿using Emby.Web.GenericEdit;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.ComponentModel;
using System.IO;

namespace Emby.MeiamSub.Thunder
{

    /// <summary>
    /// 插件入口
    /// </summary>
    public class Plugin : BasePluginSimpleUI<PluginConfiguration>, IHasThumbImage
    {

        public Plugin(IApplicationPaths applicationPaths, IApplicationHost applicationHost) : base(applicationHost)
        {
            Instance = this;
        }

        /// <summary>
        /// 插件ID
        /// </summary>
        public override Guid Id => new Guid("E4CE9DA9-EF00-417C-96F2-861C512D45EB");

        /// <summary>
        /// 插件名称
        /// </summary>
        public override string Name => "MeiamSub.Thunder";

        /// <summary>
        /// 插件描述
        /// </summary>
        public override string Description => "Download subtitles from Thunder XMP";

        /// <summary>
        /// 缩略图格式化类型
        /// </summary>
        public ImageFormat ThumbImageFormat => ImageFormat.Gif;

        /// <summary>
        /// 获取插件选项
        /// </summary>
        public PluginConfiguration Options => this.GetOptions();

        public static Plugin Instance { get; private set; }

        /// <summary>
        /// 获取插件缩略图资源流
        /// <para>修改人: Meiam</para>
        /// <para>修改时间: 2025-12-22</para>
        /// <para>备注: 增加了资源加载的安全性检查，防止因资源名不匹配导致的空引用。</para>
        /// </summary>
        /// <returns>图片资源流，若不存在则返回 null</returns>
        public Stream GetThumbImage()
        {
            var type = GetType();
            var resourceName = $"{type.Namespace}.Thumb.png";
            var stream = type.Assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                return null;
            }

            return stream;
        }
    }

    /// <summary>
    /// 插件配置类
    /// </summary>
    public class PluginConfiguration : EditableOptionsBase
    {
        public override string EditorTitle => "MeiamSub Thunder Options";

        [Description("勾选此项后，使用元数据中的剧集名称和季集编号搜索字幕")]
        public bool EnableUseMetadata { get; set; }

        public PluginConfiguration()
        {
            // 默认值
            EnableUseMetadata = false;
        }
    }
}
