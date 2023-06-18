/*
    SPDX-FileCopyrightText: 2018 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Options;
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


        internal const string ClientSpecVersion = "1.5";
        internal static readonly bool AnsiSupport;

        internal static readonly ConsoleColor DefaultColor = Console.ForegroundColor;

        internal static readonly string CachePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache");

        internal static readonly string DbPath = Path.Combine(CachePath, "index.sqlite");

        internal static readonly string UserAgent = Environment.GetEnvironmentVariable("TLDR_USER_AGENT") ??
                                                    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";


        static Program()
        {
            AnsiSupport = CheckWindowsAnsiSupport();
        }

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
            }

            if (settings.CacheUpdate) {
                Updater.Update();
            }

            // All following functions rely on the cache, so check it.
            Cache.Check();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (settings.ListLanguages) {
                Console.WriteLine(string.Join(Environment.NewLine,
                    ListLanguages().Select(lang =>
                    {
                        string name = Locale.GetLanguageName(lang);

                        return name == null ? lang : $"{lang}:\t{name}";
                    })));
            }

            if (settings.ListPlatform) {
                Console.WriteLine(string.Join(Environment.NewLine, ListPlatform()));
            }

            if (settings.Language != null) {
                if (!CheckLanguage(settings.Language)) {
                    Console.WriteLine("[ERROR] unknown language '{0}'", settings.Language);
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
                    if (builder.Length == 0) Console.WriteLine("[ERROR] unknown option '{0}'", arg);
                    return 1;
                }

                builder.Append($" {arg}");
            }

            string page = builder.ToString();

            return page.Trim().Length > 0 ? PageController.Print(page, settings.Language, settings.Platform, settings.Markdown) : 0;
        }

        private static bool CheckWindowsAnsiSupport()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return true;

            IntPtr iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

            if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) {
                return false;
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

            return SetConsoleMode(iStdOut, outConsoleMode);
        }
    }
}