using Discord;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class DexterDiscord {

        private readonly CommandHandler CommandHandler;

        public event EventHandler ConnectionChanged;

        private CancellationTokenSource CancellationToken;

        public DiscordSocketClient Client { get; private set; }

        private string _Token;

        public string Token {
            private get => _Token;
            set {
                _Token = value;
                DisposeToken();
            }
        }

        private ConnectionState _ConnectionState;

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

        public async Task StartAsync() {
            ConsoleLogger.Log("Starting Dexter. Please wait...");

            CancellationToken = new CancellationTokenSource();

            await RunAsync(Token, CancellationToken.Token);
        }

        public async Task RunAsync(string Token, CancellationToken CancellationToken) {
            ConnectionState = ConnectionState.Connecting;

            try {
                if (string.IsNullOrWhiteSpace(Token))
                    throw new ArgumentNullException(nameof(Token));

                Client = new DiscordSocketClient(
                    new DiscordSocketConfig {
                        LogLevel = LogSeverity.Info,
                    }
                );

                await Client.LoginAsync(TokenType.Bot, Token);

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

        private Task ClientOnReady() {
            Client.SetGameAsync("Use ~mail to anonymously message the staff team!", type: ActivityType.CustomStatus);

            ConsoleLogger.Log("Dexter has started successfully!");
            ConnectionState = ConnectionState.Connected;

            return Task.CompletedTask;
        }

        public void StopAsync() {
            ConsoleLogger.Log("Stopping Dexter. Please wait...");

            DisposeToken();

            ConsoleLogger.Log("Dexter has halted successfully!");
        }

        private void DisposeToken() {
            if (CancellationToken is null)
                return;

            if (ConnectionState != ConnectionState.Disconnected)
                ConnectionState = ConnectionState.Disconnecting;

            CancellationToken.Cancel();
            CancellationToken.Dispose();
            CancellationToken = null;
        }
    }
}
