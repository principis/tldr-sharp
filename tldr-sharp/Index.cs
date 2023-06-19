/*
    SPDX-FileCopyrightText: 2019 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Spectre.Console;

namespace tldr_sharp
{
    internal static class Index
    {
        internal static void Create()
        {
            AnsiConsole.Status().Start("Creating index...", Create);
        }


        internal static DateTime LastUpdate()
        {
            using var conn = new SqliteConnection("Data Source=" + Config.DbPath + ";");
            conn.Open();

            using var command = new SqliteCommand("SELECT value FROM config WHERE parameter = @parameter", conn);
            command.Parameters.Add(new SqliteParameter("@parameter", "last-update"));

            using SqliteDataReader reader = command.ExecuteReader();
            reader.Read();
            return DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture);
        }

        internal static void Create(StatusContext ctx)
        {
            var cacheDir = new DirectoryInfo(Config.CachePath);

            using var conn = new SqliteConnection("Data Source=" + Config.DbPath + ";");
            conn.Open();

            using SqliteTransaction transaction = conn.BeginTransaction();
            using (SqliteCommand command = conn.CreateCommand()) {
                command.CommandText = "CREATE TABLE pages (name VARCHAR(100), platform VARCHAR(10), lang VARCHAR(7), local INTEGER)";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE TABLE config (parameter VARCHAR(20), value VARCHAR(100))";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO config (parameter, value) VALUES(@parameter, @value)";
                command.Parameters.AddWithValue("@parameter", "last-update");
                command.Parameters.AddWithValue("@value",
                    DateTime.UtcNow.Date.ToString(CultureInfo.InvariantCulture));
                command.ExecuteNonQuery();

                // Create indexes
                command.CommandText = "CREATE INDEX names_index ON pages (name, platform, lang, local)";
                command.ExecuteNonQuery();
                command.CommandText = "CREATE INDEX lang_platform_index ON pages (lang, platform, name, local)";
                command.ExecuteNonQuery();
                command.CommandText = "CREATE INDEX platform_index ON pages (platform)";
                command.ExecuteNonQuery();
                command.CommandText = "CREATE INDEX lang_index ON pages (lang)";
                command.ExecuteNonQuery();
            }

            // Add pages
            var pageCommand = conn.CreateCommand();
            pageCommand.CommandText =
                "INSERT INTO pages (name, platform, lang, local) VALUES(@name, @platform, @lang, @local)";
            List<string> preferredLanguages = Locale.GetEnvLanguages();

            var nameParam = pageCommand.Parameters.AddWithValue("@name", null);
            var platformParam = pageCommand.Parameters.AddWithValue("@platform", null);
            var langParam = pageCommand.Parameters.AddWithValue("@lang", null);
            var localParam = pageCommand.Parameters.AddWithValue("@local", null);


            foreach (DirectoryInfo dir in cacheDir.EnumerateDirectories("*pages*")) {
                string lang = Locale.DefaultLanguage;
                bool isLocal = true;
                if (dir.Name.Contains(".")) lang = dir.Name.Split('.')[1];

                if (lang != Locale.DefaultLanguage &&
                    preferredLanguages.All(x => lang[..2] != x[..2]))
                    isLocal = false;

                foreach (DirectoryInfo osDir in dir.EnumerateDirectories())
                foreach (FileInfo file in osDir.EnumerateFiles("*.md", SearchOption.AllDirectories)) {
                    nameParam.Value = Path.GetFileNameWithoutExtension(file.Name);
                    platformParam.Value = osDir.Name;
                    langParam.Value = lang;
                    localParam.Value = isLocal;
                    pageCommand.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }

        private static List<Page> Query(string queryString, SqliteParameter[] parameters, string page = null)
        {
            using var conn = new SqliteConnection($"Data Source={Config.DbPath};");
            conn.Open();

            string commandString = page == null
                ? $"SELECT name, platform, lang, local FROM pages WHERE {queryString}"
                : $"SELECT platform, lang, local FROM pages WHERE {queryString}";

            using var command = new SqliteCommand(commandString, conn);
            command.Parameters.AddRange(parameters);

            using SqliteDataReader reader = command.ExecuteReader();
            var results = new List<Page>();

            while (reader.Read()) {
                if (page == null) {
                    results.Add(new Page(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                        reader.GetBoolean(3)));
                }
                else {
                    results.Add(new Page(page, reader.GetString(0), reader.GetString(1),
                        reader.GetBoolean(2)));
                }
            }

            return results;
        }

        internal static List<Page> QueryByLanguageAndPlatform(string language, string platform)
        {
            return Query("lang = @lang AND (platform = @platform OR platform = 'common')",
                new[]
                {
                    new SqliteParameter("@lang", language ?? Locale.GetPreferredLanguageOrDefault()),
                    new SqliteParameter("@platform", platform ?? GetPlatform())
                });
        }

        internal static List<Page> QueryByName(string page)
        {
            return Query("name = @name ORDER BY platform DESC",
                new[] {new SqliteParameter("@name", page)},
                page);
        }

        internal static List<Page> QueryByLanguage(string language)
        {
            return Query("lang = @lang",
                new[] {new SqliteParameter("@lang", language ?? Locale.GetPreferredLanguageOrDefault())});
        }

        internal static string GetPlatform()
        {
            switch (Environment.OSVersion.Platform) {
                case PlatformID.MacOSX:
                    return "osx";
                case PlatformID.Unix:
                    return "linux";
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    return "windows";
                default:
                    return "common";
            }
        }


        internal static IEnumerable<string> ListPlatform()
        {
            using var conn = new SqliteConnection($"Data Source={Config.DbPath};");
            conn.Open();

            using var command = new SqliteCommand("SELECT DISTINCT platform FROM pages", conn);
            using SqliteDataReader reader = command.ExecuteReader();

            var platform = new SortedSet<string>();
            while (reader.Read()) {
                platform.Add(reader.GetString(0));
            }

            return platform;
        }


        internal static List<string> ListLanguages()
        {
            using var conn = new SqliteConnection($"Data Source={Config.DbPath};");
            conn.Open();
            using var command = new SqliteCommand("SELECT DISTINCT lang FROM pages", conn);
            using SqliteDataReader reader = command.ExecuteReader();
            var languages = new List<string>();
            while (reader.Read()) {
                languages.Add(reader.GetString(0));
            }

            return languages;
        }


        internal static bool CheckLanguage(string language)
        {
            using var conn = new SqliteConnection($"Data Source={Config.DbPath};");
            conn.Open();
            using var command = new SqliteCommand("SELECT 1 FROM pages WHERE lang = @language", conn);
            command.Parameters.Add(new SqliteParameter("@language", language));

            using SqliteDataReader reader = command.ExecuteReader();
            return reader.HasRows;
        }

        internal static void SetPageAsDownloaded(Page page)
        {
            using var conn = new SqliteConnection("Data Source=" + Config.DbPath + ";");
            conn.Open();
            using SqliteCommand command = conn.CreateCommand();
            command.CommandText = "UPDATE pages SET local = TRUE WHERE name = @name AND lang = @lang AND platform = @platform";
            command.Parameters.Add(new SqliteParameter("@name", page.Name));
            command.Parameters.Add(new SqliteParameter("@platform", page.Platform));
            command.Parameters.Add(new SqliteParameter("@lang", page.Language));

            command.ExecuteNonQuery();
        }
    }
}