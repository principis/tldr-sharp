/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace tldr_sharp
{
    internal static class Updater
    {
        private const string Remote = "https://tldr.sh/assets/tldr.zip";

        private const string AlternativeRemote =
            "https://github.com/tldr-pages/tldr-pages.github.io/raw/master/assets/tldr.zip";

        internal static void Update()
        {
            var spinner = new CustomSpinner("Updating cache");

            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            if (Directory.Exists(tmpPath)) Directory.Delete(tmpPath, true);

            try {
                DownloadPages(Remote, tmpPath);
            }
            catch (WebException eRemote) {
                try {
                    DownloadPages(AlternativeRemote, tmpPath);
                }
                catch (WebException eAlternative) {
                    CustomConsole.WriteError(eAlternative.Message);

                    if (eRemote.Response is HttpWebResponse response &&
                        response.StatusCode == HttpStatusCode.Forbidden) {
                        Console.WriteLine("Please try to set the Cloudflare cookie and user-agent. " +
                                          "See https://github.com/principis/tldr-sharp/wiki/403-when-updating-cache.");
                    }
                    else {
                        Console.WriteLine("Please make sure you have a functioning internet connection.");
                    }

                    Environment.Exit(1);
                    return;
                }
            }

            try {
                Cache.Clear();
            }
            catch (Exception e) {
                CustomConsole.WriteError($"{e.Message}{Environment.NewLine}An error has occurred clearing the cache.");
                Environment.Exit(1);
                return;
            }

            using (Stream stream = File.OpenRead(tmpPath)) {
                using IReader reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry()) {
                    if (!reader.Entry.IsDirectory)
                        reader.WriteEntryToDirectory(Program.CachePath, new ExtractionOptions {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                }
            }

            File.Delete(tmpPath);
            spinner.Dispose();

            Index.Create();

            CleanupCache();
        }

        private static void CleanupCache()
        {
            var spinner = new CustomSpinner("Cleaning cache");

            var cacheDir = new DirectoryInfo(Program.CachePath);

            foreach (DirectoryInfo dir in cacheDir.EnumerateDirectories("*pages*")) {
                string lang = Locale.DefaultLanguage;
                if (dir.Name.Contains(".")) {
                    lang = dir.Name.Split('.')[1];
                }

                List<Page> pages = Index.QueryByLanguage(lang);

                if (pages.Count > 0 && pages[0].Local) {
                    continue;
                }

                dir.Delete(true);
            }

            spinner.Close();
        }

        private static void DownloadPages(string url, string tmpPath)
        {
            using var client = new WebClient();
            client.Headers.Add("user-agent", Program.UserAgent);

            if (Environment.GetEnvironmentVariable("TLDR_COOKIE") != null)
                client.Headers.Add(HttpRequestHeader.Cookie, Environment.GetEnvironmentVariable("TLDR_COOKIE"));

            client.DownloadFile(url, tmpPath);
        }
    }
}