using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Discord;
using Discord.Commands;
using static Dexter.Events.GreetFur;

namespace Dexter.Commands
{
    public partial class GreetFurCommands
    {

        /// <summary>
        /// Modifies the remote spreadsheet containing activity information about greetfurs.
        /// </summary>
        /// <param name="args">Special sequences that modify the behaviour of the method.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the method completes successfully.</returns>

        [Command("updatesheet")]
        [Summary("Updates the values of the active spreadsheet tracking system for GreetFurs.")]
        [ExtendedSummary("Updates the values of the active spreadsheet tracking for GreetFurs; you may extend the functionality of the command by using specific arguments:\n" +
            "**-l** or **--last**: Sets the spreadsheet to the latest full spreadsheet period\n" +
            "**-n** or **--new**: Adds any untracked users with recent activity to the Greetfur spreadsheet\n" +
            "**-s** or **--safe**: No non-empty cells will be overridden by the update operation\n" +
            "**-w [week]** or **--week [week]**: Sets the first week of the Spreadsheet to a week given by its number since tracking started.\n" +
            "**-re** or **--read-exemptions**: Reads all \"Exempt\" entries in the sheet and saves them to the relevant users for the time period requested (current by default, can be overridden with --last or --week)\n" +
            "**-tbp** or **--the-big-picture**: Updates the big picture with the same data as the activity sheet.")]
        [Alias("updatespreadsheet")]
        [RequireModerator]

        public async Task UpdateSheetCommand([Remainder] string args = "")
        {
            IUserMessage msg = await Context.Channel.SendMessageAsync("Working on it...!");
            GreetFurOptions opt = GreetFurOptions.None;

            int week = -1;

            List<string> errors = new();
            string[] splitArgs = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i < splitArgs.Length)
            {
                switch (splitArgs[i])
                {
                    case "-n":
                    case "--new":
                        opt |= GreetFurOptions.AddNewRows;
                        break;
                    case "-l":
                    case "--last":
                        opt |= GreetFurOptions.DisplayLastFull;
                        break;
                    case "-w":
                    case "--week":
                        if (++i < splitArgs.Length)
                            if (!int.TryParse(splitArgs[i], out week))
                            {
                                errors.Add($"Value {splitArgs[i].Replace('@', '-')} can't be parsed to a valid week number!");
                            }
                        else
                            errors.Add("Missing numeric parameter for \"week\".");
                        break;
                    case "-s":
                    case "--safe":
                        opt |= GreetFurOptions.Safe;
                        break;
                    case "-re":
                    case "--read-exemptions":
                        opt |= GreetFurOptions.ReadExemptions;
                        break;
                    case "-tbp":
                    case "--the-big-picture":
                        opt |= GreetFurOptions.ManageTheBigPicture;
                        break;
                    default:
                        errors.Add($"Unrecognized argument: {splitArgs[i].Replace('@', '-')}");
                        break;
                }
                i++;
            }

            if (errors.Any())
            {
                await msg.ModifyAsync(m =>
                {
                    m.Content = "Invalid Syntax";
                    m.Embed = BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Errors:")
                    .WithDescription(string.Join('\n', errors))
                                        .Build();
                });
                return;
            }

            await GreetFurService.UpdateRemoteSpreadsheet(opt, week: week);

            await msg.ModifyAsync(m => m.Content = "*Remote Spreadsheet Updated Successfully!*");
        }
    }
}
