using System.Collections.Generic;

namespace Jellyfin.MeiamSub.Thunder.Model
{
    public class SubtitleResponseRoot
    {
        public int Code { get; set; }
        public List<SublistItem> Data { get; set; }
        public string Result { get; set; }
    }

    public class SublistItem
    {
        public string Gcid { get; set; }
        public string Cid { get; set; }
        public string Url { get; set; }
        public string Ext { get; set; }
        public string Name { get; set; }
        public int Duration { get; set; }
        public string[] Languages { get; set; }

        public string Langs => Languages != null ? string.Join(",", Languages) : string.Empty;

        public int Source { get; set; }
        public int Score { get; set; }
        public int FingerprintfScore { get; set; }
        public string ExtraName { get; set; }
    }
}
