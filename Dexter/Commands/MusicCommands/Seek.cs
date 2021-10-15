using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using Victoria.Node;
using Victoria.Player;

namespace Dexter.Commands
{
    public partial class MusicCommands
    {

        [Command("seek")]
        [Summary("Seeks the music player to the timespan given.")]
        [MusicBotChannel]
        [RequireDJ]

        public async Task SeekCommand([Remainder] string seekPosition)
        {
            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Could not seek current song.")
                        .WithDescription(
                    "I couldn't find the music player for this server.\n" +
                    "Please ensure I am connected to a voice channel before using this command.")
                        .SendEmbed(Context.Channel);

                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Could not seek current song.")
                        .WithDescription("I couldn't find a playing song to seek to~!")
                        .SendEmbed(Context.Channel);

                return;
            }

            TimeSpan? result = null;

            if (seekPosition.Contains(':'))
            {

                string[] times = Array.Empty<string>();
                int h = 0, m = 0, s;

                if (seekPosition.Contains(':'))
                    times = seekPosition.Split(':');

                if (times.Length == 2)
                {
                    m = int.Parse(times[0]);
                    s = int.Parse(times[1]);
                }
                else if (times.Length == 3)
                {
                    h = int.Parse(times[0]);
                    m = int.Parse(times[1]);
                    s = int.Parse(times[2]);
                }
                else
                {
                    s = int.Parse(seekPosition);
                }

                if (s < 0 || m < 0 || h < 0)
                {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Could not seek song!")
                        .WithDescription("Please enter in positive value")
                        .SendEmbed(Context.Channel);

                    return;
                }

                result = new(h, m, s);
            }

            if (!result.HasValue)
                if (TimeSpan.TryParse(seekPosition, out TimeSpan newTime))
                    result = newTime;
                else
                {
                    await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Could not seek song!")
                        .WithDescription("The time you chose to seek could not be converted to a TimeSpan.")
                        .SendEmbed(Context.Channel);

                    return;
                }


            if (player.Track.Duration < result)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Could not seek song!")
                    .WithDescription("Value must not be greater than current track duration")
                    .SendEmbed(Context.Channel);

                return;
            }

            await player.SeekAsync(result.Value);

            await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Seeked current song to {result.HumanizeTimeSpan()}.")
                    .WithDescription($"Seeked applied {player.Track} from {player.Track.Position.HumanizeTimeSpan()} to {result.HumanizeTimeSpan()}~!")
                    .SendEmbed(Context.Channel);
        }

    }
}
