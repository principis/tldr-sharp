/*
    SPDX-FileCopyrightText: 2020 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace tldr_sharp
{
    public static class PageParser
    {
        private const string Spacing = "   ";

        private static readonly Regex PlaceholderRegex = new Regex("({{)[^}}]*}?(}})");

        internal static string ParseLine(string line, bool formatted = false)
        {
            line = line.EscapeMarkup();
            return Config.AnsiSupport ? ParseAnsiLine(line, formatted) : ParsePlainLine(line, formatted);
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
            var builder = new StringBuilder(line);

            foreach (var match in PlaceholderRegex.Matches(line).Reverse()) {
                if (match.Groups.Count != 3) continue;

                builder.Remove(match.Groups[2].Index, 2);
                builder.Insert(match.Groups[2].Index, "[/]");

                builder.Remove(match.Groups[1].Index, 2);
                builder.Insert(match.Groups[1].Index, "[green]");
            }

            int urlStart = builder.IndexOf("<");
            if (urlStart != -1) {
                int urlEnd = builder.IndexOf(">", urlStart);
                if (urlEnd != -1)
                    builder.Remove(urlEnd, 1).Insert(urlEnd, "[/][bold] ").Insert(urlStart + 1, "[underline]").Remove(urlStart, 1)
                        .Insert(urlStart, "[/]");
            }

            switch (builder[0]) {
                case '#':
                    builder.Remove(0, 2)
                        .Insert(0, "[underline bold]")
                        .Append("[/]");
                    if (formatted) builder.AppendLine();
                    break;
                case '>':
                    builder.Remove(0, 2).Insert(0, "[bold]").Append("[/]");
                    break;
                case '-':
                    if (formatted) builder.Insert(0, Environment.NewLine);
                    break;
                case '`':
                    builder.Remove(0, 1).Remove(builder.Length - 1, 1).Insert(0, "[maroon]")
                        .Insert(0, formatted ? Spacing : "")
                        .Append("[/]");
                    break;
            }

            return builder.ToString();
        }

        internal static string ParseSearchLine(string line)
        {
            var builder = new StringBuilder(line.EscapeMarkup());

            if (line.Contains("{{")) builder.Replace("{{", "[green]").Replace("}}", "[/]");

            switch (builder[0]) {
                case '#':
                    builder.Remove(0, 2);
                    break;
                case '>':
                    builder.Remove(0, 2);
                    break;
                case '-':
                    break;
                case '`':
                    builder.Remove(0, 1).Remove(builder.Length - 1, 1).Insert(0, "[maroon]").Append("[/]");
                    break;
            }

            return builder.ToString();
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
                for (int i = startIndex; i < maxSearchLength; ++i) {
                    if (char.ToLower(sb[i]) == char.ToLower(value[0])) {
                        index = 1;
                        while (index < length && char.ToLower(sb[i + index]) == char.ToLower(value[index])) {
                            ++index;
                        }

                        if (index == length) return i;
                    }
                }

                return -1;
            }

            for (int i = startIndex; i < maxSearchLength; ++i) {
                if (sb[i] == value[0]) {
                    index = 1;
                    while (index < length && sb[i + index] == value[index]) {
                        ++index;
                    }

                    if (index == length) return i;
                }
            }

            return -1;
        }
    }
}