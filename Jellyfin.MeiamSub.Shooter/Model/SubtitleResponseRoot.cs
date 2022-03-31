using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Jellyfin.MeiamSub.Shooter.Model
{
    public class SubtitleResponseRoot
    {
        public string Desc { get; set; }
        public int Delay { get; set; }
        public SubFileInfo[] Files { get; set; }
    }

    public class SubFileInfo
    {
        public string Ext { get; set; }
        public string Link { get; set; }
    }

}
