using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dexter.Workers
{

    /// <summary>
    /// The CommandHandlerService deals with invoking the command and the errors that may occur as a result.
    /// It filters the command to see if the user is not a bot and that it has the prefix set in the
    /// bot configuration. It also catches all errors that may crop up in a command, logs it, and then sends
    /// an appropriate error to the channel, pinging the developers if the error is unknown.
    /// </summary>

    public class DiscordWorker(DiscordShardedClient client, IServiceProvider services, CommandService cmdService, ILogger<DiscordWorker> logger)
    {

		private readonly DiscordShardedClient client = client;
		private readonly IServiceProvider services = services;
		private readonly CommandService cmdService = cmdService;
		private readonly ILogger<DiscordWorker> logger = logger;

        public async Task StartAsync()
		{
			using (var moduleScope = services.CreateScope())
			{
				await cmdService.AddModulesAsync(Assembly.GetExecutingAssembly(), moduleScope.ServiceProvider);
			}

			await client.LoginAsync(TokenType.Bot, Startup.Token);

			await client.StartAsync();
		}

		public async Task StopAsync()
		{
			await client.StopAsync();

			try
			{
				client.Dispose();
			}
			catch (Exception ex)
			{
				logger.Log(LogLevel.Error, ex, "Failed to dispose discord.");
			}
		}

	}

}
