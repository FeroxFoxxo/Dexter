using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Discord;
using Discord.WebSocket;

namespace Dexter.Services {

    public class GamesService : Service {

        /// <summary>
        /// The database holding all relevant dynamic data for game management.
        /// </summary>

        public GamesDB GamesDB { get; set; }

        /// <summary>
        /// The configuration file holding all server-specific and mofidiable values for behaviour.
        /// </summary>

        public FunConfiguration FunConfiguration { get; set; }

        /// <summary>
        /// This method is run after dependencies are initialized and injected, it manages hooking up the service to all relevant events.
        /// </summary>

        public override void Initialize() {
            DiscordSocketClient.MessageReceived += HandleMessage; 
        }

        private async Task HandleMessage(SocketMessage Message) {
            if (Message.Channel.Id != FunConfiguration.GamesChannel) return;

            Player Player = GamesDB.Players.Find(Message.Author.Id);

            if (Player is null) return;
            if (Player.Playing < 1) return;

        }
    }
}
