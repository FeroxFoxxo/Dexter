using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class DexterDiscordClient {
        public DiscordSocketClient Client { get; private set; }

        public async Task InitializeAsync(string Token) {
            if (string.IsNullOrWhiteSpace(Token)) {
                throw new ArgumentNullException(nameof(Token));
            }

            Client = new DiscordSocketClient(new DiscordSocketConfig {
                LogLevel = LogSeverity.Info,
            });

            await Client.LoginAsync(TokenType.Bot, Token);
        }

        public void DisposeOfClient() => Client = null;
    }
}
