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
            ConsoleLogger.Log(Configuration.START_DEXTER);

            _tokenSource = new CancellationTokenSource();

            await DexterDiscord.RunAsync(_Token, _tokenSource.Token);
        }

        public void Stop() {
            ConsoleLogger.Log(Configuration.STOP_DEXTER);

            DisposeToken();

            ConsoleLogger.Log(Configuration.STOPPED_DEXTER);
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
