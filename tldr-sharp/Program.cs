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
        private static readonly string Language = CultureInfo.CurrentCulture.Name;
        
        public static int Main(string[] args)
        {
            bool showHelp = false;
            bool update = false;
            
            bool list = false;
            bool ignorePlatform = false;

            string language = null;
            string platform = null;

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
                    o => Console.WriteLine(string.Join("\n", ListPlatform()))
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
                    "p=|platform=", "Override the default OS",
                    o => platform = o
                },
                {
                    "u|update", "Update the local cache.",
                    u => update = true
                },
                {
                    "self-update", "Check for tldr-sharp updates.",
                    u =>
                    {
                        SelfUpdate();
                        Environment.Exit(0);
                    }
                },
                {
                    "v|version", "Show version information.",
                    v =>
                    {
                        Console.WriteLine("tldr-sharp " + Assembly.GetExecutingAssembly().GetName().Version.Major + "." +
                                          Assembly.GetExecutingAssembly().GetName().Version.Minor + "." + 
                                          Assembly.GetExecutingAssembly().GetName().Version.MajorRevision + ", spec v1.0");
                        Environment.Exit(0);
                    }
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
                ListAll(ignorePlatform, language, platform);
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
            return page.Trim().Length > 0 ? GetPage(page, language, platform, markdown) : 0;
        }

        private static string GetPlatform()
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

        private static IEnumerable<string> ListPlatform()
        {
            CheckDb();
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";"))
            {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT platform FROM pages", conn))
                {
                    using (var reader = command.ExecuteReader())
                    {

                        SortedSet<string> platform = new SortedSet<string>();
                        while (reader.Read()) platform.Add(reader.GetString(0));

                        return platform;
                    }
                }
            }
        }

        private static string GetLanguage()
        {
            var languages = ListLanguages();
            return !languages.Contains(Language) ? "en-US" : Language;
        }

        private static List<(string, string)> QueryPage(string page)
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";"))
            {
                conn.Open();

                using (var command = new SqliteCommand("SELECT platform, lang FROM pages WHERE name = @name ORDER BY platform DESC", conn))
                {
                    command.Parameters.Add(new SqliteParameter("@name", page));

                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<(string, string)>();
                        while (reader.Read())
                        {
                            results.Add((reader.GetString(0), reader.GetString(1)));
                        }
                        return results;
                    }
                }
            }
        }

        private static int GetPage(string page, string language = null, string platform = null, bool markdown = false)
        {
            page = page.TrimStart().Replace(' ', '-');
            CheckDb();
            language = language ?? GetLanguage();
            var preferredLanguages = new List<string> {language, Language};

            var langs = Environment.GetEnvironmentVariable("LANGUAGE")?.Split(':').Where(x => !x.Equals(string.Empty)).ToList();
            if (langs != null)
            {
                preferredLanguages.AddRange(langs);
            }
            platform = platform ?? GetPlatform();
            string altPlatform = null;

            var results = QueryPage(page);
            
            if (!results.Contains((platform, language)))
            {
                if (results.Contains(("common", language)))
                {
                    platform = "common";
                } 
                else
                {
                    Console.Write("Page not found. ");
                    Update();
                    results = QueryPage(page);

                    if (results.Count == 0)
                    {
                        Console.WriteLine("Page not found.\nFeel free to send a pull request to: https://github.com/tldr-pages/tldr");
                        return 2;
                    }

                    bool found = false;
                    var altLanguage = string.Empty;

                    foreach (var (item1, item2) in results)
                    {
                        if (!preferredLanguages.Any(preferredLanguage => item2.Contains(preferredLanguage))) continue;
                        if (item1 != platform && item1 != "common")
                        {
                            if (altPlatform == null)
                            {
                                altPlatform = item1;
                                altLanguage = item2;
                            }
                            continue;
                        }
                        platform = item1;
                        language = item2;
                        found = true;
                    }

                    if (!found)
                    {
                        if (altPlatform == string.Empty)
                        {
                            Console.WriteLine("Page not found.\nFeel free to send a pull request to: https://github.com/tldr-pages/tldr");
                            return 2;
                        }
                        platform = altPlatform;
                        language = altLanguage;
                    }
                }
            }

            string path = Path.Combine(Path.GetDirectoryName(DbPath),
                "pages" + (language == "en-US" ? string.Empty : $".{language}"), platform, $"{page}.md");
            
            if (!File.Exists(path))
            {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }
            
            if (markdown)
                Console.WriteLine(File.ReadAllText(path));
            else
            {
                return Render(path, altPlatform ?? platform);
            }

            return 0;
        }

        private static int Render(string path, string diffPlatform = null)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }

            if (diffPlatform != null && diffPlatform != "common")
            {
                Console.WriteLine("\x1B[31m\x1b[1m[WARNING] THIS PAGE IS FOR THE " + diffPlatform.ToUpper() + " PLATFORM!\x1b[0m\n");
            }
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

                int urlStart = curLine.IndexOf("<", StringComparison.Ordinal);
                if (urlStart != -1)
                {
                    int urlEnd = curLine.Substring(urlStart).IndexOf(">", StringComparison.Ordinal);
                    if (urlEnd != -1)
                    {
                        curLine = curLine.Substring(0, urlStart) + "\x1b[21m\x1b[4m" +
                                  curLine.Substring(urlStart + 1, urlEnd - 1) + "\x1b[0m" +
                                  curLine.Substring(urlStart + urlEnd + 1);
                    }
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
                        Console.WriteLine("\x1b[39m\n" + curLine + "\x1b[0m");
                        break;
                    case '`':
                        Console.WriteLine("  \x1b[31m" + curLine.Trim('`') + "\x1b[0m");
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

        private static void ListAll(bool ignorePlatform, string language = null, string platform = null)
        {
            CheckDb();
            
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";"))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    if (ignorePlatform)
                        command.CommandText = "SELECT name FROM pages WHERE lang = @lang";
                    else
                    {
                        command.CommandText =
                            "SELECT name FROM pages WHERE lang = @lang AND (platform = @platform OR platform = 'common')";
                        command.Parameters.Add(new SqliteParameter("@platform", platform ?? GetPlatform()));
                    }

                    command.Parameters.Add(new SqliteParameter("@lang", language ?? GetLanguage()));

                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<string>();
                        while (reader.Read())
                        {
                            results.Add(reader.GetString(0));
                        }
                        Console.WriteLine(string.Join("\n", results.OrderBy(x => x, StringComparer.Ordinal.WithNaturalSort())));

                    }
                }
            }
        }

        private static SortedSet<string> ListLanguages()
        {
            CheckDb();
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";"))
            {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT lang FROM pages", conn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var languages = new SortedSet<string>();
                        while (reader.Read()) languages.Add(reader.GetString(0));

                        return languages;
                    }
                }
            }
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
            var cacheDir = new DirectoryInfo(CachePath);
           
            SqliteConnection.CreateFile(DbPath);

            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";"))
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
                        command.CommandText = "INSERT INTO pages (name, platform, lang) VALUES(@name, @platform, @lang)";

                        foreach (var dir in cacheDir.EnumerateDirectories("*pages*"))
                        {
                            var lang = "en-US";
                            if (dir.Name.Contains(".")) lang = dir.Name.Split('.')[1];

                            foreach (var osDir in dir.EnumerateDirectories())
                            {
                                foreach (var file in osDir.EnumerateFiles("*.md", SearchOption.AllDirectories))
                                {
                                    command.Parameters.AddWithValue("@name", Path.GetFileNameWithoutExtension(file.Name));
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

        private static void SelfUpdate()
        {

            using (var webclient = new WebClient())
            {
                webclient.Headers.Add ("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " + "Windows NT 5.2; .NET CLR 1.0.3705;)");
                var json = webclient.DownloadString(
                    "https://api.github.com/repos/principis/tldr-sharp/releases/latest");
                var remoteVersion = new Version(json.Substring(json.IndexOf("tag_name") + 12, 5));

                if (remoteVersion.CompareTo(Assembly.GetExecutingAssembly().GetName().Version) > 0)
                {
                    Console.WriteLine("Version {0} is available. Download it from {1}", remoteVersion, "https://github.com/principis/tldr-sharp/releases/latest");
                }
                else
                {
                    Console.WriteLine("tldr-sharp is up to date!");
                }
            }
        }
    }
}
