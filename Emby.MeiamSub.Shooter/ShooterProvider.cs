using Emby.MeiamSub.Shooter.Model;
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
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Emby.MeiamSub.Shooter
{
    /// <summary>
    /// 射手网字幕提供程序
    /// 负责与射手网 API 进行交互，通过文件哈希匹配并下载字幕。
    /// <para>修改人: Meiam</para>
    /// <para>修改时间: 2025-12-22</para>
    /// </summary>
    public class ShooterProvider : ISubtitleProvider, IHasOrder
    {
        #region 变量声明
        public const string ASS = "ass";
        public const string SSA = "ssa";
        public const string SRT = "srt";

        protected readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public int Order => 100;
        public string Name => "MeiamSub.Shooter";

        /// <summary>
        /// 支持电影、剧集
        /// </summary>
        public IEnumerable<VideoContentType> SupportedMediaTypes => new[] { VideoContentType.Movie, VideoContentType.Episode };
        #endregion

        #region 构造函数
        public ShooterProvider(ILogManager logManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
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
            // 备注: 增加异常处理，确保单个插件错误不影响系统整体运行

            try
            {
                var language = NormalizeLanguage(request.Language);

                _logger.Info("{0} Search | Target -> {1} | Language -> {2}", Name, Path.GetFileName(request.MediaPath), language);

                if (language != "chi" && language != "eng")
                {
                    _logger.Info("{0} Search | Summary -> Language not supported, skip search.", Name);
                    return Array.Empty<RemoteSubtitleInfo>();
                }

                FileInfo fileInfo = new FileInfo(request.MediaPath);

                var stopWatch = Stopwatch.StartNew();
                var hash = await ComputeFileHashAsync(fileInfo);
                stopWatch.Stop();

                _logger.Info("{0} Search | FileHash -> {1} (Took {2}ms)", new object[3] { Name, hash, stopWatch.ElapsedMilliseconds });

                HttpRequestOptions options = new HttpRequestOptions
                {
                    Url = $"https://www.shooter.cn/api/subapi.php",
                    UserAgent = $"{Name}",
                    TimeoutMs = 30000,
                    AcceptHeader = "*/*",
                };

                options.SetPostData(new Dictionary<string, string>
                {
                    { "filehash", HttpUtility.UrlEncode(hash)},
                    { "pathinfo", HttpUtility.UrlEncode(request.MediaPath)},
                    { "format", "json"},
                    { "lang", language ==  "chi" ? "chn" : "eng"}
                });

                _logger.Info("{0} Search | Request -> {1}", new object[2] { Name, _jsonSerializer.SerializeToString(options) });

                var response = await _httpClient.Post(options);

                _logger.Info("{0} Search | Response -> {1}", new object[2] { Name, _jsonSerializer.SerializeToString(response) });

                if (response.StatusCode == HttpStatusCode.OK && response.ContentType.Contains("application/json"))
                {
                    // 修改人: Meiam
                    // 修改时间: 2025-12-22
                    // 备注: 增加对射手网 API 返回非法内容(如乱码)的校验

                    string responseBody;
                    using (var reader = new StreamReader(response.Content, Encoding.UTF8))
                    {
                        responseBody = await reader.ReadToEndAsync();
                    }

                    _logger.Info("{0} Search | ResponseBody -> {1}", new object[2] { Name, responseBody });

                    if (string.IsNullOrEmpty(responseBody) || !responseBody.Trim().StartsWith("["))
                    {
                        _logger.Info("{0} Search | Summary -> API returned invalid content (likely no subtitles found or API error).", Name);
                        return Array.Empty<RemoteSubtitleInfo>();
                    }

                    var subtitleResponse = _jsonSerializer.DeserializeFromString<List<SubtitleResponseRoot>>(responseBody);

                    if (subtitleResponse != null)
                    {
                        _logger.Info("{0} Search | Response -> {1}", new object[2] { Name, _jsonSerializer.SerializeToString(subtitleResponse) });

                        var remoteSubtitles = new List<RemoteSubtitleInfo>();

                        foreach (var subFileInfo in subtitleResponse)
                        {
                            foreach (var subFile in subFileInfo.Files)
                            {
                                remoteSubtitles.Add(new RemoteSubtitleInfo()
                                {
                                    Id = Base64Encode(_jsonSerializer.SerializeToString(new DownloadSubInfo
                                    {
                                        Url = subFile.Link,
                                        Format = subFile.Ext,
                                        Language = request.Language,
                                        IsForced = request.IsForced
                                    })),
                                    Name = $"[MEIAMSUB] {Path.GetFileName(request.MediaPath)} | {request.Language} | 射手",
                                    Language = request.Language,
                                    Author = "Meiam ",
                                    ProviderName = $"{Name}",
                                    Format = subFile.Ext,
                                    Comment = $"Format : {ExtractFormat(subFile.Ext)}",
                                    IsHashMatch = true
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
            _logger.Info("{0}  DownloadSub | Request -> {1}", new object[2] { Name, id });

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

                _logger.Info($"{0} DownloadSub | Url -> {1}  |  Format -> {2} |  Language -> {3} ",
                    new object[4] { Name, downloadSub.Url, downloadSub.Format, downloadSub.Language });

                var response = await _httpClient.GetResponse(new HttpRequestOptions
                {
                    Url = downloadSub.Url,
                    UserAgent = $"{Name}",
                    TimeoutMs = 30000,
                    AcceptHeader = "*/*",
                });


                _logger.Info("{0}  DownloadSub | Request -> {1}", new object[2] { Name, response.StatusCode });


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
        /// 异步计算文件 Hash (射手网专用算法)
        /// <para>修改人: Meiam</para>
        /// <para>修改时间: 2025-12-22</para>
        /// <para>备注: 采用异步 I/O 读取文件特定位置的 4KB 数据块进行 MD5 计算。</para>
        /// </summary>
        /// <param name="fileInfo">文件信息对象</param>
        /// <returns>计算得到的文件 Hash 字符串，如果文件过小或不存在则返回空字符串</returns>
        public static async Task<string> ComputeFileHashAsync(FileInfo fileInfo)
        {
            // 修改人: Meiam
            // 修改时间: 2025-12-22
            // 备注: 改造为异步方法，优化 I/O 性能并增加 using 语句释放资源

            string ret = "";

            if (!fileInfo.Exists || fileInfo.Length < 8 * 1024)
            {
                return ret;
            }

            using (FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                long[] offset = new long[4];
                offset[3] = fileInfo.Length - 8 * 1024;
                offset[2] = fileInfo.Length / 3;
                offset[1] = fileInfo.Length / 3 * 2;
                offset[0] = 4 * 1024;

                byte[] bBuf = new byte[1024 * 4];

                for (int i = 0; i < 4; ++i)
                {
                    fs.Seek(offset[i], SeekOrigin.Begin);
                    await fs.ReadAsync(bBuf, 0, 4 * 1024);

                    using (MD5 md5Hash = MD5.Create())
                    {
                        byte[] data = md5Hash.ComputeHash(bBuf);
                        StringBuilder sBuilder = new StringBuilder();

                        for (int j = 0; j < data.Length; j++)
                        {
                            sBuilder.Append(data[j].ToString("x2"));
                        }

                        if (!string.IsNullOrEmpty(ret))
                        {
                            ret += ";";
                        }

                        ret += sBuilder.ToString();
                    }
                }
            }

            return ret;
        }
        #endregion
    }
}
