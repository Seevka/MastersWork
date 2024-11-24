using MastersWork.Configurations;

namespace MastersWork.Helpers
{
    public static class ConfigurationHelper
    {
        public static BotConfiguration LoadBotConfiguration()
        {
            return new BotConfiguration
            {
                BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN")!,
                HostAddress = Environment.GetEnvironmentVariable("HOST_ADDRESS")!
            };
        }

        public static ServerConfiguration LoadServerConfiguration()
        {
            return new ServerConfiguration
            {
                Host = Environment.GetEnvironmentVariable("HOST")!,
                Username = Environment.GetEnvironmentVariable("USERNAME")!,
                Password = Environment.GetEnvironmentVariable("PASSWORD")!
            };
        }
    }
}
