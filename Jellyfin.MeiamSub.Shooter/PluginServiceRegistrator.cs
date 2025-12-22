using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.MeiamSub.Shooter
{
    /// <summary>
    /// 插件服务注册器
    /// 负责注册插件所需的依赖服务，如 HTTP 客户端和字幕提供程序。
    /// <para>修改人: Meiam</para>
    /// <para>修改时间: 2025-12-22</para>
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="serviceCollection">服务集合</param>
        /// <param name="applicationHos">应用程序宿主</param>
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHos)
        {
            serviceCollection.AddHttpClient("MeiamSub.Shooter", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "MeiamSub.Shooter");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
            });

            serviceCollection.AddSingleton<ISubtitleProvider, ShooterProvider>();
        }
    }
}
