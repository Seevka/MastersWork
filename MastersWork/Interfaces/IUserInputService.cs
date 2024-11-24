using MastersWork.Models;

namespace MastersWork.Interfaces
{
    public interface IUserInputService
    {
        Task HandleUserInputCreateAsync(long chatId, UserState state, string text);
        Task HandleUserInputEditAsync(long chatId, UserState state, string text);
        Task PerformStartStateAsync(UserState currentState);
    }
}
