using Dexter.Configurations;
using Dexter.Abstractions;
using Discord.WebSocket;
using Dexter.Databases.FunTopics;

namespace Dexter.Commands {

    public partial class FunCommands : DiscordModule {

        public FunConfiguration FunConfiguration { get; set; }

        public DiscordSocketClient DiscordSocketClient { get; set; }

        public FunTopicsDB FunTopicsDB { get; set; }

    }

}
