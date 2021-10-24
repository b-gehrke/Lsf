using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using static Lsf.Grading.Services.Constants;

namespace Lsf.Grading.Services.Notifiers
{
    /// <summary>
    /// Factory methods to create notifiers based on the provides configuration
    /// </summary>
    public static class NotifierFactory
    {
        /// <summary>
        /// Tries to get an environment variable. If it doesn't exist or is empty, <value>null</value> is returned.
        /// </summary>
        /// <param name="key">Name of the environment variable</param>
        /// <returns>Content of the variable or <value>null</value> if variable doesn't exist or is empty</returns>
        private static string? GetFromEnv(string key)
        {
            var val = Environment.GetEnvironmentVariable(key);

            return string.IsNullOrEmpty(val) ? null : val;
        }
        
        /// <summary>
        /// Creates notifiers based on the provided configuration
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="logger">Application logger</param>
        /// <returns>Instantiated and configured notifiers</returns>
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