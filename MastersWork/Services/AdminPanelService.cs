using MastersWork.Data;
using MastersWork.Enums;
using MastersWork.Interfaces;
using MastersWork.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MastersWork.Services
{
    public class AdminPanelService(ITelegramBotClient botClient, ApplicationDbContext dbContext) : IAdminPanelService
    {
        private readonly ITelegramBotClient _botClient = botClient;
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task HandleAdminPanelAsync(long chatId, UserState state, string text)
        {
            var userState = await _dbContext.UserStates.FirstOrDefaultAsync(u => u.UserName == text);
            switch (state.CurrentStep)
            {
                case BotCreationStep.AddAdmin:
                    if (text != "Додати адміністратора ✅")
                    {
                        if (userState != null)
                        {
                            if (userState.IsAdmin)
                            {
                                await CallKeyboardAsync(chatId, $"Користувач [{text}] вже є адміністратором!", KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel));
                            }
                            else
                            {
                                userState.IsAdmin = true;
                                await _dbContext.SaveChangesAsync();
                                await CallKeyboardAsync(chatId, $"Адміністратора [{text}] успішно додано!", KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel));
                                await PerformStartStateAsync(state);
                            }
                        }
                        else
                        {
                            await CallKeyboardAsync(chatId, $"Юзернейм [{text}] не знайдено! Перевірте, чи користувач запускав бота",
                                KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel));
                        }
                    }
                    break;

                case BotCreationStep.DeleteAdmin:
                    if (text != "Видалити адміністратора ❌")
                    {
                        if (text != "Seevkaa")
                        {
                            if (userState != null)
                            {
                                if (!userState.IsAdmin)
                                {
                                    await CallKeyboardAsync(chatId, $"Користувач [{text}] вже не є адміністратором!",
                                        KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel));
                                }
                                else
                                {
                                    userState.IsAdmin = false;
                                    await _dbContext.SaveChangesAsync();
                                    await CallKeyboardAsync(chatId, $"Адміністратора [{text}] успішно видалено!",
                                        KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel));
                                    await PerformStartStateAsync(state);
                                }
                            }
                            else
                            {
                                await CallKeyboardAsync(chatId, $"Юзернейм [{text}] не знайдено! Перевірте, чи користувач запускав бота",
                                    KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel));
                            }
                        }
                        else
                        {
                            await CallKeyboardAsync(chatId, $"Гарна спроба, але не можна видаляти творця бота ;)",
                                KeyboardFactory.CreateKeyboard(KeyboardData.AdminPanel));
                        }
                    }
                    break;

                case BotCreationStep.GetAdmins:
                    var admins = await _dbContext.UserStates
                                   .Where(u => u.IsAdmin)
                                   .Select(u => u.UserName ?? "Невідомий користувач")
                                   .ToListAsync();

                    if (admins.Count != 0)
                    {
                        string adminList = "Список адміністраторів:\n" + string.Join("\n", admins);
                        await _botClient.SendTextMessageAsync(chatId, adminList);
                    }
                    else
                    {
                        await CallKeyboardAsync(chatId, "Адміністраторів не знайдено", KeyboardFactory.CreateKeyboard(KeyboardData.StartKeyboard(state.IsAdmin)));
                    }

                    await PerformStartStateAsync(state);
                    break;

                default:
                    await HandleErrorAndResetState(chatId, state);
                    break;
            }
        }

        public async Task<bool> IsAdminAsync(long chatId)
        {
            var user = await _dbContext.UserStates.FindAsync(chatId);
            return user != null && user.IsAdmin;
        }

        private async Task PerformStartStateAsync(UserState currentState)
        {
            currentState.CurrentStep = BotCreationStep.Start;
            _dbContext.UserStates.Update(currentState);
            await _dbContext.SaveChangesAsync();
        }

        private async Task CallKeyboardAsync(long chatId, string text, ReplyKeyboardMarkup replyKeyboardMarkup)
        {
            await _botClient.SendTextMessageAsync(chatId: chatId, text: text, replyMarkup: replyKeyboardMarkup);
        }

        private async Task HandleErrorAndResetState(long chatId, UserState userState)
        {
            var handleErrorKeyboard = KeyboardFactory.CreateKeyboard(KeyboardData.StartKeyboard(await IsAdminAsync(chatId)));
            await CallKeyboardAsync(chatId, "Ти написав щось не те, спробуй ще раз 😉", handleErrorKeyboard);
            userState.CurrentStep = BotCreationStep.Start;
            _dbContext.UserStates.Update(userState);
            await _dbContext.SaveChangesAsync();
        }
    }
}
