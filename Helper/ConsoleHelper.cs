
namespace SC_TranslationSetup.Helper
{
    /// <summary>
    /// Provides helper methods for writing formatted messages and interactive option lists to the console.
    /// </summary>
    /// <remarks>This class includes methods for displaying messages with different color schemes to indicate status
    /// (such as muted, success, or warning), as well as a method for presenting a selectable list of options in the
    /// console. All methods are static and intended for use in console applications to enhance user interaction and output
    /// readability.</remarks>
    internal static class ConsoleHelper
    {
        /// <summary>
        /// Writes the specified message to the console using a muted (dark gray) foreground color.
        /// </summary>
        /// <remarks>The original console foreground color is restored after the message is written.</remarks>
        /// <param name="message">The message to display in the console output.</param>
        internal static void WriteMutedLine(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Writes the specified message to the console in green to indicate a successful operation.
        /// </summary>
        /// <remarks>The console's foreground color is temporarily changed to green while the message is written,
        /// then restored to its original value.</remarks>
        /// <param name="message">The message to display in the console output.</param>
        internal static void WriteSuccessLine(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Writes the specified message to the console in red to indicate a warning.
        /// </summary>
        /// <remarks>The method temporarily changes the console's foreground color to red for the duration of the
        /// message output, then restores the original color. Use this method to highlight warning messages in console
        /// applications.</remarks>
        /// <param name="message">The warning message to display in the console output.</param>
        internal static void WriteWarningLine(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        /// <summary>
        /// Renders a list of selectable options to the console, highlighting the currently selected option.
        /// </summary>
        /// <remarks>This method updates the console output in place, allowing for interactive menu selection
        /// scenarios. The console's cursor and color settings are temporarily modified during rendering and restored after
        /// each option is written.</remarks>
        /// <param name="options">An array of strings representing the options to display in the console.</param>
        /// <param name="selectedIndex">The zero-based index of the option to highlight as selected.</param>
        /// <param name="startRow">The row position in the console at which to begin rendering the options.</param>
        /// <param name="originalForeground">The original foreground color to restore for non-selected options.</param>
        /// <param name="originalBackground">The original background color to restore for non-selected options.</param>
        private static void RenderOptions(
            string[] options,
            int selectedIndex,
            int startRow,
            ConsoleColor originalForeground,
            ConsoleColor originalBackground)
        {
            Console.SetCursorPosition(0, startRow);
            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("> ");
                }
                else
                {
                    Console.BackgroundColor = originalBackground;
                    Console.ForegroundColor = originalForeground;
                    Console.Write("  ");
                }

                Console.WriteLine(options[i].PadRight(Console.WindowWidth - 2));
            }
            Console.BackgroundColor = originalBackground;
            Console.ForegroundColor = originalForeground;
            Console.SetCursorPosition(0, startRow + selectedIndex);
        }

        /// <summary>
        /// Displays a list of options in the console and allows the user to select one using the arrow keys.
        /// </summary>
        /// <remarks>The method highlights the currently selected option and updates the display as the user
        /// navigates with the Up and Down arrow keys. The selection is confirmed with the Enter key. The console colors are
        /// temporarily changed for display and restored after the operation.</remarks>
        /// <param name="title">The title to display above the list of options. Can be null or empty if no title is desired.</param>
        /// <param name="options">The array of option strings to display for selection. Cannot be null or empty.</param>
        /// <param name="selectedPrefix">An optional prefix to display before the selected option when it is confirmed. If null or empty, no prefix is
        /// used.</param>
        /// <returns>The zero-based index of the selected option if the user confirms a selection; otherwise, -1 if the user cancels
        /// the selection by pressing the Escape key.</returns>
        internal static int SelectFromList(string? title, string[] options, string? selectedPrefix = null)
        {
            Console.WriteLine();
            var originalForeground = Console.ForegroundColor;
            var originalBackground = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(title);
            Console.ForegroundColor = originalForeground;
            int startRow = Console.CursorTop;
            int selectedIndex = 0;

            RenderOptions(options, selectedIndex, startRow, originalForeground, originalBackground);

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = selectedIndex <= 0 ? options.Length - 1 : selectedIndex - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex >= options.Length - 1 ? 0 : selectedIndex + 1;
                        break;
                    case ConsoleKey.Enter:
                        string lineBreaks = new('\n', options.Length - selectedIndex + 1);
                        Console.Write(lineBreaks);
                        if (!string.IsNullOrWhiteSpace(selectedPrefix))
                            Console.WriteLine($"{selectedPrefix}{options[selectedIndex]}");
                        return selectedIndex;
                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        return -1;
                }


                RenderOptions(options, selectedIndex, startRow, originalForeground, originalBackground);
            }
        }
    }
}