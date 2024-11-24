using Telegram.Bot.Types;

namespace MastersWork.Interfaces
{
    public interface IUpdateHandler
    {
        Task HandleUpdateAsync(Update update);
    }
}
