using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Threading.Tasks;
using Victoria.Node;
using Victoria.Player;

namespace Dexter.Commands
{
    public partial class MusicCommands
	{
		[Command("repeat")]
		[Summary("Allows repeating of the currently playing track.")]
		[MusicBotChannel]

		public async Task RepeatCommand(int amount)
		{
			if (amount < 1)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to repeat songs!")
					.WithDescription("Invalid input.\nCannot repeat a track less than once.").SendEmbed(Context.Channel);

				return;
			}

			if (amount > 10)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to repeat songs!")
					.WithDescription("You may only repeat a song 10 times at once.").SendEmbed(Context.Channel);

				return;
			}

			if (!await LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to repeat songs!")
					.WithDescription("Failed to join voice channel.\nAre you in a voice channel?").SendEmbed(Context.Channel);

				return;
			}

			if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (player.PlayerState != PlayerState.Playing)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to repeat songs!")
					.WithDescription("The player must be actively playing a track in order to loop it.").SendEmbed(Context.Channel);

					return;
				}

				var queueCopy = new Vueue<LavaTrack>();

				var curTrack = player.Track;

				for (int i = 0; i < amount; i++)
					queueCopy.Enqueue(curTrack);

				using (var enumerator = player.Vueue.GetEnumerator())
					while (enumerator.MoveNext())
						queueCopy.Enqueue(enumerator.Current);

				player.Vueue.Clear();

				foreach (var item in queueCopy)
					player.Vueue.Enqueue(item);

				string s = amount == 1 ? "" : "s";

				await BuildEmbed(EmojiEnum.Unknown)
					.WithTitle("🔂 Repeated Tracks")
					.WithDescription($"Successfully repeated **{curTrack.Title} {amount}** " + $"time{s}")
					.SendEmbed(Context.Channel);
			}
		}
	}
}