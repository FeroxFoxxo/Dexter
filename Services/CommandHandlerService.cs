using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Dexter.Databases.CustomCommands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {
    /// <summary>
    /// The CommandHandlerService deals with invoking the command and the errors that may occur as a result.
    /// It filters the command to see if the user is not a bot and that it has the prefix set in the
    /// bot configuration. It also catches all errors that may crop up in a command, logs it, and then sends
    /// an appropriate error to the channel, pinging the developers if the error is unknown.
    /// </summary>
    public class CommandHandlerService : InitializableModule {

        private readonly DiscordSocketClient Client;

        private readonly IServiceProvider Services;

        public readonly CommandService CommandService;

        private readonly BotConfiguration BotConfiguration;

        private readonly CommandModule Module;

        private readonly CustomCommandDB CustomCommandDB;

        private readonly LoggingService LoggingService;

        private static readonly string[] SensitiveCharacters = { "\\", "*", "_", "~", "`", "|", ">", "[", "(" };

        public CommandHandlerService(DiscordSocketClient _Client, CommandService _CommandService, BotConfiguration _BotConfiguration, IServiceProvider _Services, CommandModule _Module, CustomCommandDB _CustomCommandDB, LoggingService _LoggingService) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
            CommandService = _CommandService;
            Services = _Services;
            Module = _Module;
            CustomCommandDB = _CustomCommandDB;
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

            if (!(Message.Channel is IGuildChannel)) {
                if (!Message.Author.IsBot)
                    await Module.BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle($"{BotConfiguration.Bot_Name} is not avaliable in DMs!")
                        .WithDescription($"Heya! I'm not avaliable in DMs at the moment, please use {Client.GetGuild(BotConfiguration.GuildID).Name} to communicate with me!")
                        .SendEmbed(Message.Channel);
                return;
            }

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

                    CustomCommand CustomCommand = CustomCommandDB.GetCommandByNameOrAlias(CustomCommandArgs[0].ToLower());

                    if (CustomCommand != null) {
                        if (CustomCommand.Reply.Length > 0)
                            await Context.Channel.SendMessageAsync(CustomCommand.Reply.Replace("USER", Context.Message.MentionedUserIds.Count > 0 ? $"<@{Context.Message.MentionedUserIds.First()}>" : Context.Message.Author.Mention));
                        else
                            await Module.BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Misconfigured command!")
                                .WithDescription($"{CustomCommand.CommandName} has not been configured! Please contact a moderator about this. <3")
                                .SendEmbed(Context.Channel);
                    } else {
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
                        await Module.BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle(ExecuteResult.Exception.GetType().Name.Prettify())
                            .WithDescription(ExecuteResult.Exception.Message)
                            .SendEmbed(Context.Channel);
                    else
                        await Module.BuildEmbed(EmojiEnum.Annoyed)
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
