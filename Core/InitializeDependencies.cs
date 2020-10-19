using Dexter.Configuration;
using Dexter.Core.Abstractions;
using Dexter.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Figgle;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class InitializeDependencies {
        private static ServiceProvider Services;

        public static double Version { get; private set; }

        public static async Task Main(string Token, int _Version) {
            Console.Title = "Starting...";

            ServiceCollection ServiceCollection = new ServiceCollection();

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(JSONConfiguration)) && !Type.IsAbstract).ToList().ForEach(Type => {
                if (!File.Exists($"Configurations/{Type.Name}.json")) {
                    File.WriteAllText(
                        $"Configurations/{Type.Name}.json",
                        JsonSerializer.Serialize(
                            Activator.CreateInstance(Type),
                            new JsonSerializerOptions() { WriteIndented = true }
                        )
                    );

                    ServiceCollection.AddSingleton(Type);

                    Console.WriteLine($" This application does not have a configuration file for {Type.Name}! " +
                        $"A mock JSON class has been created in its place...");
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

            Assembly.GetExecutingAssembly().GetTypes()
                .Where(Type => Type.IsSubclassOf(typeof(EntityDatabase)) && !Type.IsAbstract)
                .ToList().ForEach(Type => ServiceCollection.AddSingleton(Type));

            ServiceCollection.AddSingleton(
                new DiscordSocketClient(new DiscordSocketConfig { MessageCacheSize = 1000 })
            );

            ServiceCollection.AddSingleton<CommandService>();

            ServiceCollection.AddSingleton(typeof(CommandModule), FormatterServices.GetUninitializedObject(typeof(CommandModule)));

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(InitializableModule)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => ServiceCollection.AddSingleton(Type)
            );
            
            Services = ServiceCollection.BuildServiceProvider();

            BotConfiguration BotConfiguration = Services.GetRequiredService<BotConfiguration>();

            Version = 1.0 + Convert.ToSingle(_Version) / 10.0;

            Console.Title = $"{BotConfiguration.Bot_Name} v{Version} (Discord.Net v{DiscordConfig.Version})";

            Console.ForegroundColor = ConsoleColor.Cyan;

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(BotConfiguration.Bot_Name));

            Services.GetRequiredService<CommandModule>().BotConfiguration = BotConfiguration;

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(EntityDatabase)) && !Type.IsAbstract)
                    .ToList().ForEach(
                DBType => {
                    EntityDatabase EntityDatabase = (EntityDatabase)Services.GetRequiredService(DBType);

                    EntityDatabase.Database.EnsureCreated();
                }
            );

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(InitializableModule)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => (Services.GetService(Type) as InitializableModule).AddDelegates()
            );
            
            await Services.GetRequiredService<StartupService>().StartAsync(Token);

            await Task.Delay(-1);
        }
    }
}
