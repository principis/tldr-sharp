using System;
using System.IO;
using System.Net;

namespace tldr_sharp
{
    internal readonly struct Page : IEquatable<Page>
    {
        public readonly string Name;
        public readonly string Platform;

        public readonly string Language;
        public string DirLanguage => Language == Program.DefaultLanguage ? string.Empty : $".{Language}";

        public readonly bool Local;

        public Page(string name, string platform, string language, bool local)
        {
            Name = name;
            Platform = platform;
            Language = language;
            Local = local;
        }

        public bool Equals(Page other) =>
            (Name, Platform, Language, Local) == (other.Name, other.Platform, other.Language, Local);

        public override int GetHashCode() => (Name, Platform, Language, Local).GetHashCode();

        public override string ToString()
        {
            return Name;
        }

        internal string GetPath()
        {
            return Path.Combine(Program.CachePath,
                "pages" + DirLanguage, Platform, $"{Name}.md");
        }

        internal void Download()
        {
            try {
                Cache.DownloadPage(this);
            }
            catch (Exception e) {
                throw new WebException(
                    $"An error has occurred downloading the requested page: {e.Message.Substring(7)}");
            }
        }
    }
}