using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using System;
using System.IO;

namespace Emby.MeiamSub.Shooter
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
        public override Guid Id => new Guid("038D37A2-7A1E-4C01-9B6D-AA215D29AB4C");

        /// <summary>
        /// 插件名称
        /// </summary>
        public override string Name => "MeiamSub.Shooter";

        /// <summary>
        /// 插件描述
        /// </summary>
        public override string Description => "Download subtitles from Shooter";

        /// <summary>
        /// 缩略图格式化类型
        /// </summary>
        public ImageFormat ThumbImageFormat => ImageFormat.Gif;


        public static Plugin Instance { get; private set; }

        /// <summary>
        /// 缩略图资源文件
        /// </summary>
        /// <returns></returns>
        public Stream GetThumbImage()
        {
            var type = GetType();
            var resourceName = $"{type.Namespace}.Thumb.png";
            var stream = type.Assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                // 如果找不到资源，尝试不带命名空间的名称，或者记录错误
                // 这里我们至少确保不会返回 null 导致外部空引用（虽然 Emby 可能处理 null）
                // 但为了稳健，如果真的没有，返回 null 是正确的，Emby 会显示默认图标
                return null;
            }

            return stream;
        }
    }
}
