/*
    SPDX-FileCopyrightText: 2018 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Options;
using static tldr_sharp.Index;

namespace tldr_sharp
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            try {
                return doMain(args);
            }
            catch (IOException) {
                // TODO: Implement using UnixSignal
                return 1;
            }
        }

        private static int doMain(string[] args)
        {
            var cli = new Cli();
            Cli.Settings settings;

            try {
                settings = cli.ParseArgs(args);
            }
            catch (OptionException e) {
                Console.WriteLine(e.Message);
                return 1;
            }

            if (settings.ShowHelp || args.Length == 0) {
                cli.WriteHelp(Console.Out);
                return args.Length == 0 ? 1 : 0;
            }

            if (settings.RenderFile != null) {
                return PageController.Render(settings.RenderFile);
            }

            if (settings.CacheClear) {
                Cache.Clear();
                return 0;
            }

            if (settings.CacheUpdate) {
                Updater.Update();
            }

            if (settings.SelfUpdate) {
                SelfUpdater.Check();
            }

            // All following functions rely on the cache, so check it.
            Cache.Check();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (settings.ListLanguages) {
                Cli.WriteLine(string.Join(Environment.NewLine,
                    ListLanguages().Select(lang =>
                    {
                        string name = Locale.GetLanguageName(lang);

                        return name == null ? lang : $"{lang}:\t{name}";
                    })));
            }

            if (settings.ListPlatform) {
                Cli.WriteLine(string.Join(Environment.NewLine, ListPlatform()));
            }

            if (settings.Language != null) {
                if (!CheckLanguage(settings.Language)) {
                    Cli.WriteErrorMessage($"unknown language '{settings.Language}'");
                    return 1;
                }
            }

            if (settings.List) {
                PageController.ListAll(settings.IgnorePlatform, settings.Language, settings.Platform);
                return 0;
            }

            if (settings.SearchString != null) {
                return PageController.Search(settings.SearchString, settings.Language, settings.Platform);
            }

            StringBuilder builder = new StringBuilder();
            foreach (string arg in settings.Extra) {
                if (arg.StartsWith("-")) {
                    if (builder.Length == 0) Cli.WriteErrorMessage($"unknown option '{arg}'");
                    return 1;
                }

                builder.Append($" {arg}");
            }

            string page = builder.ToString();

            return page.Trim().Length > 0 ? PageController.Print(page, settings.Language, settings.Platform, settings.Markdown) : 0;
        }
    }
}