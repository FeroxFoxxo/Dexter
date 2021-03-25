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
using Discord.Net;

namespace Dexter.Services {

    /// <summary>
    /// The CommandHandlerService deals with invoking the command and the errors that may occur as a result.
    /// It filters the command to see if the user is not a bot and that it has the prefix set in the
    /// bot configuration. It also catches all errors that may crop up in a command, logs it, and then sends
    /// an appropriate error to the channel, pinging the developers if the error is unknown.
    /// </summary>
    
    public class CommandHandlerService : Service {

        /// <summary>
        /// The ServiceProvider is where our dependencies are stored - given as a field to DiscordNet's execution method.
        /// </summary>
        
        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// The CommandService of the bot is used to check if there are any errors on the CommandExecuted event.
        /// </summary>
        
        public CommandService CommandService { get; set; }
        
        /// <summary>
        /// The CustomCommandDB is used to get our custom commands, which - if we fail as the command is unknown - we parse to find a match.
        /// </summary>
        
        public CustomCommandDB CustomCommandDB { get; set; }

        /// <summary>
        /// The LoggingService is used to log unexpected errors that may occur on command execution.
        /// </summary>
        
        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// The ProposalConfiguration is used to operate the suggestion service and confugure voting thresholds.
        /// </summary>

        public ProposalConfiguration ProposalConfiguration { get; set; }

        /// <summary>
        /// The Initialize override hooks into both the Client's MessageReceived event and the CommandService's CommandExecuted event.
        /// </summary>

        public override void Initialize() {
            DiscordSocketClient.MessageReceived += HandleCommandAsync;
            CommandService.CommandExecuted += SendCommandError;
        }

        /// <summary>
        /// The HandleCommandAsync runs on MessageReceived and will check for if the message has the bot's prefix,
        /// if the author is a bot and if we're in a guild, if so - execute!
        /// </summary>
        /// <param name="SocketMessage">The SocketMessage event is given as a parameter of MessageReceived and
        /// is used to find and execute the command if the parameters have been met.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task HandleCommandAsync(SocketMessage SocketMessage) {
            // We do not check the message if it is not an instance of a user message.
            if (SocketMessage is not SocketUserMessage Message)
                return;

            int ArgumentPosition = 0;

            // We do not parse the message if it does not have the prefix or it is from a bot.
            if (!Message.HasStringPrefix(BotConfiguration.Prefix, ref ArgumentPosition) || Message.Author.IsBot || BotConfiguration.DisallowedChannels.Contains(Message.Channel.Id))
                return;

            // Finally, if all prerequesites have returned correctly, we run and parse the command with an instance of our socket command context and our services.
            await CommandService.ExecuteAsync(new SocketCommandContext(DiscordSocketClient, Message), ArgumentPosition, ServiceProvider);
        }

        /// <summary>
        /// The SendCommandError runs on CommandExecuted and checks if the command run has encountered an error. It also handles custom commands through the result of an unknown command.
        /// </summary>
        /// <param name="CommandInfo">This gives information about the command that may have been run, such as its name.</param>
        /// <param name="CommandContext">The context command provides is with information about the message, including who sent it and the channel it was set in.</param>
        /// <param name="Result">The Result specifies the outcome of the attempted run of the command - whether it was successful or not and the error it may have run in to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public async Task SendCommandError(Optional<CommandInfo> CommandInfo, ICommandContext CommandContext, IResult Result) {
            if (Result.IsSuccess)
                return;

            try {

                switch (Result.Error) {

                    // Unmet Precondition specifies that the error is a result as one of the preconditions specified by an attribute has returned FromError.
                    case CommandError.UnmetPrecondition:
                        if (Result.ErrorReason.Length <= 0)
                            return;

                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Halt! Don't go there-")
                            .WithDescription(Result.ErrorReason)
                            .SendEmbed(CommandContext.Channel);
                        break;

                    // Bad Argument Count specifies that the command has had an invalid amount of arguments parsed to it. It will send all the commands with their parameters and summaries in response.
                    case CommandError.BadArgCount:
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You've entered an invalid amount of parameters for this command!")
                            .WithDescription($"Here are some options of parameters you can have for the command **{CommandInfo.Value.Name}**.")
                            .GetParametersForCommand(CommandInfo.Value, BotConfiguration)
                            .SendEmbed(CommandContext.Channel);
                        break;

                    // Unknown Command specifies that the parser was unable to find a command with the name specified. If this throws, we look for custom commands that may have the name and then send that it is an unknown command if there are not any returned.
                    case CommandError.UnknownCommand:
                        string[] CustomCommandArgs = CommandContext.Message.Content[BotConfiguration.Prefix.Length..].Split(' ');

                        CustomCommand CustomCommand = CustomCommandDB.GetCommandByNameOrAlias(CustomCommandArgs[0].ToLower());

                        if (CustomCommand != null) {
                            if (CustomCommand.Reply.Length > 0) {
                                string Reply = CustomCommand.Reply;

                                Reply = Reply.Replace("USER", CommandContext.Message.MentionedUserIds.Count > 0 ? $"<@{CommandContext.Message.MentionedUserIds.First()}>" : CommandContext.User.Mention);
                                Reply = Reply.Replace("AUTHOR", CommandContext.User.Mention);

                                await CommandContext.Channel.SendMessageAsync(CustomCommand.Reply.Replace("USER", CommandContext.Message.MentionedUserIds.Count > 0 ? $"<@{CommandContext.Message.MentionedUserIds.First()}>" : CommandContext.User.Mention));
                            } else
                                await BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Misconfigured command!")
                                    .WithDescription($"`{CustomCommand.CommandName}` has not been configured! Please contact a moderator about this. <3")
                                    .SendEmbed(CommandContext.Channel);
                        } else {
                            if (CommandContext.Message.Content.Length <= 1)
                                return;
                            else if (CommandContext.Message.Content.Count(Character => Character == '~') > 1 ||
                                    ProposalConfiguration.CommandRemovals.Contains(CommandContext.Message.Content.Split(' ')[0]))
                                return;
                            else {
                                IMessage Message = await CommandContext.Channel.SendMessageAsync(
                                    embed: BuildEmbed(EmojiEnum.Annoyed)
                                        .WithTitle("Unknown Command.")
                                        .WithDescription($"Oopsies! It seems as if the command **{CustomCommandArgs[0].SanitizeMarkdown()}** doesn't exist!")
                                        .Build()
                                );

                                _ = Task.Run(async () => {
                                    await Task.Delay(5000);
                                    await Message.DeleteAsync();
                                });
                            }
                        }
                        break;

                    // Parse Failed specifies that the TypeReader has been unable to parse a specific parameter of the command.
                    case CommandError.ParseFailed:
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to parse command!")
                            .WithDescription("Invalid amount of command arguments.")
                            .SendEmbed(CommandContext.Channel);
                        break;

                    // The default case specifies that this command has run into an unknown error that will need to be reported.
                    default:

                        // If we have been thrown an ObjectNotFound error, this means that the argument has been unable to be found. This could be due to caching, thus we do not need to ping the developers of this error.
                        if (Result.ToString().Contains("ObjectNotFound")) {
                            await BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle(Result.ErrorReason)
                                .WithDescription($"If you believe this was an error, please do ping a developer!\nIf the {Result.ErrorReason.Split(' ')[0].ToLower()} does exist, it may be due to caching. If so, please wait a few minutes.")
                                .SendEmbed(CommandContext.Channel);

                            return;
                        }

                        // If the error is not an ObjectNotFound error, we log the message to the console with the appropriate data.
                        await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Warning, GetType().Name.Prettify(), $"Unknown statement reached!\nCommand: {(CommandInfo.IsSpecified ? CommandInfo.Value.Name : null)}\nResult: {Result}"));

                        EmbedBuilder CommandErrorEmbed;

                        // Once logged, we check to see if the error is an ExecuteResult error as these execution results have more data about the issue that has gone wrong.
                        if (Result is ExecuteResult ExecuteResult)
                            CommandErrorEmbed = BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle(ExecuteResult.Exception.GetType().Name.Prettify())
                                .WithDescription(ExecuteResult.Exception.Message);
                        else
                            CommandErrorEmbed = BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle(Result.Error.GetType().Name.Prettify())
                                .WithDescription(Result.ErrorReason);

                        // Finally, we send the error into the channel with a ping to the developers to take notice of.
                        await CommandContext.Channel.SendMessageAsync($"Unknown error!{(BotConfiguration.PingDevelopers ? $" I'll tell the developers.\n<@&{BotConfiguration.DeveloperRoleID}>" : string.Empty)}", embed: CommandErrorEmbed.Build());
                        break;
                }
            } catch (HttpException) {
                await CommandContext.Channel.SendMessageAsync($"Haiya <@&{BotConfiguration.DeveloperRoleID}>, it seems as though the bot does not have the correct permissions to send embeds into this channel!\n" +
                    $"Command errored out on the {Result.Error.Value} error.");
            }

        }

    }

}
