using Dexter.Configurations;
using Dexter.Abstractions;
using Discord.WebSocket;
using Dexter.Databases.FunTopics;

namespace Dexter.Commands {
    public partial class FunCommands : DiscordModule {

        private readonly FunConfiguration FunConfiguration;
        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly FunTopicsDB FunTopicsDB;
        private readonly BotConfiguration BotConfiguration;

        public FunCommands(DiscordSocketClient DiscordSocketClient, FunConfiguration FunConfiguration,
                FunTopicsDB FunTopicsDB, BotConfiguration BotConfiguration) {
            this.FunConfiguration = FunConfiguration;
            this.DiscordSocketClient = DiscordSocketClient;
            this.FunTopicsDB = FunTopicsDB;
            this.BotConfiguration = BotConfiguration;
        }

    }
}
