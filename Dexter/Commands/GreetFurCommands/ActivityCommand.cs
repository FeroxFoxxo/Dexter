using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Google.Apis.Sheets.v4.Data;

namespace Dexter.Commands
{

    public partial class GreetFurCommands
    {

        /// <summary>
        /// Displays the information from the Google Sheets database corresponding to the context user.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("gfactivity")]
        [Summary("Gets the GreetFur's Activity for the fortnight.")]
        [BotChannel]
        [RequireGreetFur]

        public async Task GreetFurActivity()
        {
            Spreadsheet spreadsheets = await SheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            Sheet currentFortnight = spreadsheets.Sheets
                .Where(sheet => sheet.Properties.Title == GreetFurConfiguration.FortnightSpreadsheet)
                .FirstOrDefault();

            ValueRange columns = await SheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
                $"{currentFortnight.Properties.Title}!{GreetFurConfiguration.IDColumnIndex}1:{currentFortnight.Properties.GridProperties.RowCount}")
                .ExecuteAsync();

            int indexOfUser = -1;

            for (int i = 0; i < columns.Values.Count; i++)
            {
                if (columns.Values[i][0].Equals(Context.User.Id.ToString()))
                    indexOfUser = i + 1;
            }

            if (indexOfUser == -1)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to Find GreetFur")
                    .WithDescription("Haiya, it seems as if you're not in the GreetFur database! Are you sure you're a GreetFur? <3")
                    .SendEmbed(Context.Channel);
                return;
            }

            ValueRange rows = await SheetsService.Spreadsheets.Values.Get(GreetFurConfiguration.SpreadSheetID,
                $"{currentFortnight.Properties.Title}!A{indexOfUser}:{IntToLetters(currentFortnight.Properties.GridProperties.ColumnCount)}")
                .ExecuteAsync();

            IList<object> information = rows.Values[0];

            decimal yes = int.Parse(information[GreetFurConfiguration.Information["Yes"]].ToString());
            decimal no = int.Parse(information[GreetFurConfiguration.Information["No"]].ToString());

            decimal ratio = yes + no;

            decimal activity;

            if (ratio > 0)
                activity = Math.Round(yes / ratio * 100);
            else
                activity = 100;

            string Notes = information[GreetFurConfiguration.Information["Notes"]].ToString();

            await BuildEmbed(activity >= 66 ? EmojiEnum.Love : activity > 33 ? EmojiEnum.Wut : EmojiEnum.Annoyed)
                .WithAuthor(Context.User)
                .WithTitle("GreetFur Activity")
                .WithDescription($"**Name:** {Context.User.GetUserInformation()}\n" +
                                 $"**Manager:** {information[GreetFurConfiguration.Information["Manager"]]}\n" +
                                 (string.IsNullOrEmpty(Notes) ? "" : $"**Notes:** {Notes}"))
                .AddField("Yes", information[GreetFurConfiguration.Information["Yes"]], true)
                .AddField("No", information[GreetFurConfiguration.Information["No"]], true)
                .AddField("Exempts", information[GreetFurConfiguration.Information["Exempts"]], true)
                .AddField("Week 1", information[GreetFurConfiguration.Information["W1"]], true)
                .AddField("Week 2", information[GreetFurConfiguration.Information["W2"]], true)
                .AddField("Activity", $"{activity}%", true)
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Turns an integer value into a base-26 representation using uppercase letters.
        /// </summary>
        /// <param name="Value">A numerical value to be converted.</param>
        /// <returns>A string of uppercase letters.</returns>

        public static string IntToLetters(int? Value)
        {
            string Result = string.Empty;
            while (--Value >= 0)
            {
                Result = (char)('A' + Value % 26) + Result;
                Value /= 26;
            }
            return Result;
        }

    }

}
