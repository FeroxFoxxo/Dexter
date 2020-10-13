using Dexter.Abstractions;
using Dexter.Configuration;
using Discord.WebSocket;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands : Module {

        private readonly FunConfiguration FunConfiguration;
        private readonly DiscordSocketClient Client;

        public FunCommands(DiscordSocketClient _Client, FunConfiguration _FunConfiguration) {
            FunConfiguration = _FunConfiguration;
            Client = _Client;
        }

    }
}
