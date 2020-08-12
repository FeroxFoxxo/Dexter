using Dexter.ConsoleApp;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class DexterDiscord {

        private ConnectionState _ConnectionState;

        private readonly CommandHandler CommandHandler;

        public event EventHandler ConnectionChanged;

        public DiscordSocketClient Client { get; private set; }

        public ConnectionState ConnectionState {
            get => _ConnectionState;
            set {
                _ConnectionState = value;
                ConnectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public DexterDiscord() {
            ConnectionState = ConnectionState.Disconnected;
            CommandHandler = new CommandHandler(this);
        }

        public async Task RunAsync(string Token, CancellationToken CancellationToken) {
            ConnectionState = ConnectionState.Connecting;

            try {
                await InitializeClient(Token);

                Client.Log += ConsoleLogger.LogDiscord;
                Client.Ready += ClientOnReady;

                await CommandHandler.InitializeAsync();
                await Client.StartAsync();

                await Task.Delay(-1, CancellationToken);
            } catch (Exception Exception) {
                ConsoleLogger.LogError(Exception.Message + "\n" + Exception.StackTrace);
            } finally {
                ConnectionState = ConnectionState.Disconnected;
            }
        }

        public async Task InitializeClient(string Token) {
            if (string.IsNullOrWhiteSpace(Token))
                throw new ArgumentNullException(nameof(Token));

            Client = new DiscordSocketClient(
                new DiscordSocketConfig {
                    LogLevel = LogSeverity.Info,
                }
            );

            await Client.LoginAsync(TokenType.Bot, Token);
        }

        private Task ClientOnReady() {
            Client.SetGameAsync("Use ~mail to anonymously message the staff team!", type: ActivityType.CustomStatus);

            ConsoleLogger.Log("Dexter has started successfully!");
            ConnectionState = ConnectionState.Connected;

            return Task.CompletedTask;
        }

        public void Disconnected() {
            if (ConnectionState != ConnectionState.Disconnected)
                ConnectionState = ConnectionState.Disconnecting;
        }
    }
}
