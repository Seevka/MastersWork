using MastersWork.Data;
using MastersWork.Helpers;
using MastersWork.Interfaces;
using MastersWork.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);

var serverConfiguration = ConfigurationHelper.LoadBotConfiguration();
var botToken = serverConfiguration.BotToken ?? string.Empty;

builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddHostedService<ConfigureWebhook>();

builder.Services.AddScoped<IUpdateHandler, HandleUpdateService>();
builder.Services.AddScoped<IUserInputService, UserInputService>();
builder.Services.AddScoped<IAdminPanelService, AdminPanelService>();
builder.Services.AddScoped<IBotManagementService, BotManagementService>();
builder.Services.AddScoped<IExternalOperationsService, ExternalOperationsService>();

builder.Services.AddHttpClient("TelegramWebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botToken, httpClient));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapControllers();

app.MapPost($"/bot/{serverConfiguration.BotToken}", async (
    HttpRequest request,
    IUpdateHandler updateHandler,
    ILogger<Program> logger) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    var update = JsonConvert.DeserializeObject<Update>(body);

    if (update == null)
    {
        logger.LogWarning("Received empty update");
        return Results.BadRequest();
    }

    await updateHandler.HandleUpdateAsync(update);

    return Results.Ok();
})
.WithName("TelegramWebhook");

app.Run();
