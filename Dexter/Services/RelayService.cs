using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Databases.Relays;
using Discord.WebSocket;

namespace Dexter.Services
{

    /// <summary>
    /// The RelayService sends messages to a channel with a specified message delay as a notification.
    /// </summary>
    
    public class RelayService : Service
    {

        /// <summary>
        /// The Relay DB specifies the notifications and in which channels they are in.
        /// </summary>
        
        public RelayDB RelayDB { get; set; }

        /// <summary>
        /// The Initialize method hooks up the message recieved event to the relay method.
        /// </summary>
        
        public override void Initialize()
        {
            DiscordSocketClient.MessageReceived += CheckRelay;
        }

        /// <summary>
        /// The CheckRelay event runs on a message sent and counts the messages,
        /// sending the notification if the message count has reached the send variable.
        /// </summary>
        /// <param name="SocketMessage"></param>
        /// <returns></returns>

        public async Task CheckRelay(SocketMessage SocketMessage)
        {
            Relay Relay = RelayDB.Relays.Find(SocketMessage.Channel.Id);

            if (Relay == null)
                return;

            if (Relay.CurrentMessageCount > Relay.MessageInterval)
            {
                Relay.CurrentMessageCount = 0;

                await SocketMessage.Channel.SendMessageAsync(Relay.Message);
            }

            Relay.CurrentMessageCount += 1;

            RelayDB.SaveChanges();
        }

    }

}
