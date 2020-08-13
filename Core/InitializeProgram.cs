using Dexter.Core;
using Dexter.Core.Configuration;
using Discord;
using System;
using System.Threading.Tasks;

namespace Dexter.ConsoleApp {
    public class InitializeProgram {
        public static void Main() {
            JSONConfigurator.LoadConfigurations();

            try {
                new InitializeProgram();
            } catch (Exception Exception) {
                ConsoleLogger.LogError(Exception.StackTrace);
            }

            Console.WriteLine("\n\n Please press any key to continue...");

            _ = Console.ReadKey(true);
        }

        private readonly DiscordBot Dexter;

        private InitializeProgram() {
            Console.Title = (string) JSONConfigurator.GetConfiguration(typeof(BotConfiguration), "Bot_Name");

            Dexter = new DiscordBot();

            Dexter.ConnectionChanged += OnConnectionStateChanged;

            if (!string.IsNullOrEmpty((string) JSONConfigurator.GetConfiguration(typeof(BotConfiguration), "Token"))) {
                Dexter.Token = (string) JSONConfigurator.GetConfiguration(typeof(BotConfiguration), "Token");

                DrawState();

                for (int _ = 0; _ < 6; _++)
                    Console.WriteLine();

                _ = Dexter.StartAsync();

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
                            Dexter.Token = Token;
                            Console.Write("\n\n Applied token! ");
                        } else
                            Console.Write("\n\n Failed to apply token! Incorrect token given. ");

                        break;
                    case 2:
                        if (Dexter.ConnectionState == ConnectionState.Connected)
                            Dexter.StopAsync();
                        else if (Dexter.ConnectionState == ConnectionState.Disconnected)
                            _ = Dexter.StartAsync();
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
                " [2] Start/Stop " + JSONConfigurator.GetConfiguration(typeof(BotConfiguration), "Bot_Name") + "\n" +
                " [3] Exit Dexter"
            );

            Console.Write("\n\n Please select an action by typing its number: ");
        }

        private void DrawState() {
            int PreviousCursorLeft = Console.CursorLeft;
            int PreviousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = Dexter.ConnectionState switch {
                ConnectionState.Disconnected => ConsoleColor.Red,
                ConnectionState.Disconnecting => ConsoleColor.DarkRed,
                ConnectionState.Connecting => ConsoleColor.Yellow,
                ConnectionState.Connected => ConsoleColor.Green,
                _ => ConsoleColor.Blue,
            };

            Console.Write("\n" +
                " ██████╗ ███████╗██╗  ██╗████████╗███████╗██████╗ \n" +
                " ██╔══██╗██╔════╝╚██╗██╔╝╚══██╔══╝██╔════╝██╔══██╗\n" +
                " ██║  ██║█████╗   ╚███╔╝    ██║   █████╗  ██████╔╝\n" +
                " ██║  ██║██╔══╝   ██╔██╗    ██║   ██╔══╝  ██╔══██╗\n" +
                " ██████╔╝███████╗██╔╝ ██╗   ██║   ███████╗██║  ██║\n" +
                " ╚═════╝ ╚══════╝╚═╝  ╚═╝   ╚═╝   ╚══════╝╚═╝  ╚═╝"
            );

            Console.ResetColor();

            Console.SetCursorPosition(PreviousCursorLeft, PreviousCursorTop);
        }

        private void OnConnectionStateChanged(object Sender, EventArgs E) => DrawState();
    }
}
