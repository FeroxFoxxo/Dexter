using Dexter.ConsoleApp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dexter.Core {
    public class DexterBot {
        private CancellationTokenSource _tokenSource;

        public DexterDiscord DexterDiscord;

        private string _Token;

        public string Token {
            private get => _Token;
            set {
                _Token = value;
                Stop(true);
            }
        }

        public DexterBot() {
            DexterDiscord = new DexterDiscord();
        }

        public async Task StartAsync(bool Silent) {
            if (!Silent)
                Console.WriteLine(" Starting Dexter...");

            _tokenSource = new CancellationTokenSource();
            await DexterDiscord.RunAsync(_Token, _tokenSource.Token);
        }

        public void Stop(bool Silent) {
            if (_tokenSource is null)
                return;

            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _tokenSource = null;

            if (!Silent)
                Console.Write(Configuration.PRESS_KEY);
        }
    }
}
