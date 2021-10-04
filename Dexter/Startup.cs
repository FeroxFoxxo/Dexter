using Dexter.Abstractions;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Figgle;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Victoria;

namespace Dexter
{

    /// <summary>
    /// The Startup class is the entrance of the program. It is where dependencies are injected into all of their respected classes and where the bot starts up.
    /// </summary>

    public static class Startup
	{

        private static string p_Token;

        public static string Token { get => p_Token; }

		/// <summary>
		/// The Main method is the entrance to the program. Arguments can be added to this method and supplied
		/// through the command line of the application when it starts. It is an asynchronous task.
		/// </summary>
		/// <param name="token">[OPTIONAL] The token of the bot. Defaults to the one specified in the BotCommands if not set.</param>
		/// <param name="version">[OPTIONAL] The version of the bot specified by the release pipeline.</param>
		/// <param name="directory">[OPTIONAL] The directory you wish the databases and configurations to be in. By default this is the build directory.</param>
		/// <param name="spotifyID">[OPTIONAL] Spotify CLIENT_ID from developer.spotify.com/dashboard.</param>
		/// <param name="spotifySecret">[OPTIONAL] Spotify CLIENT_SECRET from developer.spotify.com/dashboard.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public static async Task Main(string token, string version, string directory, string spotifyID, string spotifySecret)
		{
			p_Token = token;

			// Create new WebApplication which will generate our REST-FUL API.

			var builder = WebApplication.CreateBuilder();

			// Sets the current, active directory to the working directory specified in the azure cloud.

			if (!string.IsNullOrEmpty(directory))
				Directory.SetCurrentDirectory(directory);

			string databaseDirectory = Path.Join(Directory.GetCurrentDirectory(), "Databases");

			if (!Directory.Exists(databaseDirectory))
				Directory.CreateDirectory(databaseDirectory);

			// Get information on the bot through REST.

			// Draws "STARTING..." in the color of cyan.
			Console.ForegroundColor = ConsoleColor.Cyan;

			var botInfo = await GetNameAndShardsOfBot(token);

			var name = botInfo.Key;
			var shards = botInfo.Value;

			await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(name));

			Console.Title = name;

			// Start the swager instance for debugging.

			builder.Services.AddControllers();

			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc(version, new() { Title = name, Version = version });
			});

			// Init spotify API.

			if (!string.IsNullOrEmpty(spotifyID) && !string.IsNullOrEmpty(spotifySecret))
			{
				var config = SpotifyClientConfig.CreateDefault();

				var request = new ClientCredentialsRequest(spotifyID, spotifySecret);
				var response = await new OAuthClient(config).RequestToken(request);

				builder.Services.AddSingleton(new SpotifyClient(config.WithToken(response.AccessToken)));
			}
			else
				builder.Services.AddSingleton(new SpotifyClient("UNKNOWN"));

			// Initialize our dependencies for the bot.

			builder.Services.AddSingleton(
				new CommandService(
					new CommandServiceConfig()
                    {
						IgnoreExtraArgs = true,
						CaseSensitiveCommands = false,
						LogLevel = LogSeverity.Debug,
						DefaultRunMode = RunMode.Async
                    }
				)
			);

			builder.Services.AddSingleton(
				new DiscordShardedClient(
					new DiscordSocketConfig
					{
						AlwaysDownloadUsers = true,
						MessageCacheSize = 100,
						TotalShards = shards,
						LogLevel = LogSeverity.Debug,
						GatewayIntents = GatewayIntents.All
					}
				)
			);

			builder.Services.AddSingleton<Random>();

			builder.Services.AddSingleton(provider =>
			{
				var client = provider.GetRequiredService<DiscordShardedClient>();
				return new InteractiveService(client, TimeSpan.FromMinutes(5));
			});

			builder.Services.AddLavaNode(x => { x.SelfDeaf = true; x.Port = 2333; });

			bool HasErrored = false;

			// Finds all JSON configurations and initializes them from their respective files.
			// If a JSON file is not created, a new one is initialized in its place.

			GetJSONConfigs().ForEach(async Type =>
					{
						if (!File.Exists($"Configurations/{Type.Name}.json"))
						{
							File.WriteAllText(
								$"Configurations/{Type.Name}.json",
								JsonSerializer.Serialize(
									Activator.CreateInstance(Type),
									new JsonSerializerOptions() { WriteIndented = true }
								)
							);

							builder.Services.AddSingleton(Type);

							await Debug.LogMessageAsync(
								$" This application does not have a configuration file for {Type.Name}! " +
								$"A mock JSON class has been created in its place...",
								LogSeverity.Warning
							);
						}
						else
						{
							try
							{
								object JSON = JsonSerializer.Deserialize(
									File.ReadAllText($"Configurations/{Type.Name}.json"),
									Type,
									new JsonSerializerOptions() { WriteIndented = true }
								);

								builder.Services.AddSingleton(
									Type,
									JSON
								);
							}
							catch (JsonException Exception)
							{
								await Debug.LogMessageAsync(
									$" Unable to initialize {Type.Name}! Ran into: {Exception.InnerException}.",
									LogSeverity.Error
								);

								HasErrored = true;
							}
						}
					});

			if (HasErrored)
				return;

			GetDatabases().ForEach(t => builder.Services.AddSingleton(t));

			GetCommands().ForEach(t => builder.Services.AddSingleton(t));

			GetServices().ForEach(t => builder.Services.AddSingleton(t));

			// Add hosted events to the application, which will run until it is closed.

			builder.Services.AddHostedService<ShardHost>();

			// Build the website and start up swagger to allow for quick development of the API.

			var app = builder.Build();

			// Makes sure all entity databases exist and are created if they do not.
			GetDatabases().ForEach(
				DBType =>
				{
					Database EntityDatabase = (Database) app.Services.GetRequiredService(DBType);

					EntityDatabase.Database.EnsureCreated();
				}
			);

			// Adds all the commands', databases' and services' dependencies to their properties.
			Assembly.GetExecutingAssembly().GetTypes()
					.Where(Type => (Type.IsSubclassOf(typeof(DiscordModule)) || Type.IsSubclassOf(typeof(Service)) || Type.IsSubclassOf(typeof(Database))) && !Type.IsAbstract)
					.ToList().ForEach(
				Type => Type.GetProperties().ToList().ForEach(Property =>
				{
					if (Property.PropertyType == typeof(ServiceProvider))
						Property.SetValue(app.Services.GetRequiredService(Type), app.Services);
					else
					{
						object Service = app.Services.GetService(Property.PropertyType);

						if (Service != null)
						{
							Property.SetValue(app.Services.GetRequiredService(Type), Service);
						}
					}
				})
			);


			// Connects all the event hooks in initializable modules to their designated delegates.
			GetServices().ForEach(
				Type => (app.Services.GetService(Type) as Service).Initialize()
			);

			app.UseSwagger();

			app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{name} {version}"));

			app.UseHttpsRedirection();

			app.UseAuthorization();

			app.MapControllers();

			app.Run();
		}

		private static List<Type> GetCommands() { return GetClassesOfType(typeof(DiscordModule)); }

		private static List<Type> GetDatabases() { return GetClassesOfType(typeof(Database)); }

		private static List<Type> GetJSONConfigs() { return GetClassesOfType(typeof(JSONConfig)); }

		private static List<Type> GetServices() { return GetClassesOfType(typeof(Service)); }

		private static List<Type> GetClassesOfType(Type type)
		{
			return Assembly.GetExecutingAssembly().GetTypes()
				.Where(c => c.IsClass && ((!c.IsAbstract && c.IsSubclassOf(type)) || (!c.IsInterface && c.GetInterfaces().Contains(type))))
				.ToList();
		}

		private static async Task<KeyValuePair<string, int>> GetNameAndShardsOfBot(string token)
        {
			var restClient = new DiscordRestClient();
			await restClient.LoginAsync(TokenType.Bot, token);
			var shards = await restClient.GetRecommendedShardCountAsync();
			var name = Regex.Replace(restClient.CurrentUser.Username, "[^A-Za-z0-9]", "").Replace("NewBot", "");

			return new(name, shards);
		}
	}
}
