using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using Mono.Options;
using System.IO;
using System.Linq;
using System.Net;
using NaturalSort.Extension;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace tldr_sharp
{
    internal class Program
    {
        private static readonly string CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache");
        private static readonly string DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache", "index.sqlite");

        
        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool update = false;

            OptionSet options = new OptionSet
            {
                {
                    "h|help", "Display this help text.",
                    v => showHelp = v != null
                },
                {
                    "l|list", "Show all pages for the current platform",
                    v => ListAll(false)
                },
                {
                    "a|list-all", "Show all pages",
                    v => ListAll(true)
                },
                {
                    "u|update", "Update the local cache.",
                    v => update = true
                },
                {
                    "c|clear-cache", "Clear the local cache.",
                    v => ClearCache()
                }
            };
            
            var extra = options.Parse(args);
            
            if (showHelp || args.Length == 0)
            {
                ShowHelp(options);
                return;
            }
            if (update)
            {
                Update();
                return;
            }

            string page = "";
            foreach (string arg in extra)
            {
                if (arg.StartsWith("-"))
                {
                    if (page.Equals("")) Console.WriteLine("error: unknown option '{0}'", arg);
                    break;
                }
                page += $" {arg}";
            }
            if (page.Trim().Length > 0) GetPage(page);
        }

        private static string GetOs()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return "osx";
                case PlatformID.Unix:
                    return "linux";
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    return "windows";
                default:
                    return "common";
            }
        }

        private static void GetPage(string page)
        {
            page = page.TrimStart();
            CheckDb();
            
            SqliteConnection conn = new SqliteConnection("Data Source=" + DbPath + ";");
            conn.Open();
            SqliteCommand command = new SqliteCommand("SELECT path FROM pages WHERE name = @name AND lang = @lang AND (os = @os OR os = 'common')", conn);
            command.Parameters.Add(new SqliteParameter("@name", page));
            command.Parameters.Add(new SqliteParameter("@os", GetOs()));
            command.Parameters.Add(new SqliteParameter("@lang", "en"));

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                Console.Write("Page not found. ");
                Update();
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Page not found.\nFeel free to send a pull request to: https://github.com/tldr-pages/tldr");
                    return;
                }
            }

            reader.Read();
            string path = reader.GetString(0);

            foreach (string line in File.ReadLines(path))
            {
                string curLine = line;
                if (line.Length == 0)
                {
                    continue;
                }

                if (line.Contains("{{"))
                {
                    curLine = line.Replace("{{", "\x1b[32m").Replace("}}", "\x1b[31m");
                }
                switch (curLine[0])
                {
                    case '#':
                       Console.WriteLine("\x1B[4m\x1b[1m"+ curLine.Substring(2) + "\x1b[0m\n");
                       break;
                    case '>':
                       Console.WriteLine("\x1b[1m" + curLine.Substring(2) + "\x1b[0m");
                       break;
                    case '-':
                       Console.WriteLine("\x1b[39m\n" + curLine);
                       break;
                    case '`':
                       Console.WriteLine("  \x1b[31m" + curLine.Trim('`'));
                       break;
                    default:
                       Console.WriteLine(curLine);
                       break;
                }
            }
        }

        private static void CheckDb()
        {
            if (File.Exists(DbPath)) return;
            Console.Write("Database not found. ");
            Update();
        }

        private static void ListAll(bool ignorePlatform)
        {
            CheckDb();
            SqliteConnection conn = new SqliteConnection("Data Source=" + DbPath + ";");
            conn.Open();
            SqliteCommand command = conn.CreateCommand();
            if (ignorePlatform)
            {
                command.CommandText = "SELECT name FROM pages WHERE lang = @lang";
            }
            else
            {
                command.CommandText = "SELECT name FROM pages WHERE lang = @lang AND (os = @os OR os = 'common')";
                command.Parameters.Add(new SqliteParameter("@os", GetOs()));
            }
            
            command.Parameters.Add(new SqliteParameter("@lang", "en"));
            
            var reader = command.ExecuteReader();
            var results = new List<string>();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }

            Console.WriteLine(string.Join(", ", results.OrderBy(x => x, StringComparer.Ordinal.WithNaturalSort())));
        }
        

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: tldr command [options]\n");
            Console.WriteLine("Simplified and community-driven man pages\n");
            p.WriteOptionDescriptions(Console.Out);
        }


        private static void Update()
        {
            Console.WriteLine("Updating cache...");
            
            Directory.CreateDirectory(CachePath);
            var cacheDir = new DirectoryInfo(CachePath);
            
            foreach (var file in cacheDir.EnumerateFiles())
            {
                file.Delete(); 
            }
            foreach (var dir in cacheDir.EnumerateDirectories())
            {
                dir.Delete(true); 
            }

            string tmpPath = Path.Combine(Path.GetTempPath(), "tldr");
            string zipPath = Path.Combine(tmpPath, "tldr.zip");
            
            Directory.CreateDirectory(tmpPath);
            
           
            using (var client = new WebClient())
            {
                client.DownloadFile("https://tldr-pages.github.io/assets/tldr.zip", zipPath);
            }
            
            using (Stream stream = File.OpenRead(zipPath))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(CachePath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
            
            File.Delete(zipPath);
            UpdateIndex();
        }

        private static void UpdateIndex()
        {
            Console.WriteLine("Updating index...");
            DirectoryInfo cacheDir = new DirectoryInfo(CachePath);
           
            SqliteConnection.CreateFile(DbPath);
            
            SqliteConnection conn = new SqliteConnection("Data Source=" + DbPath + ";");
            conn.Open();
            SqliteCommand command = new SqliteCommand("CREATE TABLE pages (name VARCHAR(100), path TEXT, os VARCHAR(10), lang VARCHAR(2))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
            SqliteTransaction transaction = conn.BeginTransaction();
            command = conn.CreateCommand();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.CommandText = "INSERT INTO pages (name, path, os, lang) VALUES(@name, @path, @os, @lang)";
            
            foreach (var dir in cacheDir.EnumerateDirectories("*pages*"))
            {
                string lang = "en";
                if (dir.Name.Contains(".")) lang = dir.Name.Split('.')[1];
                foreach (var osDir in dir.EnumerateDirectories())
                {
                    string os = osDir.Name;
                    foreach (var file in osDir.EnumerateFiles("*.md", SearchOption.AllDirectories))
                    {
                        command.Parameters.AddWithValue("@name", Path.GetFileNameWithoutExtension(file.Name));
                        command.Parameters.AddWithValue("@path", file.FullName);
                        command.Parameters.AddWithValue("@os", os);
                        command.Parameters.AddWithValue("@lang", lang);
                        command.ExecuteNonQuery();
                    }
                }
            }
            
            transaction.Commit();
            command.Dispose();
            conn.Close();
            Console.WriteLine("Cache updated.");
        }

        private static void ClearCache()
        {
            Console.WriteLine("Clearing cache...");
            if (Directory.Exists(CachePath))
            {
                var cacheDir = new DirectoryInfo(CachePath);
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