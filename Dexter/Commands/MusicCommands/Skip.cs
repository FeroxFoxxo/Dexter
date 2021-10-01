using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using Victoria.Node;

namespace Dexter.Commands
{
    public partial class MusicCommands
    {

        [Command("skip")]
        [Summary("Skips the current song. Pass in a number to skip multiple songs at once.")]
        public async Task SkipCommand(int? skipCount = null)
        {
            if (skipCount < 2)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to skip song!")
					.WithDescription("If you are specifiying a skip count, you must skip 2+ tracks.").SendEmbed(Context.Channel);

                return;
            }

            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to skip song!")
					.WithDescription("I couldn't find the music player for this server. " +
					"Please ensure I am connected to a voice channel before using this command.").SendEmbed(Context.Channel);

                return;
            }

			var curTrack = player.Track;
			bool emptyQueue = player.Vueue.Count == 0;
			if (curTrack == null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to skip song!")
					.WithDescription("There isn't anything to skip.").SendEmbed(Context.Channel);

				return;
			}

			if (emptyQueue)
			{
				await player.StopAsync();
				await BuildEmbed(EmojiEnum.Love).WithTitle($"Skipped {curTrack.Title}.")
					.WithDescription("No more tracks remaining.").SendEmbed(Context.Channel);
			}
			else if (!skipCount.HasValue || skipCount.Value == 1)
			{
				await player.SkipAsync();

				await BuildEmbed(EmojiEnum.Love)
					.GetNowPlaying(player.Track)
					.WithTitle($"Skipped {curTrack.Title}")
					.SendEmbed(Context.Channel);
			}
			else
			{
				int actualSkipCount = 0;
				for (int i = 0; i < skipCount.Value; i++)
				{
					try
					{
						await player.SkipAsync();
						actualSkipCount++;
					}
					catch (InvalidOperationException)
					{
						await player.StopAsync();
						break;
					}
				}

				string s = actualSkipCount == 1 ? "" : "s";

				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Songs have been skipped!")
					.WithDescription($"Skipped {actualSkipCount:N0} track{s}.").SendEmbed(Context.Channel);
			}
		}
    }
}
