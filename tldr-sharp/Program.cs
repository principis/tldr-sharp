using System;
using System.Data;
using Mono.Data.Sqlite;
using Mono.Options;
using System.IO;
using System.Net;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace tldr_sharp
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var showHelp = false;
            var update = false;

            var options = new OptionSet
            {
                {
                    "h|help", "Display this help text.",
                    v => showHelp = v != null
                },
                {
                    "u|update", "Update the local cache.",
                    v => update = true
                }
            };
            
            var extra = options.Parse(args);
            
            if (showHelp || extra.Count == 0)
            {
                ShowHelp(options);
                return;
            }
            if (update)
            {
                Update();
                return;
            }

            var page = "";
            foreach (var arg in extra)
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
            
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr-sharp", "cache", "index.sqlite");
            if (!File.Exists(dbPath))
            {
                Console.Write("Database not found. ");
                Update();
            }
            
            SqliteConnection conn = new SqliteConnection("Data Source=" + dbPath + ";");
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

            foreach (var line in File.ReadLines(path))
            {
                var curLine = line;
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
        

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: tldr command [options]\n");
            Console.WriteLine("Simplified and community-driven man pages\n");
            p.WriteOptionDescriptions(Console.Out);
        }


        private static void Update()
        {
            
            var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr-sharp", "cache");
           
            Console.WriteLine("Updating cache...");
            
            Directory.CreateDirectory(cachePath);
            var cacheDir = new DirectoryInfo(cachePath);
            
            foreach (var file in cacheDir.EnumerateFiles())
            {
                file.Delete(); 
            }
            foreach (var dir in cacheDir.EnumerateDirectories())
            {
                dir.Delete(true); 
            }

            var tmpPath = Path.Combine(Path.GetTempPath(), "tldr");
            var zipPath = Path.Combine(tmpPath, "tldr.zip");
            
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
                        reader.WriteEntryToDirectory(cachePath, new ExtractionOptions
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

            var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr-sharp", "cache");
            var cacheDir = new DirectoryInfo(cachePath);
            var dbPath = Path.Combine(cachePath, "index.sqlite");
           
            SqliteConnection.CreateFile(dbPath);
            
            var conn = new SqliteConnection("Data Source=" + dbPath + ";");
            conn.Open();
            var command = new SqliteCommand("CREATE TABLE pages (name VARCHAR(100), path TEXT, os VARCHAR(10), lang VARCHAR(2))", conn);
            command.ExecuteNonQuery();
            command.Dispose();
            var transaction = conn.BeginTransaction();
            command = conn.CreateCommand();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.CommandText = "INSERT INTO pages (name, path, os, lang) VALUES(@name, @path, @os, @lang)";
            
            foreach (var dir in cacheDir.EnumerateDirectories("*pages*"))
            {
                var lang = "en";
                if (dir.Name.Contains(".")) lang = dir.Name.Split('.')[1];
                foreach (var osDir in dir.EnumerateDirectories())
                {
                    var os = osDir.Name;
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
            Console.WriteLine("Finished.");
        }
    }
}