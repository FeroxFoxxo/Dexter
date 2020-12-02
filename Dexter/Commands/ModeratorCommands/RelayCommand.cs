using Dexter.Attributes;
using Dexter.Databases.Relay;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        [Command("relay")]
        [Summary("Adds a relay in a channel to the database, sending a message every set amount of messages.")]
        [RequireAdministrator]

        public async Task AddRelay(int MessageInterval, ITextChannel Channel, [Remainder] string Message) {
            Relay FindRelay = RelayDB.Relays.AsQueryable().Where(Relay => Relay.ChannelID.Equals(Channel.Id)).FirstOrDefault();

            if (Message.Length > 1500)
                throw new InvalidOperationException($"Heya! Please cut down on the length of your relay. " +
                    $"It should be a maximum of 1500 characters. Currently this character count sits at {Message.Length}");

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

        public async Task AddRelayCallback(Dictionary<string, string> Parameters) {
            int MessageInterval = int.Parse(Parameters["MessageInterval"]);
            ulong ChannelID = ulong.Parse(Parameters["ChannelID"]);
            string Message = Parameters["Message"];

            RelayDB.Add(new Relay {
                ChannelID = ChannelID,
                CurrentMessageCount = MessageInterval,
                MessageInterval = MessageInterval,
                Message = Message
            });

            await RelayDB.SaveChangesAsync();
        }

    }

}