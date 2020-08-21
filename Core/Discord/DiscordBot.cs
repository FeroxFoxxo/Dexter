using Dexter.Core.Configuration;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class DiscordBot {

        private readonly CommandHandler CommandHandler;

        private CancellationTokenSource CancellationToken;

        public DiscordSocketClient Client { get; private set; }

        private readonly JSONConfig JSONConfig;

        private string _Token;

        public string Token {
            private get => _Token;
            set {
                _Token = value;
                _ = Client.StopAsync();
            }
        }
        
        public DiscordBot(JSONConfig _JSONConfig) {
            JSONConfig = _JSONConfig;
            CommandHandler = new CommandHandler(this, JSONConfig);
            Client = new DiscordSocketClient();

            Client.Log += ConsoleLogger.LogDiscord;
            Client.Ready += ClientOnReady;
            Client.MessageReceived += CommandHandler.HandleCommandAsync;
        }

        public async Task StartAsync() {
            try {
                if (string.IsNullOrWhiteSpace(Token))
                    throw new ArgumentNullException(nameof(Token));

                await Client.LoginAsync(TokenType.Bot, Token);
                await Client.StartAsync();
            } catch (Exception Exception) {
                ConsoleLogger.LogError(Exception.Message + "\n" + Exception.StackTrace);
                await Client.StopAsync();
            }
        }

        private Task ClientOnReady() {
            Client.SetGameAsync("Use ~mail to anonymously message the staff team!", type: ActivityType.CustomStatus);

            return Task.CompletedTask;
        }

        public async Task StopAsync() {
            await Client.StopAsync();
        }
    }
}
