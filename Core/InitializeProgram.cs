using Dexter.Core;
using Dexter.Core.Configuration;
using Discord;
using Figgle;
using System;
using System.Threading.Tasks;

namespace Dexter.ConsoleApp {
    public class InitializeProgram {
        public static void Main() {
            JSONConfig.LoadConfig();

            try {
                new InitializeProgram();
            } catch (Exception Exception) {
                Console.WriteLine("\n " + Exception.ToString());
            }

            Console.WriteLine("\n Please press any key to continue...");

            _ = Console.ReadKey(true);
        }

        private readonly DiscordBot DiscordBot;

        private InitializeProgram() {
            Console.Title = (string) JSONConfig.Get(typeof(BotConfiguration), "Bot_Name");

            DiscordBot = new DiscordBot();

            DiscordBot.ConnectionChanged += OnConnectionStateChanged;

            if (!string.IsNullOrEmpty((string) JSONConfig.Get(typeof(BotConfiguration), "Token"))) {
                DiscordBot.Token = (string) JSONConfig.Get(typeof(BotConfiguration), "Token");

                DrawState();

                for (int _ = 0; _ < 6; _++)
                    Console.WriteLine();

                _ = DiscordBot.StartAsync();

                _ = Console.ReadKey(true);
            }

            HandleInput();
        }

        private Task HandleInput() {
            while (true) {
                DrawMenu();
                DrawState();

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

                        if (Console.ReadKey().Key == ConsoleKey.Y) {
                            DiscordBot.Token = Token;
                            Console.Write("\n\n Applied token! ");
                        } else
                            Console.Write("\n\n Failed to apply token! Incorrect token given. ");

                        break;
                    case 2:
                        if (DiscordBot.ConnectionState == ConnectionState.Connected)
                            DiscordBot.StopAsync();
                        else if (DiscordBot.ConnectionState == ConnectionState.Disconnected)
                            _ = DiscordBot.StartAsync();
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

        private void DrawMenu() {
            Console.Clear();

            for(int _ = 0; _ < 8; _++)
                Console.WriteLine();

            Console.Write(
                " [1] Edit Bot Token\n" +
                " [2] Start/Stop " + JSONConfig.Get(typeof(BotConfiguration), "Bot_Name") + "\n" +
                " [3] Exit " + JSONConfig.Get(typeof(BotConfiguration), "Bot_Name")
            );

            Console.Write("\n\n Please select an action by typing its number: ");
        }

        private void DrawState() {
            int PreviousCursorLeft = Console.CursorLeft;
            int PreviousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = DiscordBot.ConnectionState switch {
                ConnectionState.Disconnected => ConsoleColor.Red,
                ConnectionState.Disconnecting => ConsoleColor.DarkRed,
                ConnectionState.Connecting => ConsoleColor.Yellow,
                ConnectionState.Connected => ConsoleColor.Green,
                _ => ConsoleColor.Blue,
            };

            Console.Write("\n" + FiggleFonts.Standard.Render((string) JSONConfig.Get(typeof(BotConfiguration), "Bot_Name")));

            Console.ResetColor();

            Console.SetCursorPosition(PreviousCursorLeft, PreviousCursorTop);
        }

        private void OnConnectionStateChanged(object Sender, EventArgs E) => DrawState();
    }
}
