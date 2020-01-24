using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Data.Sqlite;
using static tldr_sharp.Index;

namespace tldr_sharp
{
    internal struct Page
    {
        public readonly string Name;
        public readonly string Platform;
        public readonly string Language;
        public readonly bool Local;

        public Page(string name, string platform, string language, bool local)
        {
            Name = name;
            Platform = platform;
            Language = language;
            Local = local;
        }

        public override string ToString()
        {
            return Name;
        }

        internal static int Print(string pageName, string prefLanguage = null, string platform = null,
            bool markdown = false)
        {
            pageName = pageName.TrimStart().Replace(' ', '-');

            List<string> languages;
            string language = prefLanguage;
            if (language == null) {
                languages = GetPreferredLanguages();
                if (languages.Count == 0) {
                    Console.WriteLine("[INFO] None of the preferred languages found, using {0} instead.",
                        Program.DefaultLanguage);
                    languages.Add(Program.DefaultLanguage);
                }
            }
            else {
                languages = new List<string> {language};
            }

            platform ??= GetPlatform();
            string altPlatform = null;

            List<Page> results = Query(pageName);

            if (results.Count == 0) {
                if ((DateTime.UtcNow.Date - Cache.LastUpdate()).TotalDays > 5) {
                    Console.WriteLine("Page not found.");
                    Console.Write("Cache older than 5 days. ");

                    Updater.Update();
                    results = Query(pageName);
                }

                if (results.Count == 0) return NotFound(pageName);
            }

            results = results.OrderBy(item => item,
                new PageComparer(new[] {platform, "common"}, languages)).ToList();

            Page page;
            try {
                page = Find(results, languages, platform);
            }
            catch (Exception) {
                if (prefLanguage != null) {
                    Console.WriteLine("Page not found in the requested language.");
                    return 2;
                }

                try {
                    page = FindAlternative(results, languages);
                    if (platform != page.Platform && page.Platform != "common") altPlatform = page.Platform;
                }
                catch (Exception) {
                    return NotFound(pageName);
                }
            }

            if (!page.Local) {
                using (new CustomSpinner("Page not cached, downloading")) {
                    page.Download();
                }
            }

            string path = page.GetPath();
            if (!File.Exists(path)) {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);

                return 1;
            }

            if (markdown) {
                Console.WriteLine(File.ReadAllText(path));
            }
            else {
                return Render(path, altPlatform);
            }

            return 0;
        }


        private static List<Page> Query(string page)
        {
            return Index.Query("name = @name ORDER BY platform DESC",
                new[] {new SqliteParameter("@name", page)},
                page);
        }

        internal static int Render(string path, string diffPlatform = null)
        {
            if (!File.Exists(path)) {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }

            if (diffPlatform != null) {
                if (Program.AnsiSupport) {
                    Console.WriteLine("{0}{1}[WARNING] THIS PAGE IS FOR THE {2} PLATFORM!{3}{4}", Ansi.Red, Ansi.Bold,
                        diffPlatform.ToUpper(), Ansi.Off, Environment.NewLine);
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[WARNING] THIS PAGE IS FOR THE {0} PLATFORM!{1}", diffPlatform.ToUpper(),
                        Environment.NewLine);
                    Console.ForegroundColor = Program.DefaultColor;
                }
            }

            foreach (string line in File.ReadLines(path)) {
                if (line.Length == 0) continue;
                Console.WriteLine(ParseLine(line, true));
            }

            return 0;
        }


        internal static string ParseLine(string line, bool formatted = false)
        {
            if (line.Contains("{{")) {
                line = line.Replace("{{", Program.AnsiSupport ? Ansi.Green : "")
                    .Replace("}}", Program.AnsiSupport ? Ansi.Red : "");
            }

            int urlStart = line.IndexOf("<", StringComparison.Ordinal);
            if (urlStart != -1) {
                int urlEnd = line.Substring(urlStart).IndexOf(">", StringComparison.Ordinal);
                if (urlEnd != -1)
                    line = line.Substring(0, urlStart) + Ansi.BoldOff + Ansi.Underline +
                           line.Substring(urlStart + 1, urlEnd - 1) + Ansi.Off +
                           line.Substring(urlStart + urlEnd + 1);
            }

            line = line[0] switch {
                '#' => ((Program.AnsiSupport ? Ansi.Underline + Ansi.Bold : "") + line.Substring(2) +
                        (Program.AnsiSupport ? Ansi.Off : "") + (formatted ? Environment.NewLine : "")),
                '>' => ((Program.AnsiSupport ? Ansi.Bold : "") + line.Substring(2) +
                        (Program.AnsiSupport ? Ansi.Off : "")),
                '-' => ((Program.AnsiSupport ? Ansi.Default : "") + (formatted ? Environment.NewLine : "") + line +
                        (Program.AnsiSupport ? Ansi.Off : "")),
                '`' => ((formatted ? "   " : "") + (Program.AnsiSupport ? Ansi.Red : "") + line.Trim('`') +
                        (Program.AnsiSupport ? Ansi.Off : "")),
                _ => line
            };

            return line;
        }


        private static Page Find(ICollection<Page> results,
            ICollection<string> languages, string platform)
        {
            foreach (string language in languages) {
                try {
                    return results.First(x => x.Platform == platform && x.Language == language);
                }
                catch (InvalidOperationException) { }
            }

            foreach (string language in languages) {
                try {
                    return results.First(x => x.Platform == "common" && x.Language == language);
                }
                catch (InvalidOperationException) { }
            }

            throw new Exception();
        }

        private static Page FindAlternative(
            ICollection<Page> results,
            ICollection<string> languages)
        {
            foreach (string language in languages) {
                try {
                    return results.First(x => x.Language.Equals(language));
                }
                catch (InvalidOperationException) { }
            }

            if (!languages.Contains(Program.DefaultLanguage))
                return results.First(x => x.Language.Equals(Program.DefaultLanguage));

            throw new Exception();
        }


        private static int NotFound(string page)
        {
            Console.WriteLine(
                "Page not found.{0}Feel free to create an issue at: https://github.com/tldr-pages/tldr/issues/new?title=page%20request:%20{1}",
                Environment.NewLine, page);
            return 2;
        }

        private static string GetPath(Page page)
        {
            return Path.Combine(Program.CachePath,
                "pages" + (page.Language == Program.DefaultLanguage ? string.Empty : $".{page.Language}"),
                page.Platform, $"{page.Name}.md");
        }

        internal string GetPath()
        {
            return GetPath(this);
        }

        internal void Download()
        {
            try {
                Cache.DownloadPage(this);
            }
            catch (Exception e) {
                Console.WriteLine("[ERROR] An error has occurred downloading the requested page: {0}", e.Message);
            }
        }

        internal static int Search(string searchString, string language, string platform)
        {
            language ??= GetPreferredLanguageOrDefault();
            platform ??= GetPlatform();

            SqliteParameter[] parameters =
                {new SqliteParameter("@platform", platform), new SqliteParameter("@lang", language)};
            List<Page> pages = Index.Query("lang = @lang AND (platform = @platform OR platform = 'common')",
                parameters);

            List<(string name, string[] matches)> results = pages.AsParallel().Select(page => {
                string path = GetPath(page);
                if (!page.Local) page.Download();

                return (page.Name, File.ReadLines(path).Where(line => line.Contains(searchString)).ToArray());
            }).Where(x => x.Item2.Length != 0).ToList();

            if (results.Count == 0) return 1;

            results.Sort((x, y) => string.Compare(x.Item1, y.Item1, StringComparison.Ordinal));

            foreach ((string page, string[] matches) in results)
            foreach (string line in matches) {
                if (Program.AnsiSupport) {
                    Console.WriteLine("{0}{1}{2}:\t{3}", Ansi.Magenta, page, Ansi.Default,
                        ParseLine(line).Replace(searchString, "\x1b[4m" + searchString + "\x1b[24m"));
                }
                else
                    Console.WriteLine("{0}:\t{1}", page, ParseLine(line));
            }

            return 0;
        }
    }
}