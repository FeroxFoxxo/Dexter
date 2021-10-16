using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using Victoria.Node;

namespace Dexter.Commands
{
	public partial class MusicCommands
	{

		[Command("skip")]
		[Alias("s")]
		[Summary("Skips the number of songs specified at once.")]
		[MusicBotChannel]

		public async Task SkipCommand(int skipCount)
		{
			if (skipCount < 2)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to skip song!")
					.WithDescription("If you are specifiying a skip count, you must skip 2+ tracks.").SendEmbed(Context.Channel);

				return;
			}

			if (!MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to skip song!")
					.WithDescription("I couldn't find the music player for this server.\n" +
					"Please ensure I am connected to a voice channel before using this command.").SendEmbed(Context.Channel);

				return;
			}

			int uCount = 0;
			await foreach (var _ in player.VoiceChannel.GetUsersAsync())
				uCount++;

			if (uCount >= 2 && Context.User.GetPermissionLevel(DiscordShardedClient, BotConfiguration) < PermissionLevel.DJ)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to skip song!")
					.WithDescription("You must have the DJ role to skip tracks.").SendEmbed(Context.Channel);

				// TODO VOTE SKIP

			}
			else
			{
				var curTrack = player.Track;
				bool emptyQueue = player.Vueue.Count == 0;

				if (curTrack == null)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to skip song!")
						.WithDescription("There isn't anything to skip.").SendEmbed(Context.Channel);

					return;
				}

				if (emptyQueue)
				{
					await player.StopAsync();
					await BuildEmbed(EmojiEnum.Love).WithTitle($"Skipped {curTrack.Title}.")
						.WithDescription("No more tracks remaining.").SendEmbed(Context.Channel);
				}
				else if (skipCount == 1)
				{
					await player.SkipAsync();

					await BuildEmbed(EmojiEnum.Love)
						.GetNowPlaying(player.Track)
						.AddField("Skipped", curTrack.Title)
						.SendEmbed(Context.Channel);
				}
				else
				{
					int actualSkipCount = 0;

					for (int i = 0; i < skipCount; i++)
					{
						try
						{
							await player.SkipAsync();
							actualSkipCount++;
						}
						catch (InvalidOperationException)
						{
							await player.StopAsync();
							break;
						}
					}

					string s = actualSkipCount == 1 ? "" : "s";

					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Songs have been skipped!")
						.WithDescription($"Skipped {actualSkipCount:N0} track{s}.").SendEmbed(Context.Channel);
				}
			}
		}

		[Command("skip")]
		[Alias("s")]
		[Summary("Skips the current song.")]
		[MusicBotChannel]

		public async Task SkipCommand()
		{
			await SkipCommand(1);
		}
	}
}
