using Telegram.Bot;
using MastersWork.Helpers;

namespace MastersWork.Services
{
    public class ConfigureWebhook(
        ILogger<ConfigureWebhook> logger,
        IServiceProvider serviceProvider) : IHostedService
    {
        private readonly ILogger<ConfigureWebhook> _logger = logger;
        private readonly IServiceProvider _services = serviceProvider;


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var serverConfiguration = ConfigurationHelper.LoadBotConfiguration();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            var webhookAddress = $"{serverConfiguration.HostAddress}/bot/{serverConfiguration.BotToken}";


            await botClient.SetWebhookAsync(
                url: webhookAddress,
                cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            _logger.LogInformation("Removing webhook");
            await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
        }
    }
}
