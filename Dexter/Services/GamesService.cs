using System;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Discord;
using Discord.WebSocket;

namespace Dexter.Services
{

    /// <summary>
    /// This service manages the Dexter Games subsystem and sends events to the appropriate data structures.
    /// </summary>

    public class GamesService : Service
    {

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

        public override void Initialize()
        {
            DiscordShardedClient.MessageReceived += HandleMessage;
        }

        private async Task HandleMessage(SocketMessage Message)
        {
            if (!FunConfiguration.GamesChannels.Contains(Message.Channel.Id) && Message.Channel is not IDMChannel) return;
            if (Message.Content.StartsWith(BotConfiguration.Prefix)) return;

            Player Player = GamesDB.Players.Find(Message.Author.Id);

            if (Player is null) return;
            if (Player.Playing < 1) return;

            GameInstance Instance = GamesDB.Games.Find(Player.Playing);

            if (Instance is null) return;

            GameTemplate Game = Instance.ToGameProper(BotConfiguration);

            if (Game is null) return;

            await Game.HandleMessage(Message, GamesDB, DiscordShardedClient, FunConfiguration);
        }
    }
}
