using Dexter.ConsoleApp;
using Dexter.Core.Enums;
using Dexter.Core.Exceptions;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class DexterDiscord {
        private ConnectionState _ConnectionState;
        private DexterDiscordClient Discord;
        
        public event EventHandler ConnectionChanged;

        public ConnectionState ConnectionState {
            get => _ConnectionState;
            private set {
                _ConnectionState = value;
                ConnectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DexterDiscord() {
            ConnectionState = ConnectionState.DISCONNECTED;
            Discord = new DexterDiscordClient();
        }

        public async Task RunAsync(string Token, CancellationToken CancellationToken) {
            ConnectionState = ConnectionState.CONNECTING;

            try {
                if (string.IsNullOrEmpty(Token))
                    throw new TokenDoesNotExistException();

                await Discord.InitializeAsync(Token);
                await Discord.Client.StartAsync();

                ConnectionState = ConnectionState.CONNECTED;
                await Task.Delay(-1, CancellationToken);
            } catch (Exception Exception) {
                ConsoleLogger.LogError(Exception.Message);
            } finally {
                ConnectionState = ConnectionState.DISCONNECTED;
            }
        }
    }
}
