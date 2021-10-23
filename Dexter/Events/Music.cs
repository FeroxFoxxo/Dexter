using Dexter.Abstractions;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

namespace Dexter.Events
{
	public class Music : Event
	{
		private readonly DiscordShardedClient Client;

		public readonly LavaNode LavaNode;

		private readonly ILogger<Music> Logger;
		private readonly ConcurrentDictionary<ulong, CancellationTokenSource> DisconnectTokens;
		private readonly InteractiveService Interactive;

		private readonly List<int> ShardsReady;

		public readonly Dictionary<ulong, LoopType> LoopedGuilds;

		public object LoopLocker;
		public object QueueLocker;

		public Music (DiscordShardedClient client, ILogger<Music> logger, InteractiveService interactive, ILogger<LavaNode> nodeLog)
		{
			Client = client;
			Logger = logger;
			Interactive = interactive;

			LavaNode = new LavaNode (client, new NodeConfiguration() { Port = 2333, SelfDeaf = true }, nodeLog);

			LoopLocker = new ();
			QueueLocker = new ();

			LoopedGuilds = new ();

			ShardsReady = new();
			DisconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
		}

		public override void InitializeEvents()
		{
			Client.ShardReady += ClientOnShardReady;
			Client.JoinedGuild += DisposeMusicPlayerAsync;
			Client.LeftGuild += DisposeMusicPlayerAsync;
			Client.UserVoiceStateUpdated += ProtectPlayerIntegrityOnDisconnectAsync;

			LavaNode.OnTrackStart += OnTrackStarted;
			LavaNode.OnTrackEnd += OnTrackEnded;
		}

		public void AddReadyShard(int shardId)
		{
			if (!ShardsReady.Contains(shardId))
			{
				ShardsReady.Add(shardId);
			}
		}

		private async Task ClientOnShardReady(DiscordSocketClient client)
		{
			AddReadyShard(client.ShardId);

			if (ShardsReady.Count == Client.Shards.Count)
			{
				if (!LavaNode.IsConnected)
				{
					if (Environment.OSVersion.Platform == PlatformID.Win32NT)
					{
						Process[] pname = Process.GetProcessesByName("javaw");

						if (pname.Length == 0)
						{
							Logger.LogError("Lavalink is not set up! Make sure you run the executable in the Lavalink directory, and are running Java 10+.");
							return;
						}
					}

					await LavaNode.ConnectAsync();
				}

				Logger.LogInformation($"Lava Node connected (shard {client.ShardId:N0})");
			}
		}

		private async Task OnTrackStarted(TrackStartEventArg<LavaPlayer> trackEvent)
		{
			Logger.LogInformation($"Track started for guild {trackEvent.Player.VoiceChannel.Guild.Id}:\n\t" +
								   $"[Name: {trackEvent.Track.Title} | Duration: {trackEvent.Track.Duration.HumanizeTimeSpan()}]");

			lock (LoopLocker)
				if (!LoopedGuilds.ContainsKey(trackEvent.Player.VoiceChannel.Guild.Id))
					LoopedGuilds.Add(trackEvent.Player.VoiceChannel.Guild.Id, LoopType.Off);

			if (!DisconnectTokens.TryGetValue(trackEvent.Player.VoiceChannel.Id, out var value))
				return;

			if (value.IsCancellationRequested)
				return;

			value.Cancel(true);
		}

		private async Task OnTrackEnded(TrackEndEventArg<LavaPlayer> trackEvent)
		{
			Logger.LogInformation($"Track ended for guild {trackEvent.Player.VoiceChannel.Guild.Id} " +
								   $"-> {trackEvent.Player.Vueue.Count:N0} tracks remaining.");

			if (trackEvent.Reason == TrackEndReason.LoadFailed)
			{
				Logger.LogError($"Load failed for track in guild: {trackEvent.Player.VoiceChannel.Guild.Id}\n\t" +
								 $"Track info: [Name: {trackEvent.Track.Title} | Duration: {trackEvent.Track.Duration.HumanizeTimeSpan()} | " +
								 $"Url: {trackEvent.Track.Url} | Livestream?: {trackEvent.Track.IsStream}]");

				return;
			}

			if (trackEvent.Reason != TrackEndReason.Stopped && trackEvent.Reason != TrackEndReason.Finished && trackEvent.Reason != TrackEndReason.LoadFailed)
				return;

			var player = trackEvent.Player;

			if (player == null)
				return;

			bool canDequeue;

			LavaTrack queueable;

			LoopType loopType = LoopType.Off;

			lock (LoopLocker)
				if (LoopedGuilds.ContainsKey(trackEvent.Player.VoiceChannel.Guild.Id))
					loopType = LoopedGuilds[trackEvent.Player.VoiceChannel.Guild.Id];

			if (loopType == LoopType.All)
				lock (QueueLocker)
					player.Vueue.Enqueue(player.Track);

			if (loopType != LoopType.Single)
			{
				while (true)
				{
					canDequeue = player.Vueue.TryDequeue(out queueable);

					if (queueable != null || !canDequeue)
						break;
				}
			}
			else
			{
				canDequeue = true;
				queueable = player.Track;
			}

			if (!canDequeue)
			{
				if (!DisconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
				{
					value = new CancellationTokenSource();
					DisconnectTokens.TryAdd(player.VoiceChannel.Id, value);
				}
				else if (value.IsCancellationRequested)
				{
					DisconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
					value = DisconnectTokens[player.VoiceChannel.Id];
				}

				await Task.Delay(TimeSpan.FromSeconds(15), value.Token);

				if (value.IsCancellationRequested)
					return;

				if (player.PlayerState == PlayerState.Playing)
					return;

				var dcEmbed = new EmbedBuilder()
					.WithColor(Color.Gold)
					.WithDescription("🎵 No more songs in queue, disconnecting!");

				await LavaNode.LeaveAsync(player.VoiceChannel);

				lock (LoopLocker)
					if (LoopedGuilds.ContainsKey(trackEvent.Player.VoiceChannel.Guild.Id))
						LoopedGuilds.Remove(trackEvent.Player.VoiceChannel.Guild.Id);

				await Interactive.DelayedSendMessageAndDeleteAsync
					(player.TextChannel, deleteDelay: TimeSpan.FromSeconds(10), embed: dcEmbed.Build());

				return;
			}

			await trackEvent.Player.PlayAsync(queueable);

			if (queueable is null)
				return;

			await Interactive.DelayedSendMessageAndDeleteAsync(trackEvent.Player.TextChannel, null, TimeSpan.FromSeconds(10), embed: BuildEmbed(EmojiEnum.Unknown).GetNowPlaying(queueable).Build());
		}
		
		private async Task ProtectPlayerIntegrityOnDisconnectAsync(SocketUser user, SocketVoiceState ogState, SocketVoiceState newState)
		{
			if (!AllShardsReady(Client))
				return;

			if (!user.IsBot)
				if (ogState.VoiceChannel is not null)
					if (ogState.VoiceChannel.Users.Where(user => user.Id == Client.CurrentUser.Id).FirstOrDefault() is not null)
						if (ogState.VoiceChannel.Users.Count <= 1)
						{
							await LavaNode.LeaveAsync(ogState.VoiceChannel ?? newState.VoiceChannel);

							lock (LoopLocker)
								if (LoopedGuilds.ContainsKey(ogState.VoiceChannel.Guild.Id))
									LoopedGuilds.Remove(ogState.VoiceChannel.Guild.Id);

						}

			if (user.Id != Client.CurrentUser.Id || newState.VoiceChannel != null)
				return;

			try
			{
				await LavaNode.LeaveAsync(ogState.VoiceChannel ?? newState.VoiceChannel);

				lock (LoopLocker)
					if (LoopedGuilds.ContainsKey(ogState.VoiceChannel.Guild.Id))
						LoopedGuilds.Remove(ogState.VoiceChannel.Guild.Id);
			}
			catch (Exception) { }
		}

		private bool AllShardsReady(DiscordShardedClient client)
		{
			return client.Shards.Count == ShardsReady.Count;
		}

		private async Task DisposeMusicPlayerAsync(SocketGuild guild)
		{
			if (LavaNode.TryGetPlayer(guild, out var player))
			{
				try
				{
					await LavaNode.LeaveAsync(player.VoiceChannel);
					await player.DisposeAsync();
					Logger.LogInformation($"Guild {guild.Id} had an active music player. " + "It has been properly disposed of.");
				}
				catch (Exception) { }
			}
		}
	}
}
