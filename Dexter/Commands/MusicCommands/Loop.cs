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
		[Command("loop")]
		[Summary("Allows looping of the currently playing track. Default amount is 1.")]
		public async Task LoopCommand(int amount = 1)
		{
			if (amount < 1)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to loop songs!")
					.WithDescription("Invalid input. Cannot loop a track less than once.").SendEmbed(Context.Channel);

				return;
			}

			if (amount > 10)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to loop songs!")
					.WithDescription("You may only loop a song 10 times at once.").SendEmbed(Context.Channel);

				return;
			}

			if (!await LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to loop songs!")
					.WithDescription("Failed to join voice channel. Are you in a voice channel?").SendEmbed(Context.Channel);

				return;
			}

			if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (player.PlayerState != PlayerState.Playing)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to loop songs!")
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
					.WithTitle("🔂 Loop Tracks")
					.WithDescription($"Successfully looped **{curTrack.Title} {amount}** " + $"time{s}")
					.SendEmbed(Context.Channel);
			}
		}
	}
}