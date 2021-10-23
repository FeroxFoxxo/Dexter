using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Dexter.Databases.Games
{

	/// <summary>
	/// Holds all relevant data for the Dexter Games subsystem.
	/// </summary>

	public class GamesDB : Database
	{

		/// <summary>
		/// Holds game-specific data, a set of GameInstances (or sessions).
		/// </summary>

		public DbSet<GameInstance> Games { get; set; }

		/// <summary>
		/// Holds player-specific data, like what game they're playing or their score.
		/// </summary>

		public DbSet<Player> Players { get; set; }

		/// <summary>
		/// Gets a player from their unique user <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The ID of the player to fetch.</param>
		/// <returns>A Player whose ID matches <paramref name="id"/>, or a new one matching it.</returns>

		public Player GetOrCreatePlayer(ulong id)
		{

			Player p = Players.Find(id);
			if (p is null)
			{
				p = new()
				{
					UserID = id,
					Playing = -1,
					Data = "",
					Lives = 0,
					Score = 0
				};
				Players.Add(p);
			}
			return p;
		}

		/// <summary>
		/// Gets all the players who are currently active in a given instance.
		/// </summary>
		/// <param name="instanceID">The unique ID of the game instance to fetch from.</param>
		/// <returns>An array of Players who are currently playing the instance identified by <paramref name="instanceID"/>.</returns>

		public Player[] GetPlayersFromInstance(int instanceID)
		{
			if (instanceID <= 0) return Array.Empty<Player>();
			return Players.AsQueryable().Where(p => p.Playing == instanceID).ToArray();
		}
	}
}
