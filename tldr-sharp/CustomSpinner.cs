using System;
using DustInTheWind.ConsoleTools;
using DustInTheWind.ConsoleTools.Spinners;

namespace tldr_sharp
{
    public sealed class CustomSpinner : Spinner
    {
        public CustomSpinner()
        {
            DoneText = new InlineTextBlock("[Done]", ConsoleColor.DarkGreen);
            Display();
        }

        public CustomSpinner(InlineTextBlock label)
        {
            label.Text = label.Text.TrimEnd() + " ";
            Label = label;
            DoneText = new InlineTextBlock("[Done]", ConsoleColor.DarkGreen);
            Display();
        }

        public CustomSpinner(InlineTextBlock label, InlineTextBlock doneText)
        {
            label.Text = label.Text.TrimEnd() + " ";
            Label = label;
            DoneText = doneText;
            Display();
        }

        public void Reset(InlineTextBlock label)
        {
            label.Text = label.Text.TrimEnd() + " ";
            Label = label;
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