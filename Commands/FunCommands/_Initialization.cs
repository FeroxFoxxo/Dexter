using Dexter.Configurations;
using Dexter.Abstractions;
using Discord.WebSocket;

namespace Dexter.Commands {
    public partial class FunCommands : DiscordModule {

        private readonly FunConfiguration FunConfiguration;
        private readonly DiscordSocketClient Client;
        private readonly BotConfiguration BotConfiguration;

        public FunCommands(DiscordSocketClient Client, FunConfiguration FunConfiguration, BotConfiguration BotConfiguration) : base (BotConfiguration) {
            this.FunConfiguration = FunConfiguration;
            this.Client = Client;
            this.BotConfiguration = BotConfiguration;
        }

    }
}
