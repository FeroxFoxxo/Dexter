using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Databases.CustomCommands;

namespace Dexter.Commands {
    public partial class CustomCommands : DiscordModule {

        private readonly CustomCommandDB CustomCommandDB;
        private readonly BotConfiguration BotConfiguration;

        public CustomCommands(CustomCommandDB CustomCommandDB, BotConfiguration BotConfiguration) {
            this.CustomCommandDB = CustomCommandDB;
            this.BotConfiguration = BotConfiguration;
        }

    }
}