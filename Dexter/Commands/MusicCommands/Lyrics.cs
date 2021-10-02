using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Victoria;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Dexter.Commands
{
    public partial class MusicCommands
	{

		[Command("lyrics")]
		[Summary("Replies with the lyrics to the current track that is playing.")]
		[MusicBotChannel]

		public async Task LyricsCommand()
		{
			if (!await LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to pause the player!")
					.WithDescription("Failed to join voice channel. Are you in a voice channel?")
					.SendEmbed(Context.Channel);

				return;
			}

			if (LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (player.PlayerState != PlayerState.Playing)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to find song!")
						.WithDescription("Woaaah there, I'm not playing any tracks. " +
						"Please make sure I'm playing something before trying to find the lyrics for it!")
						.SendEmbed(Context.Channel);

					return;
				}

				await SendLyricsFromTrack(player.Track);
			}
		}

		[Command("lyrics")]
		[Summary("Replies with the lyrics to the song provided.")]
		[MusicBotChannel]

		public async Task LyricsCommand([Remainder] string song)
		{
			SearchResponse searchResult;

			try
			{
				searchResult = await LavaNode.SearchAsync(SearchType.YouTube, song);
			}
			catch (Exception)
			{
				await Debug.LogMessageAsync("Lavalink is not connected! Failing with embed error...", LogSeverity.Error);

				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to play `{song}`!")
					.WithDescription("Failure: lavalink dependency missing.\nPlease check the console logs for more details.")
					.SendEmbed(Context.Channel);

				return;
			}

			await SendLyricsFromTrack(searchResult.Tracks.FirstOrDefault());
		}

		public async Task SendLyricsFromTrack (LavaTrack track)
		{
			var lyrics = await track.FetchLyricsFromGeniusAsync();

			if (string.IsNullOrWhiteSpace(lyrics))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to find song!")
					.WithDescription($"No lyrics found for {track.Title}.")
					.SendEmbed(Context.Channel);

				return;
			}

			await BuildEmbed(EmojiEnum.Unknown)
				.WithTitle($"🎶 {track.Title} Lyrics")
				.WithDescription(lyrics.Length > 1700 ? lyrics.Substring(0, 1700) : lyrics)
				.SendEmbed(Context.Channel);
		}

	}
}
