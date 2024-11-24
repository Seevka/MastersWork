using MastersWork.Models;

namespace MastersWork.Interfaces
{
    public interface IExternalOperationsService
    {
        void RunBot(BotCreationData botData);
        void StopBot(BotCreationData botData);
    }
}
