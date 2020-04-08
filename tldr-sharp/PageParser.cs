using System;
using System.Text;

namespace tldr_sharp
{
    public static class PageParser
    {
        private const string Spacing = "   ";

        internal static string ParseLine(string line, bool formatted = false)
        {
            return Program.AnsiSupport ? ParseAnsiLine(line, formatted) : ParsePlainLine(line, formatted);
        }

        private static string ParsePlainLine(string line, bool formatted = false)
        {
            var sb = new StringBuilder(line);

            if (line.Contains("{{")) sb.Replace("{{", "").Replace("}}", "");

            switch (sb[0]) {
                case '#':
                    sb.Remove(0, 2);
                    if (formatted) sb.AppendLine();
                    break;
                case '>':
                    sb.Remove(0, 2);
                    break;
                case '-':
                    if (formatted) sb.Insert(0, Environment.NewLine);
                    break;
                case '`':
                    sb.Remove(0, 1).Remove(sb.Length - 1, 1).Insert(0, formatted ? Spacing : "");
                    break;
            }

            return sb.ToString();
        }

        private static string ParseAnsiLine(string line, bool formatted = false)
        {
            var sb = new StringBuilder(line);

            if (line.Contains("{{")) sb.Replace("{{", Ansi.Green).Replace("}}", Ansi.Red);

            int urlStart = sb.IndexOf("<");
            if (urlStart != -1) {
                int urlEnd = sb.IndexOf(">", urlStart);
                if (urlEnd != -1)
                    sb.Insert(urlStart, Ansi.BoldOff + Ansi.Underline).Insert(urlEnd, Ansi.Off);
            }

            switch (sb[0]) {
                case '#':
                    sb.Remove(0, 2)
                        .Insert(0, Ansi.Underline + Ansi.Bold)
                        .Append(Ansi.Off);
                    if (formatted) sb.AppendLine();
                    break;
                case '>':
                    sb.Remove(0, 2).Insert(0, Ansi.Bold).Append(Ansi.Off);
                    break;
                case '-':
                    if (formatted) sb.Insert(0, Environment.NewLine);
                    break;
                case '`':
                    sb.Remove(0, 1).Remove(sb.Length - 1, 1).Insert(0, Ansi.Red).Insert(0, formatted ? Spacing : "")
                        .Append(Ansi.Off);
                    break;
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Returns the index of the start of the contents in a StringBuilder
        /// </summary>
        /// <param name="value">The string to find</param>
        /// <param name="startIndex">The starting index.</param>
        /// <param name="ignoreCase">if set to <c>true</c> it will ignore case</param>
        /// <returns></returns>
        private static int IndexOf(this StringBuilder sb, string value, int startIndex = 0, bool ignoreCase = false)
        {
            int index;
            int length = value.Length;
            int maxSearchLength = sb.Length - length + 1;

            if (ignoreCase) {
                for (int i = startIndex; i < maxSearchLength; ++i)
                    if (char.ToLower(sb[i]) == char.ToLower(value[0])) {
                        index = 1;
                        while (index < length && char.ToLower(sb[i + index]) == char.ToLower(value[index])) ++index;

                        if (index == length) return i;
                    }

                return -1;
            }

            for (int i = startIndex; i < maxSearchLength; ++i)
                if (sb[i] == value[0]) {
                    index = 1;
                    while (index < length && sb[i + index] == value[index])
                        ++index;

                    if (index == length) return i;
                }

            return -1;
        }
    }
}