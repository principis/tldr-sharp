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
using Spectre.Console;

namespace tldr_sharp
{
    internal static class Updater
    {
        internal static void Update()
        {
            AnsiConsole.Status().Start("Updating page cache", Update);
            Cli.WriteMessage("Page cache [green]updated.[/]");
        }

        internal static void Update(StatusContext ctx)
        {
            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            if (Directory.Exists(tmpPath)) Directory.Delete(tmpPath, true);

            ctx.Status("Updating page cache: [grey]Downloading pages[/]");
            try {
                DownloadPages(Config.ArchiveRemote, tmpPath);
            }
            catch (WebException eRemote) {
                try {
                    DownloadPages(Config.ArchiveAlternativeRemote, tmpPath);
                }
                catch (WebException eAlternative) {
                    Cli.WriteErrorMessage($"Downloading pages failed: {eAlternative.GetBaseException().Message}");

                    if (eRemote.Response is HttpWebResponse response &&
                        response.StatusCode == HttpStatusCode.Forbidden) {
                        Cli.WriteLine("Please try to set the Cloudflare cookie and user-agent. " +
                                              "See https://github.com/principis/tldr-sharp/wiki/403-when-updating-cache.");
                    }
                    else {
                        Cli.WriteLine("Please make sure you have a functioning internet connection.");
                    }

                    Environment.Exit(1);
                    return;
                }
            }

            try {
                ctx.Status("Updating page cache: [grey]Clearing cache[/]");
                Cache.Clear(ctx);
            }
            catch (Exception e) {
                Cli.WriteErrorMessage($"An error has occurred clearing the cache: {e.Message}{Environment.NewLine}");
                Environment.Exit(1);
                return;
            }

            ctx.Status("Updating page cache: [grey]Extracting pages[/]");

            using (Stream stream = File.OpenRead(tmpPath)) {
                using IReader reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry()) {
                    if (!reader.Entry.IsDirectory)
                        reader.WriteEntryToDirectory(Config.CachePath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                }
            }

            ctx.Status("Updating page cache: [grey]Creating index[/]");
            Index.Create(ctx);

            ctx.Status("Updating page cache: [grey]Optimizing cache[/]");
            File.Delete(tmpPath);
            CleanupCache();
        }

        private static void CleanupCache()
        {
            var cacheDir = new DirectoryInfo(Config.CachePath);

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
        }

        private static void DownloadPages(string url, string tmpPath)
        {
            using var client = new WebClient();
            client.Headers.Add("user-agent", Config.UserAgent);

            if (Environment.GetEnvironmentVariable("TLDR_COOKIE") != null)
                client.Headers.Add(HttpRequestHeader.Cookie, Environment.GetEnvironmentVariable("TLDR_COOKIE"));

            client.DownloadFile(url, tmpPath);
        }
    }
}