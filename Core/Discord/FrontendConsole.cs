using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord;
using Discord.WebSocket;
using Figgle;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dexter.Core.Frontend {
    public class FrontendConsole : AbstractInitializer {

        private readonly DiscordSocketClient Client;

        private readonly BotConfiguration BotConfiguration;

        public FrontendConsole(DiscordSocketClient _Client, BotConfiguration _BotConfiguration) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
        }

        public override void AddDelegates() {
            Client.Connected += DrawState;
            Client.Disconnected += (Exception) => DrawState();
        }

        public async Task RunAsync() {
            Console.Title = BotConfiguration.Bot_Name;

            await DrawState();

            if (!string.IsNullOrEmpty(BotConfiguration.Token)) {
                for (int _ = 0; _ < 6; _++)
                    await Console.Out.WriteLineAsync();

                await Client.LoginAsync(TokenType.Bot, BotConfiguration.Token);

                await Client.StartAsync();

                Console.ReadKey(true);
            }

            await HandleInput();
        }

        private async Task HandleInput() {
            while (true) {
                Console.Clear();

                await DrawState();

                for (int _ = 0; _ < 6; _++)
                    await Console.Out.WriteLineAsync();

                await Console.Out.WriteLineAsync($"\n [1] Edit Configuration");
                await Console.Out.WriteLineAsync($" [2] Start/Stop {BotConfiguration.Bot_Name}");
                await Console.Out.WriteLineAsync($" [3] Exit {BotConfiguration.Bot_Name}");

                await Console.Out.WriteAsync("\n Please select an action by typing its number: ");

                bool Success = int.TryParse(Console.ReadKey().KeyChar.ToString(), out int Choice);

                await Console.Out.WriteLineAsync();

                if (!Success)
                    await Console.Out.WriteLineAsync("\n Please enter a valid number. Don't type in the [ and ] characters. Just the number. ");

                switch (Choice) {
                    case 1:
                        Type[] ConfigurationFiles = Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.IsSubclassOf(typeof(AbstractConfiguration)) && !Type.IsAbstract).ToArray();

                        await Console.Out.WriteLineAsync("\n Configuration Files:");

                        for (int i = 0; i < ConfigurationFiles.Length; i++)
                            await Console.Out.WriteLineAsync($"     {i + 1}. {ConfigurationFiles[i].Name}");

                        await Console.Out.WriteAsync("\n Please select the number of which configuration you wish to edit: ");

                        bool SuccessConfig = int.TryParse(Console.ReadKey().KeyChar.ToString(), out int ConfigChoice);

                        await Console.Out.WriteLineAsync();

                        if (!SuccessConfig)
                            await Console.Out.WriteLineAsync("\n Please enter a valid number. Don't type in the [ and ] characters. Just the number. ");

                        if (ConfigurationFiles.Length >= ConfigChoice && ConfigChoice > 0) {
                            await Console.Out.WriteLineAsync($"\n You've chosen to edit {ConfigurationFiles[ConfigChoice].Name}. ");
                        } else
                            await Console.Out.WriteLineAsync("\n Your choice was not recognized as an option in the menu. Please try again. ");

                        break;
                    case 2:
                        try {
                            if (Client.ConnectionState == ConnectionState.Connected)
                                await Client.StopAsync();
                            else if (Client.ConnectionState == ConnectionState.Disconnected)
                                await Client.StartAsync();
                        } catch (Exception) {}
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                    default:
                        await Console.Out.WriteLineAsync("\n Your choice was not recognized as an option in the menu. Please try again. ");
                        break;
                }

                Console.ReadKey(true);
            }
        }

        private async Task DrawState() {
            int PreviousCursorLeft = Console.CursorLeft;
            int PreviousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = Client.ConnectionState switch {
                ConnectionState.Disconnected => ConsoleColor.Red,
                ConnectionState.Connected => ConsoleColor.Green,
                _ => ConsoleColor.Blue,
            };

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(BotConfiguration.Bot_Name));

            Console.ResetColor();

            Console.SetCursorPosition(PreviousCursorLeft, PreviousCursorTop);
        }
    }
}
