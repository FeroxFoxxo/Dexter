using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class CommandHandler {
        private readonly DexterDiscord Discord;
        private readonly CommandService CommandService;
        private readonly IServiceProvider Services;

        public CommandHandler(DexterDiscord _Discord) {
            Discord = _Discord;
            CommandService = new CommandService();

            ServiceCollection Collection = new ServiceCollection();

            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => typeof(ModuleBase<SocketCommandContext>).IsAssignableFrom(x))
                .ToList()
                .ForEach((x) => Collection.AddSingleton(x));

            Services = Collection.BuildServiceProvider();
        }

        public async Task InitializeAsync() {
            Discord.Client.MessageReceived += HandleCommandAsync;
            _ = await CommandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }

        private async Task HandleCommandAsync(SocketMessage s) {
            if (!(s is SocketUserMessage msg))
                return;

            var argPos = 0;

            if ((msg.HasMentionPrefix(Discord.Client.CurrentUser, ref argPos) || msg.HasCharPrefix('~', ref argPos)) && !msg.Author.IsBot) {
                var context = new SocketCommandContext(Discord.Client, msg);
                await TryRunAsBotCommand(context, argPos).ConfigureAwait(false);
            }
        }

        private async Task TryRunAsBotCommand(SocketCommandContext context, int argPos) {
            var result = await CommandService.ExecuteAsync(context, argPos, Services);

            if (!result.IsSuccess)
                ConsoleLogger.Log($"Command execution failed. Reason: {result.ErrorReason}.");
        }
    }
}
