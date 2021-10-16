using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Victoria;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Dexter.Commands
{
    public partial class MusicCommands
	{
		
		[Command("lyrics")]
		[Summary("Replies with the lyrics to the song provided.")]
		[MusicBotChannel]

		public async Task LyricsCommand([Remainder] string song)
		{
			await SendLyricsFromTrack(song);
		}

		[Command("lyrics")]
		[Summary("Replies with the lyrics to the current track that is playing.")]
		[MusicBotChannel]

		public async Task LyricsCommand()
		{
			if (!await MusicService.LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to find lyrics!")
					.WithDescription("Failed to join voice channel. Are you in a voice channel?")
					.SendEmbed(Context.Channel);

				return;
			}

			if (MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
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

				await SendLyricsFromTrack(player.Track.Title);
			}
		}

		public async Task SendLyricsFromTrack(string song)
		{
			SearchResponse searchResult;

			try
			{
				searchResult = await MusicService.LavaNode.SearchAsync(SearchType.YouTube, song);
			}
			catch (Exception)
			{
				Logger.LogError("Lavalink is not connected! Failing with embed error...");

				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to find lyrics for {song}!")
					.WithDescription("Failure: lavalink dependency missing.\nPlease check the console logs for more details.")
					.SendEmbed(Context.Channel);

				return;
			}

			foreach (var track in searchResult.Tracks)
			{
				if (track is null)
				{
					continue;
				}

				try
				{
					var lyrics = await track.FetchLyricsFromGeniusAsync();

					if (!string.IsNullOrWhiteSpace(lyrics))
					{
						await SendLyricsEmbed(lyrics, "GENIUS", track.Title);
						return;
					}

				}
				catch (Exception) { }

				try
				{
					var lyrics = await track.FetchLyricsFromOvhAsync();

					if (!string.IsNullOrWhiteSpace(lyrics))
					{
						await SendLyricsEmbed(lyrics, "OHV", track.Title);
						return;
					}

				}
				catch (Exception) { }
			}

			await BuildEmbed(EmojiEnum.Annoyed)
				.WithTitle("Unable to find song!")
				.WithDescription($"No lyrics found for:\n**{song}**.")
				.SendEmbed(Context.Channel);
		}

		private async Task SendLyricsEmbed (string fullLyrics, string name, string trackTitle)
		{
			List<EmbedBuilder> embeds = new();

			var lyricsList = fullLyrics.Split('[');

			foreach (var lyrics in lyricsList)
				if (lyrics.Length > 0)
					embeds.Add(BuildEmbed(EmojiEnum.Unknown)
						.WithTitle($"🎶 {trackTitle} - {name} Lyrics")
						.WithDescription($"{(lyricsList.Length == 1 ? "" : "[")}" +
							$"{(lyrics.Length > 1700 ? lyrics.Substring(0, 1700) : lyrics)}"));

			await CreateReactionMenu(embeds.ToArray(), Context.Channel);
		}

	}
}
