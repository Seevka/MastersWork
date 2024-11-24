using Telegram.Bot.Types.ReplyMarkups;

public static class KeyboardFactory
{
    public static ReplyKeyboardMarkup CreateKeyboard(List<string[]> buttonRows, bool resizeKeyboard = true)
    {
        var keyboardButtons = new List<KeyboardButton[]>();

        foreach (var row in buttonRows)
        {
            var buttons = new List<KeyboardButton>();
            foreach (var text in row)
            {
                buttons.Add(new KeyboardButton(text));
            }
            keyboardButtons.Add(buttons.ToArray());
        }

        return new ReplyKeyboardMarkup(keyboardButtons)
        {
            ResizeKeyboard = resizeKeyboard
        };
    }
}
