using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.CustomCommands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;

namespace Dexter.Services {

    /// <summary>
    /// The CommandHandlerService deals with invoking the command and the errors that may occur as a result.
    /// It filters the command to see if the user is not a bot and that it has the prefix set in the
    /// bot configuration. It also catches all errors that may crop up in a command, logs it, and then sends
    /// an appropriate error to the channel, pinging the developers if the error is unknown.
    /// </summary>
    public class CommandHandlerService : InitializableModule {

        private readonly DiscordSocketClient DiscordSocketClient;

        private readonly IServiceProvider ServiceProvider;

        private readonly CommandService CommandService;

        private readonly BotConfiguration BotConfiguration;

        private readonly CustomCommandDB CustomCommandDB;

        private readonly LoggingService LoggingService;
        
        /// <summary>
        /// The constructor for the CommandHandlerService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="DiscordSocketClient">The current instance of the DiscordSocketClient, which is used to hook into the MessegeRecieved delegate.</param>
        /// <param name="CommandService">The CommandService of the bot, which is used to check if there are any errors on the CommandExecuted event.</param>
        /// <param name="BotConfiguration">The BotConfiguration JSON file, which contains the prefix used to parse commands and developer mention for if the command errors unexpectedly.</param>
        /// <param name="ServiceProvider">The ServiceProvider, which is where our dependencies are stored - given as a field to DiscordNet's execution method.</param>
        /// <param name="CustomCommandDB">The CustomCommandDB is used to get our custom commands, which - if we fail as the command is unknown - we parse to find a match.</param>
        /// <param name="LoggingService">The LoggingService is used to log unexpected errors that may occur on command execution</param>
        public CommandHandlerService(DiscordSocketClient DiscordSocketClient, CommandService CommandService, BotConfiguration BotConfiguration,
                IServiceProvider ServiceProvider, CustomCommandDB CustomCommandDB, LoggingService LoggingService) {
            this.DiscordSocketClient = DiscordSocketClient;
            this.BotConfiguration = BotConfiguration;
            this.CommandService = CommandService;
            this.ServiceProvider = ServiceProvider;
            this.CustomCommandDB = CustomCommandDB;
            this.LoggingService = LoggingService;
        }

        /// <summary>
        /// The AddDelegates override hooks into both the Client's MessageRecieved event and the CommandService's CommandExecuted event.
        /// </summary>
        public override void AddDelegates() {
            DiscordSocketClient.MessageReceived += HandleCommandAsync;
            CommandService.CommandExecuted += SendCommandError;
        }

        /// <summary>
        /// The HandleCommandAsync runs on MessageReceived and will check for if the message has the bot's prefix,
        /// if the author is a bot and if we're in a guild, if so - execute!
        /// </summary>
        /// <param name="SocketMessage">The SocketMessage event is given as a parameter of MessageRecieved and
        /// is used to find and execute the command if the parameters have been met.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task HandleCommandAsync(SocketMessage SocketMessage) {
            if (SocketMessage is not SocketUserMessage Message)
                return;

            int ArgumentPosition = 0;

            if (!(Message.HasStringPrefix(BotConfiguration.Prefix, ref ArgumentPosition) ||
                    Message.HasMentionPrefix(DiscordSocketClient.CurrentUser, ref ArgumentPosition)) ||
                    Message.Author.IsBot)
                return;

            if (!(Message.Channel is IGuildChannel)) {
                if (!Message.Author.IsBot)
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle($"{DiscordSocketClient.CurrentUser.Username} is not avaliable in DMs!")
                        .WithDescription($"Heya! I'm not avaliable in DMs at the moment, " +
                            $"please use {DiscordSocketClient.GetGuild(BotConfiguration.GuildID).Name} to communicate with me!")
                        .SendEmbed(Message.Channel);
                return;
            }

            await CommandService.ExecuteAsync(new SocketCommandContext(DiscordSocketClient, Message), ArgumentPosition, ServiceProvider);
        }

        /// <summary>
        /// The SendCommandError runs on CommandExecuted and checks if the command run has encountered an error. It also handles custom commands through the result of an unknown command.
        /// </summary>
        /// <param name="CommandInfo">This gives information about the command that may have been run, such as its name.</param>
        /// <param name="CommandContext">The context command provides is with information about the message, including who sent it and the channel it was set in.</param>
        /// <param name="Result">The Result specifies the outcome of the attempted run of the command - whether it was successful or not and the error it may have run in to.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task SendCommandError(Optional<CommandInfo> CommandInfo, ICommandContext CommandContext, IResult Result) {
            if (Result.IsSuccess)
                return;

            switch (Result.Error) {
                case CommandError.UnmetPrecondition:
                    if (Result.ErrorReason.Length <= 0)
                        return;

                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Halt! Don't go there-")
                        .WithDescription(Result.ErrorReason)
                        .SendEmbed(CommandContext.Channel);
                    break;
                case CommandError.BadArgCount:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("You've entered an invalid amount of parameters for this command!")
                        .WithDescription($"Here are some options of parameters you can have for the command **{CommandInfo.Value.Name}**.")
                        .GetParametersForCommand(CommandInfo.Value)
                        .SendEmbed(CommandContext.Channel);
                    break;
                case CommandError.UnknownCommand:
                    string[] CustomCommandArgs = CommandContext.Message.Content[BotConfiguration.Prefix.Length..].Split(' ');

                    CustomCommand CustomCommand = CustomCommandDB.GetCommandByNameOrAlias(CustomCommandArgs[0].ToLower());

                    if (CustomCommand != null) {
                        if (CustomCommand.Reply.Length > 0)
                            await CommandContext.Channel.SendMessageAsync(CustomCommand.Reply.Replace("USER", CommandContext.Message.MentionedUserIds.Count > 0 ? $"<@{CommandContext.Message.MentionedUserIds.First()}>" : CommandContext.Message.Author.Mention));
                        else
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Misconfigured command!")
                                .WithDescription($"{CustomCommand.CommandName} has not been configured! Please contact a moderator about this. <3")
                                .SendEmbed(CommandContext.Channel);
                    } else {
                        //await BuildEmbed(EmojiEnum.Annoyed)
                        //    .WithTitle("Unknown Command.")
                        //    .WithDescription($"Oopsies! It seems as if the command **{CustomCommandArgs[0].SanitizeMarkdown()}** doesn't exist!")
                        //    .SendEmbed(CommandContext.Channel);
                    }
                    break;
                case CommandError.ParseFailed:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to parse command!")
                        .WithDescription("Invalid amount of command arguments.")
                        .SendEmbed(CommandContext.Channel);
                    break;
                default:
                    await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Warning, GetType().Name.Prettify(), $"Unknown statement reached!\nCommand: {(CommandInfo.IsSpecified ? CommandInfo.Value.Name : null)}\nResult: {Result}"));

                    if (!string.IsNullOrEmpty(BotConfiguration.DeveloperMention))
                        await CommandContext.Channel.SendMessageAsync($"Unknown error! I'll tell the developers.\n{BotConfiguration.DeveloperMention}");
                    else
                        await CommandContext.Channel.SendMessageAsync("Unknown error! I wanted to tell the developers, but I don't know who they are!");

                    if (Result is ExecuteResult ExecuteResult)
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle(ExecuteResult.Exception.GetType().Name.Prettify())
                            .WithDescription(ExecuteResult.Exception.Message)
                            .SendEmbed(CommandContext.Channel);
                    else
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle(Result.Error.GetType().Name.Prettify())
                            .WithDescription(Result.ErrorReason)
                            .SendEmbed(CommandContext.Channel);

                    break;
            }
        }

    }

}
