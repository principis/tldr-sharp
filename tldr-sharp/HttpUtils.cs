/*
    SPDX-FileCopyrightText: 2023 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace tldr_sharp
{
    public static class HttpUtils
    {
        private static readonly HttpClient HttpClient;

        static HttpUtils()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("User-Agent", Config.UserAgent);


            if (Environment.GetEnvironmentVariable("TLDR_COOKIE") != null) {
                HttpClient.DefaultRequestHeaders.Add("Cookie", Environment.GetEnvironmentVariable("TLDR_COOKIE"));
            }
        }

        public static Task<Stream> GetStreamAsync(string uri)
        {
            return HttpClient.GetStreamAsync(uri);
        }

        public static async Task DownloadFile(string uri, string fileName)
        {
            await using Stream s = await HttpClient.GetStreamAsync(uri);
            await using var fs = new FileStream(fileName, FileMode.CreateNew);
            await s.CopyToAsync(fs);
        }

        public static Task<string> DownloadString(string uri)
        {
            return HttpClient.GetStringAsync(uri);
        }
    }
}