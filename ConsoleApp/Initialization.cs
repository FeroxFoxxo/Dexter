using Dexter.Core;
using Dexter.Core.Enums;
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
                Dexter.Token = arguments[arguments.Length - 1];
                _ = Dexter.StartAsync(true);
            }

            await HandleInput();
        }

        private static Task HandleInput() {
            while (true) {
                DrawMenu();
                DrawState();

                bool success = int.TryParse(Console.ReadLine(), out int choice);

                Console.WriteLine();

                if (!success)
                    Console.WriteLine(Configuration.NOT_A_NUMBER);

                switch (choice) {
                    case 1:
                        string token;
                        Console.Write(Configuration.ENTER_TOKEN);
                        token = Console.ReadLine();

                        Console.WriteLine();

                        Console.WriteLine(Configuration.IS_TOKEN_CORRECT + token);
                        Console.Write(Configuration.YES_NO_PROMPT);

                        if (Console.ReadKey().Key == ConsoleKey.Y) {
                            Dexter.Token = token;
                            Console.WriteLine(Configuration.CORRECT_TOKEN);
                        } else
                            Console.WriteLine(Configuration.FAILED_TOKEN);

                        Console.Write(Configuration.PRESS_KEY);

                        break;
                    case 2:
                        if (Dexter.DexterDiscord.ConnectionState == ConnectionState.CONNECTED)
                            Dexter.Stop(false);
                        else if (Dexter.DexterDiscord.ConnectionState == ConnectionState.DISCONNECTED)
                            _ = Dexter.StartAsync(false);
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine(Configuration.INVALID_CHOICE);
                        Console.Write(Configuration.PRESS_KEY);
                        break;
                }

                _ = Console.ReadKey(true);
            }
        }

        public static void DrawMenu() {
            Console.Clear();

            for(int _ = 0; _ < 8; _++)
                Console.WriteLine();

            Console.WriteLine(Configuration.MENU_OPTIONS);

            Console.Write(Configuration.ENTER_NUMBER);
        }

        public static void DrawState() {
            int previousCursorLeft = Console.CursorLeft;
            int previousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = Dexter.DexterDiscord.ConnectionState switch
            {
                ConnectionState.DISCONNECTED => ConsoleColor.Red,
                ConnectionState.CONNECTING => ConsoleColor.Yellow,
                ConnectionState.CONNECTED => ConsoleColor.Green,
                _ => ConsoleColor.Blue,
            };

            Console.WriteLine(Configuration.HEADER);
            Console.ResetColor();

            Console.SetCursorPosition(previousCursorLeft, previousCursorTop);
        }

        private static void OnConnectionStateChanged(object Sender, EventArgs E) => DrawState();
    }
}
