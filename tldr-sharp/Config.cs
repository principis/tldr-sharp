/*
    SPDX-FileCopyrightText: 2023 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.IO;

namespace tldr_sharp
{
    public struct Config
    {
        static Config()
        {
            AnsiSupport = Windows.CheckAnsiSupport();
        }

        public const string ClientSpecification = "2.2";

        internal static readonly string CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tldr", "cache");
        internal static readonly string DbPath = Path.Combine(CachePath, "index.sqlite");

        internal static readonly string UserAgent = Environment.GetEnvironmentVariable("TLDR_USER_AGENT") ??
                                                    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

        internal static readonly bool AnsiSupport;

        internal const string ApiUrl = "https://api.github.com/repos/principis/tldr-sharp/releases/latest";
        internal const string RemoteUrl = "https://raw.githubusercontent.com/tldr-pages/tldr/main/pages";

        internal const string ArchiveRemote = "https://github.com/tldr-pages/tldr/releases/latest/download/tldr.zip";

        internal const string NewPageUrl = "https://github.com/tldr-pages/tldr/issues/new?title=page%20request:%20";
        internal const string NewTranslationUrl = "https://github.com/tldr-pages/tldr/blob/main/CONTRIBUTING.md#translations";
    }
}