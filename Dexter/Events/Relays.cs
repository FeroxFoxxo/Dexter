using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Databases.Relays;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Events
{

    /// <summary>
    /// The RelayService sends messages to a channel with a specified message delay as a notification.
    /// </summary>
    
    public class Relays : Event
    {

        /// <summary>
        /// The Initialize method hooks up the message recieved event to the relay method.
        /// </summary>
        
        public override void InitializeEvents()
        {
            DiscordShardedClient.MessageReceived += CheckRelay;
        }

        /// <summary>
        /// The CheckRelay event runs on a message sent and counts the messages,
        /// sending the notification if the message count has reached the send variable.
        /// </summary>
        /// <param name="SocketMessage"></param>
        /// <returns></returns>

        public async Task CheckRelay(SocketMessage SocketMessage)
        {
            using var scope = ServiceProvider.CreateScope();

            using var RelayDB = scope.ServiceProvider.GetRequiredService<RelayDB>();

            Relay Relay = RelayDB.Relays.Find(SocketMessage.Channel.Id);

            if (Relay == null)
                return;

            if (Relay.CurrentMessageCount > Relay.MessageInterval)
            {
                Relay.CurrentMessageCount = 0;

                await SocketMessage.Channel.SendMessageAsync(Relay.Message);
            }

            Relay.CurrentMessageCount += 1;

            await RelayDB.SaveChangesAsync();
        }

    }

}
