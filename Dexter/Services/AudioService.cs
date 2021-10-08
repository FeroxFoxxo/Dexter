using Dexter.Abstractions;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

namespace Dexter.Services
{
    public class AudioService : Service
	{
		private readonly DiscordShardedClient Client;
		private readonly LavaNode LavaNode;
		private readonly ILogger<AudioService> Logger;
		private readonly ConcurrentDictionary<ulong, CancellationTokenSource> DisconnectTokens;
		private readonly InteractiveService Interactive;

		private readonly List<int> ShardsReady;

		public object Locker;

		public AudioService (DiscordShardedClient client, LavaNode lavaNode, ILogger<AudioService> logger, InteractiveService interactive)
        {
			Client = client;
			LavaNode = lavaNode;
			Logger = logger;
			Interactive = interactive;

			Locker = new object();
			ShardsReady = new();
			DisconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
		}

		public override void Initialize()
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
				if (!LavaNode.IsConnected) {
					await LavaNode.ConnectAsync();
				}

				Logger.LogInformation($"Lava Node connected (shard {client.ShardId:N0})");
			}
		}

        private async Task OnTrackStarted(TrackStartEventArg<LavaPlayer> trackEvent)
		{
			Logger.LogInformation($"Track started for guild {trackEvent.Player.VoiceChannel.Guild.Id}:\n\t" +
								   $"[Name: {trackEvent.Track.Title} | Duration: {trackEvent.Track.Duration.HumanizeTimeSpan()}]");

			if (!DisconnectTokens.TryGetValue(trackEvent.Player.VoiceChannel.Id, out var value))
				return;

			if (value.IsCancellationRequested)
				return;

			value.Cancel(true);
		}

		private async Task OnTrackEnded(TrackEndEventArg<LavaPlayer> trackEnd)
		{
			Logger.LogInformation($"Track ended for guild {trackEnd.Player.VoiceChannel.Guild.Id} " +
								   $"-> {trackEnd.Player.Vueue.Count:N0} tracks remaining.");

			if (trackEnd.Reason == TrackEndReason.LoadFailed)
			{
				Logger.LogError($"Load failed for track in guild: {trackEnd.Player.VoiceChannel.Guild.Id}\n\t" +
								 $"Track info: [Name: {trackEnd.Track.Title} | Duration: {trackEnd.Track.Duration.HumanizeTimeSpan()} | " +
								 $"Url: {trackEnd.Track.Url} | Livestream?: {trackEnd.Track.IsStream}]");

				return;
			}

			if (trackEnd.Reason != TrackEndReason.Stopped && trackEnd.Reason != TrackEndReason.Finished && trackEnd.Reason != TrackEndReason.LoadFailed)
				return;

			var player = trackEnd.Player;

			if (player == null)
				return;

			bool canDequeue;

			LavaTrack queueable;

			while (true)
			{
				canDequeue = player.Vueue.TryDequeue(out queueable);

				if (queueable != null || !canDequeue)
					break;
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

				await Interactive.DelayedSendMessageAndDeleteAsync
					(player.TextChannel, deleteDelay: TimeSpan.FromSeconds(10), embed: dcEmbed.Build());

				return;
			}

			await trackEnd.Player.PlayAsync(queueable);

			if (queueable is null)
				return;

			await Interactive.DelayedSendMessageAndDeleteAsync(trackEnd.Player.TextChannel, null, TimeSpan.FromSeconds(10), embed: BuildEmbed(Dexter.Enums.EmojiEnum.Unknown).GetNowPlaying(queueable).Build());
		}
		
		private async Task ProtectPlayerIntegrityOnDisconnectAsync(SocketUser user, SocketVoiceState ogState, SocketVoiceState newState)
		{
			if (!AllShardsReady(Client))
				return;

			if (!user.IsBot)
				if (ogState.VoiceChannel is not null)
					if (ogState.VoiceChannel.Users.Where(user => user.Id == Client.CurrentUser.Id).FirstOrDefault() is not null)
						if (ogState.VoiceChannel.Users.Count <= 1)
							await LavaNode.LeaveAsync(ogState.VoiceChannel ?? newState.VoiceChannel);

			if (user.Id != Client.CurrentUser.Id || newState.VoiceChannel != null)
				return;

			try
			{
				await LavaNode.LeaveAsync(ogState.VoiceChannel ?? newState.VoiceChannel);
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
