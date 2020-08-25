using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord;
using Discord.WebSocket;
using Figgle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dexter.Core.DiscordApp {
    public class FrontendConsole : InitializableModule {

        private readonly DiscordSocketClient Client;

        private readonly BotConfiguration BotConfiguration;

        private readonly IServiceProvider Services;

        public FrontendConsole(DiscordSocketClient _Client, BotConfiguration _BotConfiguration, IServiceProvider _Services) {
            Client = _Client;
            BotConfiguration = _BotConfiguration;
            Services = _Services;
        }

        public override void AddDelegates() {
            Client.Connected += DrawState;
            Client.Disconnected += (Exception) => DrawState();
        }

        public async Task RunAsync() {
            await ResetConsole();

            if (!string.IsNullOrEmpty(BotConfiguration.Token)) {
                try {
                    await Client.LoginAsync(TokenType.Bot, BotConfiguration.Token);
                    await Client.StartAsync();
                } catch (Exception Exception) {
                    Console.WriteLine($"\n {Exception.Message}");
                    Console.Write("\n Please press any key to continue... ");
                }

                Console.ReadKey(true);
            }

            while (true) {
                await ResetConsole();

                if (GetOptionMenu(new string[3] {"Edit Configuration", $"Start/Stop {BotConfiguration.Bot_Name}", $"Exit {BotConfiguration.Bot_Name}"}, "Menu", out int Choice)) {
                    if (Choice == 1) {
                        List<Type> Configurations = new List<Type>();
                        List<string> ConfigurationNames = new List<string>();

                        Assembly.GetExecutingAssembly().GetTypes().Where(Type => Type.IsSubclassOf(typeof(JSONConfiguration)) && !Type.IsAbstract).ToList().ForEach(Configuration => {
                            bool HasWritableFields = false;

                            Configuration.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).ToList().ForEach(Field => {
                                try {
                                    _ = (string)Convert.ChangeType(Field.GetValue(Services.GetService(Configuration)), typeof(string));
                                    HasWritableFields = true;
                                } catch (Exception) { }
                            });

                            if (HasWritableFields) {
                                Configurations.Add(Configuration);
                                ConfigurationNames.Add(Configuration.Name);
                            }
                        });

                        if (GetOptionMenu(ConfigurationNames.ToArray(), "Configuration", out int ConfigChoice)) {
                            Type Configuration = Configurations[ConfigChoice - 1];
                            await Console.Out.WriteLineAsync($"\n You've chosen to edit {Configuration.Name}. ");

                            List<string> FieldNames = new List<string>();
                            List<FieldInfo> Fields = new List<FieldInfo>();

                            Configuration.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).ToList().ForEach(Field => {
                                try {
                                    string Value = (string)Convert.ChangeType(Field.GetValue(Services.GetService(Configuration)), typeof(string));
                                    FieldNames.Add($"{Field.FieldType.Name} {Field.Name[1..^16]} = {Value}");
                                    Fields.Add(Field);
                                } catch (Exception) { }
                            });

                            if (GetOptionMenu(FieldNames.ToArray(), "Field", out int FieldChoice)) {
                                string FieldName = Fields[FieldChoice - 1].Name[1..^16];

                                await Console.Out.WriteAsync($"\n What do you wish to set {FieldName} to? ");

                                string Value = Console.ReadLine();

                                await Console.Out.WriteLineAsync($"\n You've entered the following field for {FieldName}: {Value}");
                                await Console.Out.WriteAsync(" Are you sure you wish to wish to apply this property? [Y] or [N] ");

                                ConsoleKey Key = Console.ReadKey().Key;

                                await Console.Out.WriteLineAsync();

                                if (Key == ConsoleKey.Y) {
                                    Fields[FieldChoice - 1].SetValue(Services.GetService(Configuration), Convert.ChangeType(Value, Fields[FieldChoice - 1].FieldType));
                                    await Console.Out.WriteAsync($"\n Successfully set {FieldName} to {Value}.");
                                } else
                                    await Console.Out.WriteAsync($"\n Failed to apply {FieldName}! Incorrect {FieldName} given.");
                            }
                        }
                    } else if (Choice == 2) {
                        try {
                            if (Client.ConnectionState == ConnectionState.Connected)
                                await Client.StopAsync();
                            else if (Client.ConnectionState == ConnectionState.Disconnected) {
                                await Client.LoginAsync(TokenType.Bot, BotConfiguration.Token);
                                await Client.StartAsync();
                            }
                        } catch (Exception Exception) {
                            Console.WriteLine($"\n {Exception.Message}");
                            Console.Write("\n Please press any key to continue... ");
                        }
                    } else {
                        Environment.Exit(0);
                        break;
                    }
                }

                Console.ReadKey(true);
            }
        }

        private async Task ResetConsole() {
            Console.Clear();

            await DrawState();

            for (int _ = 0; _ < 6; _++)
                await Console.Out.WriteLineAsync();
        }

        private bool GetOptionMenu(object[] Possibilities, string Name, out int ReturnedChoice) {
            Console.WriteLine($"\n {Name}s:");

            for (int i = 0; i < Possibilities.Length; i++)
                Console.WriteLine($"    {i + 1}. {Possibilities[i]}");

            Console.Write($"\n Please select a {Name.ToLower()} by typing its number: ");

            bool Success = int.TryParse(Console.ReadKey().KeyChar.ToString(), out int Choice);

            ReturnedChoice = Choice;

            Console.WriteLine();

            if (!Success)
                Console.WriteLine("\n Please enter a valid number. Don't type in the [ and ] characters. Just the number. ");

            if (Possibilities.Length >= Choice && Choice > 0)
                return true;

            Console.WriteLine("\n Your choice was not recognized as an option in the menu. Please try again. ");
            return false;
        }

        private async Task DrawState() {
            Console.Title = BotConfiguration.Bot_Name;

            int PreviousCursorLeft = Console.CursorLeft;
            int PreviousCursorTop = Console.CursorTop;

            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = Client.ConnectionState switch {
                ConnectionState.Connected => ConsoleColor.Green,
                ConnectionState.Disconnected => ConsoleColor.Red,
                ConnectionState.Disconnecting => ConsoleColor.Red,
                _ => ConsoleColor.Yellow,
            };

            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render(BotConfiguration.Bot_Name));

            Console.ResetColor();

            Console.SetCursorPosition(PreviousCursorLeft, PreviousCursorTop);
        }
    }
}
