using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace tldr_sharp
{
    public static class Locale
    {
        internal const string DefaultLanguage = "en";
        private static readonly string Language;
        private static readonly string ConfiguredLanguage;

        static Locale()
        {
            string language = CultureInfo.CurrentCulture.Name.Replace('-', '_');
            if (language == "") {
                language = DefaultLanguage;
            }

            Language = language;

            string configuredLanguage = Environment.GetEnvironmentVariable("TLDR_LANGUAGE");
            ConfiguredLanguage = configuredLanguage != null && IsValidLanguageCode(configuredLanguage)
                ? configuredLanguage
                : null;
        }

        internal static string GetLanguageName(string code)
        {
            try {
                return CultureInfo.GetCultureInfo(code.Replace('_', '-')).EnglishName;
            }
            catch (CultureNotFoundException) {
                string language = TrimPosixLang(code);

                string name = language.Length switch {
                    2 => Iso639.Language.FromPart1(language)?.Name,
                    3 => Iso639.Language.FromPart2(language)?.Name ?? Iso639.Language.FromPart3(language)?.Name,
                    _ => null
                };

                return name ?? code;
            }
        }

        private static bool IsValidLanguageCode(string code)
        {
            string name;
            try {
                name = CultureInfo.GetCultureInfo(code.Replace('_', '-')).EnglishName;
            }
            catch (CultureNotFoundException) {
                string language = TrimPosixLang(code);

                name = language.Length switch {
                    2 => Iso639.Language.FromPart1(language)?.Name,
                    3 => Iso639.Language.FromPart2(language)?.Name ?? Iso639.Language.FromPart3(language)?.Name,
                    _ => null
                };
            }

            return name != null;
        }

        internal static List<string> GetPreferredLanguages()
        {
            var valid = new List<string>();
            if (Language == null) return valid;

            List<string> languages = Index.ListLanguages();
            List<string> trimLanguages = languages.Select(TrimPosixLang).ToList();

            foreach (string envLang in GetEnvLanguages()) {
                if (languages.Contains(envLang)) {
                    valid.Add(envLang);
                }
                else {
                    int index = trimLanguages.IndexOf(envLang);
                    while (index != -1 && index - 1 < trimLanguages.Count) {
                        valid.Add(languages[index]);

                        index = trimLanguages.IndexOf(envLang, index + 1);
                    }
                }
            }

            return valid;
        }

        internal static List<string> GetEnvLanguages()
        {
            var languages = new List<string>();

            if (ConfiguredLanguage != null) {
                languages.Add(ConfiguredLanguage);
            }

            if (Language == null) {
                return languages;
            }

            List<string> envs = Environment.GetEnvironmentVariable("LANGUAGE")
                ?.Split(':')
                .Where(x => !x.Equals(string.Empty))
                .Where(IsValidLanguageCode)
                .ToList();
            if (envs != null) {
                languages.AddRange(envs);
            }

            string parsed = TrimPosixLang(Language);
            if (!languages.Contains(parsed)) {
                languages.Add(Language);
                languages.Add(parsed);
            }
            else {
                int index = languages.IndexOf(parsed);
                languages.Insert(index, Language);
            }

            return languages;
        }

        private static string TrimPosixLang(string bcp)
        {
            if (!bcp.Contains('_')) return bcp;

            int index = bcp.IndexOf('_');

            return bcp.Substring(0, index);
        }

        internal static string GetPreferredLanguageOrDefault()
        {
            if (ConfiguredLanguage != null) {
                return ConfiguredLanguage;
            }

            if (Language == null) {
                return DefaultLanguage;
            }

            List<string> languages = GetPreferredLanguages();
            return languages.Count == 0 ? DefaultLanguage : languages[0];
        }
    }
}