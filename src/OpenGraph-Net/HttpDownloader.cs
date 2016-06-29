﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenGraph_Net
{
    // http://stackoverflow.com/a/2700707
    /// <summary>
    /// 
    /// </summary>
    public class HttpDownloader
    {
        private readonly string _referer;
        private readonly string _userAgent;

        /// <summary>
        /// 
        /// </summary>
        public Encoding Encoding { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public WebHeaderCollection Headers { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="referer"></param>
        /// <param name="userAgent"></param>
        public HttpDownloader(Uri url, string referer, string userAgent)
        {
            Encoding = Encoding.GetEncoding("ISO-8859-1");
            Url = url;
            _userAgent = userAgent;
            _referer = referer;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="referer"></param>
        /// <param name="userAgent"></param>
        public HttpDownloader(string url, string referer, string userAgent) : this(new Uri(url), referer, userAgent)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetPage()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            if (!string.IsNullOrEmpty(_referer))
                request.Referer = _referer;
            if (!string.IsNullOrEmpty(_userAgent))
                request.UserAgent = _userAgent;

            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Headers = response.Headers;
                Url = response.ResponseUri;
                return ProcessContent(response);
            }

        }

        private string ProcessContent(HttpWebResponse response)
        {
            SetEncodingFromHeader(response);

            Stream s = response.GetResponseStream();
            if (response.ContentEncoding.ToLower().Contains("gzip"))
                s = new GZipStream(s, CompressionMode.Decompress);
            else if (response.ContentEncoding.ToLower().Contains("deflate"))
                s = new DeflateStream(s, CompressionMode.Decompress);

            MemoryStream memStream = new MemoryStream();
            int bytesRead;
            byte[] buffer = new byte[0x1000];
            for (bytesRead = s.Read(buffer, 0, buffer.Length); bytesRead > 0; bytesRead = s.Read(buffer, 0, buffer.Length))
            {
                memStream.Write(buffer, 0, bytesRead);
            }
            s.Close();
            string html;
            memStream.Position = 0;
            using (StreamReader r = new StreamReader(memStream, Encoding))
            {
                html = r.ReadToEnd().Trim();
                html = CheckMetaCharSetAndReEncode(memStream, html);
            }

            return html;
        }

        private void SetEncodingFromHeader(HttpWebResponse response)
        {
            string charset = null;
            if (string.IsNullOrEmpty(response.CharacterSet))
            {
                Match m = Regex.Match(response.ContentType, @";\s*charset\s*=\s*(?<charset>.*)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    charset = m.Groups["charset"].Value.Trim(new[] { '\'', '"' });
                }
            }
            else
            {
                charset = response.CharacterSet;
            }
            if (!string.IsNullOrEmpty(charset))
            {
                try
                {
                    Encoding = Encoding.GetEncoding(charset);
                }
                catch (ArgumentException)
                {
                }
            }
        }

        private string CheckMetaCharSetAndReEncode(Stream memStream, string html)
        {
            try
            {
                var m = new Regex(@"<meta\s+.*?charset\s*=\s*(?<charset>[A-Za-z0-9_-]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(html);
                var charset = m.Success ? m.Groups["charset"].Value.ToLower() : GetCharsetFrom(html);

                if (string.IsNullOrWhiteSpace(charset)) return html;

                if ((charset == "unicode") || (charset == "utf-16")) charset = "utf-8";
            
                var metaEncoding = Encoding.GetEncoding(charset);
                if (Encoding != metaEncoding)
                {
                    memStream.Position = 0L;
                    var recodeReader = new StreamReader(memStream, metaEncoding);
                    html = recodeReader.ReadToEnd().Trim();
                    recodeReader.Close();
                }
            }
            catch (ArgumentException)
            {
            }

            return html;
        }

        private string GetCharsetFrom(string strWebPage)
        {
            if (strWebPage == null) return null;

            const string charsetSearchedKey = "charset=\"";
            var charsetStart = strWebPage.IndexOf(charsetSearchedKey, StringComparison.Ordinal);

            if (charsetStart <= 0) return null;

            charsetStart += charsetSearchedKey.Length;
            var charsetEnd = strWebPage.IndexOfAny(new[] { ' ', '\"', ';' }, charsetStart);
            return strWebPage.Substring(charsetStart, charsetEnd - charsetStart);
        }
    }
}
