using MastersWork.Data;
using MastersWork.Enums;
using MastersWork.Interfaces;
using MastersWork.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MastersWork.Services
{
    public class HandleUpdateService(
        ITelegramBotClient botClient,
        ApplicationDbContext dbContext,
        IUserInputService userInputService,
        IAdminPanelService adminPanelService,
        IBotManagementService botManagementService) : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IUserInputService _userInputService = userInputService;
        private readonly IAdminPanelService _adminPanelService = adminPanelService;
        private readonly IBotManagementService _botManagementService = botManagementService;

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            var state = await _dbContext.UserStates.FindAsync(chatId);

            if (text.StartsWith("/start") && state == null || state == null)
            {
                _dbContext.UserStates.Add(new UserState { ChatId = chatId, CurrentStep = BotCreationStep.Start, IsAdmin = false, UserName = update.Message.Chat.Username });
                await _dbContext.SaveChangesAsync();
                await HandleStartAsync(update.Message.Chat);
            }
            else if (text.StartsWith("/start") || text.StartsWith("Вийти в головне меню") || text.StartsWith("/restart") && state != null)
            {
                state.CurrentStep = BotCreationStep.Start;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await HandleStartAsync(update.Message.Chat);
            }
            else if ((text.StartsWith("Створити Бота") || text.StartsWith("/create")) && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.CreatingBot;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputCreateAsync(chatId, state, text);
            }
            else if ((text.StartsWith("Редагувати Бота") || text.StartsWith("/edit")) && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.EditingBot;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _botManagementService.HandleUserStateAsync(chatId, state);
            }
            else if (text.StartsWith("Редагувати ім'я Бота") && state != null && state.IsAdmin)
            {
                await _botClient.SendTextMessageAsync(chatId, "Введіть нове ім'я: ");
                state.CurrentStep = BotCreationStep.EditingBotName;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if (text.StartsWith("Редагувати токен Бота") && state != null && state.IsAdmin)
            {
                await _botClient.SendTextMessageAsync(chatId, "Введіть новий токен: ");
                state.CurrentStep = BotCreationStep.EditingBotToken;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if (text.StartsWith("Редагувати питання Бота") && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.EditingQA;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if (text.StartsWith("Додати питання") && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.CreateQuestionAnswer;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if (text.StartsWith("Редагувати питання") && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.EditQuestionAnswer;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if (text.StartsWith("Видалити питання") && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.DeleteQuestionAnswer;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if (text.StartsWith("Отримати список") && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.GetAllQuestionsAnswers;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if ((text.StartsWith("Вийти в головне меню") || text.StartsWith("/edit")) && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.Start;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _botManagementService.HandleUserStateAsync(chatId, state);
            }
            else if ((text.StartsWith("Запустити Бота") || text.StartsWith("/run")) && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.RunningBot;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _botManagementService.HandleUserStateAsync(chatId, state);
            }
            else if ((text.StartsWith("Зупинити Бота") || text.StartsWith("/stop")) && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.StoppingBot;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _botManagementService.HandleUserStateAsync(chatId, state);
            }
            else if ((text.StartsWith("Видалити Бота") || text.StartsWith("/delete")) && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.DeletingBot;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _botManagementService.HandleUserStateAsync(chatId, state);
            }
            else if ((text.StartsWith("Отримати інформацію про Бота") || text.StartsWith("/info")) && state != null && state.IsAdmin)
            {
                state.CurrentStep = BotCreationStep.GetBotInformation;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _botManagementService.HandleUserStateAsync(chatId, state);
            }
            else if ((text.StartsWith("Допомога") || text.StartsWith("/help")) && state != null)
            {
                await GetHelpAsync(chatId);
            }
            else if ((text.StartsWith("Адміністрування") && await _adminPanelService.IsAdminAsync(chatId)) && state != null && state.IsAdmin)
            {
                var adminKeyboard = KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel);
                await CallKeyboardAsync(chatId, "Обери дію: ", adminKeyboard);
            }
            else if (text.StartsWith("Додати адміністратора") && await _adminPanelService.IsAdminAsync(chatId) && state != null)
            {
                await _botClient.SendTextMessageAsync(chatId, "Введи юзернейм адміністратора, без @: ");
                state.CurrentStep = BotCreationStep.AddAdmin;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _adminPanelService.HandleAdminPanelAsync(chatId, state, text);
            }
            else if (text.StartsWith("Видалити адміністратора") && await _adminPanelService.IsAdminAsync(chatId) && state != null)
            {
                await _botClient.SendTextMessageAsync(chatId, "Введи юзернейм адміністратора, без @: ");
                state.CurrentStep = BotCreationStep.DeleteAdmin;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _adminPanelService.HandleAdminPanelAsync(chatId, state, text);
            }
            else if (text.StartsWith("Список адміністраторів") && await _adminPanelService.IsAdminAsync(chatId) && state != null)
            {
                state.CurrentStep = BotCreationStep.GetAdmins;
                _dbContext.UserStates.Update(state);
                await _dbContext.SaveChangesAsync();
                await _adminPanelService.HandleAdminPanelAsync(chatId, state, text);
            }
            else if (state != null && state.IsAdmin &&
                     (state.CurrentStep == BotCreationStep.EnteringBotName ||
                      state.CurrentStep == BotCreationStep.EnteringBotToken ||
                      state.CurrentStep == BotCreationStep.EnteringQA))
            {
                await _userInputService.HandleUserInputCreateAsync(chatId, state, text);
            }
            else if (state != null && state.IsAdmin &&
                    (state.CurrentStep == BotCreationStep.EditingBotName ||
                    state.CurrentStep == BotCreationStep.EditingBotToken ||
                    state.CurrentStep == BotCreationStep.EditingQA ||
                    state.CurrentStep == BotCreationStep.GetAllQuestionsAnswers ||
                    state.CurrentStep == BotCreationStep.CreateQuestionAnswer ||
                    state.CurrentStep == BotCreationStep.DeleteQuestionAnswer ||
                    state.CurrentStep == BotCreationStep.EditQuestionAnswer ||
                    state.CurrentStep == BotCreationStep.EditSpecificQuestionAnswer))
            {
                await _userInputService.HandleUserInputEditAsync(chatId, state, text);
            }
            else if (state != null && state.IsAdmin &&
                    (state.CurrentStep == BotCreationStep.AddAdmin ||
                    state.CurrentStep == BotCreationStep.DeleteAdmin))
            {
                await _adminPanelService.HandleAdminPanelAsync(chatId, state, text);
            }
            else if (state != null && state.IsAdmin == false)
            {
                await _botClient.SendTextMessageAsync(chatId, "На жаль, ти не є адміністартором, " +
                    "щоб створити свого бота. Оскільки ресурси обмежені, немає можливості, " +
                    "щоб кожен мав свого телеграм-бота. Але звернися до @Seevkaa і ми щось придумаємо ❤️");
            }
            else
            {
                await HandleErrorAndResetState(update.Message.Chat.Id, state!);
            }
        }

        private async Task HandleStartAsync(Chat chat)
        {
            var startKeyboard = KeyboardFactory.CreateKeyboard(KeyboardData.StartKeyboard(await _adminPanelService.IsAdminAsync(chat.Id)));
            await CallKeyboardAsync(chat.Id, "👋 Привіт! Тут ти можеш створити власного телеграм бота. Обери одну із опцій:", startKeyboard);
        }

        private async Task GetHelpAsync(long chatId)
        {
            await _botClient.SendTextMessageAsync(chatId, " Привіт, цей бот створений для того, щоб ти зміг створити свого Q&A бота!\r\n\r\n" +
                "Перш за все тобі потрібно отримати унікальний токен від BotFather, щоб його отримати потрібно відвідати " +
                "цього бота https://t.me/BotFather та натиснути /newbot\r\n\r\n" +
                "Після цього ти отримаєш токен, який потрібно зберегти. Як тільки ти це зробиш, натисни /create і насолоджуйся своїм Q&A ботом.\r\n\r\n" +
                "Важливо, щоб ім'я бота було написано латиницею.\r\n\r\n" +
                "У разі питань та багів писати: @Seevkaa");
        }

        private async Task HandleErrorAndResetState(long chatId, UserState userState, BotCreationStep resetStep = BotCreationStep.Start)
        {
            var handleErrorKeyboard = KeyboardFactory.CreateKeyboard(KeyboardData.StartKeyboard(await _adminPanelService.IsAdminAsync(chatId)));
            await _botClient.SendTextMessageAsync(chatId, "Ти написав щось не те, спробуй ще раз 😉", replyMarkup: handleErrorKeyboard);
            userState.CurrentStep = resetStep;
            _dbContext.UserStates.Update(userState);
            await _dbContext.SaveChangesAsync();
        }

        private async Task CallKeyboardAsync(long chatId, string text, ReplyKeyboardMarkup replyKeyboardMarkup)
        {
            await _botClient.SendTextMessageAsync(chatId: chatId, text: text, replyMarkup: replyKeyboardMarkup);
        }
    }
}
