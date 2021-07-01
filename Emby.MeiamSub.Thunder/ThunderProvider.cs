using Emby.MeiamSub.Thunder.Model;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Emby.MeiamSub.Thunder
{
    /// <summary>
    /// 迅雷字幕组件
    /// </summary>
    public class ThunderProvider : ISubtitleProvider, IHasOrder
    {
        #region 变量声明
        public const string ASS = "ass";
        public const string SSA = "ssa";
        public const string SRT = "srt";

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public int Order => 0;
        public string Name => "ThunderSubtitle";

        /// <summary>
        /// 支持电影、剧集
        /// </summary>
        public IEnumerable<VideoContentType> SupportedMediaTypes => new List<VideoContentType>() { VideoContentType.Movie, VideoContentType.Episode };
        #endregion

        #region 构造函数
        public ThunderProvider(ILogger logger, IJsonSerializer jsonSerializer,IHttpClient httpClient)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }
        #endregion

        #region 查询字幕

        /// <summary>
        /// 查询请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            _logger.Debug($"ThunderSubtitle Search | Request -> { _jsonSerializer.SerializeToString(request) }");

            var subtitles = await SearchSubtitlesAsync(request);

            return subtitles;
        }

        /// <summary>
        /// 查询字幕
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitlesAsync(SubtitleSearchRequest request)
        {
            var cid = GetCidByFile(request.MediaPath);

            var response = await _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = $"http://sub.xmp.sandai.net:8000/subxl/{cid}.json"
            });

            _logger.Debug($"ThunderSubtitle Search | Response -> { _jsonSerializer.SerializeToString(response) }");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var subtitleResponse = _jsonSerializer.DeserializeFromStream<SubtitleResponseRoot>(response.Content);

                if (subtitleResponse != null)
                {
                    var subtitles = subtitleResponse.sublist.Where(m => !string.IsNullOrEmpty(m.sname));

                    if (subtitles.Count() > 0)
                    {
                        return subtitles.Select(m => new RemoteSubtitleInfo()
                        {
                            Id = Base64Encode(_jsonSerializer.SerializeToString(new DownloadSubInfo
                            {
                                Url = m.surl,
                                Format = ExtractFormat(m.sname),
                                Language = request.Language,
                                TwoLetterISOLanguageName = request.TwoLetterISOLanguageName,
                                IsForced = request.IsForced
                            })),
                            Name = Path.GetFileName(HttpUtility.UrlDecode(m.sname)),
                            Author = "Meiam ",
                            CommunityRating = Convert.ToSingle(m.rate),
                            ProviderName = "ThunderSubtitle",
                            Format = ExtractFormat(m.sname),
                            Comment = $"Rate : { m.rate }"
                        }).OrderByDescending(m => m.CommunityRating);
                    }
                }
            }

            return Array.Empty<RemoteSubtitleInfo>();
        }
        #endregion


        #region 下载字幕
        /// <summary>
        /// 下载请求
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                _logger.Debug($"ThunderSubtitle DownloadSub | Request -> {id}");
            });

            return await DownloadSubAsync(id);
        }

        /// <summary>
        /// 下载字幕
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task<SubtitleResponse> DownloadSubAsync(string info)
        {
            var downloadSub = _jsonSerializer.DeserializeFromString<DownloadSubInfo>(Base64Decode(info));

            _logger.Debug($"ThunderSubtitle DownloadSub | Url -> { downloadSub.Url }  |  Format -> { downloadSub.Format } |  Language -> { downloadSub.Language } ");

            var response = await _httpClient.GetResponse(new HttpRequestOptions
            {
                Url = downloadSub.Url
            });

            _logger.Debug($"ThunderSubtitle DownloadSub | Response -> { response.StatusCode }");

            if (response.StatusCode == HttpStatusCode.OK)
            {

                return new SubtitleResponse()
                {
                    Language = downloadSub.Language,
                    IsForced = false,
                    Format = downloadSub.Format,
                    Stream = response.Content,
                };
            }

            return new SubtitleResponse();

        }
        #endregion

        #region 内部方法

        /// <summary>
        /// Base64 加密
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
        /// <summary>
        /// Base64 解密
        /// </summary>
        /// <param name="base64EncodedData"></param>
        /// <returns></returns>
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// 提取格式化字幕类型
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected string ExtractFormat(string text)
        {

            string result = null;

            if (text != null)
            {
                text = text.ToLower();
                if (text.Contains(ASS)) result = ASS;
                else if (text.Contains(SSA)) result = SSA;
                else if (text.Contains(SRT)) result = SRT;
                else result = null;
            }
            return result;
        }

        /// <summary>
        /// 获取文件 CID (迅雷)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string GetCidByFile(string filePath)
        {
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var reader = new BinaryReader(stream);
            var fileSize = new FileInfo(filePath).Length;
            var SHA1 = new SHA1CryptoServiceProvider();
            var buffer = new byte[0xf000];
            if (fileSize < 0xf000)
            {
                reader.Read(buffer, 0, (int)fileSize);
                buffer = SHA1.ComputeHash(buffer, 0, (int)fileSize);
            }
            else
            {
                reader.Read(buffer, 0, 0x5000);
                stream.Seek(fileSize / 3, SeekOrigin.Begin);
                reader.Read(buffer, 0x5000, 0x5000);
                stream.Seek(fileSize - 0x5000, SeekOrigin.Begin);
                reader.Read(buffer, 0xa000, 0x5000);

                buffer = SHA1.ComputeHash(buffer, 0, 0xf000);
            }
            var result = "";
            foreach (var i in buffer)
            {
                result += string.Format("{0:X2}", i);
            }
            return result;
        }
        #endregion
    }
}
