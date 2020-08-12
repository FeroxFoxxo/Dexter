using Dexter.Core;
using Discord;
using System;
using System.Threading.Tasks;

namespace Dexter.ConsoleApp {
    internal static class Initialization {
        private static DexterBot Dexter;

        private static async Task Main(string[] arguments) {
            Console.Title = "Dexter";

            Dexter = new DexterBot();

            Dexter.DexterDiscord.ConnectionChanged += OnConnectionStateChanged;

            if (arguments.Length > 0) {
                Dexter.Token = arguments[^1];

                DrawState();

                for (int _ = 0; _ < 6; _++)
                    Console.WriteLine();

                _ = Dexter.StartAsync();

                _ = Console.ReadKey(true);
            }

            await HandleInput();
        }

        private static Task HandleInput() {
            while (true) {
                DrawMenu();
                DrawState();

                bool success = int.TryParse(Console.ReadKey().KeyChar.ToString(), out int choice);

                if (!success)
                    Console.Write("\n\n Please enter a valid number. Don't type in the [ and ] characters. Just the number. ");

                switch (choice) {
                    case 1:
                        string token;
                        Console.Write("\n\n Please enter your Discord bot's token: ");
                        token = Console.ReadLine();

                        Console.Write("\n You entered the following token: " + token);
                        Console.Write("\n Is this token correct? [Y] or [N] ");

                        if (Console.ReadKey().Key == ConsoleKey.Y) {
                            Dexter.Token = token;
                            Console.Write("\n\n Applied token! ");
                        } else
                            Console.Write("\n\n Failed to apply token! Incorrect token given. ");

                        break;
                    case 2:
                        if (Dexter.DexterDiscord.ConnectionState == ConnectionState.Connected)
                            Dexter.Stop();
                        else if (Dexter.DexterDiscord.ConnectionState == ConnectionState.Disconnected)
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

        public static void DrawMenu() {
            Console.Clear();

            for(int _ = 0; _ < 8; _++)
                Console.WriteLine();

            Console.Write(
                " [1] Edit Bot Token\n" +
                " [2] Start/Stop Dexter\n" +
                " [3] Exit Dexter"
            );

            Console.Write("\n\n Please select an action by typing its number: ");
        }

        public static void DrawState() {
            int previousCursorLeft = Console.CursorLeft;
            int previousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = Dexter.DexterDiscord.ConnectionState switch {
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

            Console.SetCursorPosition(previousCursorLeft, previousCursorTop);
        }

        private static void OnConnectionStateChanged(object Sender, EventArgs E) => DrawState();
    }
}
