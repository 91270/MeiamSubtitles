using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.MeiamSub.Thunder
{
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHos)
        {
            serviceCollection.AddHttpClient("MeiamSub.Thunder", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "MeiamSub.Thunder");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
            });

            serviceCollection.AddSingleton<ISubtitleProvider, ThunderProvider>();
        }
    }
}
