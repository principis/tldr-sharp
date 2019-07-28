using System;
using System.Data;
using System.IO;
using System.Net;
using Mono.Data.Sqlite;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace tldr_sharp
{
    internal static class Updater
    {
        internal static void Update()
        {
            Console.WriteLine("Updating cache...");

            Directory.CreateDirectory(Program.CachePath);
            var cacheDir = new DirectoryInfo(Program.CachePath);

            foreach (var file in cacheDir.EnumerateFiles())
            {
                file.Delete();
            }

            foreach (var dir in cacheDir.EnumerateDirectories())
            {
                dir.Delete(true);
            }

            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
            if (Directory.Exists(tmpPath)) Directory.Delete(tmpPath, true);

            using (var client = new WebClient())
            {
                client.DownloadFile(Program.PagesUrl, tmpPath);
            }

            using (Stream stream = File.OpenRead(tmpPath))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(Program.CachePath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }

            File.Delete(tmpPath);
            UpdateIndex();
        }

        private static void UpdateIndex()
        {
            Console.WriteLine("Updating index...");
            var cacheDir = new DirectoryInfo(Program.CachePath);

            SqliteConnection.CreateFile(Program.DbPath);

            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";"))
            {
                conn.Open();
                using (var command = new SqliteCommand(
                    "CREATE TABLE pages (name VARCHAR(100), platform VARCHAR(10), lang VARCHAR(7))", conn))
                {
                    command.ExecuteNonQuery();

                    using (var transaction = conn.BeginTransaction())
                    {
                        command.Transaction = transaction;
                        command.CommandType = CommandType.Text;

                        // Create indexes
                        command.CommandText = "CREATE INDEX os_names ON pages (platform, name)";
                        command.ExecuteNonQuery();
                        command.CommandText = "CREATE INDEX lang_names ON pages (lang, name)";
                        command.ExecuteNonQuery();
                        command.CommandText = "CREATE INDEX names_index ON pages (lang, platform, name)";
                        command.ExecuteNonQuery();

                        // Add pages
                        command.CommandText =
                            "INSERT INTO pages (name, platform, lang) VALUES(@name, @platform, @lang)";

                        foreach (var dir in cacheDir.EnumerateDirectories("*pages*"))
                        {
                            var lang = "en-US";
                            if (dir.Name.Contains(".")) lang = dir.Name.Split('.')[1];

                            foreach (var osDir in dir.EnumerateDirectories())
                            {
                                foreach (var file in osDir.EnumerateFiles("*.md", SearchOption.AllDirectories))
                                {
                                    command.Parameters.AddWithValue("@name",
                                        Path.GetFileNameWithoutExtension(file.Name));
                                    command.Parameters.AddWithValue("@platform", osDir.Name);
                                    command.Parameters.AddWithValue("@lang", lang);
                                    command.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                    }
                }
            }

            Console.WriteLine("Cache updated.");
        }

        internal static void ClearCache()
        {
            Console.WriteLine("Clearing cache...");
            if (Directory.Exists(Program.CachePath))
            {
                var cacheDir = new DirectoryInfo(Program.CachePath);
                foreach (var file in cacheDir.EnumerateFiles())
                {
                    file.Delete();
                }

                foreach (var dir in cacheDir.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }

            Console.WriteLine("Cache cleared.");
        }
    }
}