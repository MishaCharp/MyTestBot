using RevGameCaptchaNotifier;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class BotService
{
    private readonly TelegramBotClient _botClient;
    private readonly DatabaseService _dbService;

    public BotService(IConfiguration config, DatabaseService dbService)
    {
        _botClient = new TelegramBotClient(config["TelegramBotToken"] ?? throw new ArgumentNullException("TelegramBotToken is missing in configuration"));
        _dbService = dbService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // слушаем всё
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cancellationToken
        );

        var me = await _botClient.GetMe(cancellationToken);
        Console.WriteLine($"Бот @{me.Username} запущен!");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message!.Text == null)
            return;

        var chatId = update.Message.Chat.Id.ToString();
        var messageText = update.Message.Text.Trim();

        if (messageText.StartsWith("/start"))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "👋 Отправьте сюда свой лицензионный ключ для привязки.",
                cancellationToken: cancellationToken
            );
            return;
        }

        var key = messageText.ToUpper();
        if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$"))
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "❌ Неверный формат ключа. Попробуйте ещё раз.",
                cancellationToken: cancellationToken
            );
            return;
        }

        var success = await _dbService.BindChatToKeyAsync(key, chatId);
        if (success)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: "✅ Ключ успешно привязан!",
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await botClient.SendMessage(
            chatId: chatId,
            text: "❌ Ошибка! Ключ не найден, не активирован или уже привязан к другому чату.",
            cancellationToken: cancellationToken
            );
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка бота: {exception.Message}");
        return Task.CompletedTask;
    }

    public async Task SendCaptchaAlertAsync(string chatId)
    {
        await _botClient.SendMessage(
            chatId: chatId,
            text: "⚠️ Внимание! Обнаружена капча. Пожалуйста, решите её на сайте.",
            cancellationToken: default
        );
    }
}