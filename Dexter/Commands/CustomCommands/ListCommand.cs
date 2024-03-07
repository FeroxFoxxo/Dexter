using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Databases.CustomCommands;
using Dexter.Enums;
using Discord.Commands;
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
			List<EmbedBuilder> embeds = [];

			var CurrentType = UserCommandSource.Unspecified;
			var CurrentBuilder = BuildEmbed(EmojiEnum.Annoyed).WithTitle("Unable to find custom commands!").WithDescription("No custom commands set.");

			foreach (var cc in CustomCommandDB.CustomCommands.ToList().OrderByDescending(x => (int) x.CommandType))
			{
				if (CurrentType != cc.CommandType || CurrentBuilder.Fields.Count >= 10)
				{
					embeds.Add(CurrentBuilder);
					CurrentBuilder = BuildEmbed(EmojiEnum.Unknown).WithTitle($"{cc.CommandType.Humanize()} Commands");
					CurrentType = cc.CommandType;
				}

				CurrentBuilder.AddField($"{BotConfiguration.Prefix}{cc.CommandName} {(string.IsNullOrEmpty(cc.Alias) ? "" : $"{cc.Alias.Replace("\"", "")}")}", $"{cc.Reply}{(cc.User > 0 ? $" - by <@{cc.User}>" : "")}");
			}

			embeds.Add(CurrentBuilder);

			List<CustomCommand> customCommandsList = [.. CustomCommandDB.CustomCommands];
			customCommandsList.Sort((a, b) => a.CommandName.CompareTo(b.CommandName));

			if (embeds.Count > 1)
            {
                embeds.RemoveAt(0);
            }

            await CreateReactionMenu([.. embeds], Context.Channel);
		}

	}

}
