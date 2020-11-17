using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Databases.Warnings;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Dexter.Commands {
    public partial class WarningCommands {

        /// <summary>
        /// The WarningsCommand runs on WARNINGS and will send a DM to the author of the message if the command is run in a bot
        /// channel and no user is specified of their own warnings. If a user is specified and the author is a moderator it will
        /// proceed to print out all the warnings of that specified member into the channel the command had been sent into.
        /// </summary>
        /// <param name="User">The User field specifies the user that you wish to get the warnings of.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>

        [Command("warnings")]
        [Summary("Returns a record of warnings.")]
        [Alias("records", "record")]
        [BotChannel]

        public async Task WarningsCommand([Optional] [RequireModeratorParameter] IUser User) {
            bool IsUserSpecified = User != null;

            if (IsUserSpecified) {
                EmbedBuilder[] Embeds = GetWarnings(User, Context.Message.Author, true);

                foreach (EmbedBuilder Embed in Embeds)
                    await Embed.SendEmbed(Context.Channel);
            } else {
                EmbedBuilder[] Embeds = GetWarnings(Context.Message.Author, Context.Message.Author, false);

                try {
                    foreach (EmbedBuilder Embed in Embeds)
                        await Embed.SendEmbed(Context.Message.Author);

                    await BuildEmbed(EmojiEnum.Love)
                        .WithTitle("Sent warnings log.")
                        .WithDescription("Heya! I've sent you a log of your warnings. " +
                            "Please note these records are not indicitive of a mute or ban, and are simply a sign of when we've had to verbally warn you in the chat.")
                        .SendEmbed(Context.Channel);
                } catch (HttpException) {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Unable to send warnings log!")
                        .WithDescription("Woa, it seems as though I'm not able to send you a log of your warnings! " +
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
        /// <param name="User">The user of whose warnings you wish to recieve.</param>
        /// <param name="RunBy">The user who has run the given warnings command.</param>
        /// <param name="ShowIssuer">Whether or not the moderators should be shown in the log. Enabled for moderators, disabled for DMed records.</param>
        /// <returns>An array of embeds containing the given users warnings.</returns>
        
        public EmbedBuilder[] GetWarnings(IUser User, IUser RunBy, bool ShowIssuer) {
            Warning[] Warnings = WarningsDB.GetWarnings(User.Id);

            if (Warnings.Length <= 0)
                return new EmbedBuilder[1] {
                    BuildEmbed(EmojiEnum.Love)
                        .WithTitle("No issued warnings!")
                        .WithDescription($"{User.Mention} has a clean slate! Go give {(User.Id == RunBy.Id ? "yourself" : "them")} a pat on the back <3")
                };

            List<EmbedBuilder> Embeds = new ();

            EmbedBuilder CurrentBuilder = BuildEmbed(EmojiEnum.Love)
                .WithTitle($"{User.Username}'s Warnings - {Warnings.Length} {(Warnings.Length == 1 ? "Entry" : "Entries")}")
                .WithDescription($"All times are displayed in {TimeZoneInfo.Local.DisplayName}");

            for(int Index = 0; Index < Warnings.Length; Index++) {
                SocketGuildUser Issuer = Context.Guild.GetUser(Warnings[Index].Issuer);

                DateTimeOffset Time = DateTimeOffset.FromUnixTimeSeconds(Warnings[Index].TimeOfIssue);

                EmbedFieldBuilder Field = new EmbedFieldBuilder()
                    .WithName($"Warning {Index} - ID {Warnings[Index].WarningID}")
                    .WithValue($"{(ShowIssuer ? $":cop: {(Issuer != null ? Issuer.GetUserInformation() : "Unknown")}\n" : "")}" +
                    $":calendar: {Time:M/d/yyyy h:mm:ss}\n" +
                    $":notepad_spiral: {Warnings[Index].Reason}");

                try {
                    CurrentBuilder.AddField(Field);
                } catch (Exception) {
                    Embeds.Add(CurrentBuilder);
                    CurrentBuilder = new EmbedBuilder().AddField(Field).WithColor(Color.Blue);
                }
            }

            Embeds.Add(CurrentBuilder);

            return Embeds.ToArray();
        }

    }
}
