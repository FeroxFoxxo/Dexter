using Dexter.Configurations;
using Dexter.Abstractions;
using Discord.WebSocket;

namespace Dexter.Commands {
    public partial class FunCommands : ModuleD {

        private readonly FunConfiguration FunConfiguration;
        private readonly DiscordSocketClient Client;

        public FunCommands(DiscordSocketClient _Client, FunConfiguration _FunConfiguration) {
            FunConfiguration = _FunConfiguration;
            Client = _Client;
        }

    }
}
