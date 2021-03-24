using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Infractions;
using Discord;
using Discord.Commands;
using Discord.Net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Linq;
using Humanizer;
using Discord.WebSocket;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// Sends an embed with the records of infractions of a specified user.
        /// </summary>
        /// <remarks>If the user is different from <c>Context.User</c>, it is Staff-only.</remarks>
        /// <param name="UserID">The target user to query.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("records")]
        [Summary("Returns a record of infractions for a set user based on their ID.")]
        [Alias("warnings", "record", "warns", "mutes")]
        [RequireModerator]
        [BotChannel]

        public async Task InfractionsCommand(ulong UserID) {
            IUser User = DiscordSocketClient.GetUser(UserID);
            
            if (User == null) {
                EmbedBuilder[] Warnings = GetWarnings(UserID, Context.User.Id, $"<@{UserID}>", $"Unknown ({UserID})", true);

                if (Warnings.Length > 1)
                    await CreateReactionMenu(Warnings, Context.Channel);
                else
                    await Warnings.FirstOrDefault().WithCurrentTimestamp().SendEmbed(Context.Channel);
            } else
                await InfractionsCommand(User);
        }

        /// <summary>
        /// The InfractionsCommand runs on RECORDS and will send a DM to the author of the message if the command is run in a bot
        /// channel and no user is specified of their own infractions. If a user is specified and the author is a moderator it will
        /// proceed to print out all the infractions of that specified member into the channel the command had been sent into.
        /// </summary>
        /// <param name="User">The User field specifies the user that you wish to get the infractions of.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("records")]
        [Summary("Returns a record of infractions for a set user or your own.")]
        [Alias("warnings", "record", "warns", "mutes")]
        [BotChannel]

        public async Task InfractionsCommand([Optional] IUser User) {
            bool IsUserSpecified = User != null;

            if (IsUserSpecified) {
                if ((Context.User as IGuildUser).GetPermissionLevel(DiscordSocketClient, BotConfiguration) >= PermissionLevel.Moderator) {
                    EmbedBuilder[] Warnings = GetWarnings(User.Id, Context.User.Id, User.Mention, User.Username, true);

                    if (Warnings.Length > 1)
                        await CreateReactionMenu(Warnings, Context.Channel);
                    else
                        await Warnings.FirstOrDefault().WithCurrentTimestamp().SendEmbed(Context.Channel);
                } else {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Halt! Don't go there-")
                        .WithDescription("Heya! To run this command with a user specified, you will need to be a moderator. <3")
                        .SendEmbed(Context.Channel);
                }
            } else {
                EmbedBuilder[] Embeds = GetWarnings(Context.User.Id, Context.User.Id, Context.User.Mention, Context.User.Username, false);

                try {
                    foreach (EmbedBuilder Embed in Embeds)
                        await Embed.SendEmbed(await Context.User.GetOrCreateDMChannelAsync());

                    if (Context.Channel.GetType() == typeof(SocketDMChannel))
                        return;

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Sent infractions log.")
                        .WithDescription("Heya! I've sent you a log of your infractions. Feel free to take a look over them in your own time! <3")
                        .SendEmbed(Context.Channel);
                } catch (HttpException) {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to send infractions log!")
                        .WithDescription("Woa, it seems as though I'm not able to send you a log of your infractions! " +
                            "This is usually indicitive of having DMs from the server blocked or me personally. " +
                            "Please note, for the sake of transparency, we often use Dexter to notify you of events that concern you - " +
                            "so it's critical that we're able to message you through Dexter. <3")
                        .SendEmbed(Context.Channel);
                }
            }
        }

        /// <summary>
        /// The GetWarnings method returns an array of embeds detailing the user's warnings, time of warning, and moderator (if enabled).
        /// </summary>
        /// <param name="User">The user whose warnings you wish to receive.</param>
        /// <param name="RunBy">The user who has run the given warnings command.</param>
        /// <param name="Mention">The stringified mention for the target user.</param>
        /// <param name="Username">The target user's username in the given context.</param>
        /// <param name="ShowIssuer">Whether or not the moderators should be shown in the log. Enabled for moderators, disabled for DMed records.</param>
        /// <returns>An array of embeds containing the given user's warnings.</returns>
        
        public EmbedBuilder[] GetWarnings(ulong User, ulong RunBy, string Mention, string Username, bool ShowIssuer) {
            Infraction[] Infractions = InfractionsDB.GetInfractions(User);

            if (Infractions.Length <= 0)
                return new EmbedBuilder[1] {
                    BuildEmbed(EmojiEnum.Love)
                        .WithTitle("No issued infractions!")
                        .WithDescription($"{Mention} has a clean slate!\n" +
                        $"Go give {(User == RunBy ? "yourself" : "them")} a pat on the back. <3")
                };

            List<EmbedBuilder> Embeds = new ();

            DexterProfile DexterProfile = InfractionsDB.GetOrCreateProfile(User);

            EmbedBuilder CurrentBuilder = BuildEmbed(EmojiEnum.Love)
                .WithTitle($"{Username}'s Infractions - {Infractions.Length} {(Infractions.Length == 1 ? "Entry" : "Entries")} and {DexterProfile.InfractionAmount} {(DexterProfile.InfractionAmount == 1 ? "Point" : "Points")}.")
                .WithDescription($"All times are displayed in {TimeZoneInfo.Local.DisplayName}");

            for (int Index = 0; Index < Infractions.Length; Index++) {
                Infraction Infraction = Infractions[Index];

                IUser Issuer = Client.GetUser(Infraction.Issuer);

                long TimeOfIssue = Infraction.TimeOfIssue;

                DateTimeOffset Time = DateTimeOffset.FromUnixTimeSeconds(TimeOfIssue > 253402300799 ? TimeOfIssue / 1000 : TimeOfIssue);

                EmbedFieldBuilder Field = new EmbedFieldBuilder()
                    .WithName($"{(Infraction.InfractionTime == 0 ? "Warning" : $"{TimeSpan.FromSeconds(Infraction.InfractionTime).Humanize().Titleize()} Mute")} {Index + 1} (ID {Infraction.InfractionID}), {(Infraction.PointCost > 0 ? "-" : "")}{Infraction.PointCost} {(Infraction.PointCost == 1 ? "Point" : "Points")}.")
                    .WithValue($"{(ShowIssuer ? $":cop: {(Issuer != null ? Issuer.GetUserInformation() : $"Unknown ({Infraction.Issuer})")}\n" : "")}" +
                        $":calendar: {Time:M/d/yyyy h:mm:ss}\n" +
                        $":notepad_spiral: {Infraction.Reason}"
                    );

                if (Index % 5 == 0 && Index != 0) {
                    Embeds.Add(CurrentBuilder);
                    CurrentBuilder = new EmbedBuilder().AddField(Field).WithColor(Color.Green);
                } else {
                    try {
                        CurrentBuilder.AddField(Field);
                    } catch (Exception) {
                        Embeds.Add(CurrentBuilder);
                        CurrentBuilder = new EmbedBuilder().AddField(Field).WithColor(Color.Green);
                    }
                }
            }

            Embeds.Add(CurrentBuilder);

            return Embeds.ToArray();
        }

    }

}
