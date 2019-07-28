using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using Mono.Data.Sqlite;
using Mono.Options;
using System.IO;
using System.Linq;
using NaturalSort.Extension;

namespace tldr_sharp
{
    internal static class Program
    {
        private const string ClientSpecVersion = "1.1";

        internal const string SelfApiUrl = "https://api.github.com/repos/principis/tldr-sharp/releases/latest";
        internal const string SelfUpdateUrl = "https://github.com/principis/tldr-sharp/releases/latest";
        internal const string PagesUrl = "https://tldr.sh/assets/tldr.zip";

        internal static readonly string CachePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache");

        internal static readonly string DbPath = Path.Combine(CachePath, "index.sqlite");

        private static readonly string Language = CultureInfo.CurrentCulture.Name;

        public static int Main(string[] args)
        {
            bool showHelp = false;

            bool list = false;
            bool ignorePlatform = false;

            string language = null;
            string platform = null;
            string search = null;

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
                    c => Updater.ClearCache()
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
                    o =>
                    {
                        CheckCache();
                        Console.WriteLine(string.Join("\n", ListPlatform()));
                    }
                },
                {
                    "list-languages", "List all languages",
                    la =>
                    {
                        CheckCache();
                        Console.WriteLine(string.Join("\n",
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
                            }).Where(x => x != null)));
                    }
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
                    "s=|search=", "Search for a string.",
                    s => search = s
                },
                {
                    "u|update", "Update the local cache.",
                    u => Updater.Update()
                },
                {
                    "self-update", "Check for tldr-sharp updates.",
                    u =>
                    {
                        SelfUpdater.CheckSelfUpdate();
                        Environment.Exit(0);
                    }
                },
                {
                    "v|version", "Show version information.",
                    v =>
                    {
                        Console.WriteLine("tldr-sharp " + Assembly.GetExecutingAssembly().GetName().Version.Major +
                                          "." +
                                          Assembly.GetExecutingAssembly().GetName().Version.Minor + "." +
                                          Assembly.GetExecutingAssembly().GetName().Version.Build +
                                          ", spec " + ClientSpecVersion);
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

            if (render != null)
            {
                return Render(render);
            }

            CheckCache();

            if (list)
            {
                ListAll(ignorePlatform, language, platform);
                return 0;
            }

            if (search != null)
            {
                return Search(search);
            }

            string page = "";
            foreach (string arg in extra)
            {
                if (arg.StartsWith("-"))
                {
                    if (page.Equals("")) Console.WriteLine("[ERROR] unknown option '{0}'", arg);
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

        private static List<(string, string)> GetPlatformPerLanguage()
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";"))
            {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT lang, platform FROM pages", conn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<(string, string)>();
                        while (reader.Read()) results.Add((reader.GetString(0), reader.GetString(1)));

                        return results;
                    }
                }
            }
        }

        private static IEnumerable<string> ListPlatform()
        {
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

                using (var command =
                    new SqliteCommand("SELECT platform, lang FROM pages WHERE name = @name ORDER BY platform DESC",
                        conn))
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
            language = language ?? GetLanguage();
            var preferredLanguages = new List<string> {language, Language};

            var langs = Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty))
                .ToList();

            if (langs != null) preferredLanguages.AddRange(langs);

            platform = platform ?? GetPlatform();
            string altPlatform = null;

            var results = QueryPage(page);

            if (results.Count == 0)
            {
                Console.Write("Page not found. ");
                Updater.Update();
                results = QueryPage(page);

                if (results.Count == 0) return PageNotFound(page);
            }

            if (!results.Contains((platform, language)))
            {
                if (results.Contains(("common", language))) platform = "common";
                else
                {
                    string tmpPlatform;
                    (tmpPlatform, language) = FindAlternativePage(results, preferredLanguages, platform);

                    if (tmpPlatform == null || language == null) return PageNotFound(page);

                    altPlatform = tmpPlatform;
                    if (platform == tmpPlatform || tmpPlatform == "common") altPlatform = null;
                    platform = tmpPlatform;
                }
            }

            string path = GetPagePath(page, language, platform);

            if (!File.Exists(path))
            {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }

            if (markdown)
                Console.WriteLine(File.ReadAllText(path));
            else
            {
                return Render(path, altPlatform);
            }

            return 0;
        }

        private static int PageNotFound(string page)
        {
            Console.WriteLine(
                "Page not found.\nFeel free to create an issue at: https://github.com/tldr-pages/tldr/issues/new?title=page%20request:%20{0}",
                page);
            return 2;
        }

        private static string GetPagePath(string name, string language, string platform)
        {
            return Path.Combine(CachePath,
                "pages" + (language == "en-US" ? string.Empty : $".{language}"), platform, $"{name}.md");
        }

        private static (string Platform, string Language) FindAlternativePage(IEnumerable<(string, string)> results,
            IReadOnlyCollection<string> preferredLanguages, string platform)
        {
            bool found = false;
            var altLanguage = string.Empty;
            string altPlatform = null;
            string language = null;

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

            if (found) return (platform, language);
            return altPlatform == string.Empty ? (null, null) : (altPlatform, altLanguage);
        }

        private static int Render(string path, string diffPlatform = null)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }

            if (diffPlatform != null)
            {
                Console.WriteLine("\x1B[31m\x1b[1m[WARNING] THIS PAGE IS FOR THE " + diffPlatform.ToUpper() +
                                  " PLATFORM!\x1b[0m\n");
            }

            foreach (var line in File.ReadLines(path))
            {
                if (line.Length == 0) continue;

                Console.WriteLine(ParseLine(line, true));
            }

            return 0;
        }

        private static string ParseLine(string line, bool formatted = false)
        {
            if (line.Contains("{{"))
            {
                line = line.Replace("{{", "\x1b[32m").Replace("}}", "\x1b[31m");
            }

            int urlStart = line.IndexOf("<", StringComparison.Ordinal);
            if (urlStart != -1)
            {
                int urlEnd = line.Substring(urlStart).IndexOf(">", StringComparison.Ordinal);
                if (urlEnd != -1)
                {
                    line = line.Substring(0, urlStart) + "\x1b[21m\x1b[4m" +
                           line.Substring(urlStart + 1, urlEnd - 1) + "\x1b[0m" +
                           line.Substring(urlStart + urlEnd + 1);
                }
            }

            switch (line[0])
            {
                case '#':
                    line = "\x1B[4m\x1b[1m" + line.Substring(2) + "\x1b[0m" + (formatted ? "\n" : "");
                    break;
                case '>':
                    line = "\x1b[1m" + line.Substring(2) + "\x1b[0m";
                    break;
                case '-':
                    line = "\x1b[39m" + (formatted ? "\n" : "") + line + "\x1b[0m";
                    break;
                case '`':
                    line = (formatted ? "   " : "") + "\x1b[31m" + line.Trim('`') + "\x1b[0m";
                    break;
            }

            return line;
        }

        private static void CheckCache()
        {
            if (!File.Exists(DbPath))
            {
                Console.WriteLine("Database not found. ");
                Updater.Update();
                return;
            }

            var langPlatforms = GetPlatformPerLanguage();

            foreach (var (lang, platform) in langPlatforms)
            {
                if (Directory.Exists(Path.Combine(CachePath,
                    "pages" + (lang == "en-US" ? string.Empty : $".{lang}"), platform))) continue;
                Console.WriteLine("Cache corrupted. ");
                Updater.Update();
                return;
            }
        }

        private static void ListAll(bool ignorePlatform, string language = null, string platform = null)
        {
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

                        Console.WriteLine(string.Join("\n",
                            results.OrderBy(x => x, StringComparer.Ordinal.WithNaturalSort())));
                    }
                }
            }
        }

        private static IEnumerable<string> ListLanguages()
        {
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

        private static int Search(string searchString)
        {
            var pages = new List<(string, string, string)>();

            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";"))
            {
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText =
                        "SELECT name, lang, platform FROM pages WHERE lang = @lang AND (platform = @platform OR platform = 'common')";
                    command.Parameters.Add(new SqliteParameter("@platform", GetPlatform()));
                    command.Parameters.Add(new SqliteParameter("@lang", GetLanguage()));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pages.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                        }
                    }
                }
            }

            var results = pages.AsParallel().Select(file =>
            {
                var (name, lang, platform) = file;
                string path = GetPagePath(name, lang, platform);

                return File.Exists(path)
                    ? (name, File.ReadLines(path).Where(line => line.Contains(searchString)).ToArray())
                    : (name, new string[0]);
            }).Where(x => x.Item2.Length != 0).ToList();

            if (results.Count == 0) return 1;

            results.Sort();
            foreach (var (page, matches) in results)
            {
                foreach (var line in matches)
                {
                    Console.WriteLine("\x1b[35m{0}\x1b[39m:\t{1}", page,
                        ParseLine(line).Replace(searchString, "\x1b[4m" + searchString + "\x1b[24m"));
                }
            }

            return 0;
        }
    }
}