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

        public async Task SeekCommand(TimeSpan seekPosition)
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

            await player.SeekAsync(seekPosition);

            await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle($"Seeked current song by {seekPosition.HumanizeTimeSpan()}.")
                    .WithDescription($"Seeked {player.Track} to {player.Track.Position.HumanizeTimeSpan()}~!")
                    .SendEmbed(Context.Channel);
        }

    }
}
