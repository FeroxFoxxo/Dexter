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

        [Command("nowplaying")]
        [Summary("Display the currently playing song.")]
        [MusicBotChannel]

        public async Task NowPlayingCommand()
        {
            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to find current song!")
                    .WithDescription("I couldn't find the music player for this server.\n" +
                    "Please ensure I am connected to a voice channel before using this command.")
                    .SendEmbed(Context.Channel);

                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                .WithTitle("Unable to find current song!")
                .WithDescription("The player must be actively playing a track in order to see its information.").SendEmbed(Context.Channel);

                return;
            }

            await BuildEmbed(EmojiEnum.Unknown)
                .GetNowPlaying(player.Track)
                .SendEmbed(Context.Channel);
        }
    }
}
