using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using System;
using System.IO;

namespace Emby.MeiamSub.Thunder
{
    public class Plugin : BasePlugin, IHasThumbImage
    {
        public override Guid Id => new Guid("E4CE9DA9-EF00-417C-96F2-861C512D45EB");

        public override string Name => "ThunderSubtitle";

        public override string Description => "Download subtitles from Thunder XMP";

        public ImageFormat ThumbImageFormat => ImageFormat.Gif;

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".Thumb.png");
        }
    }
}
