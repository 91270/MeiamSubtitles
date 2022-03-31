using Jellyfin.MeiamSub.Shooter.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;

namespace Jellyfin.MeiamSub.Shooter
{

    /// <summary>
    /// 插件入口
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
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

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
    : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }

    }
}
