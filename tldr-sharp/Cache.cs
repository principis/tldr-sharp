using System;
using System.Globalization;
using System.IO;
using System.Net;
using Mono.Data.Sqlite;

namespace tldr_sharp
{
    internal static class Cache
    {
        private const string Remote = "https://raw.githubusercontent.com/tldr-pages/tldr/master/pages";

        internal static void Check()
        {
            if (File.Exists(Program.DbPath)) return;

            CustomConsole.WriteWarning("Database not found.");
            Updater.Update();
        }

        internal static void Clear()
        {
            if (File.Exists(Program.CachePath)) File.Delete(Program.CachePath);

            var cacheDir = new DirectoryInfo(Program.CachePath);
            if (cacheDir.Exists) cacheDir.Delete(true);
            cacheDir.Create();
        }

        internal static void DownloadPage(Page page)
        {
            var pageFile = new FileInfo(page.GetPath());
            Directory.CreateDirectory(pageFile.DirectoryName ?? throw new ArgumentException());

            using (var client = new WebClient()) {
                client.Headers.Add("user-agent", Program.UserAgent);

                string language = page.DirLanguage;
                string data = client.DownloadString(address: $"{Remote}{language}/{page.Platform}/{page.Name}.md");

                using StreamWriter sw = pageFile.CreateText();
                sw.WriteLine(data);
            }

            Index.SetPageAsDownloaded(page);
        }

        internal static DateTime LastUpdate()
        {
            using var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";");
            conn.Open();

            using var command = new SqliteCommand("SELECT value FROM config WHERE parameter = @parameter", conn);
            command.Parameters.Add(new SqliteParameter("@parameter", "last-update"));

            using SqliteDataReader reader = command.ExecuteReader();
            reader.Read();
            return DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture);
        }
    }
}