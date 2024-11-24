public static class KeyboardData
{
    public static List<string[]> StartKeyboard(bool isAdmin)
    {
        var keyboard = new List<string[]>
    {
        new[] { "Створити Бота 🤖", "Редагувати Бота ✏️" },
        new[] { "Запустити Бота ✅", "Зупинити Бота 🤚" },
        new[] { "Видалити Бота ❌", "Отримати інформацію про Бота 👂" },
        new[] { "Допомога 🆘" }
    };

        if (isAdmin)
        {
            keyboard.Add(["Адміністрування"]);
        }

        return keyboard;
    }

    public static List<string[]> AdminPanel =>
    [
        ["Додати адміністратора ✅", "Видалити адміністратора ❌" ],
        ["Список адміністраторів 👂", "Вийти в головне меню 🏃"],
    ];


    public static List<string[]> EditBotKeyboard =>
        [
            ["Редагувати ім'я Бота 🤖", "Редагувати токен Бота ✏️"],
        ["Редагувати питання Бота ✏️", "Отримати інформацію про Бота 👂"],
        ["Вийти в головне меню 🏃"],
    ];

    public static List<string[]> EditQAKeyboard =>
    [
        ["Додати питання", "Редагувати питання"],
        ["Видалити питання"], ["Отримати список"],
        ["Вийти в головне меню 🏃"],
    ];
}
