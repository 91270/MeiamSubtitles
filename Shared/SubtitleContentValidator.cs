using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MeiamSubtitles.Shared
{
    internal static class SubtitleContentValidator
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly Encoding StrictUtf16Le = new UnicodeEncoding(false, true, true);
        private static readonly Encoding StrictUtf16Be = new UnicodeEncoding(true, true, true);

        static SubtitleContentValidator()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static void Validate(byte[] data, string format, string mediaType)
        {
            DecodeAndValidate(data, format, mediaType);
        }

        public static byte[] ValidateAndConvertToUtf8(byte[] data, string format, string mediaType)
        {
            var text = DecodeAndValidate(data, format, mediaType);
            return Encoding.UTF8.GetBytes(text);
        }

        private static string DecodeAndValidate(byte[] data, string format, string mediaType)
        {
            if (data == null || data.Length < 8)
            {
                throw new InvalidDataException("Subtitle response is empty.");
            }

            if ((data[0] == (byte)'P' && data[1] == (byte)'K') ||
                (data.Length >= 7 && Encoding.ASCII.GetString(data, 0, 7) == "Rar!\u001a\u0007"))
            {
                throw new InvalidDataException("Compressed subtitle responses are not supported by Thunder.");
            }

            if (mediaType?.IndexOf("html", StringComparison.OrdinalIgnoreCase) >= 0 ||
                mediaType?.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidDataException("Thunder returned an error document instead of a subtitle.");
            }

            var errorDocument = false;
            foreach (var sample in DecodeSamples(data))
            {
                if (sample.StartsWith("<", StringComparison.Ordinal) ||
                    sample.StartsWith("{", StringComparison.Ordinal))
                {
                    errorDocument = true;
                    continue;
                }

                if (IsValidSubtitle(sample, format))
                {
                    return sample;
                }

                if (sample.StartsWith("[", StringComparison.Ordinal))
                {
                    errorDocument = true;
                }
            }

            if (errorDocument)
            {
                throw new InvalidDataException("Thunder returned an error document instead of a subtitle.");
            }

            throw new InvalidDataException($"Downloaded content is not a valid {format} subtitle.");
        }

        private static bool IsValidSubtitle(string sample, string format)
        {
            return format == "srt"
                ? sample.IndexOf("-->", StringComparison.Ordinal) >= 0
                : sample.IndexOf("[Script Info]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                  sample.IndexOf("Dialogue:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<string> DecodeSamples(byte[] data)
        {
            // Decode the complete subtitle because the selected text is returned to
            // the host after validation and must not be truncated to a probe window.
            var length = data.Length;

            if (data.Length >= 2)
            {
                if (data[0] == 0xFF && data[1] == 0xFE)
                {
                    yield return DecodeSample(data, length, StrictUtf16Le);
                    yield break;
                }

                if (data[0] == 0xFE && data[1] == 0xFF)
                {
                    yield return DecodeSample(data, length, StrictUtf16Be);
                    yield break;
                }
            }

            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                yield return DecodeSample(data, length, StrictUtf8);
                yield break;
            }

            // Thunder may return UTF-16 or GB18030 subtitles without a BOM. Decode
            // strictly so invalid UTF-8 cannot hide the actual legacy encoding.
            foreach (var encoding in new[] { StrictUtf8, StrictUtf16Le, StrictUtf16Be, GetGb18030Encoding() })
            {
                string sample;
                try
                {
                    sample = DecodeSample(data, length, encoding);
                }
                catch (DecoderFallbackException)
                {
                    continue;
                }

                yield return sample;
            }
        }

        private static Encoding GetGb18030Encoding()
        {
            return Encoding.GetEncoding(54936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }

        private static string DecodeSample(byte[] data, int length, Encoding encoding)
        {
            return encoding.GetString(data, 0, length)
                .TrimStart('\uFEFF', ' ', '\r', '\n', '\t');
        }
    }
}
