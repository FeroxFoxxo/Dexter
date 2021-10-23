using System.Threading.Tasks;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;

namespace Dexter.Commands
{

	public partial class UtilityCommands
	{

		/// <summary>
		/// Displays the latency between Discord's API and the bot.
		/// </summary>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("ping")]
		[Summary("Displays the latency between both Discord and the bot.")]
		[Alias("latency")]

		public async Task PingCommand()
		{
			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("Gateway Ping")
				.WithDescription($"**{Context.Client.Latency}ms**")
				.SendEmbed(Context.Channel);
		}

	}

}
