/*
    SPDX-FileCopyrightText: 2020 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaturalSort.Extension;
using static tldr_sharp.Index;

namespace tldr_sharp
{
    public static class PageController
    {
        internal static int Search(string searchString, string language, string platform)
        {
            List<(string name, string[] matches)> results = SearchPages(searchString,
                QueryByLanguageAndPlatform(language, platform));

            if (results.Count == 0) return 1;

            results.Sort((x, y) => string.Compare(x.Item1, y.Item1, StringComparison.Ordinal));

            int keyLength = results.Aggregate("", (max, cur) => max.Length > cur.name.Length ? max : cur.name).Length +
                            1;

            if (Program.AnsiSupport) {
                keyLength += Ansi.Magenta.Length + Ansi.Default.Length;
            }

            foreach ((string page, string[] matches) in results)
            foreach (string line in matches) {
                if (Program.AnsiSupport) {
                    string selector = $"{Ansi.Magenta}{page}{Ansi.Default}:";

                    Console.WriteLine("{0}\t{1}", selector.PadRight(keyLength),
                        PageParser.ParseSearchLine(line)
                            .Replace(searchString, Ansi.Underline + searchString + Ansi.UnderlineOff));
                }
                else {
                    string selector = $"{page}:";
                    Console.WriteLine("{0}\t{1}", selector.PadRight(keyLength), PageParser.ParseLine(line));
                }
            }

            return 0;
        }

        private static List<(string name, string[] matches)> SearchPages(string searchString, List<Page> pages)
        {
            return pages.AsParallel().Select(page => {
                if (!page.Local) page.Download();

                return (page.Name, File.ReadLines(page.GetPath()).Where(line => line.Contains(searchString)).ToArray());
            }).Where(x => x.Item2.Length != 0).ToList();
        }


        internal static void ListAll(bool ignorePlatform, string language = null, string platform = null)
        {
            List<Page> pages =
                ignorePlatform ? QueryByLanguage(language) : QueryByLanguageAndPlatform(language, platform);

            foreach (Page page in pages.OrderBy(x => x.Name, StringComparer.Ordinal.WithNaturalSort()))
            {
                Console.WriteLine(page);
            }
        }

        internal static int Print(string pageName, string prefLanguage = null, string platform = null,
            bool markdown = false)
        {
            pageName = pageName.ToLower().TrimStart().Replace(' ', '-');

            List<string> languages;
            if (prefLanguage == null) {
                languages = Locale.GetPreferredLanguages();

                if (languages.Count == 0) {
                    Console.WriteLine(
                        $"[INFO] None of the preferred languages found, using {Locale.GetLanguageName(Locale.DefaultLanguage)} instead. " +
                        "See `tldr --list-languages` for a list of all available languages.");
                    languages.Add(Locale.DefaultLanguage);
                }
            }
            else {
                languages = new List<string> {prefLanguage};
            }

            platform ??= GetPlatform();
            string altPlatform = null;

            List<Page> results = QueryByName(pageName);

            if (results.Count == 0) {
                if ((DateTime.UtcNow.Date - Cache.LastUpdate()).TotalDays > 5) {
                    Console.WriteLine("Page not found.");
                    Console.Write("Cache older than 5 days. ");

                    Updater.Update();
                    results = QueryByName(pageName);
                }

                if (results.Count == 0) return NotFound(pageName);
            }

            results = results.OrderBy(item => item,
                new PageComparer(new[] {platform, "common"}, languages)).ToList();

            Page page;
            try {
                page = Find(results, languages, platform);
            }
            catch (ArgumentNullException) {
                if (prefLanguage != null) {
                    Console.WriteLine(
                        $"The `{pageName}` page could not be found in {Locale.GetLanguageName(prefLanguage)}. " +
                        $"{Environment.NewLine}Feel free to translate it: https://github.com/tldr-pages/tldr/blob/master/CONTRIBUTING.md#translations");
                    return 2;
                }

                try {
                    page = FindAlternative(results, languages);
                    if (platform != page.Platform && page.Platform != "common") altPlatform = page.Platform;
                }
                catch (ArgumentNullException) {
                    return NotFound(pageName);
                }
            }

            if (!page.Local) CustomSpinner.Run("Page not cached. Downloading", page.Download);

            string path = page.GetPath();
            if (!File.Exists(path)) {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);

                return 1;
            }

            if (markdown)
                Console.WriteLine(File.ReadAllText(path));
            else
                return Render(path, altPlatform);

            return 0;
        }

        private static Page Find(ICollection<Page> results, ICollection<string> languages, string platform)
        {
            foreach (string language in languages) {
                try {
                    return results.First(x => x.Platform == platform && x.Language == language);
                }
                catch (InvalidOperationException) {
                    // Catch exception when First does not find a page
                }
            }

            foreach (string language in languages) {
                try {
                    return results.First(x => x.Platform == "common" && x.Language == language);
                }
                catch (InvalidOperationException) {
                    // Catch exception when First does not find a page
                }
            }

            throw new ArgumentNullException();
        }

        private static Page FindAlternative(ICollection<Page> results, ICollection<string> languages)
        {
            if (!languages.Contains(Locale.DefaultLanguage)) {
                try {
                    return results.First(x => x.Language.Equals(Locale.DefaultLanguage));
                }
                catch (InvalidOperationException) { }
            }

            foreach (string language in languages) {
                try {
                    return results.First(x => x.Language.Equals(language));
                }
                catch (InvalidOperationException) { }
            }

            throw new ArgumentNullException();
        }

        private static int NotFound(string page)
        {
            Console.WriteLine(
                "Page not found.{0}Feel free to create an issue at: https://github.com/tldr-pages/tldr/issues/new?title=page%20request:%20{1}",
                Environment.NewLine, page);
            return 2;
        }

        internal static int Render(string path, string diffPlatform = null)
        {
            if (!File.Exists(path)) {
                Console.WriteLine("[ERROR] File \"{0}\" not found.", path);
                return 1;
            }

            if (diffPlatform != null) {
                if (Program.AnsiSupport) {
                    Console.WriteLine("{0}{1}[WARN] This page is for the {2} platform!{3}{4}", Ansi.Red, Ansi.Bold,
                        diffPlatform, Ansi.Off, Environment.NewLine);
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[WARN] This page is for the {0} platform!{1}", diffPlatform,
                        Environment.NewLine);
                    Console.ForegroundColor = Program.DefaultColor;
                }
            }

            foreach (string line in File.ReadLines(path)) {
                if (line.Length == 0) continue;
                Console.WriteLine(PageParser.ParseLine(line, true));
            }

            return 0;
        }
    }
}