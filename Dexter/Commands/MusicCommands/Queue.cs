using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands
{
    public partial class MusicCommands
    {

        [Command("queue")]
        [Alias("list")]
        [Summary("Displays the current queue of songs.")]
        [MusicBotChannel]

        public async Task QueueCommand()
        {
            if (!MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                        .WithTitle("Could not display queue.")
                        .WithDescription(
                    "I couldn't find the music player for this server.\n" +
                    "Please ensure I am connected to a voice channel before using this command.").SendEmbed(Context.Channel);

                return;
            }

            var embeds = player.GetQueue("🎶 Music Queue", BotConfiguration, MusicService);

            await CreateReactionMenu(embeds, Context.Channel);
        }

    }
}
