using System;

namespace tldr_sharp
{
    public class CustomConsole
    {
        public static void WriteError(string error)
        {
            if (error.StartsWith("Error:")) error = error.Substring(7);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("[ERROR]");
            Console.ResetColor();
            Console.WriteLine($" {error}");
        }
        
        public static void WriteWarning(string error)
        {
            if (error.StartsWith("Error:")) error = error.Substring(7);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("[WARN]");
            Console.ResetColor();
            Console.WriteLine($" {error}");
        }
    }
}