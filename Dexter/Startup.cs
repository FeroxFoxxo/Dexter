using Dexter.Abstractions;
using Dexter.Extensions;
using Dexter.Workers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Figgle;
using Genbox.WolframAlpha;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter
{

	/// <summary>
	/// The Startup class is the entrance of the program. It is where dependencies are injected into all of their respected classes and where the bot starts up.
	/// </summary>

	public static class Startup
	{

		private static string p_Token, p_DBUser, p_DBPassword, p_Version;

		public static string Version { get => p_Version; }
		public static string Token { get => p_Token; }
		public static string DBUser { get => p_DBUser; }
		public static string DBPassword { get => p_DBPassword; }

		/// <summary>
		/// The Main method is the entrance to the program. Arguments can be added to this method and supplied
		/// through the command line of the application when it starts. It is an asynchronous task.
		/// </summary>
		/// <param name="token">[OPTIONAL] The token of the bot. Defaults to the one specified in the BotCommands if not set.</param>
		/// <param name="version">[OPTIONAL] The version of the bot specified by the release pipeline.</param>
		/// <param name="directory">[OPTIONAL] The directory you wish the databases and configurations to be in. By default this is the build directory.</param>
		/// <param name="spotifyID">[OPTIONAL] Spotify CLIENT_ID from developer.spotify.com/dashboard.</param>
		/// <param name="spotifySecret">[OPTIONAL] Spotify CLIENT_SECRET from developer.spotify.com/dashboard.</param>
		/// <param name="dbUser">[OPTIONAL] DBUSER for the MySQL database username.</param>
		/// <param name="dbPassword">[OPTIONAL] DBPASSWORD for the MySQL database password.</param>
		/// <param name="wolframAPI">[OPTIONAL] WOLFRAM ALPHA API KEY.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public static async Task Main(string token, string version, string directory, string spotifyID, string spotifySecret, string dbUser, string dbPassword, string wolframAPI)
		{
			p_Version = version;
			p_Token = token;

			p_DBUser = dbUser;
			p_DBPassword = dbPassword;

			// Create new WebApplication which will generate our REST-FUL API.

			var builder = WebApplication.CreateBuilder();

			// Sets the current, active directory to the working directory specified in the azure cloud.

			if (!string.IsNullOrEmpty(directory))
				Directory.SetCurrentDirectory(directory);
			else
				Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			string databaseDirectory = Path.Join(Directory.GetCurrentDirectory(), "Databases");

			if (!Directory.Exists(databaseDirectory))
				Directory.CreateDirectory(databaseDirectory);

			// Get information on the bot through REST.

			Console.ForegroundColor = ConsoleColor.Cyan;

			var botInfo = await GetNameAndShardsOfBot(token);

			var name = botInfo.Key;
			var shards = botInfo.Value;

			await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(name));

			Console.Title = $"{name} v{version} (Discord.Net v{DiscordConfig.Version})";

			// Create basic logger for init.

			var logger = new LoggerConfiguration()
				.WriteTo.Console()
				.CreateLogger();

			// Start the swager instance for debugging.

			builder.Services.AddControllers();

			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc(version, new() { Title = name, Version = version });
			});

			// Init Spotify API.

			if (!string.IsNullOrEmpty(spotifyID) && !string.IsNullOrEmpty(spotifySecret))
			{
				builder.Services.AddSingleton(new ClientCredentialsRequest(spotifyID, spotifySecret));
			}
			else
				builder.Services.AddSingleton(new ClientCredentialsRequest("UNKNOWN", "UNKNOWN"));

			// Init WolfRam Alpha.

			if (!string.IsNullOrEmpty(wolframAPI))
				builder.Services.AddSingleton(new WolframAlphaClient(wolframAPI));

			// Init Google API.

			if (!File.Exists("Credentials.json"))
			{
				logger.Error(
					$"Credential file 'Credentials.json' does not exist!"
				);
			}
			else
			{
				// Open the FileStream to the related file.
				using FileStream stream = new("Credentials.json", FileMode.Open, FileAccess.Read);

				// The file token.json stores the user's access and refresh tokens, and is created
				// automatically when the authorization flow completes for the first time.

				builder.Services.AddSingleton(
					await GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.FromStream(stream).Secrets,
						new[] { SheetsService.Scope.Spreadsheets, YouTubeService.Scope.YoutubeReadonly },
						"admin",
						CancellationToken.None,
						new FileDataStore("token", true),
						new PromptCodeReceiver()
					)
				);
			}

			// Initialize our dependencies for the bot.

			builder.Services.AddSingleton(
				new CommandService(
					new CommandServiceConfig()
					{
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

			bool hasErrored = false;

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

							logger.Error(
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
								logger.Error(
									$" Unable to initialize {Type.Name}! Ran into: {Exception.InnerException}.",
									LogSeverity.Error
								);

								hasErrored = true;
							}
						}
					});

			if (hasErrored)
				return;

			GetDatabases().ForEach(t => builder.Services.AddScoped(t));

			GetEvents().ForEach(t => builder.Services.AddSingleton(t));

			// Add hosted events to the application, which will run until it is closed.

			builder.Services.AddHostedService<DiscordWorker>();

			// Build the website and start up swagger to allow for quick development of the API.

			var app = builder.Build();

			using (var scope = app.Services.CreateScope()) {

				// Makes sure all entity databases exist and are created if they do not.
				GetDatabases().ForEach(
					DBType =>
					{
						Database entityDatabase = (Database)scope.ServiceProvider.GetRequiredService(DBType);

						entityDatabase.Database.EnsureCreated();
					}
				);
				GetEvents().ForEach(
					type => app.Services.GetRequiredService(type).SetClassParameters(scope, app.Services)
				);
			}

			// Connects all the event hooks in initializable modules to their designated delegates.
			GetEvents().ForEach(
				type => (app.Services.GetService(type) as Event).InitializeEvents()
			);

			app.UseSwagger();

			app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{name} {version}"));

			app.UseHttpsRedirection();

			app.UseAuthorization();

			app.MapControllers();

			app.Run();
		}

		private static List<Type> GetDatabases() { return GetClassesOfType(typeof(Database)); }

		private static List<Type> GetJSONConfigs() { return GetClassesOfType(typeof(JSONConfig)); }

		private static List<Type> GetEvents() { return GetClassesOfType(typeof(Event)); }

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
