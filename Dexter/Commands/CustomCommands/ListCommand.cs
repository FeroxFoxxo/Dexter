using Dexter.Databases.CustomCommands;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dexter.Commands
{

    public partial class CustomCommands
    {

        /// <summary>
        /// The ListCommands method runs on CCLIST and will list all the custom commands in the database.
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("cclist")]
        [Summary("Displays all avaliable custom commands in the database.")]
        [Alias("customcommands", "ccl")]

        public async Task ListCommands()
        {
            StringBuilder customCommands = new();
            List<CustomCommand> customCommandsList = CustomCommandDB.CustomCommands.ToList();
            customCommandsList.Sort((a, b) => a.CommandName.CompareTo(b.CommandName));
            foreach(CustomCommand cc in customCommandsList)
            {
                if (IsCustomCommandActive(cc))
                    customCommands.Append($"{BotConfiguration.Prefix}{cc.CommandName}{(cc.User == 0 ? "" : $" by <@{cc.User}>")}\n");
            }
            //string customCommands = string.Join("\n", CustomCommandDB.CustomCommands.AsQueryable().Select(CustomCommand => BotConfiguration.Prefix + CustomCommand.CommandName));

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Here is a list of usable commands! <3")
                .WithDescription(customCommands.Length > 0 ? customCommands.ToString() : "No custom commands created!")
                .SendEmbed(Context.Channel);
        }

    }

}