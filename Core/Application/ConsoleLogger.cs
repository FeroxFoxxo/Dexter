using Dexter.Core.Abstractions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Dexter.Core.DiscordApp {
    public class ConsoleLogger : InitializableModule {
        private readonly DiscordSocketClient Client;
        private readonly CommandService Commands;

        public ConsoleLogger(DiscordSocketClient _Client, CommandService _Commands) {
            Client = _Client;
            Commands = _Commands;
        }

        public override void AddDelegates() {
            Client.Log += LogMessageAsync;
            Commands.Log += LogMessageAsync;
        }

        private async Task LogMessageAsync(LogMessage Event) {
            if (Event.Severity != LogSeverity.Info)
                Console.ForegroundColor = ConsoleColor.Red;

            await Console.Out.WriteLineAsync($"\n {DateTime.Now:G} - {Event} ");

            Console.ResetColor();
        }
    }
}
