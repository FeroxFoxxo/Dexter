using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Databases.Warnings;
using System;

namespace Dexter.Commands {
    public partial class WarningCommands : ModuleD {

        private readonly WarningsDB WarningsDB;
        private readonly BotConfiguration BotConfiguration;

        private readonly string RandomizedCharacters;
        private readonly Random Random;

        public WarningCommands(WarningsDB _WarningsDB, BotConfiguration _BotConfiguration) {
            WarningsDB = _WarningsDB;
            BotConfiguration = _BotConfiguration;
            RandomizedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random = new Random();
        }

    }
}
