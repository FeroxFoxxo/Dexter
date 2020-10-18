using Dexter.Core.Attributes;
using Dexter.Core.Enums;
using Dexter.Core.Extensions;
using Dexter.Databases.Warnings;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.Commands.WarningCommands {
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

            foreach (Embed Embed in Embeds)
                await Embed.SendEmbed(Context.Message.Author);
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
                    $":calendar: {Time.ToString("M/d/yyyy h:mm:ss")}\n" +
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
