using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

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
        private readonly CommandModule CommandModule;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DiscordSocketClient"></param>
        /// <param name="BotConfiguration"></param>
        /// <param name="LoggingService"></param>
        /// <param name="CommandModule"></param>
        public StartupService(DiscordSocketClient DiscordSocketClient, BotConfiguration BotConfiguration, LoggingService LoggingService, CommandModule CommandModule) {
            this.DiscordSocketClient = DiscordSocketClient;
            this.BotConfiguration = BotConfiguration;
            this.LoggingService = LoggingService;
            this.CommandModule = CommandModule;
        }

        public override void AddDelegates() {
            DiscordSocketClient.Ready += DisplayStartupVersionAsync;
        }

        public async Task StartAsync(string Token) {
            if(!string.IsNullOrEmpty(Token))
                await RunBot(Token);
            if (!string.IsNullOrEmpty(BotConfiguration.Token))
                await RunBot(BotConfiguration.Token);
            else
                await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Error, "Startup", $"The login token in the {BotConfiguration.GetType().Name.Prettify()} file was not set."));
        }

        private async Task RunBot(string Token) {
            await DiscordSocketClient.LoginAsync(TokenType.Bot, Token);
            await DiscordSocketClient.StartAsync();
        }

        private async Task DisplayStartupVersionAsync() {
            ulong Guild = BotConfiguration.GuildID;
            ulong LoggingChannel = BotConfiguration.ModerationLogChannelID;

            if (Guild == 0 || LoggingChannel == 0)
                return;

            ITextChannel Channel = DiscordSocketClient.GetGuild(Guild).GetTextChannel(LoggingChannel);

            if(BotConfiguration.EnableStartupAlert)
                await CommandModule.BuildEmbed(EmojiEnum.Love)
                .WithTitle("Startup complete!")
                .WithDescription($"This is **{BotConfiguration.Bot_Name} v{InitializeDependencies.Version}** running **Discord.Net v{DiscordConfig.Version}**!")
                .SendEmbed(Channel);
        }

    }
}
