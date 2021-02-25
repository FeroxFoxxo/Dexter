using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using System;
using Dexter.Databases.Cooldowns;
using Discord.Net;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// The Cooldown Command method runs on COMCOOLDOWN. It takes in the type of entry of cooldown you'd like to apply and sets the cooldown accordingly.
        /// </summary>
        /// <param name="EntryType">The action you wish to apply for the cooldown, either ISSUE or REVOKE.</param>
        /// <param name="User">The user you wish to remove the cooldown from.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("commCooldown")]
        [Summary("Applies a specified action to a user's commission cooldown.\n" +
            "`ISSUE [USER]` - issues a cooldown to the user.\n" +
            "`REVOKE [USER]` - removes a cooldown from a user.")]
        [Alias("commCooldown", "cooldown")]
        [RequireModerator]

        public async Task CooldownCommand (EntryType EntryType, IUser User) {
            switch (EntryType) {
                case EntryType.Issue:
                    Cooldown IssueCooldown = CooldownDB.Cooldowns.Find($"{User.Id}{CommissionCooldownConfiguration.CommissionsCornerID}");

                    if (IssueCooldown != null)
                        if (IssueCooldown.TimeOfCooldown + CommissionCooldownConfiguration.CommissionCornerCooldown < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                            CooldownDB.Cooldowns.Remove(IssueCooldown);
                            CooldownDB.SaveChanges();
                        } else {
                            DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(IssueCooldown.TimeOfCooldown);

                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Unable to issue cooldown.")
                                .WithDescription($"Haiya! The user {User.GetUserInformation()} seems to already be on cooldown. " +
                                    $"This cooldown was applied on {CooldownTime.ToLongDateString()} at {CooldownTime.ToLongTimeString()}.")
                                .WithCurrentTimestamp()
                                .SendEmbed(Context.Channel);

                            return;
                        }

                    Cooldown NewCooldown = new () {
                        Token = $"{User.Id}{CommissionCooldownConfiguration.CommissionsCornerID}",
                        TimeOfCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    };

                    CooldownDB.Cooldowns.Add(NewCooldown);

                    CooldownDB.SaveChanges();

                    DateTime NewCooldownTime = DateTime.UnixEpoch.AddSeconds(NewCooldown.TimeOfCooldown);

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Added Commission Cooldown!")
                        .WithDescription($"Successfully added commission cooldown to {User.GetUserInformation()} " +
                            $"at {NewCooldownTime.ToLongTimeString()}, {NewCooldownTime.ToLongDateString()}.")
                        .WithCurrentTimestamp()
                        .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User,
                            BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Cooldown Issued.")
                                .WithDescription($"Haiya! We've given you a commission cooldown, set at {NewCooldownTime.ToLongTimeString()}, {NewCooldownTime.ToLongDateString()}. <3")
                                .WithCurrentTimestamp()
                        );

                    break;
                case EntryType.Revoke:
                    Cooldown RevokeCooldown = CooldownDB.Cooldowns.Find($"{User.Id}{CommissionCooldownConfiguration.CommissionsCornerID}");

                    if (RevokeCooldown != null) {
                        CooldownDB.Cooldowns.Remove(RevokeCooldown);
                        CooldownDB.SaveChanges();

                        DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(RevokeCooldown.TimeOfCooldown);

                        await BuildEmbed(EmojiEnum.Unknown)
                            .WithTitle("Removed Commission Cooldown!")
                            .WithDescription($"Successfully removed commission cooldown from {User.GetUserInformation()}, " +
                                $"of whose cooldown used to be {CooldownTime.ToLongTimeString()}, {CooldownTime.ToLongDateString()}.")
                            .WithCurrentTimestamp()
                            .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User, BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Cooldown Revoked.")
                                .WithDescription($"Haiya! We've revoked your commission cooldown set at {CooldownTime.ToLongTimeString()}, " +
                                $"{CooldownTime.ToLongDateString()}. You should now be able to re-send your commission informtion into the channel. <3")
                                .WithCurrentTimestamp()
                        );
                    } else {
                        await BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to remove cooldown.")
                            .WithDescription($"Haiya! I was unable to remove the cooldown from {User.GetUserInformation()}. " +
                                $"Are you sure this is the correct ID or that they have a commission cooldown lain against them?")
                            .WithCurrentTimestamp()
                            .SendEmbed(Context.Channel);
                    }

                    break;
                default:
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable To Modify Cooldown")
                        .WithDescription($"The argument {EntryType} does not exist as an option to use on this command!")
                        .SendEmbed(Context.Channel);
                    break;
            }

        }

    }

}
