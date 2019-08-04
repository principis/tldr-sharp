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
        private const string ClientSpecVersion = "1.2";

        private const string DefaultLanguage = "en-US";

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
                    o => {
                        CheckCache();
                        Console.WriteLine(string.Join("\n", ListPlatform()));
                    }
                },
                {
                    "list-languages", "List all languages",
                    la => {
                        CheckCache();
                        Console.WriteLine(string.Join("\n",
                            ListLanguages().Select(x => {
                                try {
                                    return x + ": " + CultureInfo.GetCultureInfo(x).EnglishName;
                                } catch (CultureNotFoundException) {
                                    return null;
                                }
                            }).Where(x => x != null)));
                    }
                },
                {
                    "L=|language=|lang=", "Specifies the preferred language",
                    la => language = la
                },
                {
                    "m|markdown", "Show the markdown source of a page",
                    v => markdown = v != null
                },
                {
                    "p=|platform=", "Override the default platform",
                    o => platform = o
                },
                {
                    "s=|search=", "Search for a string",
                    s => search = s
                },
                {
                    "u|update", "Update the local cache",
                    u => Updater.Update()
                },
                {
                    "self-update", "Check for tldr-sharp updates",
                    u => {
                        SelfUpdater.CheckSelfUpdate();
                        Environment.Exit(0);
                    }
                },
                {
                    "v|version", "Show version information",
                    v => {
                        Console.WriteLine("tldr-sharp " + Assembly.GetExecutingAssembly().GetName().Version.Major +
                                          "." +
                                          Assembly.GetExecutingAssembly().GetName().Version.Minor + "." +
                                          Assembly.GetExecutingAssembly().GetName().Version.Build);
                        Console.WriteLine("tldr-pages client specification " + ClientSpecVersion);
                        Environment.Exit(0);
                    }
                }
            };

            List<string> extra;
            try {
                extra = options.Parse(args);
            } catch (OptionException e) {
                Console.WriteLine(e.Message);
                return 1;
            }

            if (showHelp || args.Length == 0) {
                options.WriteOptionDescriptions(Console.Out);
                return args.Length == 0 ? 1 : 0;
            }

            if (render != null) {
                return Render(render);
            }

            CheckCache();

            if (language != null) {
                if (!CheckLanguage(language)) {
                    Console.WriteLine("[ERROR] unknown language '{0}'", language);
                    return 1;
                }
            }

            if (list) {
                ListAll(ignorePlatform, language, platform);
                return 0;
            }

            if (search != null) {
                return Search(search, language, platform);
            }

            string page = string.Empty;
            foreach (string arg in extra) {
                if (arg.StartsWith("-")) {
                    if (page == string.Empty) Console.WriteLine("[ERROR] unknown option '{0}'", arg);
                    return 1;
                }

                page += $" {arg}";
            }

            return page.Trim().Length > 0 ? GetPage(page, language, platform, markdown) : 0;
        }

        private static string GetPlatform()
        {
            switch (Environment.OSVersion.Platform) {
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

        private static ICollection<(string Language, string Platform)> GetPlatformPerLanguage()
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT lang, platform FROM pages", conn)) {
                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        var results = new List<(string Language, string Platform)>();
                        while (reader.Read()) results.Add((reader.GetString(0), reader.GetString(1)));

                        return results;
                    }
                }
            }
        }

        private static IEnumerable<string> ListPlatform()
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT platform FROM pages", conn)) {
                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        SortedSet<string> platform = new SortedSet<string>();
                        while (reader.Read()) platform.Add(reader.GetString(0));

                        return platform;
                    }
                }
            }
        }

        private static bool CheckLanguage(string language)
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand("SELECT 1 FROM pages WHERE lang = @language", conn)) {
                    command.Parameters.Add(new SqliteParameter("@language", language));

                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        return reader.HasRows;
                    }
                }
            }
        }

        private static List<string> GetPreferredLanguages()
        {
            var valid = new List<string>();
            var languages = ListLanguages();
            if (languages.Contains(Language)) valid.Add(Language);

            Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty))
                .ToList().ForEach(delegate(string s) {
                    valid.AddRange(languages.Where(l => l.Substring(0, 2).Equals(s)));
                });

            return valid;
        }

        private static string GetPreferredLanguageOrDefault()
        {
            var languages = ListLanguages();
            if (languages.Contains(Language)) return Language;

            var prefLanguages = Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty));

            if (prefLanguages != null) {
                foreach (string lang in prefLanguages) {
                    try {
                        return languages.First(x => x.Substring(0, 2).Equals(lang));
                    } catch (InvalidOperationException) { }
                }
            }

            return DefaultLanguage;
        }

        private static List<(string, string)> QueryPage(string page)
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";")) {
                conn.Open();

                using (var command =
                    new SqliteCommand("SELECT platform, lang FROM pages WHERE name = @name ORDER BY platform DESC",
                        conn)) {
                    command.Parameters.Add(new SqliteParameter("@name", page));

                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        var results = new List<(string Platform, string Language)>();
                        while (reader.Read()) {
                            results.Add((reader.GetString(0), reader.GetString(1)));
                        }

                        return results;
                    }
                }
            }
        }

        private static int GetPage(string page, string prefLanguage = null, string platform = null,
            bool markdown = false)
        {
            page = page.TrimStart().Replace(' ', '-');

            List<string> languages;
            string language = prefLanguage;
            if (language == null) {
                languages = GetPreferredLanguages();
                if (languages.Count == 0) {
                    Console.WriteLine("[INFO] None of the preferred languages found, using {0} instead.",
                        DefaultLanguage);
                    languages.Add(DefaultLanguage);
                }
            } else {
                languages = new List<string> {language};
            }

            platform = platform ?? GetPlatform();
            string altPlatform = null;

            var results = QueryPage(page);

            if (results.Count == 0) {
                Console.Write("Page not found. ");
                Updater.Update();
                results = QueryPage(page);

                if (results.Count == 0) return PageNotFound(page);
            }

            results = results.OrderBy(item => item,
                new PageComparer(new[] {platform, "common"}, languages)).ToList();

            try {
                (platform, language) = FindPage(results, languages, platform);
            } catch (Exception) {
                try {
                    string tmpPlatform;
                    (tmpPlatform, language) = FindAlternativePage(results, languages);
                    altPlatform = tmpPlatform;
                    if (platform == tmpPlatform || tmpPlatform == "common") altPlatform = null;
                    platform = tmpPlatform;
                } catch (Exception) {
                    return PageNotFound(page);
                }
            }

            string path = GetPagePath(page, language, platform);

            if (!File.Exists(path)) {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                Updater.Update();

                return 1;
            }

            if (markdown)
                Console.WriteLine(File.ReadAllText(path));
            else {
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
                "pages" + (language == DefaultLanguage ? string.Empty : $".{language}"), platform, $"{name}.md");
        }


        private static (string Platform, string Language) FindPage(ICollection<(string, string)> results,
            ICollection<string> languages, string platform)
        {
            foreach (string language in languages) {
                if (results.Contains((platform, language))) {
                    return (platform, language);
                }
            }

            foreach (string language in languages) {
                if (results.Contains(("common", language))) {
                    return ("common", language);
                }
            }

            throw new Exception();
        }

        private static (string Platform, string Language) FindAlternativePage(
            ICollection<(string Platform, string Language)> results,
            ICollection<string> languages)
        {
            foreach (string language in languages) {
                try {
                    return results.First(x => x.Language.Equals(language));
                } catch (InvalidOperationException) { }
            }

            if (!languages.Contains(DefaultLanguage)) {
                return results.First(x => x.Language.Equals(DefaultLanguage));
            }

            throw new Exception();
        }

        private static int Render(string path, string diffPlatform = null)
        {
            if (!File.Exists(path)) {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }

            if (diffPlatform != null) {
                Console.WriteLine("\x1B[31m\x1b[1m[WARNING] THIS PAGE IS FOR THE " + diffPlatform.ToUpper() +
                                  " PLATFORM!\x1b[0m\n");
            }

            foreach (string line in File.ReadLines(path)) {
                if (line.Length == 0) continue;

                Console.WriteLine(ParseLine(line, true));
            }

            return 0;
        }

        private static string ParseLine(string line, bool formatted = false)
        {
            if (line.Contains("{{")) {
                line = line.Replace("{{", "\x1b[32m").Replace("}}", "\x1b[31m");
            }

            int urlStart = line.IndexOf("<", StringComparison.Ordinal);
            if (urlStart != -1) {
                int urlEnd = line.Substring(urlStart).IndexOf(">", StringComparison.Ordinal);
                if (urlEnd != -1) {
                    line = line.Substring(0, urlStart) + "\x1b[21m\x1b[4m" +
                           line.Substring(urlStart + 1, urlEnd - 1) + "\x1b[0m" +
                           line.Substring(urlStart + urlEnd + 1);
                }
            }

            switch (line[0]) {
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
            if (!File.Exists(DbPath)) {
                Console.WriteLine("Database not found. ");
                Updater.Update();
                return;
            }

            var langPlatforms = GetPlatformPerLanguage();

            foreach ((string lang, string platform) in langPlatforms) {
                if (Directory.Exists(Path.Combine(
                    CachePath, "pages" + (lang == DefaultLanguage ? string.Empty : $".{lang}"), platform))) continue;
                Console.WriteLine("Cache corrupted. ");
                Updater.Update();
                return;
            }
        }

        private static void ListAll(bool ignorePlatform, string language = null, string platform = null)
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";")) {
                conn.Open();
                using (SqliteCommand command = conn.CreateCommand()) {
                    if (ignorePlatform)
                        command.CommandText = "SELECT name FROM pages WHERE lang = @lang";
                    else {
                        command.CommandText =
                            "SELECT name FROM pages WHERE lang = @lang AND (platform = @platform OR platform = 'common')";
                        command.Parameters.Add(new SqliteParameter("@platform", platform ?? GetPlatform()));
                    }

                    command.Parameters.Add(new SqliteParameter("@lang", language ?? GetPreferredLanguageOrDefault()));

                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        var results = new List<string>();
                        while (reader.Read()) {
                            results.Add(reader.GetString(0));
                        }

                        Console.WriteLine(string.Join("\n",
                            results.OrderBy(x => x, StringComparer.Ordinal.WithNaturalSort())));
                    }
                }
            }
        }

        private static ICollection<string> ListLanguages()
        {
            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT lang FROM pages", conn)) {
                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        var languages = new List<string>();
                        while (reader.Read()) languages.Add(reader.GetString(0));

                        return languages;
                    }
                }
            }
        }

        private static int Search(string searchString, string language, string platform)
        {
            var pages = new List<(string, string, string)>();
            language = language ?? GetPreferredLanguageOrDefault();
            platform = platform ?? GetPlatform();

            using (var conn = new SqliteConnection("Data Source=" + DbPath + ";")) {
                conn.Open();
                using (SqliteCommand command = conn.CreateCommand()) {
                    command.CommandText =
                        "SELECT name, lang, platform FROM pages WHERE lang = @lang AND (platform = @platform OR platform = 'common')";
                    command.Parameters.Add(new SqliteParameter("@platform", platform));
                    command.Parameters.Add(new SqliteParameter("@lang", language));

                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            pages.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                        }
                    }
                }
            }

            var results = pages.AsParallel().Select(file => {
                (string name, string lang, string p) = file;
                string path = GetPagePath(name, lang, p);

                return File.Exists(path)
                    ? (name, File.ReadLines(path).Where(line => line.Contains(searchString)).ToArray())
                    : (name, new string[0]);
            }).Where(x => x.Item2.Length != 0).ToList();

            if (results.Count == 0) return 1;

            results.Sort((x, y) => string.Compare(x.Item1, y.Item1, StringComparison.Ordinal));
            foreach ((string page, var matches) in results) {
                foreach (string line in matches) {
                    Console.WriteLine("\x1b[35m{0}\x1b[39m:\t{1}", page,
                        ParseLine(line).Replace(searchString, "\x1b[4m" + searchString + "\x1b[24m"));
                }
            }

            return 0;
        }
    }

    public class PageComparer : IComparer<(string Platform, string Language)>
    {
        private readonly string[] _languages;
        private readonly string[] _platforms;

        public PageComparer(IEnumerable<string> platforms, IEnumerable<string> languages)
        {
            _languages = languages.ToArray();
            _platforms = platforms.ToArray();
        }

        public int Compare((string Platform, string Language) x, (string Platform, string Language) y)
        {
            int xIndex = Array.IndexOf(_languages, x.Language);
            int yIndex = Array.IndexOf(_languages, y.Language);

            if (xIndex == yIndex) {
                xIndex = Array.IndexOf(_platforms, x.Platform);
                yIndex = Array.IndexOf(_platforms, y.Platform);
            }

            if (xIndex == yIndex) return 0;
            if (xIndex == -1) return 1;
            if (yIndex == -1) return -1;
            return xIndex - yIndex;
        }
    }
}