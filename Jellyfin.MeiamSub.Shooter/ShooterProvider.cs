using Jellyfin.MeiamSub.Shooter.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Web;
using static System.Net.WebRequestMethods;

namespace Jellyfin.MeiamSub.Shooter
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

        private readonly ILogger<ShooterProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string ApiUrl = "https://www.shooter.cn/api/subapi.php";

        public int Order => 100;

        public string Name => "MeiamSub.Shooter";

        /// <summary>
        /// 支持电影、剧集
        /// </summary>
        public IEnumerable<VideoContentType> SupportedMediaTypes => new[] { VideoContentType.Movie, VideoContentType.Episode };
        #endregion

        #region 构造函数
        public ShooterProvider(ILogger<ShooterProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _logger.LogInformation($"{Name} Init");
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
            _logger.LogInformation($"{Name} Search | SubtitleSearchRequest -> {JsonSerializer.Serialize(request)}");

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

                _logger.LogInformation("{Provider} Search | Target -> {File} | Language -> {Lang}", Name, Path.GetFileName(request.MediaPath), language);

                if (language != "chi" && language != "eng")
                {
                    _logger.LogInformation("{Provider} Search | Summary -> Language not supported, skip search.", Name);
                    return Array.Empty<RemoteSubtitleInfo>();
                }

                FileInfo fileInfo = new(request.MediaPath);

                var stopWatch = Stopwatch.StartNew();
                var hash = await ComputeFileHashAsync(fileInfo);
                stopWatch.Stop();

                _logger.LogInformation("{Provider} Search | FileHash -> {Hash} (Took {Elapsed}ms)", Name, hash, stopWatch.ElapsedMilliseconds);

                var formData = new Dictionary<string, string>
                {
                    { "filehash", hash},
                    { "pathinfo", request.MediaPath},
                    { "format", "json"},
                    { "lang", language ==  "chi" ? "chn" : "eng"}
                };

                var content = new FormUrlEncodedContent(formData);

                // 设置请求头
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using var httpClient = _httpClientFactory.CreateClient(Name);

                // 发送 POST 请求
                var response = await httpClient.PostAsync(ApiUrl, content);

                _logger.LogInformation($"{Name} Search | Response -> {JsonSerializer.Serialize(response)}");

                // 处理响应

                if (response.IsSuccessStatusCode && response.Content.Headers.Any(m => m.Value.Contains("application/json; charset=utf-8")))

                {

                    var responseBody = await response.Content.ReadAsStringAsync();



                    _logger.LogInformation($"{Name} Search | ResponseBody -> {responseBody} ");



                    if (string.IsNullOrEmpty(responseBody) || !responseBody.Trim().StartsWith("["))



                    {



                        _logger.LogInformation($"{Name} Search | Summary -> API returned invalid content (likely no subtitles found or API error).");



                        return Array.Empty<RemoteSubtitleInfo>();



                    }







                    var subtitles = JsonSerializer.Deserialize<List<SubtitleResponseRoot>>(responseBody);

                    _logger.LogInformation($"{Name} Search | Response -> {JsonSerializer.Serialize(subtitles)}");

                    if (subtitles != null)
                    {

                        var remoteSubtitles = new List<RemoteSubtitleInfo>();

                        foreach (var subFileInfo in subtitles)
                        {
                            foreach (var subFile in subFileInfo.Files)
                            {
                                remoteSubtitles.Add(new RemoteSubtitleInfo()
                                {
                                    Id = Base64Encode(JsonSerializer.Serialize(new DownloadSubInfo
                                    {
                                        Url = subFile.Link,
                                        Format = subFile.Ext,
                                        Language = request.Language,
                                        TwoLetterISOLanguageName = request.TwoLetterISOLanguageName,
                                    })),
                                    Name = $"[MEIAMSUB] {Path.GetFileName(request.MediaPath)} | {request.TwoLetterISOLanguageName} | 射手",
                                    Author = "Meiam ",
                                    ProviderName = $"{Name}",
                                    Format = subFile.Ext,
                                    Comment = $"Format : {ExtractFormat(subFile.Ext)}",
                                    IsHashMatch = true
                                });
                            }
                        }

                        _logger.LogInformation($"{Name} Search | Summary -> Get  {remoteSubtitles.Count}  Subtitles");

                        return remoteSubtitles;
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Provider} Search | Exception -> [{Type}] {Message}", Name, ex.GetType().Name, ex.Message);
            }

            _logger.LogInformation($"{Name} Search | Summary -> Get  0  Subtitles");

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
            // 修改人: Meiam
            // 修改时间: 2025-12-22
            // 备注: 增加异常处理

            try
            {
                var downloadSub = JsonSerializer.Deserialize<DownloadSubInfo>(Base64Decode(info));

                if (downloadSub == null)
                {
                    return new SubtitleResponse();
                }

                _logger.LogInformation($"{Name} DownloadSub | Url -> {downloadSub.Url}  |  Format -> {downloadSub.Format} |  Language -> {downloadSub.Language} ");

                using var options = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(downloadSub.Url)
                };

                using var httpClient = _httpClientFactory.CreateClient(Name);

                var response = await httpClient.SendAsync(options);

                _logger.LogInformation($"{Name} DownloadSub | Response -> {response.StatusCode}");

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{0} DownloadSub | Error -> {1}", Name, ex.Message);
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
                    await fs.ReadExactlyAsync(bBuf, 0, 4 * 1024);

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
