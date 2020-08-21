using Dexter.Core;
using Dexter.Core.Configuration;
using Discord;
using Figgle;
using System;
using System.Threading.Tasks;

namespace Dexter.ConsoleApp {
    public class InitializeProgram {

        private readonly DiscordBot DiscordBot;

        private readonly JSONConfig JSONConfig;

        private ConsoleColor State;

        public static async Task Main() {
            await new InitializeProgram().InitializeBot();
        }

        private InitializeProgram() {
            JSONConfig = new JSONConfig();

            JSONConfig.LoadConfig();

            Console.Title = (string) JSONConfig.Get(typeof(BotConfiguration), "Bot_Name");

            DiscordBot = new DiscordBot(JSONConfig);

            State = ConsoleColor.Red;
        }

        public async Task InitializeBot() {
            DiscordBot.Client.Connected += Client_Connected;
            DiscordBot.Client.Disconnected += Client_Disconnected;

            await DrawState();

            if (!string.IsNullOrEmpty((string)JSONConfig.Get(typeof(BotConfiguration), "Token"))) {
                DiscordBot.Token = (string)JSONConfig.Get(typeof(BotConfiguration), "Token");

                for (int _ = 0; _ < 6; _++)
                    Console.WriteLine();

                await DiscordBot.StartAsync();

                _ = Console.ReadKey();
            }

            await HandleInput();
        }

        private Task Client_Disconnected(Exception arg) {
            State = ConsoleColor.Red;
            return DrawState();
        }

        private Task Client_Connected() {
            State = ConsoleColor.Green;
            return DrawState();
        }

        private async Task HandleInput() {
            while (true) {
                await DrawMenu();

                bool Success = int.TryParse(Console.ReadKey().KeyChar.ToString(), out int choice);

                if (!Success)
                    Console.Write("\n\n Please enter a valid number. Don't type in the [ and ] characters. Just the number. ");

                switch (choice) {
                    case 1:
                        string Token;
                        Console.Write("\n\n Please enter your Discord bot's token: ");
                        Token = Console.ReadLine();

                        Console.Write("\n You entered the following token: " + Token);
                        Console.Write("\n Is this token correct? [Y] or [N] ");

                        if (Console.ReadKey().Key == ConsoleKey.Y)
                            DiscordBot.Token = Token;

                        break;
                    case 2:
                        if (DiscordBot.Client.ConnectionState == ConnectionState.Connected)
                            await DiscordBot.StopAsync();
                        else if (DiscordBot.Client.ConnectionState == ConnectionState.Disconnected)
                            await DiscordBot.StartAsync();
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.Write("\n Your choice was not recognized as an option in the menu. Please try again. ");
                        break;
                }

                Console.ReadKey(true);
            }
        }

        private async Task DrawMenu() {
            Console.Clear();

            await DrawState();

            for(int _ = 0; _ < 8; _++)
                Console.WriteLine();

            Console.Write(
                " [1] Edit Bot Token\n" +
                " [2] Start/Stop " + JSONConfig.Get(typeof(BotConfiguration), "Bot_Name") + "\n" +
                " [3] Exit " + JSONConfig.Get(typeof(BotConfiguration), "Bot_Name")
            );

            Console.Write("\n\n Please select an action by typing its number: ");
        }

        private Task DrawState() {
            int PreviousCursorLeft = Console.CursorLeft;
            int PreviousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = State;

            Console.Write("\n" + FiggleFonts.Standard.Render((string) JSONConfig.Get(typeof(BotConfiguration), "Bot_Name")));

            Console.ResetColor();

            Console.SetCursorPosition(PreviousCursorLeft, PreviousCursorTop);

            return Task.CompletedTask;
        }
    }
}
