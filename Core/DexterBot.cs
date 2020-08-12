using Dexter.ConsoleApp;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class DexterBot {
        private CancellationTokenSource _tokenSource;

        public DexterDiscord DexterDiscord { get; }

        private string _Token;

        public string Token {
            private get => _Token;
            set {
                _Token = value;
                DisposeToken();
            }
        }

        public DexterBot() {
            DexterDiscord = new DexterDiscord();
        }

        public async Task StartAsync() {
            ConsoleLogger.Log("Starting Dexter. Please wait...");

            _tokenSource = new CancellationTokenSource();

            await DexterDiscord.RunAsync(_Token, _tokenSource.Token);
        }

        public void Stop() {
            ConsoleLogger.Log("Stopping Dexter. Please wait...");

            DisposeToken();

            ConsoleLogger.Log("Dexter has halted successfully!");
        }

        private void DisposeToken() {
            if (_tokenSource is null)
                return;

            DexterDiscord.Disconnected();
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _tokenSource = null;
        }
    }
}
