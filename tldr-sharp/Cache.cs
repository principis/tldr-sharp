/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.IO;
using System.Net;
using Spectre.Console;

namespace tldr_sharp
{
    internal static class Cache
    {
        internal static void Check()
        {
            if (File.Exists(Config.DbPath)) return;

            Cli.WriteWarningMessage("Database not found.");
            Updater.Update();
            Cli.WriteLine();
        }

        internal static void Clear()
        {
            AnsiConsole.Status().Start("Clearing page cache", Clear);
            Cli.WriteMessage("Page cache [green]cleared.[/]");
        }

        internal static void Clear(StatusContext ctx)
        {
            if (File.Exists(Config.CachePath)) File.Delete(Config.CachePath);

            var cacheDir = new DirectoryInfo(Config.CachePath);
            if (cacheDir.Exists) cacheDir.Delete(true);
            cacheDir.Create();
        }

        internal static void DownloadPage(Page page)
        {
            AnsiConsole.Status().Start($"Downloading {page.Name}...", ctx => DownloadPage(page, ctx));
            Cli.WriteMessage($"Page {page.Name} [green]downloaded.[/]");
        }

        internal static void DownloadPage(Page page, StatusContext ctx)
        {
            var pageFile = new FileInfo(page.GetPath());
            Directory.CreateDirectory(pageFile.DirectoryName ?? throw new ArgumentException());

            using (var client = new WebClient()) {
                client.Headers.Add("user-agent", Config.UserAgent);

                string language = page.DirLanguage;
                string data = client.DownloadString(address: $"{Config.RemoteUrl}{language}/{page.Platform}/{page.Name}.md");

                using StreamWriter sw = pageFile.CreateText();
                sw.WriteLine(data);
            }

            Index.SetPageAsDownloaded(page);
        }

        internal static DateTime LastUpdate()
        {
            return Index.LastUpdate();
        }
    }
}