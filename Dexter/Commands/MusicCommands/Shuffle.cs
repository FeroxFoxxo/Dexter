using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands
{
	public partial class MusicCommands
	{

		[Command("shuffle")]
		[Alias("reshuffle")]
		[Summary("Shuffles the music queue in a random order.")]
		[MusicBotChannel]

		public async Task ShuffleCommand()
		{
			if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to shuffle queue!")
					.WithDescription(
					"I couldn't find the music player for this server.\n" +
					"Please ensure I am connected to a voice channel before using this command.").SendEmbed(Context.Channel);

				return;
			}

			if (!player.Vueue.Any())
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to shuffle queue!")
					.WithDescription(
					"There aren't any songs in the queue.\n" +
					"Please add songs to the queue with the `play` command and try again.").SendEmbed(Context.Channel);

				return;
			}

			player.Vueue.Shuffle();

			var embeds = player.GetQueue("🔀 Queue Shuffle", BotConfiguration, MusicService);

			await CreateReactionMenu(embeds, Context.Channel);
		}

	}
}
