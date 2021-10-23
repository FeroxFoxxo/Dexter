using Dexter.Abstractions;
using Dexter.Extensions;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;

namespace Dexter.Events
{
	public class Logging : Event
	{

		private readonly DiscordShardedClient client;

		private readonly CommandService commandService;

		private readonly ILogger<Logging> logger;

		public Logging(DiscordShardedClient client, ILogger<Logging> logger, CommandService commandService)
		{
			this.client = client;
			this.logger = logger;
			this.commandService = commandService;
		}

		public override void InitializeEvents()
		{
			commandService.Log += async (logMessage) =>
			{
				if (logMessage.Message is not null)
					logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, logMessage.Message);
				else
					logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, "Internal Exception");
			};

			client.Log += async (logMessage) =>
			{
				if (logMessage.Message is not null)
				{
					if (!logMessage.Message.Contains("unknown dispatch", StringComparison.OrdinalIgnoreCase))
						logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, logMessage.Message);
				}
				else
					logger.Log(logMessage.Severity.ToLogLevel(), logMessage.Exception, "Internal Exception");
			};

			client.ShardConnected += async (client) =>
				logger.LogDebug($"Shard {client.ShardId} connected");

			client.ShardDisconnected += async (ex, client) =>
				logger.LogError(ex, $"Shard {client.ShardId} disconnected");

			client.ShardReady += async (client) =>
				logger.LogInformation($"Shard {client.ShardId} ready. Guilds: {client.Guilds.Count:N0}");

			client.ShardLatencyUpdated += async (oldLatency, newLatency, client) =>
				logger.LogTrace($"Shard {client.ShardId}'s latency has updated. [Old: {oldLatency}ms | New: {newLatency}ms]");

			client.ChannelCreated += async (channel) =>
				logger.LogDebug($"Channel Created [Type: {channel.GetType()} | Name: #{(channel as SocketGuildChannel)?.Name} | ID: {channel.Id} | Guild: {(channel as SocketGuildChannel)?.Guild}]");

			client.ChannelDestroyed += async (channel) =>
				logger.LogDebug($"Channel Deleted [Name: #{(channel as SocketGuildChannel)?.Name} | ID: {channel.Id} | Guild: {(channel as SocketGuildChannel)?.Guild}]");

			client.ChannelUpdated += async (channel, channel2) =>
				logger.LogTrace($"Channel Updated [Name: #{channel} | New Name: #{channel2} | ID: {channel.Id} | Guild: {(channel as SocketGuildChannel)?.Guild}]");

			client.JoinedGuild += async (guild) =>
				logger.LogInformation($"Joined Guild [Name: {guild.Name} | ID: {guild.Id}]");

			client.LeftGuild += async (guild) =>
				logger.LogInformation($"Left Guild [Name: {guild.Name} | ID: {guild.Id}]");

			client.MessageDeleted += async (cache, _) =>
			{
				if (cache.Value is not null)
					logger.LogTrace($"Message Deleted [Author: {cache.Value.Author} | ID: {cache.Id}]");
			};

			client.MessageUpdated += async (cache, msg, _) =>
			{
				if (cache.Value is not null)
					if (cache.Value.Embeds == null && cache.Value.Content != msg.Content)
						logger.LogTrace($"Message Updated [Author: {cache.Value.Author} | ID: {cache.Id}]");
			};

			client.MessageReceived += async (msg) =>
				logger.LogTrace($"Message Received [Author: {msg.Author} | ID: {msg.Id} | Bot: {msg.Author.IsBot}]");

			client.RoleCreated += async (role) =>
				logger.LogDebug($"Role Created [Name: {role.Name} | Role ID: {role.Id} | Guild: {role.Guild}]");

			client.RoleDeleted += async (role) =>
				logger.LogDebug($"Role Deleted [Name: {role.Name} | Role ID: {role.Id} | Guild: {role.Guild}]");

			client.RoleUpdated += async (role, role2) =>
				logger.LogTrace($"Role Updated [Name: {role.Name} | New Name: {role2.Name} | ID: {role.Id} | Guild: {role.Guild}]");

			client.UserBanned += async (user, guild) =>
				logger.LogDebug($"User Banned [User: {user} | User ID: {user.Id} | Guild: {guild.Name}]");

			client.UserUnbanned += async (user, guild) =>
				logger.LogDebug($"User Un-Banned [User: {user} | User ID: {user.Id} | Guild: {guild.Name}]");

			client.UserJoined += async (user) =>
				logger.LogDebug($"User Joined Guild [User: {user} | User ID: {user.Id} | Guild: {user.Guild}]");

			client.UserVoiceStateUpdated += async (user, _, _) =>
				logger.LogTrace($"User Voice State Updated: [User: {user}]");
		}
	}
}
