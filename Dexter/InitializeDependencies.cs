using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Figgle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Text.Json;
using Fergun.Interactive;
using Discord.Rest;

namespace Dexter
{

    /// <summary>
    /// The InitializeDependencies class is the entrance of the program. It is where dependencies are injected into all of their respected classes and where the bot starts up.
    /// </summary>

    public static class InitializeDependencies
    {

        /// <summary>
        /// The Main method is the entrance to the program. Arguments can be added to this method and supplied
        /// through the command line of the application when it starts. It is an asynchronous task.
        /// </summary>
        /// <param name="token">[OPTIONAL] The token of the bot. Defaults to the one specified in the BotCommands if not set.</param>
        /// <param name="parsedVersion">[OPTIONAL] The version of the bot specified by the release pipeline, is 0 by default.</param>
        /// <param name="workingDirectory">[OPTIONAL] The directory you wish the databases and configurations to be in. By default this is the build directory.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task Main(string token, int parsedVersion, string workingDirectory)
        {
            Console.Title = "Starting...";

            string version = string.Empty;

            // Sets the version based on the release pipeline version.
            string versioning = Math.Abs(Math.Round(Convert.ToSingle(parsedVersion) / 100 - .001, 2)).ToString();

            if (versioning.Split('.').Length <= 1)
                version = $"{versioning}.0.0";
            else if (versioning.Split('.')[1].Length == 1)
                version = $"{versioning[0..^1]}{versioning[^1].ToString()}.0";
            else
                version = $"{versioning[0..^1]}.{versioning[^1].ToString()}";

            if (version == "0.0.0")
                version = ".DEVELOPER";

            // Sets the current, active directory to the working directory specified in the azure cloud.
            if (!string.IsNullOrEmpty(workingDirectory))
                Directory.SetCurrentDirectory(workingDirectory);

            string databaseDirectory = Path.Join(Directory.GetCurrentDirectory(), "Databases");

            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);

            // Creates a ServiceCollection of the depencencies the project needs.
            ServiceCollection serviceCollection = new();

            serviceCollection.AddSingleton<Random>();

            serviceCollection.AddSingleton<DiscordShardedClient>();

            serviceCollection.AddSingleton(provider =>
            {
                var restClient = new DiscordRestClient ();
                restClient.LoginAsync(TokenType.Bot, token).GetAwaiter().GetResult();
                int shards = restClient.GetRecommendedShardCountAsync().GetAwaiter().GetResult();

                var client = new DiscordShardedClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 200,
                    TotalShards = shards,
                    LogLevel = LogSeverity.Debug
                });

                return client;
            });

            serviceCollection.AddSingleton<CommandService>();

            serviceCollection.AddSingleton(
                new CommandServiceConfig { IgnoreExtraArgs = true }
            );

            serviceCollection.AddSingleton(provider =>
            {
                var client = provider.GetRequiredService<DiscordShardedClient>();
                return new InteractiveService(client, TimeSpan.FromMinutes(5));
            });

            bool HasErrored = false;

            // Finds all JSON configurations and initializes them from their respective files.
            // If a JSON file is not created, a new one is initialized in its place.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(JSONConfig)) && !Type.IsAbstract).ToList().ForEach(async Type =>
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

                            serviceCollection.AddSingleton(Type);

                            await Debug.LogMessageAsync (
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

                                serviceCollection.AddSingleton(
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

            // Adds all commands, databases and services that can be initialized to the service collection.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => (Type.IsSubclassOf(typeof(DiscordModule)) || Type.IsSubclassOf(typeof(Service)) || Type.IsSubclassOf(typeof(Database))) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => serviceCollection.TryAddSingleton(Type)
            );

            // Builds the service collection.
            ServiceProvider ServiceProvider = serviceCollection.BuildServiceProvider();

            // Draws "STARTING..." in the color of cyan.
            Console.ForegroundColor = ConsoleColor.Cyan;

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(ServiceProvider.GetRequiredService<BotConfiguration>().BotName));

            // Makes sure all entity databases exist and are created if they do not.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(Database)) && !Type.IsAbstract)
                    .ToList().ForEach(
                DBType =>
                {
                    Database EntityDatabase = (Database)ServiceProvider.GetRequiredService(DBType);

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
                        Property.SetValue(ServiceProvider.GetRequiredService(Type), ServiceProvider);
                    else
                    {
                        object Service = ServiceProvider.GetService(Property.PropertyType);

                        if (Service != null)
                        {
                            Property.SetValue(ServiceProvider.GetRequiredService(Type), Service);
                        }
                    }
                })
            );

            // Connects all the event hooks in initializable modules to their designated delegates.
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(Service)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => (ServiceProvider.GetService(Type) as Service).Initialize()
            );


            ServiceProvider.GetRequiredService<DiscordShardedClient>().Log += Debug.LogMessageAsync;
            ServiceProvider.GetRequiredService<CommandService>().Log += Debug.LogMessageAsync;

            // Runs the bot using the token specified as a commands line argument.
            await ServiceProvider.GetRequiredService<StartupService>().StartAsync(token, version);

            // Sets the bot to continue running forever until the process is culled.
            await Task.Delay(-1);
        }

    }

}
