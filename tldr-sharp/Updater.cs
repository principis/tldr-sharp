/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
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
            try {
                ctx.Status("Updating page cache: [grey]Clearing cache[/]");
                Cache.Clear(ctx);
            }
            catch (Exception e) {
                Cli.WriteErrorMessage($"An error has occurred clearing the cache: {e.Message}{Environment.NewLine}");
                Environment.Exit(1);
                return;
            }

            ctx.Status("Updating page cache: [grey]Downloading pages[/]");

            try {
                await using var response = await HttpUtils.GetStreamAsync(Config.ArchiveRemote);
                using var reader = new ZipArchive(response);

                var extractPath = Path.GetFullPath(Config.CachePath);
                if (!extractPath.EndsWith(Path.DirectorySeparatorChar))
                    extractPath += Path.DirectorySeparatorChar.ToString();

                foreach (var entry in reader.Entries) {
                    var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                    if (!destinationPath.StartsWith(extractPath, StringComparison.Ordinal)) {
                        throw new IOException("Malicious zip file entry tries to extract outside cache");
                    }

                    if (Path.GetFileName(destinationPath).Length == 0) {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        entry.ExtractToFile(destinationPath);
                    }
                }
            }
            catch (Exception e) {
                Cli.WriteErrorMessage($"Downloading pages failed: {e.Message}");
                Environment.Exit(1);
                return;
            }

            ctx.Status("Updating page cache: [grey]Creating index[/]");
            Index.Create(ctx);

            ctx.Status("Updating page cache: [grey]Optimizing cache[/]");
            CleanupCache();
        }

        private static void CleanupCache()
        {
            var cacheDir = new DirectoryInfo(Config.CachePath);

            foreach (DirectoryInfo dir in cacheDir.EnumerateDirectories("pages*")) {
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