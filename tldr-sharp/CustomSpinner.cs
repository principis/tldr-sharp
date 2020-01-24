using System;
using System.Linq;
using DustInTheWind.ConsoleTools;
using DustInTheWind.ConsoleTools.Spinners;

namespace tldr_sharp
{
    public sealed class CustomSpinner : Spinner
    {
        public CustomSpinner(InlineTextBlock label = null, InlineTextBlock doneText = null)
        {
            if (label != null)
            {
                label.Text = label.Text.TrimEnd() + " ";
            }
            Label = label ?? Label;
            DoneText = doneText ?? new InlineTextBlock("[Done]", ConsoleColor.DarkGreen);
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
            catch {
                spinner.DoneText = new InlineTextBlock("[Error]", ConsoleColor.DarkRed);
                spinner.Close();
                throw;
            }
        }
    }
}