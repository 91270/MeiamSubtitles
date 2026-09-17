using Jellyfin.MeiamSub.Thunder.Model;
using MeiamSubtitles.Shared;
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

namespace Jellyfin.MeiamSub.Thunder
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

        private readonly ILogger<ThunderProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions _deserializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public int Order => 100;
        public string Name => "MeiamSub.Thunder";

        /// <summary>
        /// 支持电影、剧集
        /// </summary>
        public IEnumerable<VideoContentType> SupportedMediaTypes => new[] { VideoContentType.Movie, VideoContentType.Episode };
        #endregion

        #region 构造函数
        public ThunderProvider(ILogger<ThunderProvider> logger, IHttpClientFactory httpClientFactory)
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
        
                    var subtitles = await SearchSubtitlesAsync(request, cancellationToken);
        
                    return subtitles;
                }

        /// <summary>
        /// 查询字幕
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitlesAsync(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            // 修改人: Meiam (移植自 PR #133 by Mayfly777w)
            // 修改时间: 2026-02-11
            // 备注: 增加元数据搜索选项，可以以元数据中的系列名和季集信息来作为匹配对象

            try
            {
                if (request == null)
                {
                    _logger.LogInformation("{Provider} Search | Request is null", Name);
                    return Array.Empty<RemoteSubtitleInfo>();
                }

                var language = NormalizeLanguage(request.Language);

                // 根据配置决定搜索名称：使用元数据 或 文件名
                string movieName;
                if (Plugin.Instance?.Configuration?.EnableUseMetadata == true)
                {
                    if (request.ContentType == VideoContentType.Episode)
                    {
                        movieName = $"{request.SeriesName} S{request.ParentIndexNumber}E{request.IndexNumber}";
                    }
                    else if (request.ContentType == VideoContentType.Movie)
                    {
                        movieName = request.Name;
                    }
                    else
                    {
                        movieName = !string.IsNullOrEmpty(request.MediaPath)
                            ? Path.GetFileName(request.MediaPath)
                            : string.Empty;
                    }
                }
                else
                {
                    movieName = !string.IsNullOrEmpty(request.MediaPath)
                        ? Path.GetFileName(request.MediaPath)
                        : string.Empty;
                }

                _logger.LogInformation(Name + " Search | Target -> " + movieName + " | Language -> " + language);

                if (language != "chi")
                {
                    _logger.LogInformation(Name + " Search | Summary -> Language not supported, skip search.");
                    return Array.Empty<RemoteSubtitleInfo>();
                }

                if (string.IsNullOrEmpty(request.MediaPath))
                {
                    _logger.LogInformation(Name + " Search | Summary -> MediaPath is empty, skip search.");
                    return Array.Empty<RemoteSubtitleInfo>();
                }

                var cid = string.Empty;
                var stopWatch = Stopwatch.StartNew();
                try
                {
                    cid = await GetCidByFileAsync(request.MediaPath, cancellationToken);
                    _logger.LogInformation("{Provider} Search | FileHash -> {FileHash} (Took {ElapsedMilliseconds}ms)",
                        Name, cid, stopWatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger.LogWarning("{Provider} Search | File hash unavailable ({ExceptionType}: {Message}), continue with name search.",
                        Name, ex.GetType().Name, ex.Message);
                }
                finally
                {
                    stopWatch.Stop();
                }

                using var options = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api-shoulei-ssl.xunlei.com/oracle/subtitle?name={Uri.EscapeDataString(movieName)}")
                };

                using var httpClient = _httpClientFactory.CreateClient(Name);

                using var response = await httpClient.SendAsync(options, cancellationToken);

                _logger.LogInformation("{Provider} Search | Response -> {StatusCode}", Name, response.StatusCode);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var subtitleResponse = JsonSerializer.Deserialize<SubtitleResponseRoot>(await response.Content.ReadAsStringAsync(), _deserializeOptions);

                    if (subtitleResponse != null)
                    {
                        var subtitles = (subtitleResponse.Data ?? new List<SublistItem>())
                            .Where(m => !string.IsNullOrWhiteSpace(m.Name) &&
                                !string.IsNullOrWhiteSpace(m.Url) &&
                                NormalizeFormat(m.Ext) != null)
                            .GroupBy(m => m.Url, StringComparer.OrdinalIgnoreCase)
                            .Select(m => m.First())
                            .OrderByDescending(m => !string.IsNullOrEmpty(cid) && string.Equals(cid, m.Cid, StringComparison.OrdinalIgnoreCase))
                            .ThenByDescending(m => m.Langs?.Contains("简体", StringComparison.OrdinalIgnoreCase) == true)
                            .Take(20)
                            .ToList();

                        var remoteSubtitles = new List<RemoteSubtitleInfo>();

                        if (subtitles.Count() > 0)
                        {
                            foreach (var item in subtitles)
                            {
                                remoteSubtitles.Add(new RemoteSubtitleInfo()
                                {
                                    Id = Base64Encode(JsonSerializer.Serialize(new DownloadSubInfo
                                    {
                                        Url = item.Url,
                                        Format = NormalizeFormat(item.Ext),
                                        Language = request.Language,
                                        TwoLetterISOLanguageName = request.TwoLetterISOLanguageName,
                                    })),
                                    Name = $"[MEIAMSUB] {item.Name} | {(item.Langs == string.Empty ? "未知" : item.Langs)} | 迅雷",
                                    Author = "Meiam ",
                                    ProviderName = $"{Name}",
                                    Format = NormalizeFormat(item.Ext),
                                    Comment = $"Format : {NormalizeFormat(item.Ext)}",
                                    IsHashMatch = !string.IsNullOrEmpty(cid) && string.Equals(cid, item.Cid, StringComparison.OrdinalIgnoreCase),
                                });
                            }
                        }

                        _logger.LogInformation($"{Name} Search | Summary -> Get  {subtitles.Count()}  Subtitles");

                        return remoteSubtitles;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Name} Search | Exception -> {ex.Message}");
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

            return await DownloadSubAsync(id, cancellationToken);
        }

        /// <summary>
        /// 下载字幕
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task<SubtitleResponse> DownloadSubAsync(string info, CancellationToken cancellationToken)
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

                using var response = await httpClient.SendAsync(options, cancellationToken);

                _logger.LogInformation($"{Name} DownloadSub | Response -> {response.StatusCode}");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var format = NormalizeFormat(downloadSub.Format)
                        ?? throw new InvalidDataException($"Unsupported subtitle format: {downloadSub.Format}");
                    var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    var utf8Data = SubtitleContentValidator.ValidateAndConvertToUtf8(
                        data,
                        format,
                        response.Content.Headers.ContentType?.MediaType);
                    var stream = new MemoryStream(utf8Data, writable: false);

                    return new SubtitleResponse()
                    {
                        Language = downloadSub.Language,
                        IsForced = false,
                        Format = format,
                        Stream = stream,
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Provider} DownloadSub | Exception -> [{Type}] {Message}", Name, ex.GetType().Name, ex.Message);
            }

            throw new InvalidDataException($"{Name} subtitle download failed.");

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

        private static string NormalizeFormat(string format)
        {
            var value = format?.Trim().TrimStart('.').ToLowerInvariant();
            return value == SRT || value == ASS || value == SSA ? value : null;
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
        private async Task<string> GetCidByFileAsync(string filePath, CancellationToken cancellationToken)
        {
            // 修改人: Meiam
            // 修改时间: 2026-06-11
            // 备注: 增加网络路径拦截，避免 FileStream 打开非本地路径抛出异常
            if (string.IsNullOrEmpty(filePath) ||
                filePath.Contains("://") ||
                filePath.StartsWith("smb://", StringComparison.OrdinalIgnoreCase) ||
                filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Network path detected, skip hash calculation: {Path}", filePath);
                return string.Empty;
            }

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                var fileSize = new FileInfo(filePath).Length;
                using (var sha1 = SHA1.Create())
                {
                    var buffer = new byte[0xf000];
                    if (fileSize < 0xf000)
                    {
                        await stream.ReadExactlyAsync(buffer, 0, (int)fileSize, cancellationToken);
                        buffer = sha1.ComputeHash(buffer, 0, (int)fileSize);
                    }
                    else
                    {
                        await stream.ReadExactlyAsync(buffer, 0, 0x5000, cancellationToken);
                        stream.Seek(fileSize / 3, SeekOrigin.Begin);
                        await stream.ReadExactlyAsync(buffer, 0x5000, 0x5000, cancellationToken);
                        stream.Seek(fileSize - 0x5000, SeekOrigin.Begin);
                        await stream.ReadExactlyAsync(buffer, 0xa000, 0x5000, cancellationToken);

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
