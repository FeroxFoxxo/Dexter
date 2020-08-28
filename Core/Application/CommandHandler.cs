using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Core.DiscordApp {
    public class CommandHandler : InitializableModule {
        private readonly DiscordSocketClient Client;

        private readonly IServiceProvider Services;

        public readonly CommandService CommandService;

        private readonly BotConfiguration BotConfiguration;

        public CommandHandler(DiscordSocketClient _Client, CommandService _CommandService, BotConfiguration _BotConfiguration, IServiceProvider _Services) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
            CommandService = _CommandService;
            Services = _Services;
        }

        public override void AddDelegates() {
            Client.Ready += () => Client.SetGameAsync("Spotify", null, ActivityType.Listening);

            Client.MessageReceived += HandleCommandAsync;
            CommandService.CommandExecuted += SendCommandError;
            CommandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }
        
        public async Task HandleCommandAsync(SocketMessage SocketMessage) {
            if (!(SocketMessage is SocketUserMessage Message))
                return;

            int ArgumentPosition = 0;
            if (!(Message.HasStringPrefix(BotConfiguration.Prefix, ref ArgumentPosition) ||
                    Message.HasMentionPrefix(Client.CurrentUser, ref ArgumentPosition)) ||
                    Message.Author.IsBot)
                return;

            await CommandService.ExecuteAsync(new CommandModule(Client, Message, BotConfiguration), ArgumentPosition, Services);
        }

        public async Task SendCommandError(Optional<CommandInfo> Command, ICommandContext Context, IResult Result) {
            if (Result.IsSuccess)
                return;

            if (Context is CommandModule Modules)
                switch (Result.Error) {
                    case CommandError.BadArgCount:
                        await Modules.BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You've entered an invalid amount of parameters for this command!")
                            .WithDescription($"Here are some options of parameters you can have for the command **{Command.Value.Name}**.")
                            .GetParametersForCommand(CommandService, Command.Value.Name)
                            .SendEmbed(Context.Channel);
                        break;
                    case CommandError.UnmetPrecondition:
                        await Modules.BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Access Denied")
                            .WithDescription(Result.ErrorReason)
                            .SendEmbed(Context.Channel);
                        break;
                    case CommandError.UnknownCommand:
                        await Modules.BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unknown Command")
                            .WithDescription($"Oopsies! It seems as if the command **{Context.Message.Content.Split(' ')[0]}** doesn't exist!")
                            .SendEmbed(Context.Channel);
                        break;
                    default:
                        if (Result is ExecuteResult ExecuteResult)
                            await Modules.BuildEmbed(EmojiEnum.Annoyed)
                             .WithTitle(Regex.Replace(ExecuteResult.Exception.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                             .WithDescription(ExecuteResult.Exception.Message)
                             .SendEmbed(Context.Channel);
                        else
                            await Modules.BuildEmbed(EmojiEnum.Annoyed)
                             .WithTitle(Regex.Replace(Result.Error.GetType().Name, @"(?<!^)(?=[A-Z])", " "))
                             .WithDescription(Result.ErrorReason)
                             .SendEmbed(Context.Channel);
                        break;
                }
            else
                Console.WriteLine("\n Unable to cast context to CommandModule!");
        }
    }
}
