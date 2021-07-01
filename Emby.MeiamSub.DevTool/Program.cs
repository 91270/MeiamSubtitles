using System;
using System.IO;
using System.Security.Cryptography;

namespace Emby.Subtitle.DevTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(getCid($"X:\\Download\\冰路营救 (2021)\\冰路营救 (2021) 1080p AC3.mkv"));
            Console.ReadKey();
        }

        private static string getCid(string filePath)
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
                result += String.Format("{0:X2}", i);
            }
            return result;
        }
    }
}
