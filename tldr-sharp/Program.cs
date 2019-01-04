using System;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Mono.Data.Sqlite;
using Mono.Options;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NaturalSort.Extension;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace tldr_sharp
{
    internal class Program
    {
        private static readonly string CachePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache");
        private static readonly string DbPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache",
                "index.sqlite");
        private static readonly string Language = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
        
        public static int Main(string[] args)
        {
            bool showHelp = false;
            bool update = false;
            
            bool list = false;
            bool ignorePlatform = false;
            bool singleColumn = false;

            string language = null;
            string os = null;

            bool markdown = false;
            string render = null;
            
            OptionSet options = new OptionSet
            {
                "Usage: tldr command [options]\n",
                "Simplified and community-driven man pages\n",
                {
                    "a|list-all", "List all pages",
                    a => list = ignorePlatform = a != null
                },
                {
                    "c|clear-cache", "Clear the local cache",
                    c => ClearCache()
                },
                {
                    "f=|render=", "Render a specific markdown file",
                    v => render = v
                },
                {
                    "h|help", "Display this help text",
                    h => showHelp = h != null
                },
                {
                    "l|list", "List all pages for the current platform and language",
                    l => list = l != null
                },
                {
                    "list-os", "List all OS's",
                    o => Console.WriteLine(string.Join("\n", ListOs()))
                },
                {
                    "list-languages", "List all languages",
                    la => Console.WriteLine(string.Join("\n",
                        ListLanguages().Select(x =>
                        {
                            try
                            {
                                return x + ": " + CultureInfo.GetCultureInfo(x).EnglishName;
                            }
                            catch (CultureNotFoundException)
                            {
                                return null;
                            }
                        }).SkipWhile(x => x == null)))
                },
                {
                    "lang=", "Override the default language",
                    la => language = la
                },
                {
                    "m|markdown", "Show the markdown source of a page",
                    v => markdown = v != null
                },
                {
                    "os=", "Override the default OS",
                    o => os = o
                },
                {
                    "u|update", "Update the local cache",
                    u => update = true
                },
                {
                    "V|version", "Show version information",
                    v =>
                    {
                        Console.WriteLine("tldr-sharp " + Assembly.GetExecutingAssembly().GetName().Version);
                        Environment.Exit(0);
                    }
                },
                {
                    "1", "List all pages in single column",
                    a => singleColumn = a != null, true
                }
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
            if (showHelp || args.Length == 0)
            {
                options.WriteOptionDescriptions(Console.Out);
                return args.Length == 0 ? 1 : 0;
            }
            if (list)
            {
                ListAll(ignorePlatform, singleColumn, language, os);
                return 0;
            }
            if (update)
            {
                Update();
                return 0;
            }
            if (render != null)
            {
                return Render(render);
            }
            
            string page = "";
            foreach (string arg in extra)
            {
                if (arg.StartsWith("-"))
                {
                    if (page.Equals("")) Console.WriteLine("error: unknown option '{0}'", arg);
                    return 1;
                }
                page += $" {arg}";
            }
            return page.Trim().Length > 0 ? GetPage(page, language, os, markdown) : 0;
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

        private static IEnumerable<string> ListOs()
        {
            CheckDb();
            SqliteConnection conn = new SqliteConnection("Data Source=" + DbPath + ";");
            conn.Open();
            SqliteCommand command = new SqliteCommand("SELECT DISTINCT os FROM pages", conn);
            var reader = command.ExecuteReader();
           
            SortedSet<string> os = new SortedSet<string>();
            while (reader.Read()) os.Add(reader.GetString(0));
            reader.Dispose();
            conn.Dispose();

            return os;
        }

        private static string GetLanguage()
        {
            var languages = ListLanguages();
            return !languages.Contains(Language) ? "en" : Language;
        }

        private static int GetPage(string page, string language = null, string os = null, bool markdown = false)
        {
            page = page.TrimStart();
            CheckDb();
            
            SqliteConnection conn = new SqliteConnection("Data Source=" + DbPath + ";");
            conn.Open();
            SqliteCommand command = new SqliteCommand(
                "SELECT path FROM pages WHERE name = @name AND lang = @lang AND (os = @os OR os = 'common')",
                conn);
            command.Parameters.Add(new SqliteParameter("@name", page));
            command.Parameters.Add(new SqliteParameter("@os", os ?? GetOs()));
            command.Parameters.Add(new SqliteParameter("@lang", language ?? GetLanguage()));

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                Console.Write("Page not found. ");
                Update();
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                {
                    Console.WriteLine(
                        "Page not found.\nFeel free to send a pull request to: https://github.com/tldr-pages/tldr");
                    return 2;
                }
            }

            reader.Read();
            string path = reader.GetString(0);

            if (markdown)
                Console.WriteLine(File.ReadAllText(path));
            else
                return Render(path);
            return 0;
        }

        private static int Render(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }
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
                        Console.WriteLine("\x1B[4m\x1b[1m" + curLine.Substring(2) + "\x1b[0m\n");
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

            return 0;
        }

        private static void CheckDb()
        {
            if (File.Exists(DbPath)) return;
            Console.Write("Database not found. ");
            Update();
        }

        private static void ListAll(bool ignorePlatform, bool singleColumn, string language = null, string os = null)
        {
            CheckDb();
            SqliteConnection conn = new SqliteConnection("Data Source=" + DbPath + ";");
            conn.Open();
            SqliteCommand command = conn.CreateCommand();
            if (ignorePlatform)
                command.CommandText = "SELECT name FROM pages WHERE lang = @lang";
            else
            {
                command.CommandText = "SELECT name FROM pages WHERE lang = @lang AND (os = @os OR os = 'common')";
                command.Parameters.Add(new SqliteParameter("@os", os ?? GetOs()));
            }
            command.Parameters.Add(new SqliteParameter("@lang", language ?? GetLanguage()));
            
            var reader = command.ExecuteReader();
            var results = new List<string>();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }

            Console.WriteLine(singleColumn
                ? string.Join("\n", results.OrderBy(x => x, StringComparer.Ordinal.WithNaturalSort()))
                : string.Join(", ", results.OrderBy(x => x, StringComparer.Ordinal.WithNaturalSort())));
        }


        private static SortedSet<string> ListLanguages()
        {
            CheckDb();
            SqliteConnection conn = new SqliteConnection("Data Source=" + DbPath + ";");
            conn.Open();
            SqliteCommand command = new SqliteCommand("SELECT DISTINCT lang FROM pages", conn);
            var reader = command.ExecuteReader();
           
            SortedSet<string> languages = new SortedSet<string>();
            while (reader.Read()) languages.Add(reader.GetString(0));
            reader.Dispose();
            conn.Dispose();

            return languages;

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
                client.DownloadFile("https://tldr.sh/assets/tldr.zip", zipPath);
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
            SqliteCommand command = new SqliteCommand(
                "CREATE TABLE pages (name VARCHAR(100), path TEXT, os VARCHAR(10), lang VARCHAR(2))",
                conn);
            
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