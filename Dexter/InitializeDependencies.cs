using Dexter.Abstractions;
using Dexter.Services;
using Discord.Commands;
using Discord.WebSocket;
using Figgle;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Discord;
using System.Timers;
using System.Threading.Tasks;

namespace Dexter {
    /// <summary>
    /// The InitializeDependencies class is the entrance of the program. It is where dependencies are injected into all of their respected classes and where the bot starts up.
    /// </summary>
    public static class InitializeDependencies {

        /// <summary>
        /// The current version of the bot, specified in the format X.X.X.
        /// </summary>
        public static string Version { get; private set; }

        /// <summary>
        /// The instance of the ServiceProvider, which is used for various things like embed building and confirmations.
        /// </summary>
        public static ServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// The Main method is the entrance to the program. Arguments can be added to this method and supplied
        /// through the command line of the application when it starts. It is an asynchronous task.
        /// </summary>
        /// <param name="Token">[OPTIONAL] The token of the bot. Defaults to the one specified in the BotCommands if not set.</param>
        /// <param name="Version">[OPTIONAL] The version of the bot specified by the release pipeline, is 0 by default.</param>
        /// <param name="WorkingDirectory">[OPTIONAL] The directory you wish the databases and configurations to be in. By default this is the build directory.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public static async Task Main(string Token, int Version, string WorkingDirectory) {
            // Set title to "Starting..."
            Console.Title = "Starting...";

            // Draws "STARTING..." in the color of cyan.
            Console.ForegroundColor = ConsoleColor.Cyan;

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render("Starting..."));

            // Sets the version based on the release pipeline version.
            string Versioning = Math.Round(Convert.ToSingle(Version) / 100 - .001, 2).ToString();

            if (Versioning.Split('.').Length <= 1)
                InitializeDependencies.Version = $"{Versioning}.0.0";
            else if (Versioning.Split('.')[1].Length == 1)
                InitializeDependencies.Version = $"{Versioning[0..^1]}{Versioning[^1].ToString()}.0";
            else
                InitializeDependencies.Version = $"{Versioning[0..^1]}.{Versioning[^1].ToString()}";

            // Sets the current, active directory to the working directory specified in the azure cloud.
            if (!string.IsNullOrEmpty(WorkingDirectory))
                Directory.SetCurrentDirectory(WorkingDirectory);

            string DatabaseDirectory = Path.Join(Directory.GetCurrentDirectory(), "Databases");

            if (!Directory.Exists(DatabaseDirectory))
                Directory.CreateDirectory(DatabaseDirectory);

            // Creates a ServiceCollection of the depencencies the project needs.
            ServiceCollection ServiceCollection = new();

            // Adds an instance of the DiscordSocketClient to the collection, specifying the cache it should retain should be 1000 messages in size.
            DiscordSocketClient DiscordSocketClient = new(
                new DiscordSocketConfig {
                    MessageCacheSize = 1000,
                    GatewayIntents = GatewayIntents.GuildWebhooks | GatewayIntents.Guilds | GatewayIntents.GuildPresences | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions
                                    | GatewayIntents.DirectMessages | GatewayIntents.DirectMessageReactions | GatewayIntents.GuildVoiceStates,
                    ExclusiveBulkDelete = false
                }
            );

            ServiceCollection.AddSingleton(DiscordSocketClient);

            // Adds an instance of the CommandService, which is what calls our various commands.
            CommandService CommandService = new();
            ServiceCollection.AddSingleton(CommandService);

            ServiceCollection.AddSingleton<ModuleService>();

            // Adds an instance of LoggingService, which allows us to log to the console.
            LoggingService LoggingService = new(DiscordSocketClient, CommandService);
            ServiceCollection.AddSingleton(LoggingService);

            // Finds all JSON configurations and initializes them from their respective files.
            // If a JSON file is not created, a new one is initialized in its place.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(JSONConfiguration)) && !Type.IsAbstract).ToList().ForEach(async Type => {
                        if (!File.Exists($"Configurations/{Type.Name}.json")) {
                            File.WriteAllText(
                                $"Configurations/{Type.Name}.json",
                                JsonSerializer.Serialize(
                                    Activator.CreateInstance(Type),
                                    new JsonSerializerOptions() { WriteIndented = true }
                                )
                            );

                            ServiceCollection.TryAddSingleton(Type);

                            await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Warning, "Initialization",
                                $" This application does not have a configuration file for {Type.Name}! " +
                                $"A mock JSON class has been created in its place..."));
                        } else
                            ServiceCollection.AddSingleton(
                                Type,
                                JsonSerializer.Deserialize(
                                    File.ReadAllText($"Configurations/{Type.Name}.json"),
                                    Type,
                                    new JsonSerializerOptions() { WriteIndented = true }
                                )
                            );
                    });

            // Finds all the Entity databases and adds them respecively to the service collection.
            Assembly.GetExecutingAssembly().GetTypes()
                .Where(Type => Type.IsSubclassOf(typeof(EntityDatabase)) && !Type.IsAbstract)
                .ToList().ForEach(Type => ServiceCollection.TryAddSingleton(Type));

            // Adds all commands that can be initialized to the service collection.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(DiscordModule)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => ServiceCollection.TryAddSingleton(Type)
            );

            // Adds all services that can be initialized to the service collection.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(InitializableModule)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => ServiceCollection.TryAddSingleton(Type)
            );

            // Builds the service collection to the provider.
            ServiceProvider = ServiceCollection.BuildServiceProvider();

            // Makes sure all entity databases exist and are created if they do not.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(EntityDatabase)) && !Type.IsAbstract)
                    .ToList().ForEach(
                DBType => {
                    EntityDatabase EntityDatabase = (EntityDatabase)ServiceProvider.GetRequiredService(DBType);

                    EntityDatabase.Database.EnsureCreated();
                }
            );

            // Connects all the event hooks in initializable modules to their designated delegates.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(InitializableModule)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => (ServiceProvider.GetService(Type) as InitializableModule).Initialize()
            );

            // Runs the bot using the token specified as a commands line argument.
            await ServiceProvider.GetRequiredService<StartupService>().StartAsync(Token);

            await ServiceProvider.GetRequiredService<ModuleService>().EnableModules();

            // Sets the bot to continue running forever until the process is culled.
            await Task.Delay(-1);
        }

    }
}
