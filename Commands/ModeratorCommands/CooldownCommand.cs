using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Linq;
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
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        [Command("comCooldown")]
        [Summary("Applies a specified action to a user's commission cooldown. This action can either be to ISSUE or REVOKE a cooldown.")]
        [Alias("commCooldown", "cooldown")]
        [RequireModerator]

        public async Task CooldownCommand (EntryType EntryType, IUser User) {
            switch (EntryType) {
                case EntryType.Issue:
                    Cooldown IssueCooldown = CooldownDB.CommissionCooldowns.AsQueryable()
                        .Where(Cooldown => Cooldown.Identifier == Context.Message.Author.Id.ToString()).FirstOrDefault();

                    if (IssueCooldown != null)
                        if (IssueCooldown.TimeOfCooldown + CommissionCooldownConfiguration.CommissionCornerCooldown < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                            CooldownDB.CommissionCooldowns.Remove(IssueCooldown);
                            await CooldownDB.SaveChangesAsync();
                            IssueCooldown = null;
                        }

                    if (IssueCooldown != null) {
                        DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(IssueCooldown.TimeOfCooldown);

                        await BuildEmbed(EmojiEnum.Love)
                            .WithTitle("Unable to issue cooldown.")
                            .WithDescription($"Haiya! The user {User.GetUserInformation()} seems to already be on cooldown. " +
                                $"This cooldown was applied on {CooldownTime.ToLongDateString()} at {CooldownTime.ToLongTimeString()}.")
                            .WithCurrentTimestamp()
                            .SendEmbed(Context.Channel);
                    } else {
                        Cooldown NewCooldown = new () {
                            Identifier = User.Id.ToString(),
                            TimeOfCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };

                        CooldownDB.CommissionCooldowns.Add(NewCooldown);

                        await CooldownDB.SaveChangesAsync();

                        DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(NewCooldown.TimeOfCooldown);

                        EmbedBuilder Embed = BuildEmbed(EmojiEnum.Love)
                            .WithTitle("Added Commission Cooldown!")
                            .WithDescription($"Successfully added commission cooldown to {User.GetUserInformation()} " +
                                $"at {CooldownTime.ToLongTimeString()}, {CooldownTime.ToLongDateString()}.")
                            .WithCurrentTimestamp();

                        try {
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Cooldown Issued.")
                                .WithDescription($"Haiya! We've given you a commission cooldown, set at {CooldownTime.ToLongTimeString()}, {CooldownTime.ToLongDateString()}. <3")
                                .WithCurrentTimestamp()
                                .SendEmbed(await User.GetOrCreateDMChannelAsync());

                            Embed.AddField("Successfully notified.", "The DM was successfully sent!");
                        } catch (HttpException) {
                            Embed.AddField("Failed to notify.", "This fluff may have either blocked DMs from the server or me!");
                        }

                        await Embed.SendEmbed(Context.Channel);
                    }

                    break;
                case EntryType.Revoke:
                    Cooldown RevokeCooldown = CooldownDB.CommissionCooldowns.AsQueryable()
                        .Where(Cooldown => Cooldown.Identifier == Context.Message.Author.Id.ToString()).FirstOrDefault();

                    if (RevokeCooldown != null) {
                        CooldownDB.CommissionCooldowns.Remove(RevokeCooldown);
                        await CooldownDB.SaveChangesAsync();

                        DateTime CooldownTime = DateTime.UnixEpoch.AddSeconds(RevokeCooldown.TimeOfCooldown);

                        EmbedBuilder Embed = BuildEmbed(EmojiEnum.Unknown)
                            .WithTitle("Removed Commission Cooldown!")
                            .WithDescription($"Successfully removed commission cooldown from {User.GetUserInformation()}, " +
                                $"of whose cooldown used to be {CooldownTime.ToLongTimeString()}, {CooldownTime.ToLongDateString()}.")
                            .WithCurrentTimestamp();

                        try {
                            await BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Cooldown Revoked.")
                                .WithDescription($"Haiya! We've revoked your commission cooldown set at {CooldownTime.ToLongTimeString()}, " +
                                $"{CooldownTime.ToLongDateString()}. You should now be able to re-send your commission informtion into the channel. <3")
                                .WithCurrentTimestamp()
                                .SendEmbed(await User.GetOrCreateDMChannelAsync());

                            Embed.AddField("Successfully notified.", "The DM was successfully sent!");
                        } catch (HttpException) {
                            Embed.AddField("Failed to notify.", "This fluff may have either blocked DMs from the server or me!");
                        }

                        await Embed.SendEmbed(Context.Channel);
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
                    throw new ArgumentOutOfRangeException(EntryType.ToString());
            }

        }

    }

}