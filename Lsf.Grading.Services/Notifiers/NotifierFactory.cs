using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using static Lsf.Grading.Services.Constants;

namespace Lsf.Grading.Services.Notifiers
{
    public static class NotifierFactory
    {
        private static string? GetFromEnv(string key)
        {
            var val = Environment.GetEnvironmentVariable(key);

            return string.IsNullOrEmpty(val) ? null : val;
        }
        
        public static IEnumerable<INotifier> CreateFromConfig(IConfiguration config, ILogger logger)
        {
            var telegramSection = config.GetSection(CONF_TELEGRAM);
            if (telegramSection.Exists())
            {
                yield return new TelegramNotifier(logger, telegramSection.Get<TelegramNotifier.Config>());
            } else if (GetFromEnv(ENV_TELEGRAM_BOT_TOKEN) is string token && GetFromEnv(ENV_TELEGRAM_CHAT_ID) is string chatId)
            {
                yield return new TelegramNotifier(logger, new TelegramNotifier.Config {TelegramChatId = chatId, TelegramBotAccessToken = token});
            }


            var callbackSection = config.GetSection(CONF_CALLBACK);
            if (callbackSection.Exists())
            {
                yield return new CallbackUrlNotifier(callbackSection.Get<CallbackUrlNotifier.Config>());
            }
            
        } 
    }
}