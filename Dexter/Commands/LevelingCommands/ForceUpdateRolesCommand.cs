using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;
using Discord.Rest;
using static Dexter.Events.Leveling;

namespace Dexter.Commands
{
	public partial class LevelingCommands
	{

		/// <summary>
		/// Forces a specific user's roles to align with their Dexter level in case any event desyncs them.
		/// </summary>
		/// <param name="target">The target user to update roles for.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

		[Command("forceupdateroles")]
		[Summary("Corrects a target user's ranked roles in case they're off-sync with their Dexter level.")]
		[RequireGreetFur]
		[CommandCooldown(60)]

		public async Task ForceUpdateRolesCommand(IGuildUser target)
		{
			RestGuildUser restTarget = await DiscordShardedClient.Rest.GetGuildUserAsync(Context.Guild?.Id ?? BotConfiguration.GuildID, target.Id);

			RoleModificationResponse response = await LevelingService.UpdateRolesWithInfo(restTarget, true);

			await Context.Channel.SendMessageAsync(response.ToString());
		}

	}
}
