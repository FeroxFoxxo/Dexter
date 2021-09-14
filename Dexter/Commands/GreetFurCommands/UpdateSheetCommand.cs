using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dexter.Services.GreetFurService;

namespace Dexter.Commands
{
    public partial class GreetFurCommands
    {

        [Command("updatesheet", RunMode = RunMode.Async)]
        [Summary("Updates the values of the active spreadsheet tracking system for GreetFurs.")]
        [Alias("updatespreadsheet")]
        [RequireModerator]

        public async Task UpdateSheetCommand([Remainder] string args = "")
        {
            IUserMessage msg = await Context.Channel.SendMessageAsync("Working on it...!");
            GreetFurOptions opt = GreetFurOptions.None;

            string[] splitArgs = args.Split(' ');
            if (splitArgs.Contains("-n") || splitArgs.Contains("--new"))
            {
                opt |= GreetFurOptions.AddNewRows;
            }
            if (splitArgs.Contains("-l") || splitArgs.Contains("--last"))
            {
                opt |= GreetFurOptions.DisplayLastFull;
            }

            await GreetFurService.UpdateRemoteSpreadsheet(opt);

            await msg.ModifyAsync(m => m.Content = "*Remote Spreadsheet Updated Successfully!*");
        }

    }
}
