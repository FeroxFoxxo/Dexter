using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Databases.CustomCommands;

namespace Dexter.Commands.CustomCommands {
    public partial class CustomCommands : ModuleD {

        private readonly CustomCommandDB CustomCommandDB;
        private readonly BotConfiguration BotConfiguration;

        public CustomCommands(CustomCommandDB _CustomCommandDB, BotConfiguration _BotConfiguration) {
            CustomCommandDB = _CustomCommandDB;
            BotConfiguration = _BotConfiguration;
        }

    }
}