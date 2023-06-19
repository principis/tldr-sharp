/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Spectre.Console;

namespace tldr_sharp
{
    internal static class SelfUpdater
    {
        internal static void Check()
        {
            AnsiConsole.Status().StartAsync("Checking for update", Check).Wait();
        }

        private static async Task Check(StatusContext ctx)
        {
            string json;

            try {
                json = await HttpUtils.DownloadString(Config.ApiUrl);
            }
            catch (HttpRequestException e) {
                Cli.WriteErrorMessage($"{e.Message}{Environment.NewLine}Please make sure you have a functioning internet connection.");

                Environment.Exit(1);
                return;
            }

            var remoteVersion = new Version(
                json.Split(new[] {","}, StringSplitOptions.None)
                    .First(s => s.Contains("tag_name"))
                    .Split(':')[1]
                    .Trim('"', ' ', 'v'));

            if (remoteVersion.CompareTo(Assembly.GetExecutingAssembly().GetName().Version) <= 0) {
                Cli.WriteMessage("[green]tldr-sharp is up to date![/]");
                return;
            }

            Cli.WriteMessage($"[green]Version {remoteVersion} is available.[/]");
            Cli.WriteLine($"Download it from https://github.com/principis/tldr-sharp/releases/");
        }
    }
}