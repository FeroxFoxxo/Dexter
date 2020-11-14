using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class GreetFurCommands {

        [Command("greetfur")]
        [Summary("Gets the GreetFur's Activity for the fortnight.")]
        [BotChannel]

        public async Task GreetFurCommand() {
            Spreadsheet Spreadsheet = await SheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            Sheet CurrentFortnight = Spreadsheet.Sheets
                .Where(Sheet => Sheet.Properties.Title == GreetFurConfiguration.FortnightSpreadsheet)
                .FirstOrDefault();

            ValueRange Columns = await SheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
                $"{CurrentFortnight.Properties.Title}!{GreetFurConfiguration.IDColumnIndex}1:{CurrentFortnight.Properties.GridProperties.RowCount}")
                .ExecuteAsync();

            int IndexOfUser = -1;

            for (int Index = 0; Index < Columns.Values.Count; Index++) {
                if (Columns.Values[Index][0].Equals(Context.Message.Author.Id))
                    IndexOfUser = Index + 1;
            }

            if (IndexOfUser == -1) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to Find GreetFur")
                    .WithDescription("Haiya, it seems as if you're not in the GreetFur database! Are you sure you're a GreetFur? <3")
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
                return;
            }

            ValueRange Rows = await SheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
                $"{CurrentFortnight.Properties.Title}!A{IndexOfUser}:{IntToLetters(CurrentFortnight.Properties.GridProperties.ColumnCount)}")
                .ExecuteAsync();

            IList<object> Information = Rows.Values[0];

            decimal Yes = int.Parse(Information[GreetFurConfiguration.Information["Yes"]].ToString());
            decimal No = int.Parse(Information[GreetFurConfiguration.Information["No"]].ToString());

            decimal Ratio = Yes + No;

            decimal Activity;

            if (Ratio > 0)
                Activity = Math.Round(Yes / Ratio * 100);
            else
                Activity = 100;

            string Notes = Information[GreetFurConfiguration.Information["Notes"]].ToString();

            await BuildEmbed(EmojiEnum.Love)
                .WithAuthor(Context.Message.Author)
                .WithTitle("GreetFur Activity")
                .WithDescription($"**Name:** {Context.Message.Author.GetUserInformation()}\n" +
                                 $"**Manager:** {Information[GreetFurConfiguration.Information["Manager"]]}\n" +
                                 (string.IsNullOrEmpty(Notes) ? "" : $"**Notes:** {Notes}"))
                .AddField("Yes", Information[GreetFurConfiguration.Information["Yes"]], true)
                .AddField("No", Information[GreetFurConfiguration.Information["No"]], true)
                .AddField("Exempts", Information[GreetFurConfiguration.Information["Exempts"]], true)
                .AddField("Week 1", Information[GreetFurConfiguration.Information["W1"]], true)
                .AddField("Week 2", Information[GreetFurConfiguration.Information["W2"]], true)
                .AddField("Activity", $"{Activity}%", true)
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        public static string IntToLetters(int? Value) {
            string Result = string.Empty;
            while (--Value >= 0) {
                Result = (char)('A' + Value % 26) + Result;
                Value /= 26;
            }
            return Result;
        }

    }
}
