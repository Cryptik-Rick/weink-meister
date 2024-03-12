public class ConsoleMenu
{
    private string[] options;
    private string welcomeMessage;
    private int selectedOptionIndex = 0;

    public ConsoleMenu(string welcomeMessage, string[] options)
    {
        this.welcomeMessage = welcomeMessage;
        this.options = options;
    }

    public int ShowMenu()
    {
        Console.CursorVisible = false;
        ConsoleKeyInfo key;

        do
        {
            Console.Clear();
            Console.WriteLine(welcomeMessage);
            Console.WriteLine();
            DrawMenu();

            key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (selectedOptionIndex > 0)
                        selectedOptionIndex--;
                    break;
                case ConsoleKey.DownArrow:
                    if (selectedOptionIndex < options.Length - 1)
                        selectedOptionIndex++;
                    break;
                case ConsoleKey.Enter:
                    return selectedOptionIndex;
                case ConsoleKey.Q:
                    return -1;
            }
        } while (key.Key != ConsoleKey.Escape && key.Key != ConsoleKey.Enter);

        // Return -1 if the menu was exited without making a selection
        return -1;
    }

    private void DrawMenu()
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (i == selectedOptionIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
            }

            Console.WriteLine($"   {options[i]}");

            Console.ResetColor();
        }
    }
}
