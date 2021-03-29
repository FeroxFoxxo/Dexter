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
using System.Threading.Tasks;

namespace Dexter {

    /// <summary>
    /// The InitializeDependencies class is the entrance of the program. It is where dependencies are injected into all of their respected classes and where the bot starts up.
    /// </summary>
    
    public static class InitializeDependencies {

        /// <summary>
        /// The Main method is the entrance to the program. Arguments can be added to this method and supplied
        /// through the command line of the application when it starts. It is an asynchronous task.
        /// </summary>
        /// <param name="Token">[OPTIONAL] The token of the bot. Defaults to the one specified in the BotCommands if not set.</param>
        /// <param name="ParsedVersion">[OPTIONAL] The version of the bot specified by the release pipeline, is 0 by default.</param>
        /// <param name="WorkingDirectory">[OPTIONAL] The directory you wish the databases and configurations to be in. By default this is the build directory.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public static async Task Main(string Token, int ParsedVersion, string WorkingDirectory) {
            // Set title to "Starting..."
            Console.Title = "Starting...";

            // Draws "STARTING..." in the color of cyan.
            Console.ForegroundColor = ConsoleColor.Cyan;

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render("Starting..."));

            string Version = string.Empty;

            // Sets the version based on the release pipeline version.
            string Versioning = Math.Abs(Math.Round(Convert.ToSingle(ParsedVersion) / 100 - .001, 2)).ToString();

            if (Versioning.Split('.').Length <= 1)
                Version = $"{Versioning}.0.0";
            else if (Versioning.Split('.')[1].Length == 1)
                Version = $"{Versioning[0..^1]}{Versioning[^1].ToString()}.0";
            else
                Version = $"{Versioning[0..^1]}.{Versioning[^1].ToString()}";

            if (Version == "0.0.0")
                Version = ".DEVELOPER";

            // Sets the current, active directory to the working directory specified in the azure cloud.
            if (!string.IsNullOrEmpty(WorkingDirectory))
                Directory.SetCurrentDirectory(WorkingDirectory);

            string DatabaseDirectory = Path.Join(Directory.GetCurrentDirectory(), "Databases");

            if (!Directory.Exists(DatabaseDirectory))
                Directory.CreateDirectory(DatabaseDirectory);

            // Creates a ServiceCollection of the depencencies the project needs.
            ServiceCollection ServiceCollection = new();

            ServiceCollection.AddSingleton<Random>();

            // Adds an instance of the DiscordSocketClient to the collection, specifying the cache it should retain should be 1000 messages in size.
            DiscordSocketClient DiscordSocketClient = new(
                new DiscordSocketConfig {
                    MessageCacheSize = 5000,
                    ExclusiveBulkDelete = false
                }
            );

            ServiceCollection.AddSingleton(DiscordSocketClient);

            // Adds an instance of the CommandService, which is what calls our various commands.
            CommandService CommandService = new(
                new CommandServiceConfig {
                    IgnoreExtraArgs = true
                }
            );

            ServiceCollection.AddSingleton(CommandService);

            // Adds an instance of LoggingService, which allows us to log to the console.
            LoggingService LoggingService = new() {
                DiscordSocketClient = DiscordSocketClient,
                CommandService = CommandService
            };

            ServiceCollection.AddSingleton(LoggingService);

            bool HasErrored = false;

            // Finds all JSON configurations and initializes them from their respective files.
            // If a JSON file is not created, a new one is initialized in its place.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(JSONConfig)) && !Type.IsAbstract).ToList().ForEach(async Type => {
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
                        } else {
                            try {
                                object JSON = JsonSerializer.Deserialize(
                                    File.ReadAllText($"Configurations/{Type.Name}.json"),
                                    Type,
                                    new JsonSerializerOptions() { WriteIndented = true }
                                );

                                ServiceCollection.AddSingleton(
                                    Type,
                                    JSON
                                );
                            } catch (JsonException Exception) {
                                await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Error, "Initialization",
                                    $" Unable to initialize {Type.Name}! Ran into: {Exception.InnerException}."));

                                HasErrored = true;
                            }
                        }
                    });

            if (HasErrored)
                return;

            // Adds all commands, databases and services that can be initialized to the service collection.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => (Type.IsSubclassOf(typeof(DiscordModule)) || Type.IsSubclassOf(typeof(Service)) || Type.IsSubclassOf(typeof(Database))) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => ServiceCollection.TryAddSingleton(Type)
            );

            // Builds the service collection.
            ServiceProvider ServiceProvider = ServiceCollection.BuildServiceProvider();

            // Makes sure all entity databases exist and are created if they do not.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(Database)) && !Type.IsAbstract)
                    .ToList().ForEach(
                DBType => {
                    Database EntityDatabase = (Database)ServiceProvider.GetRequiredService(DBType);

                    EntityDatabase.Database.EnsureCreated();
                }
            );

            // Adds all the commands', databases' and services' dependencies to their properties.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => (Type.IsSubclassOf(typeof(DiscordModule)) || Type.IsSubclassOf(typeof(Service)) || Type.IsSubclassOf(typeof(Database))) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => Type.GetProperties().ToList().ForEach(Property => {
                    if (Property.PropertyType == typeof(ServiceProvider))
                        Property.SetValue(ServiceProvider.GetRequiredService(Type), ServiceProvider);

                    object Service = ServiceProvider.GetService(Property.PropertyType);

                    if (Service != null)
                        Property.SetValue(ServiceProvider.GetRequiredService(Type), Service);
                })
            );

            // Connects all the event hooks in initializable modules to their designated delegates.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(Service)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => (ServiceProvider.GetService(Type) as Service).Initialize()
            );

            // Runs the bot using the token specified as a commands line argument.
            await ServiceProvider.GetRequiredService<StartupService>().StartAsync(Token, Version);

            // Sets the bot to continue running forever until the process is culled.
            await Task.Delay(-1);
        }

    }

}
