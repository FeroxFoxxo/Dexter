using Dexter.Core;
using Discord;
using System;
using System.Threading.Tasks;

namespace Dexter.ConsoleApp {
    internal static class Initialization {
        private static DexterBot Dexter;

        private static async Task Main(string[] arguments) {
            Console.Title = Configuration.BOT_NAME;

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
                    Console.Write(Configuration.NOT_A_NUMBER);

                switch (choice) {
                    case 1:
                        string token;
                        Console.Write(Configuration.ENTER_TOKEN);
                        token = Console.ReadLine();

                        Console.Write(Configuration.IS_TOKEN_CORRECT + token);
                        Console.Write(Configuration.YES_NO_PROMPT);

                        if (Console.ReadKey().Key == ConsoleKey.Y) {
                            Dexter.Token = token;
                            Console.Write(Configuration.CORRECT_TOKEN);
                        } else
                            Console.Write(Configuration.FAILED_TOKEN);

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
                        Console.Write(Configuration.INVALID_CHOICE);
                        break;
                }

                Console.ReadKey(true);
            }
        }

        public static void DrawMenu() {
            Console.Clear();

            for(int _ = 0; _ < 8; _++)
                Console.WriteLine();

            Console.Write(Configuration.MENU_OPTIONS);

            Console.Write(Configuration.ENTER_NUMBER);
        }

        public static void DrawState() {
            int previousCursorLeft = Console.CursorLeft;
            int previousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = Dexter.DexterDiscord.ConnectionState switch
            {
                ConnectionState.Disconnected => ConsoleColor.Red,
                ConnectionState.Disconnecting => ConsoleColor.DarkRed,
                ConnectionState.Connecting => ConsoleColor.Yellow,
                ConnectionState.Connected => ConsoleColor.Green,
                _ => ConsoleColor.Blue,
            };

            Console.Write(Configuration.HEADER);
            Console.ResetColor();

            Console.SetCursorPosition(previousCursorLeft, previousCursorTop);
        }

        private static void OnConnectionStateChanged(object Sender, EventArgs E) => DrawState();
    }
}
