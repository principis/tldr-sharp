using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Mono.Data.Sqlite;

namespace tldr_sharp
{
    internal static class Index
    {
        internal static void Create()
        {
            using var spinner = new CustomSpinner("Creating index");

            var cacheDir = new DirectoryInfo(Program.CachePath);

            SqliteConnection.CreateFile(Program.DbPath);

            using var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";");
            conn.Open();
            
            using var command = new SqliteCommand(conn);
            using SqliteTransaction transaction = conn.BeginTransaction();
            
            command.CommandText =
                "CREATE TABLE pages (name VARCHAR(100), platform VARCHAR(10), lang VARCHAR(7), local INTEGER)";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE config (parameter VARCHAR(20), value VARCHAR(100))";
            command.ExecuteNonQuery();

            command.CommandText = "INSERT INTO config (parameter, value) VALUES(@parameter, @value)";
            command.Parameters.AddWithValue("@parameter", "last-update");
            command.Parameters.AddWithValue("@value",
                DateTime.UtcNow.Date.ToString(CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;

            // Create indexes
            command.CommandText = "CREATE INDEX names_index ON pages (name, platform, lang, local)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE INDEX lang_platform_index ON pages (lang, platform, name, local)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE INDEX platform_index ON pages (platform)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE INDEX lang_index ON pages (lang)";
            command.ExecuteNonQuery();

            // Add pages
            command.CommandText =
                "INSERT INTO pages (name, platform, lang, local) VALUES(@name, @platform, @lang, @local)";
            List<string> preferredLanguages = Index.GetEnvLanguages();

            foreach (DirectoryInfo dir in cacheDir.EnumerateDirectories("*pages*")) {
                var lang = "en_US";
                var isLocal = true;
                if (dir.Name.Contains(".")) lang = dir.Name.Split('.')[1];

                if (lang != Program.DefaultLanguage &&
                    preferredLanguages.All(x => lang.Substring(0, 2) != x.Substring(0, 2)))
                    isLocal = false;

                foreach (DirectoryInfo osDir in dir.EnumerateDirectories())
                foreach (FileInfo file in osDir.EnumerateFiles("*.md", SearchOption.AllDirectories)) {
                    command.Parameters.AddWithValue("@name",
                        Path.GetFileNameWithoutExtension(file.Name));
                    command.Parameters.AddWithValue("@platform", osDir.Name);
                    command.Parameters.AddWithValue("@lang", lang);
                    command.Parameters.AddWithValue("@local", isLocal);
                    command.ExecuteNonQuery();
                }

                if (lang != Program.DefaultLanguage && !preferredLanguages.Contains(lang)) dir.Delete(true);
            }

            transaction.Commit();
        }
        internal static List<Page> Query(string query, SqliteParameter[] parameters, string page = null)
        {
            using var conn = new SqliteConnection($"Data Source={Program.DbPath};");
            conn.Open();

            string commandString = page == null
                ? $"SELECT name, platform, lang, local FROM pages WHERE {query}"
                : $"SELECT platform, lang, local FROM pages WHERE {query}";

            using var command = new SqliteCommand(commandString, conn);
            command.Parameters.AddRange(parameters);

            using SqliteDataReader reader = command.ExecuteReader();
            var results = new List<Page>();

            while (reader.Read()) {
                if (page == null)
                    results.Add(new Page(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                        reader.GetBoolean(3)));
                else
                    results.Add(new Page(page, reader.GetString(0), reader.GetString(1),
                        reader.GetBoolean(2)));
            }

            return results;
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
            using var conn = new SqliteConnection($"Data Source={Program.DbPath};");
            conn.Open();

            using var command = new SqliteCommand("SELECT DISTINCT platform FROM pages", conn);
            using SqliteDataReader reader = command.ExecuteReader();

            var platform = new SortedSet<string>();
            while (reader.Read()) platform.Add(reader.GetString(0));

            return platform;
        }


        internal static List<string> GetPreferredLanguages()
        {
            var valid = new List<string>();
            ICollection<string> languages = ListLanguages();
            if (languages.Contains(Program.Language)) valid.Add(Program.Language);

            Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty))
                .ToList().ForEach(delegate(string s) {
                    valid.AddRange(languages.Where(l => l.Substring(0, 2).Equals(s)));
                });

            return valid;
        }

        internal static List<string> GetEnvLanguages()
        {
            var languages = new List<string> {Program.Language};
            List<string> envs = Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty))
                .ToList();
            if (envs != null) languages.AddRange(envs);
            return languages;
        }


        internal static string GetPreferredLanguageOrDefault()
        {
            ICollection<string> languages = ListLanguages();
            if (languages.Contains(Program.Language)) return Program.Language;

            IEnumerable<string> prefLanguages = Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty));

            if (prefLanguages != null) {
                foreach (string lang in prefLanguages) {
                    try {
                        return languages.First(x => x.Substring(0, 2).Equals(lang));
                    }
                    catch (InvalidOperationException) { }
                }
            }

            return Program.DefaultLanguage;
        }


        internal static ICollection<string> ListLanguages()
        {
            using var conn = new SqliteConnection($"Data Source={Program.DbPath};");
            conn.Open();
            using var command = new SqliteCommand("SELECT DISTINCT lang FROM pages", conn);
            using SqliteDataReader reader = command.ExecuteReader();
            var languages = new List<string>();
            while (reader.Read()) languages.Add(reader.GetString(0));

            return languages;
        }


        internal static bool CheckLanguage(string language)
        {
            using var conn = new SqliteConnection($"Data Source={Program.DbPath};");
            conn.Open();
            using var command = new SqliteCommand("SELECT 1 FROM pages WHERE lang = @language", conn);
            command.Parameters.Add(new SqliteParameter("@language", language));

            using SqliteDataReader reader = command.ExecuteReader();
            return reader.HasRows;
        }

        internal static void SetPageAsDownloaded(Page page)
        {
            using var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";");
            conn.Open();
            using SqliteCommand command = conn.CreateCommand();
            command.CommandText =
                "UPDATE pages SET local = TRUE WHERE name = @name AND lang = @lang AND platform = @platform";
            command.Parameters.Add(new SqliteParameter("@name", page.Name));
            command.Parameters.Add(new SqliteParameter("@platform", page.Platform));
            command.Parameters.Add(new SqliteParameter("@lang", page.Language));

            command.ExecuteNonQuery();
        }
    }
}