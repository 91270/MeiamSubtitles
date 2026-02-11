using Jellyfin.MeiamSub.Thunder.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;

namespace Jellyfin.MeiamSub.Thunder
{
    /// <summary>
    /// 插件入口
    /// <para>修改人: Meiam (移植自 PR #133 by Mayfly777w)</para>
    /// <para>修改时间: 2026-02-11</para>
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
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

        /// <summary>
        /// 获取插件配置页面列表
        /// </summary>
        /// <returns>配置页面信息</returns>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                }
            };
        }
    }
}
