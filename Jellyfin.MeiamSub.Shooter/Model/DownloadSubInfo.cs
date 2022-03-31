using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.MeiamSub.Shooter.Model
{
    public class DownloadSubInfo
    {
        public string Url { get; set; }
        public string Format { get; set; }
        public string Language { get; set; }
        public string TwoLetterISOLanguageName { get; set; }
        public bool? IsForced { get; set; }
    }
}
