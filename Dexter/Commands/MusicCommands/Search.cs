using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
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
		[Alias("search", "p")]
		[Summary("Searches for the desired song. Returns top 5 most popular results. Click on one of the reaction icons to play the appropriate track.")]
		[MusicBotChannel]

		public async Task SearchCommand([Remainder] string search)
		{
			if (!await MusicService.LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle($"Unable to search!")
						.WithDescription("Failed to join voice channel.\nAre you in a voice channel?")
						.SendEmbed(Context.Channel);

				return;
			}

			try
			{
				await MusicService.LavaNode.SearchAsync(SearchType.YouTube, search);
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

			if (MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
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
						using YouTubeService youtube = new(new BaseClientService.Initializer()
						{
							HttpClientInitializer = MusicService.ServiceProvider.GetService<UserCredential>(),
							ApplicationName = Context.Client.CurrentUser.Username,
						});

						if (abUrl.Contains("list"))
						{
							var query = HttpUtility.ParseQueryString(uriResult.Query);

							var searchRequest = youtube.PlaylistItems.List("snippet");

							searchRequest.PlaylistId = query["list"];
							searchRequest.MaxResults = 25;

							var searchResponse = await searchRequest.ExecuteAsync();

							List<string> songs = new();

							if (query["v"] is not null)
							{

								var searchRequestV = youtube.Videos.List("snippet");
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

							var searchRequest = youtube.Videos.List("snippet");
							searchRequest.Id = query["v"];
							var searchResponse = await searchRequest.ExecuteAsync();

							var youTubeVideo = searchResponse.Items.FirstOrDefault();

							await SearchSingleTrack($"{youTubeVideo.Snippet.ChannelTitle} {youTubeVideo.Snippet.Title}", player, false);
						}
					}
					else if (baseUrl.Contains("soundcloud"))
					{
						await SearchSingleTrack($"{abUrl.Split('/').TakeLast(2).First()} {abUrl.Split('/').Last()}".Replace('-', ' '), player, false);
					}
					else if (baseUrl.Contains("spotify"))
					{
						var config = SpotifyClientConfig.CreateDefault();

						var response = await new OAuthClient(config).RequestToken(ClientCredentialsRequest);

						var spotifyAPI = new SpotifyClient(config.WithToken(response.AccessToken));

						string id = abUrl.Split('/').Last().Split('?').First();

						if (abUrl.Contains("playlist"))
						{
							var playlist = await spotifyAPI.Playlists.GetItems(id);

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
							var track = await spotifyAPI.Tracks.Get(id);

							await SearchSingleTrack($"{track.Artists.First().Name} {track.Name}", player, false);
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
					await SearchSingleTrack(search, player, true);
				}
			}
		}

		public async Task SearchPlaylist(string[] playlist, LavaPlayer player)
		{
			bool wasEmpty = player.Vueue.Count == 0 && player.PlayerState != PlayerState.Playing;

			List<LavaTrack> tracks = new ();

			foreach (string search in playlist)
			{
				SearchResponse searchResult = await MusicService.LavaNode.SearchAsync(SearchType.YouTube, search);

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
						lock (MusicService.QueueLocker)
						{
							player.Vueue.Enqueue(track);
						}
					}
				}
			}

			EmbedBuilder[] embeds;

			if (wasEmpty)
				embeds = player.GetQueue("🎶 Playlist Music Queue", BotConfiguration, MusicService);
			else
				embeds = tracks.ToArray().GetQueueFromTrackArray("🎶 Playlist Music Queue", BotConfiguration);

			await CreateReactionMenu(embeds, Context.Channel);
		}

		public async Task SearchSingleTrack(string search, LavaPlayer player, bool searchList)
		{
			SearchResponse searchResult;

			searchResult = await MusicService.LavaNode.SearchAsync(SearchType.YouTube, search);

			var track = searchResult.Tracks.FirstOrDefault();

			if (track == null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to search!")
					.WithDescription($"The requested search: **{search}**, returned no results.")
					.SendEmbed(Context.Channel);

				return;
			}

			if (searchList)
			{
				var topResults = searchResult.Tracks.Count <= 5 ? searchResult.Tracks.ToList() : searchResult.Tracks.Take(5).ToList();

				string line1 = topResults.Count <= 5
					? $"I found {topResults.Count} tracks matching your search."
					: $"I found {searchResult.Tracks.Count:N0} tracks matching your search, here are the top 5.";

				var embedFields = new List<EmbedFieldBuilder>();

				var options = new List<string>();

				for (int i = 0; i < topResults.Count; i++)
				{
					if (options.Contains(topResults[i].Title))
						continue;

					options.Add(topResults[i].Title);

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
					new SelectionBuilder<string>()
						.WithSelectionPage(PageBuilder.FromEmbed(embed))
						.WithOptions(options)
						.WithInputType(InputType.SelectMenus)
						.WithDeletion(DeletionOptions.Invalid)
						.Build()
					, Context.Channel, TimeSpan.FromMinutes(2));

				await result.Message.DeleteAsync();

				if (!result.IsSuccess)
					return;

				track = searchResult.Tracks.Where(search => result.Value.EndsWith(search.Title)).FirstOrDefault();
			}

			if (player.Vueue.Count == 0 && player.PlayerState != PlayerState.Playing)
			{
				await player.PlayAsync(track);
				await BuildEmbed(EmojiEnum.Unknown).GetNowPlaying(track).SendEmbed(Context.Channel);
			}
			else
			{
				lock (MusicService.QueueLocker)
				{
					player.Vueue.Enqueue(track);
				}

				await BuildEmbed(EmojiEnum.Unknown).GetQueuedTrack(track, player.Vueue.Count).SendEmbed(Context.Channel);
			}
		}

		[Command("pause")]
		[Alias("resume", "play", "unpause", "p")]
		[Summary("Toggles whether this player is currently paused. Use while songs are playing to pause the player, use while a player is paused to resume it.")]
		[MusicBotChannel]

		public async Task PauseCommand()
		{
			if (!await MusicService.LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to pause the player!")
					.WithDescription("Failed to join voice channel.\nAre you in a voice channel?").SendEmbed(Context.Channel);

				return;
			}

			if (MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (player.PlayerState == PlayerState.Paused)
				{
					await player.ResumeAsync();
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Resumed the player.")
						.WithDescription($"Successfully resumed {player.Track.Title}")
						.SendEmbed(Context.Channel);
				}
				else if (player.PlayerState == PlayerState.Playing)
				{
					await player.PauseAsync();
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Paused the player.")
						.WithDescription($"Successfully paused {player.Track.Title}")
						.SendEmbed(Context.Channel);
				}
				else if (player.PlayerState == PlayerState.Stopped)
				{
					var track = player.Vueue.FirstOrDefault();

					if (track is not null)
					{
						await player.PlayAsync(track);

						await player.SkipAsync();
					}

					if (player.Track is not null && track is not null)
						await BuildEmbed(EmojiEnum.Love)
							.WithTitle("Resumed the player.")
							.WithDescription($"Successfully resumed {player.Track.Title}")
							.SendEmbed(Context.Channel);
					else
						await BuildEmbed(EmojiEnum.Love)
							.WithTitle("Could not resume the player.")
							.WithDescription($"No tracks currently in queue!")
							.SendEmbed(Context.Channel);
				}
				else
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to pause the player!")
						.WithDescription("The player must be either in a playing or paused state to use this command.\n" +
												   $"Current state is **{player.PlayerState.Humanize()}**.")
						.SendEmbed(Context.Channel);
				}
			}
		}
	}
}
