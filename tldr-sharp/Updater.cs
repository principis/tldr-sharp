/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using Spectre.Console;

namespace tldr_sharp
{
    internal static class Updater
    {
        internal static void Update()
        {
            AnsiConsole.Status().StartAsync("Updating page cache", Update).Wait();
            Cli.WriteMessage("Page cache [green]updated.[/]");
        }

        internal static async Task Update(StatusContext ctx)
        {
            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            if (Directory.Exists(tmpPath)) Directory.Delete(tmpPath, true);

            ctx.Status("Updating page cache: [grey]Downloading pages[/]");
            try {
                await DownloadPages(Config.ArchiveRemote, tmpPath);
            }
            catch (WebException e) {
                Cli.WriteErrorMessage($"Downloading pages failed: {e.GetBaseException().Message}");
                Environment.Exit(1);
                return;
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

            using (var archive = ZipArchive.Open(tmpPath)) {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory)) {
                    entry.WriteToDirectory(Config.CachePath, new ExtractionOptions()
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

        private static async Task DownloadPages(string url, string tmpPath)
        {
            await HttpUtils.DownloadFile(url, tmpPath);
        }
    }
}