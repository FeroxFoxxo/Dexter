using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using Figgle;

namespace Dexter.Services {

    /// <summary>
    /// The Startup Serivce module applies the token for the bot, as well as running the bot once all dependencies have loaded up.
    /// Furthermore, it logs and sends a message to the moderation channel when it does start up, including various information
    /// like its bot and Discord.NET versionings.
    /// </summary>
    public class StartupService : InitializableModule {

        private readonly DiscordSocketClient DiscordSocketClient;

        private readonly LoggingService LoggingService;

        private readonly BotConfiguration BotConfiguration;

        /// <summary>
        /// The constructor for the StartupService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="DiscordSocketClient">An instance of the DiscordSocketClient, is what we use to hook into the ready event.</param>
        /// <param name="BotConfiguration">The BotConfiguration, which contains the bot's token, as well as which channel to send the startup message to.</param>
        /// <param name="LoggingService">An instance of the Logging Service, which we use to log if the token has not been set to the console.</param>
        public StartupService(DiscordSocketClient DiscordSocketClient, BotConfiguration BotConfiguration, LoggingService LoggingService) {
            this.DiscordSocketClient = DiscordSocketClient;
            this.BotConfiguration = BotConfiguration;
            this.LoggingService = LoggingService;
        }

        /// <summary>
        /// The AddDelegates method hooks the client ready event to the Display Startup Version Async method.
        /// </summary>
        public override void AddDelegates() {
            DiscordSocketClient.Ready += DisplayStartupVersionAsync;
        }

        /// <summary>
        /// The Start Async method runs at the end of the dependencies having been initialized,
        /// and is what runs the bot using the token and logs if the token doesn't exist to the console.
        /// </summary>
        /// <param name="Token">A string, containing the Token from the command line arguments.
        /// Returns false if does not exist and flows onto the token specified in the BotConfiguration.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task StartAsync(string Token) {
            if(!string.IsNullOrEmpty(Token))
                await RunBot(Token);
            if (!string.IsNullOrEmpty(BotConfiguration.Token))
                await RunBot(BotConfiguration.Token);
            else
                await LoggingService.TryLogMessage(new LogMessage(LogSeverity.Error, "Startup", $"The login token in the {BotConfiguration.GetType().Name.Prettify()} file was not set."));
        }

        /// <summary>
        /// The Run Bot method logs into the bot using the token specified as a parameter and then starts the bot asynchronously.
        /// </summary>
        /// <param name="Token">A string containing the token from which we use to log into Discord.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task RunBot(string Token) {
            LoggingService.SetOutputToLocked(true);
            await DiscordSocketClient.LoginAsync(TokenType.Bot, Token);
            await DiscordSocketClient.StartAsync();
        }

        /// <summary>
        /// The Display Startup Version Async method runs on ready and is what attempts to log the initialization of the bot
        /// to a specified guild that the bot has sucessfully started and the versionings that it is running.
        /// </summary>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task DisplayStartupVersionAsync() {
            ulong Guild = BotConfiguration.GuildID;
            ulong LoggingChannel = BotConfiguration.ModerationLogChannelID;

            if (Guild == 0 || LoggingChannel == 0)
                return;

            ITextChannel Channel = DiscordSocketClient.GetGuild(Guild).GetTextChannel(LoggingChannel);

            Console.Clear();

            Console.Title = $"{DiscordSocketClient.CurrentUser.Username} v{InitializeDependencies.Version} (Discord.Net v{DiscordConfig.Version})";

            Console.ForegroundColor = ConsoleColor.Cyan;

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(DiscordSocketClient.CurrentUser.Username));

            LoggingService.SetOutputToLocked(false);

            if (BotConfiguration.EnableStartupAlert)
                await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Startup complete!")
                .WithDescription($"This is **{DiscordSocketClient.CurrentUser.Username} v{InitializeDependencies.Version}** running **Discord.Net v{DiscordConfig.Version}**!")
                .SendEmbed(Channel);
        }

    }

}
