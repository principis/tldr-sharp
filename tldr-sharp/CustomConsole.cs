/*
    SPDX-FileCopyrightText: 2020 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;

namespace tldr_sharp
{
    public static class CustomConsole
    {
        public static void WriteError(string error)
        {
            if (error.StartsWith("Error:")) error = error.Substring(7);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("[ERROR]");
            Console.ResetColor();
            Console.WriteLine($" {error}");
        }

        public static void WriteWarning(string warning)
        {
            if (warning.StartsWith("Error:")) warning = warning.Substring(7);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("[WARN]");
            Console.ResetColor();
            Console.WriteLine($" {warning}");
        }
    }
}