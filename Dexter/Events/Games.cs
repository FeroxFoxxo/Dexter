using System;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Games;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Events
{

	/// <summary>
	/// This service manages the Dexter Games subsystem and sends events to the appropriate data structures.
	/// </summary>

	public class Games : Event
	{
		/// <summary>
		/// The configuration file holding all server-specific and mofidiable values for behaviour.
		/// </summary>

		public FunConfiguration FunConfiguration { get; set; }

		/// <summary>
		/// This method is run after dependencies are initialized and injected, it manages hooking up the service to all relevant events.
		/// </summary>

		public override void InitializeEvents()
		{
			DiscordShardedClient.MessageReceived += HandleMessage;
		}

		private async Task HandleMessage(SocketMessage Message)
		{
			if (!FunConfiguration.GamesChannels.Contains(Message.Channel.Id) && Message.Channel is not IDMChannel) return;
			if (Message.Content.StartsWith(BotConfiguration.Prefix)) return;

			using var scope = ServiceProvider.CreateScope();

			using var GamesDB = scope.ServiceProvider.GetRequiredService<GamesDB>();

			Player Player = GamesDB.Players.Find(Message.Author.Id);

			if (Player is null) return;
			if (Player.Playing < 1) return;

			GameInstance Instance = GamesDB.Games.Find(Player.Playing);

			if (Instance is null) return;

			GameTemplate Game = Instance.ToGameProper(BotConfiguration);

			if (Game is null) return;

			await Game.HandleMessage(Message, GamesDB, DiscordShardedClient, FunConfiguration);

			await GamesDB.SaveChangesAsync();
		}
	}
}
