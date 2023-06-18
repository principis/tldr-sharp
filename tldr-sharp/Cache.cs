/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Globalization;
using System.IO;
using System.Net;
using Mono.Data.Sqlite;

namespace tldr_sharp
{
    internal static class Cache
    {
        internal static void Check()
        {
            if (File.Exists(Config.DbPath)) return;

            CustomConsole.WriteWarning("Database not found.");
            Updater.Update();
        }

        internal static void Clear()
        {
            if (File.Exists(Config.CachePath)) File.Delete(Config.CachePath);

            var cacheDir = new DirectoryInfo(Config.CachePath);
            if (cacheDir.Exists) cacheDir.Delete(true);
            cacheDir.Create();
        }

        internal static void DownloadPage(Page page)
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
            using var conn = new SqliteConnection("Data Source=" + Config.DbPath + ";");
            conn.Open();

            using var command = new SqliteCommand("SELECT value FROM config WHERE parameter = @parameter", conn);
            command.Parameters.Add(new SqliteParameter("@parameter", "last-update"));

            using SqliteDataReader reader = command.ExecuteReader();
            reader.Read();
            return DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture);
        }
    }
}