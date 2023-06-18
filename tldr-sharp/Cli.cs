/*
    SPDX-FileCopyrightText: 2023 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mono.Options;

namespace tldr_sharp
{
    public class Cli
    {
        private OptionSet _optionSet;


        public Settings ParseArgs(string[] args)
        {
            var settings = new Settings();

            var optionset = new OptionSet
            {
                "Usage: tldr command [options]" + Environment.NewLine,
                "Simplified and community-driven man pages" + Environment.NewLine,
                {
                    "a|list-all", "List all pages",
                    v => settings.List = settings.IgnorePlatform = v != null
                },
                {
                    "c|clear-cache", "Clear the cache",
                    v => settings.CacheClear = v != null
                },
                {
                    "f=|render=", "Render a specific markdown file",
                    v => settings.RenderFile = v
                },
                {
                    "h|help", "Display this help text",
                    v => settings.ShowHelp = v != null
                },
                {
                    "l|list", "List all pages for the current platform and language",
                    v => settings.List = v != null
                },
                {
                    "list-platforms", "List all platforms",
                    v => settings.ListPlatform = v != null
                },
                {
                    "list-languages", "List all languages",
                    v => settings.ListLanguages = v != null
                },
                {
                    "L=|language=|lang=", "Specifies the preferred language",
                    v => settings.Language = v
                },
                {
                    "m|markdown", "Show the markdown source of a page",
                    v => settings.Markdown = v != null
                },
                {
                    "p=|platform=", "Override the default platform",
                    o => settings.Platform = o
                },
                {
                    "s=|search=", "Search for a string",
                    s => settings.SearchString = s
                },
                {
                    "u|update", "Update the cache",
                    v => settings.CacheUpdate = v != null
                },
                {
                    "self-update", "Check for tldr-sharp updates",
                    u =>
                    {
                        // SelfUpdater.CheckSelfUpdate();
                        Environment.Exit(0);
                    }
                },
                {
                    "v|version", "Show version information",
                    v =>
                    {
                        FileVersionInfo version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                        Console.WriteLine($"tldr-sharp {version.ProductMajorPart}.{version.ProductMinorPart}.{version.ProductBuildPart}");
                        Console.WriteLine("tldr-pages client specification " + Program.ClientSpecVersion);
                        Console.WriteLine(version.LegalCopyright);
                        Console.WriteLine(@"License GPLv3+: GNU GPL version 3 or later <https://www.gnu.org/licenses/gpl-3.0.html>.
This is free software: you are free to change and redistribute it.
There is NO WARRANTY, to the extent permitted by law.");
                        Environment.Exit(0);
                    }
                }
            };

            settings.Extra = optionset.Parse(args);
            _optionSet = optionset;

            return settings;
        }

        public void WriteHelp(TextWriter o)
        {
            _optionSet.WriteOptionDescriptions(o);
        }

        public class Settings
        {
            public bool CacheUpdate { get; internal set; }
            public bool CacheClear { get; internal set; }
            public bool IgnorePlatform { get; internal set; }
            public bool List { get; internal set; }
            public bool ListPlatform { get; internal set; }
            public bool ListLanguages { get; internal set; }
            public bool Markdown { get; internal set; }
            public string? RenderFile { get; internal set; }
            public bool ShowHelp { get; internal set; }

            public string? Language { get; internal set; }
            public string? Platform { get; internal set; }
            public string? SearchString { get; internal set; }

            public List<string>? Extra { get; internal set; }
        }
    }
}