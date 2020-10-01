using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dexter.Core.DiscordApp {
    public class CommandHandler : InitializableModule {
        private readonly DiscordSocketClient Client;

        private readonly IServiceProvider Services;

        public readonly CommandService CommandService;

        private readonly BotConfiguration BotConfiguration;

        private readonly CommandModule Module;

        public CommandHandler(DiscordSocketClient _Client, CommandService _CommandService, BotConfiguration _BotConfiguration, IServiceProvider _Services, CommandModule _Module) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
            CommandService = _CommandService;
            Services = _Services;
            Module = _Module;
        }

        public override void AddDelegates() {
            Client.Ready += () => Client.SetGameAsync("Spotify", null, ActivityType.Listening);

            Client.MessageReceived += HandleCommandAsync;
            CommandService.CommandExecuted += SendCommandError;
            CommandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }
        
        public async Task HandleCommandAsync(SocketMessage SocketMessage) {
            if (SocketMessage is not SocketUserMessage Message)
                return;

            int ArgumentPosition = 0;

            if (!(Message.HasStringPrefix(BotConfiguration.Prefix, ref ArgumentPosition) ||
                    Message.HasMentionPrefix(Client.CurrentUser, ref ArgumentPosition)) ||
                    Message.Author.IsBot)
                return;

            await CommandService.ExecuteAsync(new CommandModule(Client, Message, BotConfiguration), ArgumentPosition, Services);
        }

        public async Task SendError(IMessageChannel Channel, Type ErrorType, string ErrorReason) {
            await Module.BuildEmbed(EmojiEnum.Annoyed)
             .WithTitle(Regex.Replace(ErrorType.Name, @"(?<!^)(?=[A-Z])", " "))
             .WithDescription(ErrorReason)
             .SendEmbed(Channel);
        }

        private async Task SendCommandError(Optional<CommandInfo> Command, ICommandContext Context, IResult Result) {
            if (Result.IsSuccess)
                return;

            switch (Result.Error) {
                case CommandError.BadArgCount:
                    await Module.BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("You've entered an invalid amount of parameters for this command!")
                        .WithDescription($"Here are some options of parameters you can have for the command **{Command.Value.Name}**.")
                        .GetParametersForCommand(CommandService, Command.Value.Name)
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnmetPrecondition:
                    await Module.BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Access Denied")
                        .WithDescription(Result.ErrorReason)
                        .SendEmbed(Context.Channel);
                    break;
                case CommandError.UnknownCommand:
                    await Module.BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unknown Command")
                        .WithDescription($"Oopsies! It seems as if the command **{Context.Message.Content.Split(' ')[0]}** doesn't exist!")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    if (Result is ExecuteResult ExecuteResult)
                        await SendError(Context.Channel, ExecuteResult.Exception.GetType(), ExecuteResult.Exception.Message);
                    else
                        await SendError(Context.Channel, Result.Error.GetType(), Result.ErrorReason);
                    break;
            }
        }
    }
}
