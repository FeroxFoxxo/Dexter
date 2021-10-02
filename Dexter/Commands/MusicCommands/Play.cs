using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using DexterSlash.Services;
using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;


namespace Dexter.Commands
{
    public partial class MusicCommands
	{

		[Command("play")]
		[Summary("Immediately plays or enqueues the most popular result of the requested search.")]
		[MusicBotChannel]

		public async Task PlayCommand([Remainder] string search)
		{
			if (!await LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to play `{search}`!")
					.WithDescription("Failed to join voice channel. Are you in a voice channel?")
					.SendEmbed(Context.Channel);

				return;
			}

			if (LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				SearchResponse searchResult;

				try
				{
					searchResult = await LavaNode.SearchAsync(SearchType.YouTube, search);
				}
				catch (Exception)
				{
					await Debug.LogMessageAsync("Lavalink is not connected! Failing with embed error...", LogSeverity.Error);

					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle($"Unable to play `{search}`!")
						.WithDescription("Failure: lavalink dependency missing. Please check the console logs for more details.")
						.SendEmbed(Context.Channel);

					return;
				}

				var track = searchResult.Tracks.FirstOrDefault();

				if (track == null)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to play `{search}`!")
					.WithDescription("The requested search returned no results.")
					.SendEmbed(Context.Channel);

					return;
				}

				if (player.Vueue.Count == 0 && player.PlayerState != PlayerState.Playing && player.PlayerState != PlayerState.Paused)
				{
					await player.PlayAsync(track);
                    await BuildEmbed(EmojiEnum.Unknown).GetNowPlaying(track).SendEmbed(Context.Channel);
				}
				else
				{
					lock (AudioService.Locker)
						player.Vueue.Enqueue(track);

					await BuildEmbed(EmojiEnum.Unknown).GetQueuedTrack(track, player.Vueue.Count).SendEmbed(Context.Channel);
				}
			}
		}

		[Command("play")]
		[MusicBotChannel]

		public async Task PlayCommand()
		{
			if (!await LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Failed to play track!")
					.WithDescription("Failed to join voice channel. Are you in a voice channel?").SendEmbed(Context.Channel);

				return;
			}

			if (LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (player.PlayerState == PlayerState.Paused)
				{
					await player.ResumeAsync();
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Resumed the player.")
						.WithDescription($"Successfully resumed `{player.Track.Title}`")
						.SendEmbed(Context.Channel);
				}
				else
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Failed to play track!")
						.WithDescription("The player must be paused in order to resume it.").SendEmbed(Context.Channel);
				}
			}
		}
	}
}
