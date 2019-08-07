using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;

namespace tldr_sharp
{
    internal static class Index
    {
        internal static List<Page> Query(string query, SqliteParameter[] parameters, string page = null)
        {
            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();

                string commandString = page == null
                    ? $"SELECT name, platform, lang, local FROM pages WHERE {query}"
                    : $"SELECT platform, lang, local FROM pages WHERE {query}";
                using (var command = new SqliteCommand(commandString, conn)) {
                    command.Parameters.AddRange(parameters);

                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        var results = new List<Page>();
                        while (reader.Read()) {
                            if (page == null) {
                                results.Add(new Page(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                                    reader.GetBoolean(3)));
                            } else {
                                results.Add(new Page(page, reader.GetString(0), reader.GetString(1),
                                    reader.GetBoolean(2)));
                            }
                        }

                        return results;
                    }
                }
            }
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
            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT platform FROM pages", conn)) {
                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        SortedSet<string> platform = new SortedSet<string>();
                        while (reader.Read()) platform.Add(reader.GetString(0));

                        return platform;
                    }
                }
            }
        }


        internal static List<string> GetPreferredLanguages()
        {
            var valid = new List<string>();
            var languages = ListLanguages();
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
            var envs = Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty))
                .ToList();
            if (envs != null) languages.AddRange(envs);
            return languages;
        }


        internal static string GetPreferredLanguageOrDefault()
        {
            var languages = ListLanguages();
            if (languages.Contains(Program.Language)) return Program.Language;

            var prefLanguages = Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty));

            if (prefLanguages != null) {
                foreach (string lang in prefLanguages) {
                    try {
                        return languages.First(x => x.Substring(0, 2).Equals(lang));
                    } catch (InvalidOperationException) { }
                }
            }

            return Program.DefaultLanguage;
        }


        internal static ICollection<string> ListLanguages()
        {
            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand("SELECT DISTINCT lang FROM pages", conn)) {
                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        var languages = new List<string>();
                        while (reader.Read()) languages.Add(reader.GetString(0));

                        return languages;
                    }
                }
            }
        }


        internal static bool CheckLanguage(string language)
        {
            using (var conn = new SqliteConnection("Data Source=" + Program.DbPath + ";")) {
                conn.Open();
                using (var command = new SqliteCommand("SELECT 1 FROM pages WHERE lang = @language", conn)) {
                    command.Parameters.Add(new SqliteParameter("@language", language));

                    using (SqliteDataReader reader = command.ExecuteReader()) {
                        return reader.HasRows;
                    }
                }
            }
        }
    }
}