namespace MastersWork.Configurations
{
    public record BotConfiguration
    {
        public required string BotToken { get; init; }
        public required string HostAddress { get; init; }
    }
}
