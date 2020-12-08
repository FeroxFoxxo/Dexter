using Dexter.Abstractions;
using Dexter.Configurations;
using Discord.WebSocket;
using System;

namespace Dexter.Commands {

    public partial class MuzzleCommands : DiscordModule {

        public MuzzleConfiguration MuzzleConfiguration { get; set; }

        public DiscordSocketClient DiscordSocketClient { get; set; }

        public Random Random { get; set; }

    }

}
