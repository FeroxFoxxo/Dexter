using Dexter.Commands;
using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dexter.Core.DiscordApp {
    public class CommandHandler : AbstractInitializer {
        private readonly DiscordSocketClient Client;

        private readonly IServiceProvider Services;

        private readonly CommandService CommandService;

        private readonly BotConfiguration BotConfiguration;

        private readonly HelpCommands HelpCommands;

        public CommandHandler(DiscordSocketClient _Client, CommandService _CommandService, BotConfiguration _BotConfiguration, IServiceProvider _Services, HelpCommands _HelpCommands) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
            CommandService = _CommandService;
            Services = _Services;
            HelpCommands = _HelpCommands;
        }

        public override void AddDelegates() {
            Client.MessageReceived += HandleCommandAsync;
            Client.Ready += ReadyAsync;
            CommandService.CommandExecuted += HelpCommands.SendCommandError;
            CommandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }

        public async Task ReadyAsync() {
            await Client.SetGameAsync("Spotify", null, Discord.ActivityType.Listening);
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
