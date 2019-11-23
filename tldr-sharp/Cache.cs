using System;
using System.Collections.Generic;
using System.Data;
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
            
            Console.WriteLine("Database not found. ");
            Updater.Update();
        }

        internal static void CreateSchema()
        {
            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();

                using (SqliteTransaction transaction = conn.BeginTransaction()) 
                using (var command = new SqliteCommand(conn)) {
                    command.Transaction = transaction;
                    command.CommandType = CommandType.Text;
                    
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

                    // Create indexes
                    command.CommandText = "CREATE INDEX os_names ON pages (platform, name)";
                    command.ExecuteNonQuery();
                    command.CommandText = "CREATE INDEX lang_names ON pages (lang, name)";
                    command.ExecuteNonQuery();
                    command.CommandText = "CREATE INDEX names_index ON pages (lang, platform, name)";
                    command.ExecuteNonQuery();
                
                    transaction.Commit();
                }
            }
        }

        internal static void Clear()
        {
            DirectoryInfo cacheDir;
            try {
                if (File.Exists(Program.CachePath)) File.Delete(Program.CachePath);

                cacheDir = new DirectoryInfo(Program.CachePath);
                
                if (cacheDir.Exists) {
                    cacheDir.Delete(true);
                }
            } catch (Exception e) {
                Console.WriteLine("[ERROR] {0}", e.Message);
                Environment.Exit(1);
                return;
            }

            cacheDir.Create();
        }

        internal static void DownloadPage(Page page)
        {
            var pageFile = new FileInfo(page.GetPath());
            Directory.CreateDirectory(pageFile.DirectoryName ?? throw new ArgumentException());
            
            using (var client = new WebClient()) {
                client.Headers.Add("user-agent", Program.UserAgent);

                
                string language = page.Language == Program.DefaultLanguage ? string.Empty : $".{page.Language}";
                
                string data = client.DownloadString($"{Remote}{language}/{page.Platform}/{page.Name}.md");
                using (StreamWriter sw = pageFile.CreateText()) {
                    sw.WriteLine(data);
                }
            }
            
            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();
                using (SqliteCommand command = conn.CreateCommand()) {
                    command.CommandText =
                        "UPDATE pages SET local = TRUE WHERE name = @name AND lang = @lang AND platform = @platform";
                    command.Parameters.Add(new SqliteParameter("@name", page.Name));
                    command.Parameters.Add(new SqliteParameter("@platform", page.Platform));
                    command.Parameters.Add(new SqliteParameter("@lang", page.Language));

                    command.ExecuteNonQuery();
                }
            }
        }

        internal static DateTime LastUpdate()
        {
            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();

                using (var command = new SqliteCommand("SELECT value FROM config WHERE parameter = @parameter", conn)) {
                    command.Parameters.Add(new SqliteParameter("@parameter", "last-update"));

                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        reader.Read();
                        return DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture);
                    }
                }
            }
        }
    }
}