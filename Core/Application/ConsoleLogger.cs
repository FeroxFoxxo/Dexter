using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Dexter.Core.DiscordApp {
    public class ConsoleLogger : InitializableModule {
        private readonly DiscordSocketClient Client;
        private readonly CommandHandler Commands;
        private readonly BotConfiguration BotConfiguration;

        public ConsoleLogger(DiscordSocketClient _Client, CommandHandler _Commands, BotConfiguration _BotConfiguration) {
            Client = _Client;
            Commands = _Commands;
            BotConfiguration = _BotConfiguration;
        }

        public override void AddDelegates() {
            Client.Log += LogMessageAsync;
            Commands.CommandService.Log += LogMessageAsync;
        }
        
        public async Task LogMessageAsync(LogMessage Event) {
            if (Event.Severity != LogSeverity.Info) {
                Console.ForegroundColor = ConsoleColor.Red;

                string ErrorMessage = $"{Event.Message}";

                if (Event.Exception != null)
                    return;

                await Commands.SendError(
                    Client.GetGuild(BotConfiguration.ErrorChannel["Guild"])
                        .GetTextChannel(BotConfiguration.ErrorChannel["Channel"]),
                    Event.Exception.GetType(),
                    ErrorMessage.Length > 1750 ? ErrorMessage.Substring(0, 1750) : ErrorMessage
                );
            }

            await Console.Out.WriteLineAsync($"\n {DateTime.Now:G} - {Event} ");

            Console.ResetColor();
        }
    }
}
