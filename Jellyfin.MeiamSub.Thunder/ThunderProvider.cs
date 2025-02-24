using Jellyfin.MeiamSub.Thunder.Model;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.MeiamSub.Thunder
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

        private readonly ILogger<ThunderProvider> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly JsonSerializerOptions _deserializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public int Order => 1;
        public string Name => "MeiamSub.Thunder";

        /// <summary>
        /// 支持电影、剧集
        /// </summary>
        public IEnumerable<VideoContentType> SupportedMediaTypes => new List<VideoContentType>() { VideoContentType.Movie, VideoContentType.Episode };
        #endregion

        #region 构造函数
        public ThunderProvider(ILogger<ThunderProvider> logger)
        {
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _logger.LogInformation($"{Name} Init");
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
            _logger.LogInformation($"{Name} Search | SubtitleSearchRequest -> { JsonSerializer.Serialize(request) }");

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
            if (request.Language != "chi")
            {
                return Array.Empty<RemoteSubtitleInfo>();
            }

            var cid = GetCidByFile(request.MediaPath);

            _logger.LogInformation($"{Name} Search | FileHash -> { cid }");

            using var options = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api-shoulei-ssl.xunlei.com/oracle/subtitle?name={Path.GetFileName(request.MediaPath)}"),
                Headers =
                    {
                        UserAgent = { new ProductInfoHeaderValue(new ProductHeaderValue($"{Name}")) },
                        Accept = { new MediaTypeWithQualityHeaderValue("*/*") },
                    }
            };

            var response = await _httpClient.SendAsync(options);

            _logger.LogInformation($"{Name} Search | Response -> { JsonSerializer.Serialize(response) }");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var subtitleResponse = JsonSerializer.Deserialize<SubtitleResponseRoot>(await response.Content.ReadAsStringAsync(), _deserializeOptions);

                if (subtitleResponse != null)
                {
                    _logger.LogInformation($"{Name} Search | Response -> { JsonSerializer.Serialize(subtitleResponse) }");

                    var subtitles = subtitleResponse.Data.Where(m => !string.IsNullOrEmpty(m.Name));

                    var remoteSubtitleInfos = new List<RemoteSubtitleInfo>();

                    if (subtitles.Count() > 0)
                    {
                        foreach (var item in subtitles)
                        {
                            remoteSubtitleInfos.Add(new RemoteSubtitleInfo()
                            {
                                Id = Base64Encode(JsonSerializer.Serialize(new DownloadSubInfo
                                {
                                    Url = item.Url,
                                    Format = item.Ext,
                                    Language = request.Language,
                                    TwoLetterISOLanguageName = request.TwoLetterISOLanguageName,
                                })),
                                Name = $"[MEIAMSUB] {item.Name} | {(item.Langs == string.Empty ? "未知" : item.Langs)} | 迅雷",
                                Author = "Meiam ",
                                ProviderName = $"{Name}",
                                Format = item.Ext,
                                Comment = $"Format : {item.Ext}",
                                IsHashMatch = cid == item.Cid,
                            });
                        }
                    }

                    _logger.LogInformation($"{Name} Search | Summary -> Get  {subtitles.Count()}  Subtitles");

                    return remoteSubtitleInfos;
                }
            }

            _logger.LogInformation($"{Name} Search | Summary -> Get  0  Subtitles");

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
            _logger.LogInformation($"{Name} DownloadSub | Request -> {id}");

            return await DownloadSubAsync(id);
        }

        /// <summary>
        /// 下载字幕
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task<SubtitleResponse> DownloadSubAsync(string info)
        {
            var downloadSub = JsonSerializer.Deserialize<DownloadSubInfo>(Base64Decode(info));

            if (downloadSub == null)
            {
                return new SubtitleResponse();
            }

            _logger.LogInformation($"{Name} DownloadSub | Url -> { downloadSub.Url }  |  Format -> { downloadSub.Format } |  Language -> { downloadSub.Language } ");

            using var options = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(downloadSub.Url),
                Headers =
                    {
                        UserAgent = { new ProductInfoHeaderValue(new ProductHeaderValue($"{Name}")) },
                        Accept = { new MediaTypeWithQualityHeaderValue("*/*") }
                    }
            };

            var response = await _httpClient.SendAsync(options);

            _logger.LogInformation($"{Name} DownloadSub | Response -> { response.StatusCode }");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var stream = await response.Content.ReadAsStreamAsync();

                return new SubtitleResponse()
                {
                    Language = downloadSub.Language,
                    IsForced = false,
                    Format = downloadSub.Format,
                    Stream = stream,
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
            var sha1 = SHA1.Create();
            var buffer = new byte[0xf000];
            if (fileSize < 0xf000)
            {
                reader.Read(buffer, 0, (int)fileSize);
                buffer = sha1.ComputeHash(buffer, 0, (int)fileSize);
            }
            else
            {
                reader.Read(buffer, 0, 0x5000);
                stream.Seek(fileSize / 3, SeekOrigin.Begin);
                reader.Read(buffer, 0x5000, 0x5000);
                stream.Seek(fileSize - 0x5000, SeekOrigin.Begin);
                reader.Read(buffer, 0xa000, 0x5000);

                buffer = sha1.ComputeHash(buffer, 0, 0xf000);
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
