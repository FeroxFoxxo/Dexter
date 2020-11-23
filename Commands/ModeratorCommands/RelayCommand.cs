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
                Context.Message.Author.Id,
                $"{Context.Message.Author.GetUserInformation()} has suggested that `{Message}` should be added to the channel {Channel} with an interval of {MessageInterval} messages.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The relay to `#{Channel}` with the message `{(Message.Length > 300 ? $"{Message.Substring(0, 300)}..." : Message)}` for every {MessageInterval} messages has been suggested!")
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


        [Command("clearrelay")]
        [Summary("Clears a channel's relay in the related database.")]
        [RequireAdministrator]

        public async Task ClearRelay(ITextChannel Channel) {
            Relay FindRelay = RelayDB.Relays.AsQueryable().Where(Relay => Relay.ChannelID.Equals(Channel.Id)).FirstOrDefault();

            if (FindRelay == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Relay already exists!")
                    .WithDescription($"The relay to the channel {Channel} does not exist and thus can not be removed!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await SendForAdminApproval(RemoveRelayCallback,
                new Dictionary<string, string>() {
                    { "ChannelID", Channel.Id.ToString() }
                },
                Context.Message.Author.Id,
                $"{Context.Message.Author.GetUserInformation()} has suggested that `{FindRelay.Message}` should be removed from the channel {Channel} which has an interval of {FindRelay.MessageInterval}.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The relay to `#{Channel}` with the message `{(FindRelay.Message.Length > 300 ? $"{FindRelay.Message.Substring(0, 300)}..." : FindRelay.Message)}` for every {FindRelay.MessageInterval} messages has been suggested for removal!")
                .WithDescription($"Once it has passed admin approval, it will be removed from the database.")
                .SendEmbed(Context.Channel);
        }

        public async Task RemoveRelayCallback(Dictionary<string, string> Parameters) {
            ulong ChannelID = ulong.Parse(Parameters["ChannelID"]);

            Relay RelayToRemove = RelayDB.Relays.AsQueryable().Where(Relay => Relay.ChannelID.Equals(ChannelID)).FirstOrDefault();

            RelayDB.Relays.Remove(RelayToRemove);
        }

    }

}