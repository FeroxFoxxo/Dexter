using Dexter.Abstractions;
using Dexter.Databases.Relay;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Services {

    public class RelayService : InitializableModule {

        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly RelayDB RelayDB;

        public RelayService (DiscordSocketClient DiscordSocketClient, RelayDB RelayDB) {
            this.DiscordSocketClient = DiscordSocketClient;
            this.RelayDB = RelayDB;
        }

        public override void Initialize() {
            DiscordSocketClient.MessageReceived += CheckRelay;
        }

        public async Task CheckRelay(SocketMessage SocketMessage) {
            Relay Relay = RelayDB.Relays.AsQueryable().Where(Relay => Relay.ChannelID.Equals(SocketMessage.Channel.Id)).FirstOrDefault();

            if (Relay == null)
                return;

            if (Relay.CurrentMessageCount > Relay.MessageInterval) {
                Relay.CurrentMessageCount = 0;

                await SocketMessage.Channel.SendMessageAsync(Relay.Message);
            }

            Relay.CurrentMessageCount += 1;

            await RelayDB.SaveChangesAsync();
        }

    }

}
