using Dexter.Abstractions;
using Dexter.Configuration;
using Dexter.CustomCommands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Dexter.Services {
    public class CommandHandlerService : InitializableModule {

        private readonly DiscordSocketClient Client;

        private readonly IServiceProvider Services;

        public readonly CommandService CommandService;

        private readonly BotConfiguration BotConfiguration;

        private readonly CommandModule Module;

        private readonly CustomCommandsService CustomCommandsService;

        private readonly LoggingService LoggingService;

        private static readonly string[] SensitiveCharacters = { "\\", "*", "_", "~", "`", "|", ">", "[", "(" };

        public CommandHandlerService(DiscordSocketClient _Client, CommandService _CommandService, BotConfiguration _BotConfiguration, IServiceProvider _Services, CommandModule _Module, CustomCommandsService _CustomCommandsService, LoggingService _LoggingService) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
            CommandService = _CommandService;
            Services = _Services;
            Module = _Module;
            CustomCommandsService = _CustomCommandsService;
            LoggingService = _LoggingService;
        }

        public override void AddDelegates() {
            Client.MessageReceived += HandleCommandAsync;
            CommandService.CommandExecuted += SendCommandError;
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
                    string[] CustomCommandArgs = Context.Message.Content[BotConfiguration.Prefix.Length..].Split(' ');

                    if (CustomCommandsService.TryGetCommand(CustomCommandArgs[0], out CustomCommand CustomCommand))
                        await CustomCommand.ExecuteCommand(Context, Module);
                    else {
                        await Module.BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unknown Command")
                        .WithDescription($"Oopsies! It seems as if the command **{SanitizeMarkdown(CustomCommandArgs[0])}** doesn't exist!")
                        .SendEmbed(Context.Channel);
                    }
                    break;
                case CommandError.ParseFailed:
                    await Module.BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to parse command!")
                        .WithDescription("Invalid amount of command arguments.")
                        .SendEmbed(Context.Channel);
                    break;
                default:
                    await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Warning, GetType().Name.Prettify(), $"Unknown statement reached!\nCommand: {(Command.IsSpecified ? Command.Value : null)}\nResult: {Result}"));

                    if (!string.IsNullOrEmpty(BotConfiguration.DeveloperMention))
                        await Context.Channel.SendMessageAsync($"Unknown error! I'll tell the developers.\n{BotConfiguration.DeveloperMention}");
                    else
                        await Context.Channel.SendMessageAsync("Unknown error! I wanted to tell the developers, but I don't know who they are!");

                    if (Result is ExecuteResult ExecuteResult)
                        await new EmbedBuilder()
                            .WithColor(Color.Red)
                            .WithTitle(ExecuteResult.Exception.GetType().Name.Prettify())
                            .WithDescription(ExecuteResult.Exception.Message)
                            .SendEmbed(Context.Channel);
                    else
                        await new EmbedBuilder()
                            .WithColor(Color.Red)
                            .WithTitle(Result.Error.GetType().Name.Prettify())
                            .WithDescription(Result.ErrorReason)
                            .SendEmbed(Context.Channel);

                    break;
            }
        }

        private static string SanitizeMarkdown(string Text) {
            foreach (string Unsafe in SensitiveCharacters)
                Text = Text.Replace(Unsafe, $"\\{Unsafe}");
            return Text;
        }

    }
}
