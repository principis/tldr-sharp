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

            int keyLength = results.Aggregate("", (max, cur) => max.Length > cur.name.Length ? max : cur.name).Length + 1;

            foreach ((string page, string[] matches) in results)
            foreach (string line in matches) {
                string selector = $"[magenta]{page}[/]:";

                Cli.WriteMessage("{0}\t{1}", selector.PadRight(keyLength + 12),
                    PageParser.ParseSearchLine(line).Replace(searchString, $"[underline]{searchString}[/]"));
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

            foreach (Page page in pages.OrderBy(x => x.Name, StringComparer.Ordinal.WithNaturalSort())) {
                Cli.WriteLine(page.ToString());
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
                    Cli.WriteWarningMessage($"None of the preferred languages found, using [underline]{Locale.GetLanguageName(Locale.DefaultLanguage)}[/] instead. " +
                                            $"See `tldr --list-languages` for a list of all available languages.{Environment.NewLine}");
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
                    Cli.WriteWarningMessage("Page not found.");
                    Cli.WriteLine("Cache older than 5 days.");

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
                    Cli.WriteWarningMessage($"The `{pageName}` page could not be found in {Locale.GetLanguageName(prefLanguage)}.");
                    Cli.WriteLine($"Feel free to translate it: {Config.NewTranslationUrl}");

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

            if (!page.Local) {
                Cli.WriteWarningMessage("Page not cached.");
                page.Download();
                Cli.WriteLine();
            }

            string path = page.GetPath();
            if (!File.Exists(path)) {
                Cli.WriteErrorMessage($"File \"{path}\" not found!");

                return 1;
            }

            if (markdown)
                Cli.WriteLine(File.ReadAllText(path));
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
            Cli.WriteWarningMessage($"Page not found.");
            Cli.WriteLine($"Feel free to create an issue at: {Config.NewPageUrl}{page}");

            return 2;
        }

        internal static int Render(string path, string diffPlatform = null)
        {
            if (!File.Exists(path)) {
                Cli.WriteErrorMessage($"File \"{path}\" not found!");

                return 1;
            }

            if (diffPlatform != null) {
                Cli.WriteWarningMessage($"This page is for the {diffPlatform} platform!");
            }

            foreach (string line in File.ReadLines(path)) {
                if (line.Length == 0) continue;
                Cli.WriteMessage(PageParser.ParseLine(line, true));
            }

            return 0;
        }
    }
}