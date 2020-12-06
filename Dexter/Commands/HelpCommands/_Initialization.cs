using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Attributes.Classes;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Commands {

    [EssentialModule]
    public partial class HelpCommands : DiscordModule {

        public CommandService CommandService { get; set; }

        public DiscordSocketClient DiscordSocketClient { get; set; }

    }

}
