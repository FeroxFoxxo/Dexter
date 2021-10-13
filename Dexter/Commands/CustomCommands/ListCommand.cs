using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Databases.CustomCommands;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Text;
using Discord;
using Humanizer;

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
            Dictionary<UserCommandSource, EmbedBuilder> embeds = new ();

            foreach (var cc in CustomCommandDB.CustomCommands.ToList())
            {
                if (!embeds.ContainsKey(cc.CommandType))
                    embeds.Add(cc.CommandType, BuildEmbed(EmojiEnum.Unknown).WithTitle(cc.CommandType.Humanize()));

                embeds[cc.CommandType].AddField($"{cc.CommandName} {(string.IsNullOrEmpty(cc.Alias) ? "" : $"{cc.Alias.Replace("\"", "")}")}", $"{cc.Reply}{(cc.User > 0 ? $" - by {Context.Client.GetUser(cc.User)}" : "")}");
            }


            List<CustomCommand> customCommandsList = CustomCommandDB.CustomCommands.ToList();
            customCommandsList.Sort((a, b) => a.CommandName.CompareTo(b.CommandName));

            if (embeds.Count == 0)
                embeds.Add(UserCommandSource.Unspecified, BuildEmbed(EmojiEnum.Annoyed).WithTitle("Unable to find custom commands!").WithDescription("No custom commands set."));

            CreateReactionMenu(embeds.OrderByDescending(x => (int) x.Key).Select(x => x.Value).ToArray(), Context.Channel);
        }

    }

}