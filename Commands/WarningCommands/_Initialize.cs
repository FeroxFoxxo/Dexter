using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Databases.Warnings;
using System;

namespace Dexter.Commands.WarningCommands {
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
