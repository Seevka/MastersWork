using MastersWork.Models;

namespace MastersWork.Interfaces
{
    public interface IBotManagementService
    {
        Task HandleUserStateAsync(long chatId, UserState state);
        Task EditingBotAsync(long chatId, UserState state);
        Task RunningBotAsync(long chatId);
        Task DeletingBotAsync(long chatId);
        Task StoppingBotAsync(long chatId);
        Task GettingBotListAsync(long chatId);
    }
}
