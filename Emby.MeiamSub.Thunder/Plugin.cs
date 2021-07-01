using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using System;
using System.IO;

namespace Emby.MeiamSub.Thunder
{

    /// <summary>
    /// 插件入口
    /// </summary>
    public class Plugin : BasePlugin, IHasThumbImage
    {
        /// <summary>
        /// 插件ID
        /// </summary>
        public override Guid Id => new Guid("E4CE9DA9-EF00-417C-96F2-861C512D45EB");

        /// <summary>
        /// 插件名称
        /// </summary>
        public override string Name => "ThunderSubtitle";

        /// <summary>
        /// 插件描述
        /// </summary>
        public override string Description => "Download subtitles from Thunder XMP";

        /// <summary>
        /// 缩略图格式化类型
        /// </summary>
        public ImageFormat ThumbImageFormat => ImageFormat.Gif;

        /// <summary>
        /// 缩略图资源文件
        /// </summary>
        /// <returns></returns>
        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".Thumb.png");
        }
    }
}
