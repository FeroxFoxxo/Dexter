using Dexter.Abstractions;
using Dexter.Configuration;
using Dexter.Databases.CustomCommands;

namespace Dexter.Commands.CustomCommands {
    public partial class CustomCommands : Module {

        private readonly CustomCommandDB CustomCommandDB;
        private readonly BotConfiguration BotConfiguration;

        public CustomCommands(CustomCommandDB _CustomCommandDB, BotConfiguration _BotConfiguration) {
            CustomCommandDB = _CustomCommandDB;
            BotConfiguration = _BotConfiguration;
        }

    }
}