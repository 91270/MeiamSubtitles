using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.MeiamSub.Thunder.Model
{
    public class DownloadSubInfo
    {
        public string Url { get; set; }
        public string Format { get; set; }
        public string Language { get; set; }
        public bool? IsForced { get; set; }
    }
}
