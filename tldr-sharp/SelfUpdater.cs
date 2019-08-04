using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace tldr_sharp
{
    internal static class SelfUpdater
    {
        private const string ApiUrl = "https://api.github.com/repos/principis/tldr-sharp/releases/latest";
        private const string UpdateUrl = "https://github.com/principis/tldr-sharp/releases/download/";

        internal static void CheckSelfUpdate()
        {
            using (var client = new WebClient())
            {
                client.Headers.Add("user-agent",
                    "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                string json;
                try
                {
                    json = client.DownloadString(ApiUrl);
                }
                catch (WebException e)
                {
                    Console.Write("[ERROR] Please make sure you have a functioning internet connection. ");
                    Console.WriteLine($"[ERROR] {e.Message}");
                    Environment.Exit(1);
                    return;
                }

                var remoteVersion =
                    new Version(json.Substring(json.IndexOf("tag_name", StringComparison.Ordinal) + 12, 5));

                if (remoteVersion.CompareTo(Assembly.GetExecutingAssembly().GetName().Version) > 0)
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        ConsoleKey response;
                        do
                        {
                            Console.Write("Version {0} is available. Do you want to update? [N/y]: ", remoteVersion);
                            response = Console.ReadKey(false).Key;
                            if (response != ConsoleKey.Enter)
                                Console.WriteLine();
                        } while (response != ConsoleKey.Y && response != ConsoleKey.N && response != ConsoleKey.Enter);

                        if (response != ConsoleKey.Y) return;

                        Console.WriteLine("Select desired method:");
                        Console.WriteLine("0\tx86 Debian package (.deb)");
                        Console.WriteLine("1\tx64 Debian package (.deb)");
                        Console.WriteLine("2\tx86 install script (.sh)");
                        Console.WriteLine("3\tx64 install script (.sh)");
                        string option;
                        int optionNb;
                        do
                        {
                            Console.Write("Enter number 0..3: ");
                            option = Console.ReadLine();
                        } while (!int.TryParse(option, out optionNb) || optionNb > 3 || optionNb < 0);

                        Console.WriteLine();

                        switch (optionNb)
                        {
                            case 0:
                                SelfUpdate(UpdateType.Debian, remoteVersion);
                                break;
                            case 1:
                                SelfUpdate(UpdateType.DebianX64, remoteVersion);
                                break;
                            case 2:
                                SelfUpdate(UpdateType.Script, remoteVersion);
                                break;
                            case 3:
                                SelfUpdate(UpdateType.ScriptX64, remoteVersion);
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Version {0} is available. Download it from {1}", remoteVersion,
                            UpdateUrl);
                    }
                }
                else
                {
                    Console.WriteLine("tldr-sharp is up to date!");
                }
            }
        }

        private static void SelfUpdate(UpdateType type, Version version)
        {
            Console.WriteLine("[INFO] Updating tldr-sharp to v" + version);
            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (File.Exists(tmpPath)) File.Delete(tmpPath);

            string url;
            try
            {
                url = GetUpdateUrl(type, version);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ERROR] {0}", e.Message);
                Environment.Exit(1);
                return;
            }

            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(url, tmpPath);
                }
                catch (WebException e)
                {
                    Console.Write("[ERROR] Please make sure you have a functioning internet connection. ");
                    Console.WriteLine($"{e.Message}");
                    Environment.Exit(1);
                    return;
                }
            }

            var startInfo = new ProcessStartInfo()
                {FileName = "/bin/bash", UseShellExecute = false, RedirectStandardOutput = true};

            switch (type)
            {
                case UpdateType.Debian:
                case UpdateType.DebianX64:
                    startInfo.Arguments = "-c \"sudo dpkg -i " + tmpPath + "\"";
                    break;
                case UpdateType.Script:
                case UpdateType.ScriptX64:
                    startInfo.Arguments = "-c \"bash " + tmpPath + "\"";
                    break;
            }

            using (var process = new Process() {StartInfo = startInfo,})
            {
                process.Start();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                while (!process.HasExited)
                {
                    Thread.Sleep(100);
                }

                File.Delete(tmpPath);

                if (process.ExitCode != 0)
                {
                    Console.WriteLine(
                        "[ERROR] Oops! Something went wrong!\nHelp us improve your experience by sending an error report.");
                    Environment.Exit(1);
                }

                Updater.ClearCache();
                Console.WriteLine("[INFO] Done!" + version);
                Environment.Exit(0);
            }
        }

        private static string GetUpdateUrl(UpdateType type, Version version)
        {
            string downloadUrl = $"{UpdateUrl}v{version.Major}.{version.Minor}.{version.Build}/" +
                                 $"tldr-sharp_{version.Major}.{version.Minor}.{version.Build}";
            switch (type)
            {
                case UpdateType.Debian:
                    return $"{downloadUrl}.deb";
                case UpdateType.DebianX64:
                    return $"{downloadUrl}_x64.deb";
                case UpdateType.Script:
                    return $"{downloadUrl}_linux.sh";
                case UpdateType.ScriptX64:
                    return $"{downloadUrl}_linux_x64.sh";
            }

            return null;
        }

        private enum UpdateType
        {
            Debian,
            DebianX64,
            Script,
            ScriptX64
        }
    }
}