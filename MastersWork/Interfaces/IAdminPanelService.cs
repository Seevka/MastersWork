using MastersWork.Models;

namespace MastersWork.Interfaces
{
    public interface IAdminPanelService
    {
        Task HandleAdminPanelAsync(long chatId, UserState state, string text);
        Task<bool> IsAdminAsync(long chatId);
    }
}
