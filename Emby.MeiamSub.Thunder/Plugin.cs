using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;

namespace Emby.MeiamSub.Thunder
{

    /// <summary>
    /// 插件入口
    /// </summary>
    public class Plugin : BasePlugin, IHasThumbImage
    {

        public Plugin(IApplicationPaths applicationPaths)
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
}
