using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Newtonsoft.Json;

namespace Dexter.Commands
{

	public partial class LevelingCommands
	{

		/// <summary>
		/// Loads levels from an existing levels.json file.
		/// </summary>
		/// <param name="arg">Special arguments that modify loading such as FORCE</param>
		/// <returns>A <c>Task</c> object, which can be awaited until it completes successfully.</returns>

		[Command("loadlevels")]
		[Summary("Loads levels from a json file into the system. To force replacement of levels that already exist type \"FORCE\" after the command.")]
		[RequireAdministrator]

		public async Task LoadLevelsCommand(string arg = "")
		{

			bool force = arg == "FORCE";
			LevelRecord[] records;
			try
			{
				records = JsonConvert.DeserializeObject<LevelRecord[]>(
					File.ReadAllText(Path.Join(Directory.GetCurrentDirectory(), "Images", "OtherMedia", "LevelData", "levels.json"))
					);
			}
			catch (FileNotFoundException e)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("No levels file!")
					.WithDescription(e.Message)
					.SendEmbed(Context.Channel);
				return;
			}

			IUserMessage loadMsg = await Context.Channel.SendMessageAsync($"Loading level entries... 0/{records.Length}");

			int total = 0;
			int count = 0;
			foreach (LevelRecord r in records)
			{
				UserLevel ul = null;
				if (++total % 1000 == 0)
				{
					await loadMsg.ModifyAsync(m => m.Content = $"Loading level entries... {total}/{records.Length}");
				}
				try
				{
					ul = LevelingDB.GetOrCreateLevelData(r.Id);
					if (force || ul.TextXP < r.Xp)
					{
						ul.TextXP = r.Xp;
						count++;
					}
				}
				catch (InvalidOperationException e)
				{
					ul = LevelingDB.GetOrCreateLevelData(r.Id);
					try
					{
						if (force || ul?.TextXP < r.Xp)
						{
							ul.TextXP = r.Xp;
							count++;
						}
					}
					catch
					{
						await Context.Channel.SendMessageAsync($"Failed to load user {r.Id}");
					}
				}
			}

			if (count > 0)
			{
				await BuildEmbed(EmojiEnum.Love)
					.WithTitle($"Loaded {count} levels")
					.WithDescription($"The level data for {count} users has been added to the local database.")
					.SendEmbed(Context.Channel);
				return;
			}

			await BuildEmbed(EmojiEnum.Annoyed)
				.WithTitle("No loaded levels!")
				.WithDescription("All inputs in the data are either registered or have less XP than in the database. If you wish to force an overwrite, type \"FORCE\" after the command.")
				.SendEmbed(Context.Channel);
		}

#pragma warning disable 0649
		//Unassigned variables (Assigned through JSON Deserialization)
		private class LevelRecord
		{
			public LevelRecord(ulong id, int level, long xp)
			{
				Id = id;
				Level = level;
				Xp = xp;
			}

			public ulong Id { get; set; }
			public int Level { get; set; }
			public long Xp { get; set; }
		}
#pragma warning restore 0649

		const string mee6apiurl = "https://mee6.xyz/api/plugins/levels/leaderboard/";

		/// <summary>
		/// Loads all levels from the mee6 api in a given page range.
		/// </summary>
		/// <param name="min">The starting page to load</param>
		/// <param name="max">The last page to load</param>
		/// <param name="args">Other arguments that modify the loading mode like TRANSFORM or FORCE</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("webloadlevels")]
		[Summary("Usage: `webloadlevels [minpage] [maxpage] (Args)`. Loads levels from the mee6 API into the system. To force replacement of levels that already exist type \"FORCE\" after the command.")]
		[RequireAdministrator]

		public async Task LoadLevelsFromMee6Command(int min = 0, int max = 50, [Remainder] string args = "")
		{
			if (min < 0) min = 0;

			if (min > max)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Invalid range!")
					.WithDescription($"*max* must exceed *min*. Received *min* of {min} and *max* of {max}")
					.SendEmbed(Context.Channel);
				return;
			}

			IUserMessage loadMsg = await Context.Channel.SendMessageAsync($"Loading level entries... 0/{(max - min + 1) * 100}");
			int total = 0;
			int count = 0;
			int page = 0;

			string[] argsArr = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			bool force = argsArr?.Contains("FORCE") ?? false;
			bool transform = argsArr?.Contains("TRANSFORM") ?? false;
			bool updateroles = argsArr?.Contains("UPDATEROLES") ?? false;

			using HttpClient web = new();
			string dataStr;
			LBData data;

			try
			{
				for (page = min; page <= max; page++)
				{
					string url = $"{mee6apiurl}{LevelingConfiguration.Mee6SyncGuildId}?&page={page}";
					dataStr = await web.GetStringAsync(url);
					data = JsonConvert.DeserializeObject<LBData>(dataStr);

					if (data?.players?.FirstOrDefault() is null)
						throw new ArgumentOutOfRangeException($"Reached empty leaderboard page ({page}) before reaching max ({max}). Loaded a total of {count} entries out of {total} attempts.");

					foreach (LBPlayer p in data.players)
					{
						long xp = p.xp;
						if (transform)
						{
							xp = LevelingConfiguration.GetXPForLevel(LevelTransformation.Metricate.Run(p.DetailedLevel));
						}
						UserLevel ul = null;
						try
						{
							ul = LevelingDB.GetOrCreateLevelData(p.id);
							if (force || ul.TextXP < xp)
							{
								ul.TextXP = xp;
								count++;
								if (updateroles)
									await LevelingService.UpdateRoles(Context.Guild.GetUser(ul.UserID), force);
							}
						}
						catch (Exception e)
						{
							ul = LevelingDB.GetOrCreateLevelData(p.id);

							try
							{
								if (force || ul?.TextXP < xp)
								{
									ul.TextXP = xp;
									count++;
									if (updateroles)
										await LevelingService.UpdateRoles(Context.Guild.GetUser(ul.UserID), force);
								}
							}
							catch
							{
								await Context.Channel.SendMessageAsync($"Failed to load user {p.id} from page {page}.");
							}
						}
						if (++total % 50 == 0)
						{
							await loadMsg.ModifyAsync(m => m.Content = $"Loading level entries... {total}/{(max - min + 1) * 100} - page {page}/{max}. (modified {count} values.)");
						}
					}
				}
			}
			catch (ArgumentOutOfRangeException e)
			{
				await BuildEmbed(EmojiEnum.Sign)
					.WithTitle("Completed Loading with Errors")
					.WithDescription(e.ParamName)
					.SendEmbed(Context.Channel);
				return;
			}
			catch (HttpException e)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("HTTP Exception Reached")
					.WithDescription($"Error {e.HttpCode}: {e.Message}.\n" +
						$"Completed task up to page {--page}. Assigning {count} new values from {total} total processed entries.")
					.SendEmbed(Context.Channel);
				return;
			}

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Completed Loading")
				.WithDescription($"Loaded pages up to {--page}. Assigned {count} new values from {total} total processed entries.")
				.SendEmbed(Context.Channel);
		}

#pragma warning disable IDE1006 // Naming Styles
		[Serializable]
		private class LBPlayer
		{
			public long xp { get; set; }
			public long[] detailed_xp { get; set; }
			public ulong id { get; set; }
			public int level { get; set; }

			public float DetailedLevel => level + ((float)detailed_xp[0] / detailed_xp[1]);

			public override string ToString()
			{
				return $"LVL {level} {xp} = {detailed_xp[2]} ({detailed_xp[0]}/{detailed_xp[1]}) - ID = {id}";
			}
		}

		[Serializable]
		private class LBData
		{
			public LBPlayer[] players { get; set; }
		}
#pragma warning restore IDE1006 // Naming Styles

		private class LevelTransformation
		{
			public Dictionary<int, int> equivalences;

			public LevelTransformation(int[] reffrom, int[] refto)
			{
				equivalences = new();
				for (int i = 0; i < Math.Min(reffrom.Length, refto.Length); i++)
				{
					equivalences.Add(reffrom[i], refto[i]);
				}
			}

			public float Run(float reflevel)
			{
				if (reflevel < 0) return 0;

				float slope = 1;
				KeyValuePair<int, int> last = new(0, 0);
				foreach (KeyValuePair<int, int> e in equivalences)
				{
					slope = (float)(e.Value - last.Value) / (e.Key - last.Key);
					if (reflevel < e.Key)
					{
						return last.Value + (reflevel - last.Key) * slope;
					}
					last = e;
				}
				return last.Value + (reflevel - last.Key) * slope;
			}

			public static LevelTransformation Metricate =>
					new(new int[] { 0, 1, 7, 14, 21, 28, 35, 42, 49, 55, 61, 67, 73, 79, 85, 90, 95, 100 },
						new int[] { 0, 1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 });
		}
	}
}
