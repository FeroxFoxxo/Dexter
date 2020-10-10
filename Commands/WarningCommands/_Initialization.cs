using Dexter.Core.Abstractions;
using Dexter.Databases.Warnings;

namespace Dexter.Commands.WarningCommands {
    public class WarningCommands : Module {

        private readonly WarningsDB WarningsDB;

        public WarningCommands(WarningsDB _WarningsDB) {
            WarningsDB = _WarningsDB;
        }

    }
}
