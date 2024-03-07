using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Text.RegularExpressions;
using System;
using Discord.Net;

namespace Dexter.Commands
{

	public partial class MuzzleCommands
	{

		/// <summary>
		/// Mutes a given user for a configured amount of time, if no user is specified, it defaults to Context.User.
		/// </summary>
		/// <remarks>
		/// Using this command on a target different from Context.User requires Staff permissions.
		/// This command has a 1-minute cooldown.
		/// </remarks>
		/// <param name="args">Optional parameter, indicates target user.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("sleep", ignoreExtraArgs: true)]
		[Summary("Issue the command to be muted for a short while and go get some sleepy time!")]
		[CommandCooldown(60)]

		public async Task SleepCommand([Remainder] string args = "")
		{
			string argID = Regex.Match(args, @"[0-9]{18}").Value;

			ulong idToMuzzle;
			if (!string.IsNullOrEmpty(argID))
			{
				if (Context.User.GetPermissionLevel(DiscordShardedClient, BotConfiguration) < PermissionLevel.Moderator)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Insufficient permissions")
						.WithDescription("You aren't allowed to muzzle other users, you silly bean!")
						.SendEmbed(Context.Channel);
					return;
				}
				idToMuzzle = ulong.Parse(argID);
			}
			else
            {
                idToMuzzle = Context.User.Id;
            }

            IGuildUser toMuzzle = Context.Guild.GetUser(idToMuzzle);

			TimeSpan duration = TimeSpan.FromSeconds(MuzzleConfiguration.SleepDuration);
			if (toMuzzle.TimedOutUntil.HasValue && toMuzzle.TimedOutUntil.Value.Subtract(DateTime.Now) > duration)
			{
				await Context.Channel.SendMessageAsync($"This is timed out for longer than the target duration!");
				return;
			}

			try
			{
				await TimeoutUser(toMuzzle, duration);

				await Context.Channel.SendMessageAsync($"Goodnight, **{toMuzzle.Username}~!**. Sleep well <3");
			} 
			catch (HttpException)
            {
				await Context.Channel.SendMessageAsync($"I wasn't able to timeout {toMuzzle.Nickname ?? toMuzzle.Username}! It seems my power is dwindling :(");
            }
		}

	}

}
