using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Dexter.Commands
{
    public partial class MusicCommands
	{

		[Command("play")]
		[Alias("search")]
		[Summary("Searches for the desired song. Returns top 5 most popular results. Click on one of the reaction icons to play the appropriate track.")]
		[MusicBotChannel]

		public async Task SearchCommand([Remainder] string search)
		{
			if (!await LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle($"Unable to search for `{search}`!")
						.WithDescription("Failed to join voice channel.\nAre you in a voice channel?")
						.SendEmbed(Context.Channel);

				return;
			}

			if (LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if(Uri.TryCreate(search, UriKind.Absolute, out Uri uriResult))
                {
					string baseUrl = uriResult.GetLeftPart(UriPartial.Authority);

					Console.WriteLine(baseUrl);
				}

				await SearchSingleTrack (search, player);
			}
		}

		public async Task SearchSingleTrack(string search, LavaPlayer player)
		{
			SearchResponse searchResult;

			try
			{
				if (search.Contains("soundcloud") || (search.Contains("sound") && search.Contains("cloud")))
                {
                    searchResult = await LavaNode.SearchAsync(SearchType.SoundCloud, search);

					if (searchResult.Tracks.FirstOrDefault() is null)
						searchResult = await LavaNode.SearchAsync(SearchType.YouTube, search);
				}
				else
					searchResult = await LavaNode.SearchAsync(SearchType.YouTube, search);
			}
			catch (Exception)
			{
				Logger.LogError("Lavalink is not connected!\nFailing with embed error...");

				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to search for `{search}`!")
					.WithDescription("Failure: lavalink dependency missing.\nPlease check the console logs for more details.")
					.SendEmbed(Context.Channel);

				return;
			}

			var track = searchResult.Tracks.FirstOrDefault();

			if (track == null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to search for `{search}`!")
					.WithDescription("The requested search returned no results.")
					.SendEmbed(Context.Channel);

				return;
			}

			var topResults = searchResult.Tracks.Count <= 5 ? searchResult.Tracks.ToList() : searchResult.Tracks.Take(5).ToList();

			string line1 = topResults.Count <= 5
				? $"I found {topResults.Count} tracks matching your search."
				: $"I found {searchResult.Tracks.Count:N0} tracks matching your search, here are the top 5.";

			var embedFields = new List<EmbedFieldBuilder>();

			var options = new Dictionary<string, int>();

			for (int i = 0; i < topResults.Count; i++)
			{
				if (options.ContainsKey(topResults[i].Title))
					continue;

				options.Add(topResults[i].Title, i);

				embedFields.Add(new()
				{
					Name = $"#{i + 1}. {topResults[i].Title}",
					Value = $"Uploader: {topResults[i].Author}\n" + $"Duration: {topResults[i].Duration.HumanizeTimeSpan()}"
				});
			}

			var embed = BuildEmbed(EmojiEnum.Unknown)
				.WithTitle("Search Results:")
				.WithDescription($"{Context.User.Mention}, {line1}")
				.WithFields(embedFields)
				.Build();

			var result = await Interactive.SendSelectionAsync(
				new SelectionBuilder<KeyValuePair<string, int>>()
					.WithSelectionPage(PageBuilder.FromEmbed(embed))
					.WithOptions(options)
					.WithInputType(InputType.SelectMenus)
					.WithDeletion(DeletionOptions.Invalid)
					.Build()
				, Context.Channel, TimeSpan.FromMinutes(2));

			if (result.IsSuccess)
			{
				var newTrack = topResults.ElementAt(result.Value.Value);

				await result.Message.DeleteAsync();

				if (player.Vueue.Count == 0 && player.PlayerState != PlayerState.Playing)
				{
					await player.PlayAsync(newTrack);
					await BuildEmbed(EmojiEnum.Unknown).GetNowPlaying(newTrack).SendEmbed(Context.Channel);
				}
				else
				{
					lock (AudioService.Locker)
					{
						player.Vueue.Enqueue(newTrack);
					}

					await BuildEmbed(EmojiEnum.Unknown).GetQueuedTrack(newTrack, player.Vueue.Count).SendEmbed(Context.Channel);
				}
			}
		}
	}
}
