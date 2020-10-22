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

namespace Dexter.Commands {
    public partial class WarningCommands {

        [Command("warnings")]
        [Summary("Returns the records of the specified user.")]
        [Alias("records", "record")]
        [RequireModerator]

        public async Task WarningsCommand(IGuildUser User) {
            Embed[] Embeds = GetWarnings(User, true);

            foreach (Embed Embed in Embeds)
                await Embed.SendEmbed(Context.Channel);
        }

        [Command("warnings")]
        [Summary("Returns your own, personal record of warnings.")]
        [Alias("records", "record")]

        public async Task WarningsCommand() {
            Embed[] Embeds = GetWarnings(Context.Message.Author, false);

            try {
                foreach (Embed Embed in Embeds)
                    await Embed.SendEmbed(Context.Message.Author);

                await Context.BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Sent warnings log.")
                    .WithDescription("Heya! I've sent you a log of your warnings. " +
                        "Please note these records are not indicitive of a mute or ban, and are simply a sign of when we've had to verbally warn you in the chat.")
                    .SendEmbed(Context.Channel);
            } catch (HttpException) {
                await Context.BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to send warnings log!")
                    .WithDescription("Woa, it seems as though I'm not able to send you a log of your warnings! " +
                        "This is usually indicitive of having DMs from the server blocked or me personally! " +
                        "Please note, for the sake of transparency, we often use Dexter to notify you of events that concern you - " +
                        "so it's critical that we're able to message you through Dexter. <3")
                    .SendEmbed(Context.Channel);
            }
        }

        public Embed[] GetWarnings(IUser User, bool ShowIssuer) {
            Warning[] Warnings = WarningsDB.GetWarnings(User.Id);

            if (Warnings.Length <= 0)
                return new Embed[1] {
                    Context.BuildEmbed(EmojiEnum.Love)
                        .WithTitle("No issued warnings!")
                        .WithDescription($"{User.Mention} has a clean slate! Go give them a pat on the back <3")
                        .Build()
                };

            List<Embed> Embeds = new List<Embed>();

            EmbedBuilder CurrentBuilder = Context.BuildEmbed(EmojiEnum.Love)
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
                    Embeds.Add(CurrentBuilder.Build());
                    CurrentBuilder = new EmbedBuilder().AddField(Field).WithColor(Color.Blue);
                }
            }

            Embeds.Add(CurrentBuilder.Build());

            return Embeds.ToArray();
        }

    }
}
