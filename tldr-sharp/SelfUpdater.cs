/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using DustInTheWind.ConsoleTools.Controls.InputControls;

namespace tldr_sharp
{
    internal static class SelfUpdater
    {
        private const string ApiUrl = "https://api.github.com/repos/principis/tldr-sharp/releases/latest";
        private const string UpdateUrl = "https://github.com/principis/tldr-sharp/releases/download/";

        private const string ScriptUrl =
            "https://raw.githubusercontent.com/principis/tldr-sharp/main/scripts/linux_install.sh";

        internal static void CheckSelfUpdate()
        {
            var spinner = new CustomSpinner("Checking for update");

            using var client = new WebClient();
            client.Headers.Add("user-agent", Program.UserAgent);

            string json;

            try {
                json = client.DownloadString(ApiUrl);
            }
            catch (WebException e) {
                CustomConsole.WriteError(
                    $"{e.Message}{Environment.NewLine}Please make sure you have a functioning internet connection.");

                Environment.Exit(1);
                return;
            }

            var remoteVersion = new Version(
                json.Split(new[] {","}, StringSplitOptions.None)
                    .First(s => s.Contains("tag_name"))
                    .Split(':')[1]
                    .Trim('"', ' ', 'v'));

            spinner.Dispose();
            if (remoteVersion.CompareTo(Assembly.GetExecutingAssembly().GetName().Version) <= 0) {
                Console.WriteLine("tldr-sharp is up to date!");
                return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                var updateQuestion =
                    new YesNoQuestion($"Version {remoteVersion} is available. Do you want to update?") {
                        DefaultAnswer = YesNoAnswer.No,
                        ForegroundColor = null,
                        QuestionForegroundColor = null,
                        BackgroundColor = null,
                        QuestionBackgroundColor = null
                    };

                if (updateQuestion.ReadAnswer() != YesNoAnswer.Yes) return;

                Console.WriteLine("Select desired method:");
                Console.WriteLine("0\tDebian package (.deb)");
                Console.WriteLine("1\tRPM package (.rpm)");
                Console.WriteLine("2\tinstall script (.sh)");

                string option;
                int optionNb;
                do {
                    Console.Write("Enter number 0..2: ");
                    option = Console.ReadLine();
                } while (!int.TryParse(option, out optionNb) || optionNb > 3 || optionNb < 0);

                Console.WriteLine();

                switch (optionNb) {
                    case 0:
                        SelfUpdate(UpdateType.Debian, remoteVersion);
                        break;
                    case 1:
                        SelfUpdate(UpdateType.Rpm, remoteVersion);
                        break;
                    case 2:
                        SelfUpdate(UpdateType.Script, remoteVersion);
                        break;
                }
            }
            else {
                Console.WriteLine("Version {0} is available. Download it from {1}", remoteVersion,
                    "https://github.com/principis/tldr-sharp/releases/");
            }
        }

        private static void SelfUpdate(UpdateType type, Version version)
        {
            Console.WriteLine($"Updating tldr-sharp to v{version}");
            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            if (File.Exists(tmpPath)) File.Delete(tmpPath);

            string url = GetUpdateUrl(type, version);

            using (var client = new WebClient()) {
                try {
                    client.DownloadFile(url, tmpPath);
                }
                catch (WebException e) {
                    CustomConsole.WriteError(
                        $"{e.Message}{Environment.NewLine}Please make sure you have a functioning internet connection.");
                    Environment.Exit(1);
                    return;
                }
            }

            var startInfo = new ProcessStartInfo {
                FileName = "/bin/bash",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = type switch {
                    UpdateType.Debian => "-c \"sudo dpkg -i " + tmpPath + "\"",
                    UpdateType.Rpm => "-c \"sudo rpm -i " + tmpPath + "\"",
                    UpdateType.Script => "-c \"bash " + tmpPath + "\"",
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                }
            };

            using (var process = new Process {StartInfo = startInfo}) {
                process.Start();
                Console.Write(process.StandardOutput.ReadToEnd());

                while (!process.HasExited) Thread.Sleep(100);

                File.Delete(tmpPath);

                if (process.ExitCode != 0) {
                    Console.WriteLine(
                        "[ERROR] Oops! Something went wrong!{0}Help us improve your experience by sending an error report.",
                        Environment.NewLine);
                    Environment.Exit(1);
                }
            }

            CustomSpinner.Run("Clearing Cache", Cache.Clear);

            Console.WriteLine("[INFO] v{0} installed.", version);
            Environment.Exit(0);
        }

        private static string GetUpdateUrl(UpdateType type, Version version)
        {
            string downloadUrl = $"{UpdateUrl}v{version.Major}.{version.Minor}.{version.Build}/tldr-sharp";

            return type switch {
                UpdateType.Debian => $"{downloadUrl}.deb",
                UpdateType.Rpm => $"{downloadUrl}.rpm",
                UpdateType.Script => ScriptUrl,
                _ => null
            };
        }

        private enum UpdateType
        {
            Debian,
            Rpm,
            Script
        }
    }
}