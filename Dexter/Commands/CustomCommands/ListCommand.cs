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
using System;

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
            List<EmbedBuilder> embeds = new ();

            var CurrentType = UserCommandSource.Unspecified;
            var CurrentBuilder = BuildEmbed(EmojiEnum.Annoyed).WithTitle("Unable to find custom commands!").WithDescription("No custom commands set.");

            foreach (var cc in CustomCommandDB.CustomCommands.ToList().OrderByDescending(x => (int) x.CommandType))
            {
                if (CurrentType != cc.CommandType || CurrentBuilder.Fields.Count >= 20)
                {
                    embeds.Add(CurrentBuilder);
                    CurrentBuilder = BuildEmbed(EmojiEnum.Unknown).WithTitle(cc.CommandType.Humanize());
                    CurrentType = cc.CommandType;
                }

                CurrentBuilder.AddField($"{cc.CommandName} {(string.IsNullOrEmpty(cc.Alias) ? "" : $"{cc.Alias.Replace("\"", "")}")}{(cc.User > 0 ? $" - by <@{cc.User}>" : "")}", cc.Reply);
            }

            List<CustomCommand> customCommandsList = CustomCommandDB.CustomCommands.ToList();
            customCommandsList.Sort((a, b) => a.CommandName.CompareTo(b.CommandName));

            if (embeds.Count > 1)
                embeds.RemoveAt(0);

            CreateReactionMenu(embeds.ToArray(), Context.Channel);
        }

    }

}