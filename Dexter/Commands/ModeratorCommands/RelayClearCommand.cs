using Dexter.Attributes.Methods;
using Dexter.Databases.Relays;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// Suggests the removal of a channel's relay from the relevant database.
        /// </summary>
        /// <remarks>This action requires administrator approval.</remarks>
        /// <param name="Channel">The target channel where the relay is to be removed.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("clearrelay")]
        [Summary("Clears a channel's relay in the related database.")]
        [RequireAdministrator]

        public async Task ClearRelay(ITextChannel Channel) {
            Relay FindRelay = RelayDB.Relays.Find(Channel.Id);

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
                Context.User.Id,
                $"{Context.User.GetUserInformation()} has suggested that `{FindRelay.Message}` should be removed from the channel {Channel} which has an interval of {FindRelay.MessageInterval} messages.");

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The relay to `#{Channel}` with the message `{(FindRelay.Message.Length > 100 ? $"{FindRelay.Message.Substring(0, 100)}..." : FindRelay.Message)}` for every {FindRelay.MessageInterval} messages has been suggested for removal!")
                .WithDescription($"Once it has passed admin approval, it will be removed from the database.")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Directly removes the target relay from a given channel's respective database.
        /// </summary>
        /// <param name="Parameters">
        /// A string-string dictionary containing a defition for "ChannelID".
        /// This value should be parsable to a <c>ulong</c> (Channel ID).
        /// </param>

        public void RemoveRelayCallback(Dictionary<string, string> Parameters) {
            ulong ChannelID = ulong.Parse(Parameters["ChannelID"]);

            Relay RelayToRemove = RelayDB.Relays.Find(ChannelID);

            RelayDB.Relays.Remove(RelayToRemove);

            RelayDB.SaveChanges();
        }

    }

}