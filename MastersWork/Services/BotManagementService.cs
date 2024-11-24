using MastersWork.Data;
using MastersWork.Enums;
using MastersWork.Interfaces;
using MastersWork.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace MastersWork.Services
{
    public class BotManagementService(
        ITelegramBotClient botClient,
        ApplicationDbContext dbContext,
        IExternalOperationsService externalOperationsService,
        IUserInputService userInputService) : IBotManagementService
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly IExternalOperationsService _externalOperationsService = externalOperationsService;
        private readonly IUserInputService _userInputService = userInputService;

        public async Task HandleUserStateAsync(long chatId, UserState state)
        {
            var userState = await _dbContext.UserStates.FindAsync(chatId);
            switch (state.CurrentStep)
            {
                case BotCreationStep.EditingBot:
                    await EditingBotAsync(chatId, userState!);
                    break;

                case BotCreationStep.DeletingBot:
                    await DeletingBotAsync(chatId);
                    break;

                case BotCreationStep.RunningBot:
                    await RunningBotAsync(chatId);
                    break;

                case BotCreationStep.StoppingBot:
                    await StoppingBotAsync(chatId);
                    break;

                case BotCreationStep.GetBotInformation:
                    await GettingBotListAsync(chatId);
                    break;

                default:
                    await HandleErrorAndResetState(chatId, userState!);
                    break;
            }
        }

        public async Task EditingBotAsync(long chatId, UserState state)
        {
            var editBotData = await _dbContext.BotCreationDatas.FirstOrDefaultAsync(b => b.ChatId == chatId);
            if (editBotData == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "У тебе не створено жодного бота для редагування!");
            }
            else if (editBotData.IsBotWorking == true)
            {
                await StoppingBotAsync(chatId);
                state.CurrentStep = BotCreationStep.EditingBot;
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, "");
            }
            else
            {
                state.CurrentStep = BotCreationStep.EditingBot;
                await _dbContext.SaveChangesAsync();
                await _userInputService.HandleUserInputEditAsync(chatId, state, "");
            }
        }

        public async Task RunningBotAsync(long chatId)
        {
            var runningBotData = await _dbContext.BotCreationDatas.FirstOrDefaultAsync(b => b.ChatId == chatId);
            if (runningBotData == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "У тебе не створено жодного бота, щоб запустити!");
            }
            else if (runningBotData.IsBotWorking == true)
            {
                await _botClient.SendTextMessageAsync(chatId, "Бот вже запущений!");
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "Твій бот розгортається, зачекай...");
                _externalOperationsService.RunBot(runningBotData);
                runningBotData.IsBotWorking = true;
                await _dbContext.SaveChangesAsync();
                await _botClient.SendTextMessageAsync(chatId, $"[{runningBotData.BotName}] успішно запущено.");
            }
        }

        public async Task DeletingBotAsync(long chatId)
        {
            var deleteBotData = await _dbContext.BotCreationDatas.FirstOrDefaultAsync(b => b.ChatId == chatId);
            if (deleteBotData == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "У тебе не створено жодного бота для видалення!");
            }
            else if (deleteBotData.IsBotWorking == true)
            {
                await StoppingBotAsync(chatId);
                _dbContext.BotCreationDatas.Remove(deleteBotData);
                await _dbContext.SaveChangesAsync();
                await _botClient.SendTextMessageAsync(chatId, $"[{deleteBotData.BotName}] успішно видалено.");
            }
            else
            {
                _dbContext.BotCreationDatas.Remove(deleteBotData);
                await _dbContext.SaveChangesAsync();
                await _botClient.SendTextMessageAsync(chatId, $"[{deleteBotData.BotName}] успішно видалено.");
            }
        }

        public async Task StoppingBotAsync(long chatId)
        {
            var stoppingBotData = await _dbContext.BotCreationDatas.FirstOrDefaultAsync(b => b.ChatId == chatId);
            if (stoppingBotData == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "У тебе не створено жодного бота, щоб зупинити!");
            }
            else if (stoppingBotData.IsBotWorking == false)
            {
                await _botClient.SendTextMessageAsync(chatId, "Бот вже зупинений");
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "Твій бот зупиняється, зачекай...");
                _externalOperationsService.StopBot(stoppingBotData);
                stoppingBotData.IsBotWorking = false;
                await _dbContext.SaveChangesAsync();
                await _botClient.SendTextMessageAsync(chatId, $"[{stoppingBotData.BotName}] успішно зупинено.");
            }
        }

        public async Task GettingBotListAsync(long chatId)
        {
            var gettingBotData = await _dbContext.BotCreationDatas.FirstOrDefaultAsync(b => b.ChatId == chatId);
            if (gettingBotData == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "У тебе не створено жодного бота!");
            }
            else
            {
                var qaList = gettingBotData.QA;
                string qaDisplay = qaList != null && qaList.Count > 0
                    ? string.Join("\n", qaList.Select(qa => $"Питання: {qa.Question}, Відповідь: {qa.Answer}"))
                    : "Немає записів Q&A.";

                await _botClient.SendTextMessageAsync(chatId,
                    $"Ім'я: {gettingBotData.BotName}\r\nТокен: {gettingBotData.Token}\r\nQ&A:\r\n{qaDisplay}");
            }
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
