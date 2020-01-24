using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Data.Sqlite;
using Mono.Options;
using NaturalSort.Extension;
using static tldr_sharp.Index;

namespace tldr_sharp
{
    internal static class Program
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        
        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        
        
        private const string ClientSpecVersion = "1.2";
        internal const string DefaultLanguage = "en_US";
        internal static bool AnsiSupport = true;
        internal static readonly ConsoleColor DefaultColor = Console.ForegroundColor;

        internal static readonly string Language = CultureInfo.CurrentCulture.Name.Replace('-', '_');

        internal static readonly string CachePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache");

        internal static readonly string DbPath = Path.Combine(CachePath, "index.sqlite");

        internal static readonly string UserAgent = Environment.GetEnvironmentVariable("TLDR_USER_AGENT") ??
                                                    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

        public static int Main(string[] args)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                IntPtr iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

                if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) {
                    AnsiSupport = false;
                }
                else {
                    outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

                    if (!SetConsoleMode(iStdOut, outConsoleMode)) AnsiSupport = false;
                }
            }


            var showHelp = false;

            var list = false;
            var ignorePlatform = false;

            string language = null;
            string platform = null;
            string search = null;

            var markdown = false;
            string render = null;

            var options = new OptionSet {
                "Usage: tldr command [options]" + Environment.NewLine,
                "Simplified and community-driven man pages" + Environment.NewLine, {
                    "a|list-all", "List all pages",
                    a => list = ignorePlatform = a != null
                }, {
                    "c|clear-cache", "Clear the cache",
                    c => {
                        using (new CustomSpinner("Clearing cache")) {
                            Cache.Clear();
                        }

                        Environment.Exit(0);
                    }
                }, {
                    "f=|render=", "Render a specific markdown file",
                    v => render = v
                }, {
                    "h|help", "Display this help text",
                    h => showHelp = h != null
                }, {
                    "l|list", "List all pages for the current platform and language",
                    l => list = l != null
                }, {
                    "list-os", "List all platforms",
                    o => {
                        Cache.Check();
                        Console.WriteLine(string.Join(Environment.NewLine, ListPlatform()));
                    }
                }, {
                    "list-languages", "List all languages",
                    la => {
                        Cache.Check();
                        Console.WriteLine(string.Join(Environment.NewLine,
                            ListLanguages().Select(x => {
                                try {
                                    return x + ": " + CultureInfo.GetCultureInfo(x.Replace('_', '-')).EnglishName;
                                }
                                catch (CultureNotFoundException) {
                                    return null;
                                }
                            }).Where(x => x != null)));
                    }
                }, {
                    "L=|language=|lang=", "Specifies the preferred language",
                    la => language = la
                }, {
                    "m|markdown", "Show the markdown source of a page",
                    v => markdown = v != null
                }, {
                    "p=|platform=", "Override the default platform",
                    o => platform = o
                }, {
                    "s=|search=", "Search for a string",
                    s => search = s
                }, {
                    "u|update", "Update the cache",
                    u => Updater.Update()
                }, {
                    "self-update", "Check for tldr-sharp updates",
                    u => {
                        SelfUpdater.CheckSelfUpdate();
                        Environment.Exit(0);
                    }
                }, {
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
            }
            catch (OptionException e) {
                Console.WriteLine(e.Message);
                return 1;
            }

            if (showHelp || args.Length == 0) {
                options.WriteOptionDescriptions(Console.Out);
                return args.Length == 0 ? 1 : 0;
            }

            if (render != null) return Page.Render(render);

            // All following functions rely on the cache, so check it.
            Cache.Check();

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

            if (search != null) return Page.Search(search, language, platform);

            var page = string.Empty;
            foreach (string arg in extra) {
                if (arg.StartsWith("-")) {
                    if (page == string.Empty) Console.WriteLine("[ERROR] unknown option '{0}'", arg);
                    return 1;
                }

                page += $" {arg}";
            }

            return page.Trim().Length > 0 ? Page.Print(page, language, platform, markdown) : 0;
        }

        private static void ListAll(bool ignorePlatform, string language = null, string platform = null)
        {
            List<Page> pages;
            if (ignorePlatform) {
                pages = Query("lang = @lang",
                    new[] {new SqliteParameter("@lang", language ?? GetPreferredLanguageOrDefault())});
            }
            else {
                pages = Query("lang = @lang AND (platform = @platform OR platform = 'common')",
                    new[] {
                        new SqliteParameter("@lang", language ?? GetPreferredLanguageOrDefault()),
                        new SqliteParameter("@platform", platform ?? GetPlatform())
                    });
            }
            
            Console.WriteLine(string.Join(Environment.NewLine,
                pages.OrderBy(x => x.Name, StringComparer.Ordinal.WithNaturalSort())));
        }
    }
}