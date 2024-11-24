using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

string projectRoot = Directory.GetCurrentDirectory();
string filePath = Path.Combine(projectRoot, "data.json");
string jsonString = System.IO.File.ReadAllText(filePath);
var data = JsonSerializer.Deserialize<BotData>(jsonString);


Dictionary<string, string> qaDictionary;
var qaList = JsonSerializer.Deserialize<List<QuestionAnswer>>(data!.QA!);
qaDictionary = qaList!.ToDictionary(qa => qa.Question, qa => qa.Answer);
var keyboardButtons = qaDictionary.Keys
    .Select(question => new KeyboardButton(question))
    .Chunk(2)
    .Select(chunk => chunk.ToArray())
    .ToArray();

ReplyKeyboardMarkup dynamicKeyboard = new(keyboardButtons)
{
    ResizeKeyboard = true
};

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(data!.Token!, cancellationToken: cts.Token);
var me = await bot.GetMe();
await bot.DeleteWebhook();
await bot.DropPendingUpdates();
bot.OnError += OnError;
bot.OnMessage += OnMessage;

Console.WriteLine($"@{me.Username} is running... Press Escape to terminate");
//while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
//cts.Cancel();
await Task.Delay(Timeout.Infinite, cts.Token);


async Task OnError(Exception exception, HandleErrorSource source)
{
    Console.WriteLine(exception);
    await Task.Delay(2000, cts.Token);
}

async Task OnMessage(Message msg, UpdateType type)
{
    if (msg.Text == "/start")
    {
        await bot.SendMessage(
        chatId: msg.Chat,
        text: $"Привіт, у Telegram боті {me.Username} ти знайдеш відповіді на всі свої запитання!",
        replyMarkup: dynamicKeyboard);
    }
    else if (qaDictionary.Any(response => response.Key.Contains(msg.Text!)))
    {

        await bot.SendMessage(
        chatId: msg.Chat,
        text: qaDictionary.FirstOrDefault(answer => answer.Key == msg.Text).Value);

    }
    else
    {
        await bot.SendMessage(
        chatId: msg.Chat,
        text: "Відповідь не знайдена, спробуй ще раз!",
        replyMarkup: dynamicKeyboard);
    }
}

public class QuestionAnswer
{
    public required string Question { get; set; }
    public required string Answer { get; set; }
}

public class BotData
{
    public string? Token { get; set; } = "";

    public string? QA { get; set; } = "";
}