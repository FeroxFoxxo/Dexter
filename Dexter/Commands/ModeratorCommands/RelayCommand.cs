using Dexter.Attributes.Methods;
using Dexter.Databases.Relays;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// Sends a request to add a relay to a channel, which adds an item to the relay database.
        /// A relay sends a preset message every set number of messages.
        /// </summary>
        /// <remarks>This process requires an administrator approval stage.</remarks>
        /// <param name="MessageInterval">The amount of messages between each message sent by the relay.</param>
        /// <param name="Channel">The channel to target and configure in the database.</param>
        /// <param name="Message">The message to send when the interval condition is met.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("relay")]
        [Summary("Adds a relay in a channel to the database, sending a message every set amount of messages.")]
        [RequireAdministrator]

        public async Task AddRelay(int MessageInterval, ITextChannel Channel, [Remainder] string Message) {
            Relay FindRelay = RelayDB.Relays.AsQueryable().Where(Relay => Relay.ChannelID.Equals(Channel.Id)).FirstOrDefault();

            if (Message.Length > 1500) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable To Add Relay.")
                    .WithDescription($"Heya! Please cut down on the length of your relay. " +
                    $"It should be a maximum of 1500 characters. Currently this character count sits at {Message.Length}")
                    .SendEmbed(Context.Channel);
            }

            if (FindRelay != null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Relay already exists!")
                    .WithDescription($"The relay to the channel {Channel} already exists with the message of {(FindRelay.Message.Length > 300 ? $"{FindRelay.Message.Substring(0, 300)}..." : FindRelay.Message)} at an interval of {FindRelay.MessageInterval}.")
                    .SendEmbed(Context.Channel);
                return;
            }

            await SendForAdminApproval(AddRelayCallback,
                new Dictionary<string, string>() {
                    { "MessageInterval", MessageInterval.ToString() },
                    { "ChannelID", Channel.Id.ToString() },
                    { "Message", Message }
                },
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that `{Message}` should be added to the channel {Channel} with an interval of {MessageInterval} messages.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The relay to `#{Channel}` with the message `{(Message.Length > 100 ? $"{Message.Substring(0, 100)}..." : Message)}` for every {MessageInterval} messages has been suggested!")
                .WithDescription($"Once it has passed admin approval, it will run on this channel.")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Directly adds a new relay to the corresponding database.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing definitions for "MessageInterval", "ChannelID", and "Message".
        /// Each of these values should be parsable to an <c>int</c>, <c>ulong</c> (Channel ID), and <c>string</c> respectively. 
        /// </param>

        public void AddRelayCallback(Dictionary<string, string> Parameters) {
            int MessageInterval = int.Parse(Parameters["MessageInterval"]);
            ulong ChannelID = ulong.Parse(Parameters["ChannelID"]);
            string Message = Parameters["Message"];

            RelayDB.Add(new Relay {
                ChannelID = ChannelID,
                CurrentMessageCount = MessageInterval,
                MessageInterval = MessageInterval,
                Message = Message
            });

            RelayDB.SaveChanges();
        }

    }

}