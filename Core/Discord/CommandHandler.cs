using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class CommandHandler : AbstractInitializer {
        private readonly DiscordSocketClient Client;

        private readonly IServiceProvider Services;

        private readonly CommandService CommandService;

        private readonly BotConfiguration BotConfiguration;

        public CommandHandler(DiscordSocketClient _Client, CommandService _CommandService, BotConfiguration _BotConfiguration, IServiceProvider _Services) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
            CommandService = _CommandService;
            Services = _Services;
        }

        public override void AddDelegates() {
            Client.MessageReceived += HandleCommandAsync;
        }

        public async Task HandleCommandAsync(SocketMessage SocketMessage) {
            if (!(SocketMessage is SocketUserMessage Message)) return;

            int ArgumentPosition = 0;
            if (!(Message.HasStringPrefix(BotConfiguration.Prefix, ref ArgumentPosition) ||
                Message.HasMentionPrefix(Client.CurrentUser, ref ArgumentPosition)) ||
                Message.Author.IsBot)
                return;

            await CommandService.ExecuteAsync(new SocketCommandContext(Client, Message), ArgumentPosition, Services);
        }
    }
}
