using Dexter.Abstractions;
using Dexter.Extensions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;

namespace DexterSlash.Services
{
    public class LoggingService : Service
    {

		private readonly DiscordShardedClient Client;
		private readonly ILogger<LoggingService> Logger;

		public LoggingService(DiscordShardedClient client, ILogger<LoggingService> logger)
        {
			Client = client;
			Logger = logger;
        }

		public override void Initialize()
		{
			Client.Log += async (logMessage) =>
			{
				if (logMessage.Message is not null)
				{
					if (!logMessage.Message.Contains("unknown dispatch", StringComparison.OrdinalIgnoreCase))
						Logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, logMessage.Message);
				}
				else
					Logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, "Internal Exception");
			};

			Client.ShardConnected += async (client) =>
				Logger.LogDebug($"Shard {client.ShardId} connected");

			Client.ShardDisconnected += async (ex, client) =>
				Logger.LogError(ex, $"Shard {client.ShardId} disconnected");

			Client.ShardReady += async (client) =>
				Logger.LogInformation($"Shard {client.ShardId} ready. Guilds: {client.Guilds.Count:N0}");

			Client.ShardLatencyUpdated += async (oldLatency, newLatency, client) =>
				Logger.LogTrace($"Shard {client.ShardId}'s latency has updated. [Old: {oldLatency}ms | New: {newLatency}ms]");

			Client.ChannelCreated += async (channel) =>
				Logger.LogDebug($"Channel Created [Type: {channel.GetType()} | Name: #{(channel as SocketGuildChannel)?.Name} | ID: {channel.Id} | Guild: {(channel as SocketGuildChannel)?.Guild}]");

			Client.ChannelDestroyed += async (channel) =>
				Logger.LogDebug($"Channel Deleted [Name: #{(channel as SocketGuildChannel)?.Name} | ID: {channel.Id} | Guild: {(channel as SocketGuildChannel)?.Guild}]");

			Client.ChannelUpdated += async (channel, channel2) =>
				Logger.LogTrace($"Channel Updated [Name: #{channel} | New Name: #{channel2} | ID: {channel.Id} | Guild: {(channel as SocketGuildChannel)?.Guild}]");

			Client.JoinedGuild += async (guild) =>
				Logger.LogInformation($"Joined Guild [Name: {guild.Name} | ID: {guild.Id}]");

			Client.LeftGuild += async (guild) =>
				Logger.LogInformation($"Left Guild [Name: {guild.Name} | ID: {guild.Id}]");

			Client.MessageDeleted += async (cache, _) =>
			{
				if (cache.Value is not null)
					Logger.LogTrace($"Message Deleted [Author: {cache.Value.Author} | ID: {cache.Id}]");
			};

			Client.MessageUpdated += async (cache, msg, _) =>
			{
				if (cache.Value is not null)
					if (cache.Value.Embeds == null && cache.Value.Content != msg.Content)
						Logger.LogTrace($"Message Updated [Author: {cache.Value.Author} | ID: {cache.Id}]");
			};

			Client.MessageReceived += async (msg) =>
				Logger.LogTrace($"Message Received [Author: {msg.Author} | ID: {msg.Id} | Bot: {msg.Author.IsBot}]");

			Client.RoleCreated += async (role) =>
				Logger.LogDebug($"Role Created [Name: {role.Name} | Role ID: {role.Id} | Guild: {role.Guild}]");

			Client.RoleDeleted += async (role) =>
				Logger.LogDebug($"Role Deleted [Name: {role.Name} | Role ID: {role.Id} | Guild: {role.Guild}]");

			Client.RoleUpdated += async (role, role2) =>
				Logger.LogTrace($"Role Updated [Name: {role.Name} | New Name: {role2.Name} | ID: {role.Id} | Guild: {role.Guild}]");

			Client.UserBanned += async (user, guild) =>
				Logger.LogDebug($"User Banned [User: {user} | User ID: {user.Id} | Guild: {guild.Name}]");

			Client.UserUnbanned += async (user, guild) =>
				Logger.LogDebug($"User Un-Banned [User: {user} | User ID: {user.Id} | Guild: {guild.Name}]");

			Client.UserJoined += async (user) =>
				Logger.LogDebug($"User Joined Guild [User: {user} | User ID: {user.Id} | Guild: {user.Guild}]");

			Client.UserVoiceStateUpdated += async (user, _, _) =>
				Logger.LogTrace($"User Voice State Updated: [User: {user}]");
		}
    }
}
