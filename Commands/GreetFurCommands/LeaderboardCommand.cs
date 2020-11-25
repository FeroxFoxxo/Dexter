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

        [Command("gflevels")]
        [Summary("Returns a leaderboard of all the GreetFurs and their activity levels.")]
        [BotChannel]
        [Alias("gfleaderboard")]

        public async Task GreetFurLeaderboard () {
            if (SheetsService == null)
                await SetupGoogleSheets();

            Spreadsheet Spreadsheet = await SheetsService.Spreadsheets.Get(GreetFurConfiguration.SpreadSheetID).ExecuteAsync();

            throw new Exception("This command has yet to be implemented!");
        }

    }
}