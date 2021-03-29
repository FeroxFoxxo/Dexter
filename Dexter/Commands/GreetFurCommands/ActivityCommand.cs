using Dexter.Attributes.Methods;
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

        /// <summary>
        /// Displays the information from the Google Sheets database corresponding to the context user.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("gfactivity")]
        [Summary("Gets the GreetFur's Activity for the fortnight.")]
        [BotChannel]
        [RequireGreetFur]

        public async Task GreetFurActivity () {
            if (SheetsService == null)
                await SetupGoogleSheets();

            Spreadsheet Spreadsheet = await SheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            Sheet CurrentFortnight = Spreadsheet.Sheets
                .Where(Sheet => Sheet.Properties.Title == GreetFurConfiguration.FortnightSpreadsheet)
                .FirstOrDefault();

            ValueRange Columns = await SheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
                $"{CurrentFortnight.Properties.Title}!{GreetFurConfiguration.IDColumnIndex}1:{CurrentFortnight.Properties.GridProperties.RowCount}")
                .ExecuteAsync();

            int IndexOfUser = -1;

            for (int Index = 0; Index < Columns.Values.Count; Index++) {
                if (Columns.Values[Index][0].Equals(Context.User.Id.ToString()))
                    IndexOfUser = Index + 1;
            }

            if (IndexOfUser == -1) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to Find GreetFur")
                    .WithDescription("Haiya, it seems as if you're not in the GreetFur database! Are you sure you're a GreetFur? <3")
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

            await BuildEmbed(Activity >= 66 ? EmojiEnum.Love : Activity > 33 ? EmojiEnum.Wut : EmojiEnum.Annoyed)
                .WithAuthor(Context.User)
                .WithTitle("GreetFur Activity")
                .WithDescription($"**Name:** {Context.User.GetUserInformation()}\n" +
                                 $"**Manager:** {Information[GreetFurConfiguration.Information["Manager"]]}\n" +
                                 (string.IsNullOrEmpty(Notes) ? "" : $"**Notes:** {Notes}"))
                .AddField("Yes", Information[GreetFurConfiguration.Information["Yes"]], true)
                .AddField("No", Information[GreetFurConfiguration.Information["No"]], true)
                .AddField("Exempts", Information[GreetFurConfiguration.Information["Exempts"]], true)
                .AddField("Week 1", Information[GreetFurConfiguration.Information["W1"]], true)
                .AddField("Week 2", Information[GreetFurConfiguration.Information["W2"]], true)
                .AddField("Activity", $"{Activity}%", true)
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Turns an integer value into a base-26 representation using uppercase letters.
        /// </summary>
        /// <param name="Value">A numerical value to be converted.</param>
        /// <returns>A string of uppercase letters.</returns>
        
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
