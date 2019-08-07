using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using Mono.Data.Sqlite;
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
            Console.WriteLine("Updating cache...");

            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            if (Directory.Exists(tmpPath)) Directory.Delete(tmpPath, true);
            
            try {
                DownloadPages(Remote, tmpPath);
            } catch (WebException eRemote) {
                Console.WriteLine($"[ERROR] {eRemote.Message}");

                Console.WriteLine("Trying alternative url...");

                try {
                    DownloadPages(AlternativeRemote, tmpPath);
                } catch (WebException eAlternative) {
                    Console.WriteLine($"[ERROR] {eAlternative.Message}");
                    if (eRemote.Response is HttpWebResponse response &&
                        response.StatusCode == HttpStatusCode.Forbidden) {
                        Console.WriteLine("Please try to set the Cloudflare cookie and user-agent. " +
                                          "See https://github.com/principis/tldr-sharp/wiki/403-when-updating-cache.");
                    } else {
                        Console.Write("[ERROR] Please make sure you have a functioning internet connection. ");
                    }

                    Environment.Exit(1);
                    return;
                }
            }


            Cache.Clear();

            using (Stream stream = File.OpenRead(tmpPath))
            using (IReader reader = ReaderFactory.Open(stream)) {
                while (reader.MoveToNextEntry()) {
                    if (!reader.Entry.IsDirectory) {
                        reader.WriteEntryToDirectory(Program.CachePath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }

            File.Delete(tmpPath);
            CreateIndex();

            Console.WriteLine("Cache updated.");
        }

        private static void DownloadPages(string url, string tmpPath)
        {
            using (var client = new WebClient()) {
                client.Headers.Add("user-agent", Program.UserAgent);
                if (Environment.GetEnvironmentVariable("TLDR_COOKIE") != null) {
                    client.Headers.Add(HttpRequestHeader.Cookie, Environment.GetEnvironmentVariable("TLDR_COOKIE"));
                }

                client.DownloadFile(url, tmpPath);
            }
        }

        private static void CreateIndex()
        {
            Console.WriteLine("Creating index...");
            var cacheDir = new DirectoryInfo(Program.CachePath);

            SqliteConnection.CreateFile(Program.DbPath);

            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand(conn))
                using (SqliteTransaction transaction = conn.BeginTransaction()) {
                    command.CommandText =
                        "CREATE TABLE pages (name VARCHAR(100), platform VARCHAR(10), lang VARCHAR(7), local INTEGER)";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE TABLE config (parameter VARCHAR(20), value VARCHAR(100))";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO config (parameter, value) VALUES(@parameter, @value)";
                    command.Parameters.AddWithValue("@parameter", "last-update");
                    command.Parameters.AddWithValue("@value",
                        DateTime.UtcNow.Date.ToString(CultureInfo.InvariantCulture));
                    command.ExecuteNonQuery();
                    command.Transaction = transaction;
                    command.CommandType = CommandType.Text;

                    // Create indexes
                    command.CommandText = "CREATE INDEX names_index ON pages (name, platform, lang, local)";
                    command.ExecuteNonQuery();
                    command.CommandText = "CREATE INDEX lang_platform_index ON pages (lang, platform, name, local)";
                    command.ExecuteNonQuery();
                    command.CommandText = "CREATE INDEX platform_index ON pages (platform)";
                    command.ExecuteNonQuery();
                    command.CommandText = "CREATE INDEX lang_index ON pages (lang)";
                    command.ExecuteNonQuery();

                    // Add pages
                    command.CommandText =
                        "INSERT INTO pages (name, platform, lang, local) VALUES(@name, @platform, @lang, @local)";
                    var preferredLanguages = Index.GetEnvLanguages();

                    foreach (DirectoryInfo dir in cacheDir.EnumerateDirectories("*pages*")) {
                        string lang = "en-US";
                        bool isLocal = true;
                        if (dir.Name.Contains(".")) lang = dir.Name.Split('.')[1];

                        if (lang != Program.DefaultLanguage &&
                            preferredLanguages.All(x => lang.Substring(0, 2) != x.Substring(0, 2))) {
                            isLocal = false;
                        }

                        foreach (DirectoryInfo osDir in dir.EnumerateDirectories()) {
                            foreach (FileInfo file in osDir.EnumerateFiles("*.md", SearchOption.AllDirectories)) {
                                command.Parameters.AddWithValue("@name",
                                    Path.GetFileNameWithoutExtension(file.Name));
                                command.Parameters.AddWithValue("@platform", osDir.Name);
                                command.Parameters.AddWithValue("@lang", lang);
                                command.Parameters.AddWithValue("@local", isLocal);
                                command.ExecuteNonQuery();
                            }
                        }

                        if (lang != Program.DefaultLanguage && !preferredLanguages.Contains(lang)) {
                            dir.Delete(true);
                        }
                    }

                    transaction.Commit();
                }
            }
        }
    }
}