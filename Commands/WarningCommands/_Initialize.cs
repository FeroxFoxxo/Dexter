using Dexter.Abstractions;
using Dexter.Databases.Warnings;

namespace Dexter.Commands {
    public partial class WarningCommands : DiscordModule {

        private readonly WarningsDB WarningsDB;

        public WarningCommands(WarningsDB _WarningsDB) {
            WarningsDB = _WarningsDB;
        }

    }
}
