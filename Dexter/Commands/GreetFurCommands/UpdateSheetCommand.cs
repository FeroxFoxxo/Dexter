using Dexter.Attributes.Methods;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Commands
{
    public partial class GreetFurCommands
    {

        [Command("updatesheet")]
        [Summary("Updates the values of the active spreadsheet tracking system for GreetFurs.")]
        [Alias("updatespreadsheet")]
        [RequireModerator]

        public async Task UpdateSheetCommand([Remainder] string args = "")
        {
            await GreetFurService.UpdateRemoteSpreadsheet();

            await Context.Channel.SendMessageAsync("*Remote Spreadsheet Updated Successfully!*");
        }

    }
}
