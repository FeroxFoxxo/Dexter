using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter
{
	public class ShardHost : IHostedService
	{
		private readonly DiscordShardedClient Client;
		private readonly ILogger<ShardHost> Logger;

		public ShardHost(DiscordShardedClient client, ILogger<ShardHost> logger)
		{
			Client = client;
			Logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await Client.LoginAsync(TokenType.Bot, Startup.Token);

			await Client.StartAsync();
		}

        public async Task StopAsync(CancellationToken cancellationToken)
        {
			await Client.StopAsync();

			try
			{
				Client.Dispose();
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error, ex, "Failed to dispose discord");
			}
		}
    }
}
