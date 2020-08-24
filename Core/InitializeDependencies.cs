using Dexter.Core.Abstractions;
using Dexter.Core.Frontend;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class InitializeDependencies {
        public static async Task Main() {
            ServiceCollection ServiceCollection = new ServiceCollection();

            ServiceCollection.AddSingleton<DiscordSocketClient>();

            ServiceCollection.AddSingleton<CommandService>();

            Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.IsSubclassOf(typeof(AbstractConfiguration)) && !Type.IsAbstract).ToList().ForEach(Type => {
                if (!File.Exists($"{Type.Name}.json")) {
                    File.WriteAllText($"{Type.Name}.json", JsonSerializer.Serialize(Activator.CreateInstance(Type), new JsonSerializerOptions() { WriteIndented = true }));
                    ServiceCollection.AddSingleton(Type);
                } else
                    ServiceCollection.AddSingleton(Type, JsonSerializer.Deserialize(File.ReadAllText($"{Type.Name}.json"), Type, new JsonSerializerOptions() { WriteIndented = true }));
            });

            Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.IsSubclassOf(typeof(AbstractInitializer)) && !Type.IsAbstract).ToList().ForEach(Type => ServiceCollection.AddSingleton(Type));
            
            Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.IsSubclassOf(typeof(AbstractModule)) && !Type.IsAbstract).ToList().ForEach(Type => ServiceCollection.AddSingleton(Type));

            ServiceProvider Services = ServiceCollection.BuildServiceProvider();

            Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.IsSubclassOf(typeof(AbstractInitializer)) && !Type.IsAbstract).ToList().ForEach(Type => (Services.GetService(Type) as AbstractInitializer).AddDelegates());

            await Services.GetRequiredService<FrontendConsole>().RunAsync();

            await Task.Delay(-1);
        }
    }
}
