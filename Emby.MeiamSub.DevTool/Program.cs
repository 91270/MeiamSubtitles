using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Emby.Subtitle.DevTool
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            // 设置控制台编码为 UTF8 防止中文乱码
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("================ MeiamSubtitles 调试工具 ================");

            // 待测试的影音文件路径
            var testFilePath = @"D:\Source\MeiamSubtitles\TestServer\Movie\2009\三傻大闹宝莱坞\三傻大闹宝莱坞 (2009) - 1080p.mkv";

            if (!File.Exists(testFilePath))
            {
                Console.WriteLine($"[错误] 文件不存在: {testFilePath}");
                return;
            }

            Console.WriteLine($"[文件] {testFilePath}");
            Console.WriteLine("-------------------------------------------------------");

            // 1. 射手网 (Shooter)
            Console.WriteLine("\n[1/2] 正在请求：射手网 (Shooter)...");
            var shooterHash = ComputeShooterHash(testFilePath);
            Console.WriteLine($" > HASH: {shooterHash}");
            await TestShooterApi(testFilePath, shooterHash);

            // 2. 迅雷影音 (Thunder)
            Console.WriteLine("\n[2/2] 正在请求：迅雷影音 (Thunder)...");
            var thunderCid = await GetThunderCidAsync(testFilePath);
            Console.WriteLine($" > CID:  {thunderCid}");
            await TestThunderApi(testFilePath, thunderCid);

            Console.WriteLine("\n-------------------------------------------------------");
            Console.WriteLine("调试结束，按任意键退出...");
            Console.ReadKey();
        }

        #region API 测试方法

        private static async Task TestShooterApi(string filePath, string hash)
        {
            try
            {
                var url = "https://www.shooter.cn/api/subapi.php";
                var formData = new Dictionary<string, string>
                {
                    { "filehash", hash},
                    { "pathinfo", Path.GetFileName(filePath)},
                    { "format", "json"},
                    { "lang", "chn"}
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($" > STATUS: {response.StatusCode}");

                if (!result.Trim().StartsWith("["))
                {
                    Console.WriteLine($" > [警告] API 返回了非法内容 (可能已失效或乱码): {result}");
                }
                else
                {
                    Console.WriteLine($" > RETURN: {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" > ERROR: {ex.Message}");
            }
        }

        private static async Task TestThunderApi(string filePath, string cid)
        {
            try
            {
                // 迅雷搜索接口通常基于文件名，CID 用于后续匹配校验
                var fileName = Path.GetFileName(filePath);
                var url = $"https://api-shoulei-ssl.xunlei.com/oracle/subtitle?name={HttpUtility.UrlEncode(fileName)}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "MeiamSub.Thunder");

                var response = await _httpClient.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($" > STATUS: {response.StatusCode}");
                Console.WriteLine($" > RETURN: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" > ERROR: {ex.Message}");
            }
        }

        #endregion

        #region HASH 算法实现

        /// <summary>
        /// 射手网 HASH 算法
        /// </summary>
        public static string ComputeShooterHash(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists || fileInfo.Length < 8 * 1024) return "";

            using (FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read))
            {
                long[] offset = new long[4];
                offset[3] = fileInfo.Length - 8 * 1024;
                offset[2] = fileInfo.Length / 3;
                offset[1] = fileInfo.Length / 3 * 2;
                offset[0] = 4 * 1024;

                string ret = "";
                byte[] bBuf = new byte[4096];

                using (MD5 md5 = MD5.Create())
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        fs.Seek(offset[i], SeekOrigin.Begin);
                        fs.Read(bBuf, 0, 4096);
                        byte[] data = md5.ComputeHash(bBuf);
                        StringBuilder sBuilder = new StringBuilder();
                        for (int j = 0; j < data.Length; j++) sBuilder.Append(data[j].ToString("x2"));
                        if (!string.IsNullOrEmpty(ret)) ret += ";";
                        ret += sBuilder.ToString();
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// 迅雷 CID 算法 (基于 SHA1)
        /// </summary>
        public static async Task<string> GetThunderCidAsync(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
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
                    foreach (var i in buffer) result += string.Format("{0:X2}", i);
                    return result;
                }
            }
        }

        /// <summary>
        /// QQ 播放器 VUID 算法
        /// </summary>
        public static async Task<string> ComputeQQVuidAsync(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists || fileInfo.Length < 8 * 1024) return "";

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                long[] offsets = new long[3];
                offsets[0] = 0;
                offsets[1] = fileInfo.Length / 3;
                offsets[2] = fileInfo.Length - 8 * 1024;

                StringBuilder combinedMd5 = new StringBuilder();
                byte[] buffer = new byte[4096];

                using (var md5 = MD5.Create())
                {
                    foreach (var offset in offsets)
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        await fs.ReadAsync(buffer, 0, 4096);
                        byte[] hashBytes = md5.ComputeHash(buffer);
                        foreach (byte b in hashBytes) combinedMd5.Append(b.ToString("x2"));
                    }
                    byte[] finalHash = md5.ComputeHash(Encoding.ASCII.GetBytes(combinedMd5.ToString()));
                    StringBuilder result = new StringBuilder();
                    foreach (byte b in finalHash) result.Append(b.ToString("x2"));
                    return result.ToString();
                }
            }
        }

        #endregion
    }
}
