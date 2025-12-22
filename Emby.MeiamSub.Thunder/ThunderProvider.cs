using Emby.MeiamSub.Thunder.Model;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.MeiamSub.Thunder
{
    /// <summary>
    /// 迅雷看看字幕提供程序
    /// 负责与迅雷 API 进行交互，通过 CID (Content ID) 匹配并下载字幕。
    /// <para>修改人: Meiam</para>
    /// <para>修改时间: 2025-12-22</para>
    /// </summary>
    public class ThunderProvider : ISubtitleProvider, IHasOrder
    {
        #region 变量声明
        public const string ASS = "ass";
        public const string SSA = "ssa";
        public const string SRT = "srt";

        protected readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public int Order => 100;

        public string Name => "MeiamSub.Thunder";

        /// <summary>
        /// 支持电影、剧集
        /// </summary>
        public IEnumerable<VideoContentType> SupportedMediaTypes => new[] { VideoContentType.Movie, VideoContentType.Episode };
        #endregion

        #region 构造函数
        public ThunderProvider(ILogManager logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _logger.Info("{0} Init", new object[1] { Name });
        }
        #endregion

        #region 查询字幕

        /// <summary>
        /// 搜索字幕 (ISubtitleProvider 接口实现)
        /// 根据媒体信息请求字幕列表。
        /// </summary>
        /// <param name="request">包含媒体路径、语言等信息的搜索请求对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>远程字幕信息列表</returns>
        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            _logger.Info("{0} Search | SubtitleSearchRequest -> {1}", new object[2] { Name, _jsonSerializer.SerializeToString(request) });

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
            // 修改人: Meiam
            // 修改时间: 2025-12-22
            // 备注: 增加异常处理

            try
            {
                var language = NormalizeLanguage(request.Language);

                _logger.Info("{0} Search | Target -> {1} | Language -> {2}", Name, Path.GetFileName(request.MediaPath), language);

                if (language != "chi")
                {
                    _logger.Info("{0} Search | Summary -> Language not supported, skip search.", Name);
                    return Array.Empty<RemoteSubtitleInfo>();
                }

                var stopWatch = Stopwatch.StartNew();
                var cid = await GetCidByFileAsync(request.MediaPath);
                stopWatch.Stop();

                _logger.Info("{0} Search | FileHash -> {1} (Took {2}ms)", new object[3] { Name, cid, stopWatch.ElapsedMilliseconds });


                HttpRequestOptions options = new HttpRequestOptions
                {
                    Url = $"https://api-shoulei-ssl.xunlei.com/oracle/subtitle?name={Path.GetFileName(request.MediaPath)}",
                    UserAgent = $"{Name}",
                    TimeoutMs = 30000,
                    AcceptHeader = "*/*",
                };
                var response = await _httpClient.GetResponse(options);

                _logger.Info("{0} Search | Response -> {1}", new object[2] { Name, _jsonSerializer.SerializeToString(response) });

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var subtitleResponse = _jsonSerializer.DeserializeFromStream<SubtitleResponseRoot>(response.Content);

                    if (subtitleResponse.Code == 0)
                    {
                        _logger.Info("{0} Search | Response -> {1}", new object[2] { Name, _jsonSerializer.SerializeToString(subtitleResponse) });

                        var subtitles = subtitleResponse.Data.Where(m => !string.IsNullOrEmpty(m.Name));

                        var remoteSubtitles = new List<RemoteSubtitleInfo>();

                        if (subtitles.Count() > 0)
                        {
                            foreach (var item in subtitles)
                            {
                                remoteSubtitles.Add(new RemoteSubtitleInfo()
                                {
                                    Id = Base64Encode(_jsonSerializer.SerializeToString(new DownloadSubInfo
                                    {
                                        Url = item.Url,
                                        Format = item.Ext,
                                        Language = request.Language,
                                        IsForced = request.IsForced
                                    })),
                                    Name = $"[MEIAMSUB] {item.Name} | {(item.Langs == string.Empty ? "未知" : item.Langs)} | 迅雷",
                                    Language = request.Language,
                                    Author = "Meiam ",
                                    ProviderName = $"{Name}",
                                    Format = item.Ext,
                                    Comment = $"Format : {item.Ext}",
                                    IsHashMatch = cid == item.Cid,
                                });
                            }
                        }

                        _logger.Info("{0} Search | Summary -> Get  {1}  Subtitles", new object[2] { Name, remoteSubtitles.Count });

                        return remoteSubtitles;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("{0} Search | Exception -> [{1}] {2}", Name, ex.GetType().Name, ex.Message);
            }

            _logger.Info("{0} Search | Summary -> Get  0  Subtitles", new object[1] { Name });


            return Array.Empty<RemoteSubtitleInfo>();
        }
        #endregion

        #region 下载字幕
        /// <summary>
        /// 获取字幕内容 (ISubtitleProvider 接口实现)
        /// 根据字幕 ID 下载具体的字幕文件流。
        /// </summary>
        /// <param name="id">字幕唯一标识符 (Base64 编码的 JSON 数据)</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含字幕流的响应对象</returns>
        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            _logger.Info("{0} DownloadSub | Request -> {1}", new object[2] { Name, id });

            return await DownloadSubAsync(id);
        }

        /// <summary>
        /// 下载字幕
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task<SubtitleResponse> DownloadSubAsync(string info)
        {
            // 修改人: Meiam
            // 修改时间: 2025-12-22
            // 备注: 增加异常处理

            try
            {
                var downloadSub = _jsonSerializer.DeserializeFromString<DownloadSubInfo>(Base64Decode(info));

                if (downloadSub == null)
                {
                    return new SubtitleResponse();
                }

                _logger.Info("{0} DownloadSub | Url -> {1}  |  Format -> {2} |  Language -> {3} ",
                    new object[4] { Name, downloadSub.Url, downloadSub.Format, downloadSub.Language });

                var response = await _httpClient.GetResponse(new HttpRequestOptions
                {
                    Url = downloadSub.Url,
                    UserAgent = $"{Name}",
                    TimeoutMs = 30000,
                    AcceptHeader = "*/*",
                });

                _logger.Info("{0} DownloadSub | Response -> {1}", new object[2] { Name, response.StatusCode });

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
            }
            catch (Exception ex)
            {
                _logger.Error("{0} DownloadSub | Error -> {1}", Name, ex.Message);
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
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            text = text.ToLower();
            if (text.Contains(ASS)) return ASS;
            if (text.Contains(SSA)) return SSA;
            if (text.Contains(SRT)) return SRT;

            return null;
        }

        /// <summary>
        /// 规范化语言代码
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrEmpty(language)) return language;

            if (language.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zh", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("zho", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("chi", StringComparison.OrdinalIgnoreCase))
            {
                return "chi";
            }
            if (language.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                language.Equals("eng", StringComparison.OrdinalIgnoreCase))
            {
                return "eng";
            }
            return language;
        }

        /// <summary>
        /// 异步计算文件 CID (迅雷专用算法)
        /// <para>修改人: Meiam</para>
        /// <para>修改时间: 2025-12-22</para>
        /// <para>备注: 采用异步 I/O 读取文件特定位置的数据块进行 SHA1 计算。</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>计算得到的 CID 字符串</returns>
        private async Task<string> GetCidByFileAsync(string filePath)
        {
            // 修改人: Meiam
            // 修改时间: 2025-12-22
            // 备注: 改造为异步方法，优化 I/O 性能，使用 SHA1.Create() 替代旧 API，并增加 using 语句释放资源

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                // 使用 BinaryReader 配合 BaseStream 需要小心，因为 BinaryReader 本身不支持异步 Read
                // 这里我们直接操作 stream 进行异步读取，不再使用 BinaryReader，因为只是读取字节数组

                var fileSize = new FileInfo(filePath).Length;
                using (var sha1 = SHA1.Create())
                {
                    var buffer = new byte[0xf000];
                    if (fileSize < 0xf000)
                    {
                        await stream.ReadAsync(buffer, 0, (int)fileSize);
                        buffer = sha1.ComputeHash(buffer, 0, (int)fileSize);
                    }
                    else
                    {
                        await stream.ReadAsync(buffer, 0, 0x5000);
                        stream.Seek(fileSize / 3, SeekOrigin.Begin);
                        await stream.ReadAsync(buffer, 0x5000, 0x5000);
                        stream.Seek(fileSize - 0x5000, SeekOrigin.Begin);
                        await stream.ReadAsync(buffer, 0xa000, 0x5000);

                        buffer = sha1.ComputeHash(buffer, 0, 0xf000);
                    }
                    var result = "";
                    foreach (var i in buffer)
                    {
                        result += string.Format("{0:X2}", i);
                    }
                    return result;
                }
            }
        }
        #endregion
    }
}
