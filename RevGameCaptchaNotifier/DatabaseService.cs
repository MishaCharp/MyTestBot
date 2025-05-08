using Supabase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace RevGameCaptchaNotifier
{
    public class DatabaseService
    {
        private readonly Supabase.Client _client;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration config, ILogger<DatabaseService> logger)
        {
            var url = config["Supabase:Url"] ?? throw new ArgumentNullException(nameof(config), "Supabase URL is missing in configuration");
            var apiKey = config["Supabase:ApiKey"] ?? throw new ArgumentNullException(nameof(config), "Supabase API key is missing in configuration");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            _client = new Supabase.Client(url, apiKey, options);
            _client.InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> BindChatToKeyAsync(string key, string chatId)
        {
            try
            {
                var allKeys = await _client
                    .From<KeyModel>()
                    .Select("*")
                    .Get();

                var keyModel = allKeys.Models
                    .FirstOrDefault(k => k.Key == key && k.Activated && k.ChatId == null);

                if (keyModel == null)
                {
                    _logger.LogInformation("Key {Key} not found or already linked", key);
                    return false;
                }

                keyModel.ChatId = chatId;

                await _client
                    .From<KeyModel>()
                    .Update(keyModel);

                _logger.LogInformation("Successfully linked key {Key} to chat {ChatId}", key, chatId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error binding chatId {ChatId} to key {Key}", chatId, key);
                return false;
            }
        }

        public async Task<string?> GetChatIdByKeyAsync(string key)
        {
            try
            {
                var allKeys = await _client
                    .From<KeyModel>()
                    .Select("*")
                    .Get();

                var keyModel = allKeys.Models
                    .FirstOrDefault(k => k.Key == key);

                if (keyModel == null)
                {
                    _logger.LogInformation("Key {Key} not found when retrieving chat ID", key);
                    return null;
                }

                _logger.LogInformation("Found chatId {ChatId} for key {Key}", keyModel.ChatId ?? "null", key);
                return keyModel.ChatId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chatId for key {Key}", key);
                return null;
            }
        }
    }
}
