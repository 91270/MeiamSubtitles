using MediaBrowser.Model.Plugins;

namespace Jellyfin.MeiamSub.Thunder.Configuration
{
    /// <summary>
    /// 插件配置类
    /// <para>修改人: Meiam (移植自 PR #133 by Mayfly777w)</para>
    /// <para>修改时间: 2026-02-11</para>
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// 是否使用元数据中的剧集名称和季集编号搜索字幕
        /// <para>启用后，剧集使用 "{SeriesName} S{Season}E{Episode}" 格式搜索</para>
        /// <para>电影使用元数据中的影片名称搜索</para>
        /// <para>默认关闭，使用文件名搜索</para>
        /// </summary>
        public bool EnableUseMetadata { get; set; }

        public PluginConfiguration()
        {
            EnableUseMetadata = false;
        }
    }
}
