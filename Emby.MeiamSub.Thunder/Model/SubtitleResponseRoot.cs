using System.Collections.Generic;

namespace Emby.MeiamSub.Thunder.Model
{
    public class SublistItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string scid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string sname { get; set; }
        /// <summary>
        /// 未知语言
        /// </summary>
        public string language { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string rate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string surl { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int svote { get; set; }
    }

    public class SubtitleResponseRoot
    {
        /// <summary>
        /// 
        /// </summary>
        public List<SublistItem> sublist { get; set; }
    }
}
