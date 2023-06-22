/*
    SPDX-FileCopyrightText: 2023 Arthur Bols <arthur@bols.dev>

    SPDX-License-Identifier: GPL-3.0-or-later
*/

using System;
using System.Runtime.InteropServices;

namespace tldr_sharp;

internal static class Windows
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    public static bool CheckAnsiSupport()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT) return true;

        IntPtr iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

        if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) return false;

        outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

        return SetConsoleMode(iStdOut, outConsoleMode);
    }
}