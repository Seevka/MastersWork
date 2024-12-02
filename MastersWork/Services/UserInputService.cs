using MastersWork.Data;
using MastersWork.Enums;
using MastersWork.Interfaces;
using MastersWork.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Telegram.Bot;

namespace MastersWork.Services
{
    public class UserInputService(ITelegramBotClient botClient, ApplicationDbContext dbContext) : IUserInputService
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task HandleUserInputCreateAsync(long chatId, UserState state, string text)
        {
            var userState = await _dbContext.UserStates.FindAsync(chatId);
            var botData = await _dbContext.BotCreationDatas.FirstOrDefaultAsync(b => b.ChatId == chatId);

            if (botData != null && botData.IsCompleted == true)
            {
                await _botClient.SendTextMessageAsync(chatId, "У вас вже створено бота, редагуйте або видаліть існуючий!");
            }
            else
            {
                switch (state.CurrentStep)
                {
                    case BotCreationStep.CreatingBot:
                        await _botClient.SendTextMessageAsync(chatId, "Перш за все введіть ім'я у форматі назва_факультету_bot, наприклад: FPMI_bot.");
                        userState!.CurrentStep = BotCreationStep.EnteringBotName;
                        _dbContext.UserStates.Update(userState);
                        await _dbContext.SaveChangesAsync();
                        break;

                    case BotCreationStep.EnteringBotName:
                        if (await IsBotNameExistAtDB(text))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Таке ім'я вже існує, обери інше!.");
                            break;
                        }
                        else if (IsUkrainian(text))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Ім'я повинно бути латиницею!");
                            break;
                        }
                        else if (botData == null)
                        {
                            botData = new BotCreationData { ChatId = chatId };
                            _dbContext.BotCreationDatas.Add(botData);
                            botData.BotName = text;
                            await _botClient.SendTextMessageAsync(chatId, "Тепер введіть токен, отриманий від BotFather.");
                            userState!.CurrentStep = BotCreationStep.EnteringBotToken;
                            _dbContext.UserStates.Update(userState);
                            await _dbContext.SaveChangesAsync();
                        }
                        break;

                    case BotCreationStep.EnteringBotToken:
                        botData!.Token = text;

                        await _botClient.SendTextMessageAsync(chatId, "Тепер введіть питання та відповіді у форматі 'Питання;Відповідь'. Введіть /done, коли закінчите.");
                        userState!.CurrentStep = BotCreationStep.EnteringQA;
                        _dbContext.UserStates.Update(userState);
                        await _dbContext.SaveChangesAsync();
                        break;

                    case BotCreationStep.EnteringQA:
                        if (text == "/done")
                        {
                            botData!.IsCompleted = true;
                            await PerformStartStateAsync(state);
                            await _botClient.SendTextMessageAsync(chatId, "Бот успішно створений! Не забудьте запустити його.");
                        }
                        else
                        {
                            var parts = text.Split(';', 2);
                            if (parts.Length == 2)
                            {
                                var question = parts[0].Trim();
                                var answer = parts[1].Trim();

                                if (botData!.QA == null)
                                    botData.QA = [];

                                botData.QA.Add(new QuestionAnswer { Question = question, Answer = answer });
                                await _dbContext.SaveChangesAsync();

                                await _botClient.SendTextMessageAsync(chatId, "Питання та відповідь збережено. Введіть наступне питання та відповідь, або /done, щоб закінчити.");
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(chatId, "Неправильний формат. Будь ласка, введіть у форматі 'Питання:Відповідь'.");
                            }
                        }
                        break;

                    default:
                        await HandleErrorAndResetState(chatId, userState!);
                        break;
                }
            }
        }

        public async Task HandleUserInputEditAsync(long chatId, UserState state, string text)
        {
            var userState = await _dbContext.UserStates.FindAsync(chatId);
            var botData = await _dbContext.BotCreationDatas.FirstOrDefaultAsync(b => b.ChatId == chatId);
            var editBotKeyboard = KeyboardFactory.CreateKeyboard(KeyboardData.EditBotKeyboard);
            var editQAKeyboard = KeyboardFactory.CreateKeyboard(KeyboardData.EditQAKeyboard);

            switch (state.CurrentStep)
            {
                case BotCreationStep.EditingBot:
                    await _botClient.SendTextMessageAsync(chatId, "Оберіть, що хочете редагувати:", replyMarkup: editBotKeyboard);
                    break;

                case BotCreationStep.EditingBotName:
                    if (text != "Редагувати ім'я Бота 🤖")
                    {
                        if (await IsBotNameExistAtDB(text))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Таке ім'я вже існує, обери інше!");
                        }
                        else if (IsUkrainian(text))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Ім'я повинно бути латиницею!");
                        }
                        else
                        {
                            botData!.BotName = text;
                            await _botClient.SendTextMessageAsync(chatId, "Успішно змінено, не забудьте запустити бота", replyMarkup: editBotKeyboard);
                            await PerformStartStateAsync(state);
                        }
                    }
                    break;

                case BotCreationStep.EditingBotToken:
                    if (text != "Редагувати токен Бота ✏️")
                    {
                        botData!.Token = text;
                        _dbContext.UserStates.Update(userState!);
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendTextMessageAsync(chatId, "Успішно змінено, не забудьте запустити бота", replyMarkup: editBotKeyboard);
                        await PerformStartStateAsync(state);
                    }
                    break;

                case BotCreationStep.EditingQA:
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Оберіть дію:", replyMarkup: editQAKeyboard);
                    }
                    break;

                case BotCreationStep.GetAllQuestionsAnswers:
                    {
                        var qaList = botData!.QA;
                        if (qaList!.Count != 0)
                        {
                            var listMessage = string.Join("\n", qaList!.Select((qa, index) => $"{index + 1}. {qa.Question} - {qa.Answer}"));
                            await _botClient.SendTextMessageAsync(chatId, $"Список питань:\n{listMessage}", replyMarkup: editQAKeyboard);
                            await PerformStartStateAsync(state);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Список питань порожній.");
                        }
                    }
                    break;

                case BotCreationStep.CreateQuestionAnswer:
                    {
                        if (text == "/done")
                        {
                            await PerformStartStateAsync(state);
                            await _botClient.SendTextMessageAsync(chatId, "Додавання питань завершено, не забудьте запустити бота після усіх змін", replyMarkup: editQAKeyboard);
                        }
                        else if (text.Contains(';'))
                        {
                            var parts = text.Split(';', 2);
                            if (parts.Length == 2)
                            {
                                var question = parts[0].Trim();
                                var answer = parts[1].Trim();

                                if (botData!.QA == null)
                                    botData.QA = [];

                                botData.QA.Add(new QuestionAnswer { Question = question, Answer = answer });
                                _dbContext.BotCreationDatas.Update(botData);
                                await _dbContext.SaveChangesAsync();

                                await _botClient.SendTextMessageAsync(chatId, "Питання та відповідь збережено. Введіть наступне питання та відповідь, або /done, щоб закінчити.");
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(chatId, "Неправильний формат. Будь ласка, введіть у форматі 'Питання;Відповідь'.");
                            }
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Будь ласка, введіть у форматі 'Питання;Відповідь', або /done, щоб завершити.");
                        }
                    }
                    break;

                case BotCreationStep.EditQuestionAnswer:
                    {
                        if (text == "Редагувати питання")
                        {
                            await _botClient.SendTextMessageAsync(chatId,
                                "Введіть номер питання, яке хочете редагувати. Наприклад: 1");
                        }
                        else if (int.TryParse(text, out var index) && index > 0 && index <= botData!.QA!.Count)
                        {
                            userState!.CurrentStep = BotCreationStep.EditSpecificQuestionAnswer;
                            userState.TempData = index.ToString();

                            _dbContext.UserStates.Update(userState);
                            await _dbContext.SaveChangesAsync();

                            var questionToEdit = botData.QA[index - 1];
                            await _botClient.SendTextMessageAsync(chatId,
                                $"Ви вибрали для редагування:\n" +
                                $"Питання: '{questionToEdit.Question}'\n" +
                                $"Відповідь: '{questionToEdit.Answer}'\n\n" +
                                "Введіть нове питання та відповідь у форматі;\n" +
                                "Новий текст питання:Новий текст відповіді");
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Невірний ввід. Спробуйте ще раз.");
                        }
                    }
                    break;

                case BotCreationStep.EditSpecificQuestionAnswer:
                    {
                        var indexToChange = int.Parse(userState!.TempData!);
                        var specificQuestionToEdit = botData!.QA![indexToChange - 1];

                        var specificEditParts = text.Split(';');
                        if (specificEditParts.Length == 2)
                        {
                            var newQuestion = specificEditParts[0].Trim();
                            var newAnswer = specificEditParts[1].Trim();

                            specificQuestionToEdit.Question = newQuestion;
                            specificQuestionToEdit.Answer = newAnswer;

                            _dbContext.BotCreationDatas.Update(botData);
                            userState.CurrentStep = BotCreationStep.Start;
                            userState.TempData = null;

                            _dbContext.UserStates.Update(userState);
                            await _dbContext.SaveChangesAsync();

                            await _botClient.SendTextMessageAsync(chatId,
                                $"Питання та відповідь успішно оновлено:\n" +
                                $"Питання: '{specificQuestionToEdit.Question}'\n" +
                                $"Відповідь: '{specificQuestionToEdit.Answer}', не забудьте запустити бота після усіх змін", replyMarkup: editQAKeyboard);
                            await PerformStartStateAsync(state);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId,
                                "Невірний формат. Введіть нове питання та відповідь у форматі;\n" +
                                "Новий текст питання:Новий текст відповіді");
                        }
                    }
                    break;

                case BotCreationStep.DeleteQuestionAnswer:
                    {
                        if (text == "Видалити питання")
                        {
                            await _botClient.SendTextMessageAsync(chatId,
                                "Введіть номер питання, яке хочете видалити. Наприклад: 1");
                        }
                        else if (int.TryParse(text, out var index) && index > 0 && index <= botData!.QA!.Count)
                        {
                            var removedQA = botData.QA[index - 1];
                            botData.QA.RemoveAt(index - 1);

                            _dbContext.BotCreationDatas.Update(botData);
                            await _dbContext.SaveChangesAsync();

                            await _botClient.SendTextMessageAsync(chatId,
                                $"Питання '{removedQA.Question}' успішно видалено, не забудьте запустити бота після усіх змін", replyMarkup: editQAKeyboard);

                            await PerformStartStateAsync(state);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Невірний номер питання. Спробуйте ще раз.");
                        }
                    }
                    break;

                default:
                    await HandleErrorAndResetState(chatId, userState!);
                    break;
            }
        }

        public async Task PerformStartStateAsync(UserState currentState)
        {
            currentState.CurrentStep = BotCreationStep.Start;
            _dbContext.UserStates.Update(currentState);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsBotNameExistAtDB(string botName)
        {
            return await _dbContext.BotCreationDatas.AnyAsync(name => name.BotName == botName);
        }

        public bool IsUkrainian(string text)
        {
            string pattern = @"[А-ЩЬЮЯЄІЇҐа-щьюяєіїґ]";
            return Regex.IsMatch(text, pattern);
        }

        private async Task HandleErrorAndResetState(long chatId, UserState userState)
        {
            var handleErrorKeyboard = KeyboardFactory.CreateKeyboard(KeyboardData.StartKeyboard(await IsAdminAsync(chatId)));
            await _botClient.SendTextMessageAsync(chatId, "Ти написав щось не те, спробуй ще раз 😉", replyMarkup: handleErrorKeyboard);
            userState.CurrentStep = BotCreationStep.Start;
            _dbContext.UserStates.Update(userState);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<bool> IsAdminAsync(long chatId)
        {
            var user = await _dbContext.UserStates.FindAsync(chatId);
            return user != null && user.IsAdmin;
        }
    }
}
