using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Victoria.Player;

namespace Dexter.Commands
{
    public partial class MusicCommands
	{
		[Command("loop")]
		[Alias("repeat")]
		[Summary("Toggles looping of the current playlist between `single` / `all` / `off`.")]
		[MusicBotChannel]

		public async Task LoopCommand(LoopType loopType)
		{
			if (!await MusicService.LavaNode.SafeJoinAsync(Context.User, Context.Channel))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to loop songs!")
					.WithDescription("Failed to join voice channel.\nAre you in a voice channel?").SendEmbed(Context.Channel);

				return;
			}

			if (MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				if (player.PlayerState != PlayerState.Playing)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to loop songs!")
					.WithDescription("The player must be actively playing a track in order to loop it.").SendEmbed(Context.Channel);

					return;
				}


				lock (MusicService.LoopLocker)
					MusicService.LoopedGuilds[player.VoiceChannel.Guild.Id] = loopType;

				EmbedBuilder builder = BuildEmbed(EmojiEnum.Unknown);

				switch (loopType)
				{
					case LoopType.Single:
						builder
							.WithTitle($"🔂 Repeated Current Track")
							.WithDescription($"Successfully started repeating **{player.Track.Title}**.");
						break;
					case LoopType.All:
						builder
							.WithTitle($"🔂 Looping Tracks")
							.WithDescription($"Successfully started looping **{player.Vueue.Count + 1} tracks**.");
						break;
					case LoopType.Off:
						builder
							.WithTitle($"🔂 Stopped Looping Tracks")
							.WithDescription($"Successfully stopped looping the current queue.");
						break;
				}

				await builder.SendEmbed(Context.Channel);
			}
		}
	}
}