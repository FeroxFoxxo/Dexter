using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using System;
using Dexter.Databases.Cooldowns;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// The Cooldown Command method runs on COOLDOWN. It takes in the type of entry of cooldown you'd like to apply and sets the cooldown accordingly.
        /// </summary>
        /// <param name="EntryType">The action you wish to apply for the cooldown, either ISSUE or REVOKE.</param>
        /// <param name="User">The user you wish to remove the cooldown from.</param>
        /// <param name="TextChannel">The text channel you want to have the cooldown removed from.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("cooldown")]
        [Summary("Applies a specified action to a user's commission cooldown.\n" +
            "`ISSUE [USER] [CHANNELID]` - issues a cooldown to the user.\n" +
            "`REVOKE [USER] [CHANNELID]` - removes a cooldown from a user.")]
        [Alias("commCooldown", "cooldown")]
        [RequireModerator]

        public async Task CooldownCommand (EntryType EntryType, IUser User, ITextChannel TextChannel) {
            switch (EntryType) {
                case EntryType.Issue:
                    Cooldown IssueCooldown = CooldownDB.Cooldowns.Find($"{User.Id}{TextChannel.Id}");

                    if (IssueCooldown != null)
                        if (IssueCooldown.TimeOfCooldown + CommissionCooldownConfiguration.ChannelCooldowns[TextChannel.Id]["CooldownTime"] < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                            CooldownDB.Cooldowns.Remove(IssueCooldown);
                            CooldownDB.SaveChanges();
                        } else {
                            DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(IssueCooldown.TimeOfCooldown);

                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle($"Unable To Issue {TextChannel} Cooldown.")
                                .WithDescription($"Haiya! The user {User.GetUserInformation()} seems to already be on cooldown. " +
                                    $"This cooldown in was applied on {CooldownTime.ToLongDateString()} at {CooldownTime.ToLongTimeString()}.")
                                .WithCurrentTimestamp()
                                .SendEmbed(Context.Channel);

                            return;
                        }

                    Cooldown NewCooldown = new () {
                        Token = $"{User.Id}{TextChannel.Id}",
                        TimeOfCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    };

                    CooldownDB.Cooldowns.Add(NewCooldown);

                    CooldownDB.SaveChanges();

                    DateTime NewCooldownTime = DateTime.UnixEpoch.AddSeconds(NewCooldown.TimeOfCooldown);

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle($"Added {TextChannel} Cooldown!")
                        .WithDescription($"Successfully added cooldown to {User.GetUserInformation()} " +
                            $"at {NewCooldownTime.ToLongTimeString()}, {NewCooldownTime.ToLongDateString()}.")
                        .WithCurrentTimestamp()
                        .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User,
                            BuildEmbed(EmojiEnum.Love)
                                .WithTitle($"{TextChannel} Cooldown Issued.")
                                .WithDescription($"Haiya! We've given you a cooldown set at {NewCooldownTime.ToLongTimeString()}, {NewCooldownTime.ToLongDateString()}. <3")
                                .WithCurrentTimestamp()
                        );

                    break;
                case EntryType.Revoke:
                    Cooldown RevokeCooldown = CooldownDB.Cooldowns.Find($"{User.Id}{TextChannel.Id}");

                    if (RevokeCooldown != null) {
                        CooldownDB.Cooldowns.Remove(RevokeCooldown);
                        CooldownDB.SaveChanges();

                        DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(RevokeCooldown.TimeOfCooldown);

                        await BuildEmbed(EmojiEnum.Unknown)
                            .WithTitle($"Removed {TextChannel} Cooldown!")
                            .WithDescription($"Successfully removed cooldown from {User.GetUserInformation()}, " +
                                $"of whose cooldown used to be {CooldownTime.ToLongTimeString()}, {CooldownTime.ToLongDateString()}.")
                            .WithCurrentTimestamp()
                            .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User, BuildEmbed(EmojiEnum.Love)
                                .WithTitle($"{TextChannel} Cooldown Revoked.")
                                .WithDescription($"Haiya! We've revoked your cooldown set at {CooldownTime.ToLongTimeString()}, " +
                                $"{CooldownTime.ToLongDateString()}. You should now be able to re-send your informtion into the channel. <3")
                                .WithCurrentTimestamp()
                        );
                    } else {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle($"Unable To Remove {TextChannel} Cooldown.")
                            .WithDescription($"Haiya! I was unable to remove the cooldown from {User.GetUserInformation()}. " +
                                $"Are you sure this is the correct ID or that they have a cooldown lain against them?")
                            .WithCurrentTimestamp()
                            .SendEmbed(Context.Channel);
                    }

                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle($"Unable To Modify {TextChannel} Cooldown")
                        .WithDescription($"The argument {EntryType} does not exist as an option to use on this command!")
                        .SendEmbed(Context.Channel);
                    break;
            }

        }

    }

}
