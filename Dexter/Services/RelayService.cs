using Dexter.Abstractions;
using Dexter.Databases.Relays;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Dexter.Services {

    public class RelayService : Service {

        public RelayDB RelayDB { get; set; }

        public override void Initialize() {
            DiscordSocketClient.MessageReceived += CheckRelay;
        }

        public async Task CheckRelay(SocketMessage SocketMessage) {
            Relay Relay = RelayDB.Relays.Find(SocketMessage.Channel.Id);

            if (Relay == null)
                return;

            if (Relay.CurrentMessageCount > Relay.MessageInterval) {
                Relay.CurrentMessageCount = 0;

                await SocketMessage.Channel.SendMessageAsync(Relay.Message);
            }

            Relay.CurrentMessageCount += 1;

            RelayDB.SaveChanges();
        }

    }

}
