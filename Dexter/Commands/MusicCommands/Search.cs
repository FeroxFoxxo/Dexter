using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Victoria.Player;
using Victoria.Responses.Search;
using SearchResponse = Victoria.Responses.Search.SearchResponse;

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
						.WithTitle($"Unable to search!")
						.WithDescription("Failed to join voice channel.\nAre you in a voice channel?")
						.SendEmbed(Context.Channel);

				return;
			}

			try
			{
				await LavaNode.SearchAsync(SearchType.YouTube, search);
			}
			catch (Exception)
			{
				Logger.LogError("Lavalink is not connected!\nFailing with embed error...");

				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to search!")
					.WithDescription("Failure: lavalink dependency missing.\nPlease check the console logs for more details.")
					.SendEmbed(Context.Channel);

				return;
			}

			if (LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (Uri.TryCreate(search, UriKind.Absolute, out Uri uriResult))
				{
					string baseUrl = uriResult.Host;
					string abUrl = uriResult.AbsoluteUri;

					if (baseUrl.Contains("music.youtube"))
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle($"Unable to search YT Music!")
							.WithDescription("This music type is not implemented. Please contact a developer!")
							.SendEmbed(Context.Channel);
					}
					else if (baseUrl.Contains("youtube") || baseUrl.Contains("youtu.be"))
					{
						if (abUrl.Contains("list"))
						{
							var query = HttpUtility.ParseQueryString(uriResult.Query);

							var searchRequest = YouTubeService.PlaylistItems.List("snippet");

							searchRequest.PlaylistId = query["list"];
							searchRequest.MaxResults = 25;

							var searchResponse = await searchRequest.ExecuteAsync();

							List<string> songs = new();

							if (query["v"] is not null)
							{

								var searchRequestV = YouTubeService.Videos.List("snippet");
								searchRequestV.Id = query["v"];
								var searchResponseV = await searchRequestV.ExecuteAsync();

								var youTubeVideo = searchResponseV.Items.FirstOrDefault();

								songs.Add($"{youTubeVideo.Snippet.ChannelTitle} {youTubeVideo.Snippet.Title}");
							}

							foreach (var item in searchResponse.Items)
								songs.Add(item.Snippet.Title);
							
							await SearchPlaylist(songs.ToArray(), player);
						}
						else
						{
							var query = HttpUtility.ParseQueryString(uriResult.Query);

							var searchRequest = YouTubeService.Videos.List("snippet");
							searchRequest.Id = query["v"];
							var searchResponse = await searchRequest.ExecuteAsync();

							var youTubeVideo = searchResponse.Items.FirstOrDefault();

							await SearchSingleTrack($"{youTubeVideo.Snippet.ChannelTitle} {youTubeVideo.Snippet.Title}", player, SearchType.YouTube);
						}
					}
					else if (baseUrl.Contains("soundcloud"))
					{
						await SearchSingleTrack($"{abUrl.Split('/').TakeLast(2).First()} {abUrl.Split('/').Last()}".Replace('-', ' '), player, SearchType.YouTube);
					}
					else if (baseUrl.Contains("spotify"))
					{
						string id = abUrl.Split('/').Last().Split('?').First();

						if (abUrl.Contains("playlist"))
						{
							var playlist = await SpotifyAPI.Playlists.GetItems(id);

							List<string> songs = new();

							foreach (var item in playlist.Items)
							{
								if (item.Track is FullTrack track)
								{
									songs.Add($"{track.Artists.First().Name} {track.Name}");
								}
                            }

							if (songs.Any())
							{
								await SearchPlaylist(songs.ToArray(), player);
							}
							else
								await BuildEmbed(EmojiEnum.Annoyed)
									.WithTitle($"Unable to search spotify!")
									.WithDescription("None of these tracks could be resolved. Please contact a developer!")
									.SendEmbed(Context.Channel);
						}
						else if (abUrl.Contains("track"))
						{
							var track = await SpotifyAPI.Tracks.Get(id);

							await SearchSingleTrack($"{track.Artists.First().Name} {track.Name}", player, SearchType.YouTube);
						}
						else
						{
							await BuildEmbed(EmojiEnum.Annoyed)
								.WithTitle($"Unable to search spotify!")
								.WithDescription("This music type is not implemented. Please contact a developer!")
								.SendEmbed(Context.Channel);
						}
					}
				}
				else
				{
					SearchResponse searchResult;

					searchResult = await LavaNode.SearchAsync(SearchType.YouTube, search);

					var track = searchResult.Tracks.FirstOrDefault();

					if (track == null)
					{
						await BuildEmbed(EmojiEnum.Annoyed)
							.WithTitle($"Unable to search!")
							.WithDescription($"The requested search: **{search}**, returned no results.")
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

					await SearchSingleTrack(result.Value.Key, player, SearchType.YouTube);

					await result.Message.DeleteAsync();
				}
			}
		}

		public async Task SearchPlaylist(string[] playlist, LavaPlayer player)
		{
			bool wasEmpty = player.Vueue.Count == 0 && player.PlayerState != PlayerState.Playing;

			List<LavaTrack> tracks = new ();

			foreach (string search in playlist)
			{
				SearchResponse searchResult = await LavaNode.SearchAsync(SearchType.YouTube, search);

				var track = searchResult.Tracks.FirstOrDefault();

				if (track is not null)
                {
					tracks.Add(track);
					if (player.Vueue.Count == 0 && player.PlayerState != PlayerState.Playing)
					{
						await player.PlayAsync(track);
					}
					else
					{
						lock (AudioService.Locker)
						{
							player.Vueue.Enqueue(track);
						}
					}
				}
			}

			EmbedBuilder[] embeds;

			if (wasEmpty)
				embeds = player.GetQueue("🎶 Playlist Music Queue", BotConfiguration);
			else
				embeds = tracks.ToArray().GetQueueFromTrackArray("🎶 Playlist Music Queue", BotConfiguration);

			CreateReactionMenu(embeds, Context.Channel);
		}

		public async Task SearchSingleTrack(string search, LavaPlayer player, SearchType type)
		{
			SearchResponse searchResult = await LavaNode.SearchAsync(type, search);

			var track = searchResult.Tracks.FirstOrDefault();

			if (track == null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to search for {search}!")
					.WithDescription("The requested search returned no results.")
					.SendEmbed(Context.Channel);

				return;
			}

			if (player.Vueue.Count == 0 && player.PlayerState != PlayerState.Playing)
			{
				await player.PlayAsync(track);
				await BuildEmbed(EmojiEnum.Unknown).GetNowPlaying(track).SendEmbed(Context.Channel);
			}
			else
			{
				lock (AudioService.Locker)
				{
					player.Vueue.Enqueue(track);
				}

				await BuildEmbed(EmojiEnum.Unknown).GetQueuedTrack(track, player.Vueue.Count).SendEmbed(Context.Channel);
			}
		}
	}
}
