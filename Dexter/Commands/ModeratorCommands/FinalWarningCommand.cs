using Dexter.Attributes.Methods;
using Dexter.Databases.FinalWarns;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Discord.Net;
using Humanizer;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands {
    partial class ModeratorCommands {

        /// <summary>
        /// Issues a final warning to a target <paramref name="User"/>, mutes then for <paramref name="MuteDuration"/>, and adds a detailed entry about the final warn to the Final Warns database.
        /// </summary>
        /// <param name="User">The target user to final warn.</param>
        /// <param name="MuteDuration">The duration of the mute attached to the final warn.</param>
        /// <param name="Reason">The reason behind the final warn.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("finalwarn")]
        [Summary("Issues a final warning to a user, mutes them and records the final warn.")]
        [Alias("warnfinal")]
        [RequireModerator]
        [BotChannel]

        public async Task IssueFinalWarn(IGuildUser User, TimeSpan MuteDuration, [Remainder] string Reason) {
            
            if(FinalWarnsDB.IsUserFinalWarned(User)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("User is already final warned!")
                    .WithDescription($"The target user, <@{User.Id}> ({User.Id}), already has an active final warn. If you wish to overwrite this final warn, first remove the already existing one.")
                    .SendEmbed(Context.Channel);

                return;
            }

            FinalWarnsDB.SetOrCreateFinalWarn(Context.User as IGuildUser, User, MuteDuration, Reason);

            await this.MuteUser(User, MuteDuration);

            try {
                await BuildEmbed(EmojiEnum.Sign)
                    .WithTitle($"You were issued a **final warning** from {Context.Guild.Name}!")
                    .WithDescription($"As part of the final warning, you were muted for {MuteDuration.Humanize()}.")
                    .AddField("Reason", Reason)
                    .WithCurrentTimestamp()
                    .SendEmbed(await User.GetOrCreateDMChannelAsync());

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Message sent successfully!")
                    .WithDescription($"The target user, <@{User.Id}> ({User.Id}), has been informed of their current status.")
                    .AddField("Mute Duration", MuteDuration.Humanize())
                    .AddField("Reason", Reason)
                    .SendEmbed(Context.Channel);
            }
            catch (HttpException) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Message failed!")
                    .WithDescription($"The target user, <@{User.Id}> ({User.Id}), might have DMs disabled or might have blocked me... :c\nThe final warning has been recorded to the database regardless.")
                    .SendEmbed(Context.Channel);
            }
        }

        /// <summary>
        /// Revokes a final warning if an active one exists for target <paramref name="User"/>, and informs them of this.
        /// </summary>
        /// <param name="User">The user whose final warn is to be revoked.</param>
        /// <param name="Reason">The reason why the final warn was revoked. (Optional)</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("delfinalwarn")]
        [Summary("Revokes a user's final warn, though it remains in records.")]
        [Alias("revokefinalwarn", "deletefinalwarn", "removefinalwarn")]
        [RequireModerator]
        [BotChannel]

        public async Task RevokeFinalWarn(IGuildUser User, [Remainder] string Reason = "") {
            if(!FinalWarnsDB.TryRevokeFinalWarn(User)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("No active final warn found!")
                    .WithDescription($"Wasn't able to revoke final warning for user <@{User.Id}> ({User.Id}), since no active warn exists.")
                    .SendEmbed(Context.Channel);

                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Final warn successfully revoked.")
                .WithDescription($"Successfully revoked final warning for user <@{User.Id}> ({User.Id}). You can still query records about this final warn.")
                .AddField(Reason.Length > 0, "Reason:", Reason)
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Your final warn has been revoked!")
                .WithDescription("The staff team has convened and decided to revoke your final warn. Be careful, you can't receive more than two final warns! A third final warn is an automatic ban.")
                .AddField(Reason.Length > 0, "Reason:", Reason)
                .WithCurrentTimestamp()
                .SendEmbed(await User.GetOrCreateDMChannelAsync());
        }

        /// <summary>
        /// Gets the information for a final warn attached to <paramref name="User"/>, if any.
        /// </summary>
        /// <param name="User">The user to query for in the final warnings database.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("getfinalwarn")]
        [Summary("Gets the relevant information for a user's final warn.")]
        [Alias("queryfinalwarn")]
        [RequireModerator]
        [BotChannel]

        public async Task GetFinalWarn(IGuildUser User) {
            FinalWarn Warn = FinalWarnsDB.FinalWarns.Find(User.Id);

            if (Warn == null) {
                await BuildEmbed(EmojiEnum.Wut)
                    .WithTitle("Target user is not final warned!")
                    .WithDescription($"User <@{User.Id}> ({User.Id}) has no final warns to their name!")
                    .SendEmbed(Context.Channel);

                return;
            }

            await BuildEmbed(EmojiEnum.Sign)
                .WithTitle("Final warn found!")
                .WithDescription($"User <@{User.Id}> ({User.Id}) has {(Warn.EntryType == EntryType.Revoke ? "a **revoked**" : "an **active**")} final warn!")
                .AddField("Reason:", Warn.Reason)
                .AddField("Issued by:", $"<@{Warn.IssuerID}> ({Warn.IssuerID})")
                .AddField("Mute Duration:", TimeSpan.FromSeconds(Warn.MuteDuration).Humanize())
                .AddField("Issued on:", DateTimeOffset.FromUnixTimeSeconds(Warn.IssueTime).Humanize())
                .SendEmbed(Context.Channel);
        }
    }
}
