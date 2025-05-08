using RevGameCaptchaNotifier;

var builder = WebApplication.CreateBuilder(args);

// Добавляем логирование
builder.Services.AddLogging(logging => logging.AddConsole());

// Регистрируем сервисы
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<BotService>();
builder.Services.AddHostedService<BotHostedService>();

var app = builder.Build();

// Минимальное API для получения событий о капче
app.MapPost("/captcha", async (CaptchaRequest request, DatabaseService db, BotService bot, ILogger<Program> logger) =>
{
    try
    {
        var chatId = await db.GetChatIdByKeyAsync(request.Key);
        if (chatId == null)
        {
            logger.LogWarning("ChatId not found for key {Key}", request.Key);
            return Results.BadRequest("Чат не найден для ключа");
        }

        await bot.SendCaptchaAlertAsync(chatId);
        logger.LogInformation("Captcha alert sent to chatId {ChatId} for key {Key}", chatId, request.Key);
        return Results.Ok("Уведомление о капче отправлено");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing captcha request for key {Key}", request.Key);
        return Results.StatusCode(500);
    }
});

app.Run();

// Модель запроса
public record CaptchaRequest(string Key);

// Фоновая служба для бота
public class BotHostedService : BackgroundService
{
    private readonly BotService _botService;
    private readonly ILogger<BotHostedService> _logger;

    public BotHostedService(BotService botService, ILogger<BotHostedService> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Telegram bot...");
            await _botService.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Telegram bot");
            throw; // Позволяет хосту обработать ошибку и завершить приложение
        }
    }
}