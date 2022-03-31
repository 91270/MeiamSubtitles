using Jellyfin.MeiamSub.Thunder.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Serialization;
using System;

namespace Jellyfin.MeiamSub.Thunder
{
    /// <summary>
    /// 插件入口
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
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


        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }
    }
}
