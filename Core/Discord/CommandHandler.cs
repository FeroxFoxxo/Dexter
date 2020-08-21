using Dexter.Core.Configuration;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class CommandHandler {
        private readonly DiscordBot Discord;

        private readonly IServiceProvider Services;

        private readonly CommandService CommandService;

        private readonly JSONConfig JSONConfig;

        public CommandHandler(DiscordBot _Discord, JSONConfig _JSONConfig) {
            Discord = _Discord;
            JSONConfig = _JSONConfig;
            CommandService = new CommandService();

            ServiceCollection Collection = new ServiceCollection();

            Collection.AddSingleton(JSONConfig);

            Services = Collection.BuildServiceProvider();

            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => typeof(AbstractModule).IsAssignableFrom(x) && !x.IsAbstract)
                .ToList()
                .ForEach((x) => CommandService.AddModuleAsync(x, Services));
        }

        public async Task HandleCommandAsync(SocketMessage SocketMessage) {
            if (!(SocketMessage is SocketUserMessage Message))
                return;

            int ArgumentPosition = 0;

            if ((Message.HasMentionPrefix(Discord.Client.CurrentUser, ref ArgumentPosition) || Message.HasCharPrefix('~', ref ArgumentPosition)) && !Message.Author.IsBot)
                await CommandService.ExecuteAsync(new SocketCommandContext(Discord.Client, Message), ArgumentPosition, Services);
        }
    }
}
