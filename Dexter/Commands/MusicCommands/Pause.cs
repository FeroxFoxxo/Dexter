using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Humanizer;
using System.Threading.Tasks;
using Victoria.Node;
using Victoria.Player;


namespace Dexter.Commands
{
    public partial class MusicCommands
	{

		[Command("pause")]
		[Summary("Toggles whether this player is currently paused. Use while songs are playing to pause the player, use while a player is paused to resume it.")]
		[MusicBotChannel]

		public async Task PauseCommand()
		{
			if (!await LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to pause the player!")
					.WithDescription("Failed to join voice channel. Are you in a voice channel?").SendEmbed(Context.Channel);

				return;
			}

			if (LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (player.PlayerState == PlayerState.Paused)
				{
					await player.ResumeAsync();
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Resumed the player.")
						.WithDescription($"Successfully resumed `{player.Track.Title}`")
						.SendEmbed(Context.Channel);
				}
				else if (player.PlayerState == PlayerState.Playing)
				{
					await player.PauseAsync();
					await BuildEmbed(EmojiEnum.Love)
						.WithTitle("Paused the player.")
						.WithDescription($"Successfully paused `{player.Track.Title}`")
						.SendEmbed(Context.Channel);
				}
				else
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle("Unable to pause the player!")
						.WithDescription("The player must be either in a playing or paused state to use this command.\n" +
												   $"Current state is **{player.PlayerState.Humanize()}**.")
						.SendEmbed(Context.Channel);
				}
			}
		}

	}
}
