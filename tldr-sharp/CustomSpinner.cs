using System;
using DustInTheWind.ConsoleTools;
using DustInTheWind.ConsoleTools.Spinners;

namespace tldr_sharp
{
    public sealed class CustomSpinner : Spinner
    {
        public CustomSpinner(InlineTextBlock label = null, InlineTextBlock doneText = null)
        {
            if (label != null) label.Text = label.Text.TrimEnd() + " ";
            Label = label ?? Label;
            DoneText = doneText ?? new InlineTextBlock("[Done]", ConsoleColor.DarkGreen);
            Display();
        }

        public void Reset(InlineTextBlock label)
        {
            Label = label.Text = label.Text.TrimEnd() + " ";
            DoneText = new InlineTextBlock("[Done]", ConsoleColor.DarkGreen);
            Display();
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            Console.ResetColor();
        }

        public static void Run(InlineTextBlock label, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var spinner = new CustomSpinner(label);

            spinner.Display();
            try {
                action();
                spinner.Close();
            }
            catch (Exception e) {
                spinner.DoneText = null;
                string error = e.Message;
                if (error.StartsWith("Error:")) error = error.Substring(7);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("[ERROR]");
                Console.ResetColor();
                Console.Write($" {error}");
                
                spinner.Close();
                Environment.Exit(1);
            }
        }
    }
}