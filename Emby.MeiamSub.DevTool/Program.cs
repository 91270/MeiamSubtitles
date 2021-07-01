using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Emby.Subtitle.DevTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(ComputeFileHash($"X:\\Favorites\\Movie\\八佰 (2020)\\八佰 (2020) 1080p TrueHD.mkv"));
            //Console.WriteLine(ComputeFileHash($"D:\\Documents\\Downloads\\testidx.avi"));
            Console.ReadKey();
        }

        private static string GetCid(string filePath)
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

        public static string ComputeFileHash(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            string ret = "";

            if (!fileInfo.Exists || fileInfo.Length < 8 * 1024)
            {
                return ret;
            }

            FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);

            long[] offset = new long[4];
            offset[3] = fileInfo.Length - 8 * 1024;
            offset[2] = fileInfo.Length / 3;
            offset[1] = fileInfo.Length / 3 * 2;
            offset[0] = 4 * 1024;

            byte[] bBuf = new byte[1024 * 4];

            for (int i = 0; i < 4; ++i)
            {
                fs.Seek(offset[i], 0);
                fs.Read(bBuf, 0, 4 * 1024);

                MD5 md5Hash = MD5.Create();
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

            fs.Close();

            return ret;
        }
    }
}
