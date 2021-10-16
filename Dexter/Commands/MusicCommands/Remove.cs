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

        [Command("remove")]
        [Summary("Removes a song at a given position in the queue.")]
        [MusicBotChannel]
        [RequireDJ]

        public async Task RemoveCommand(int index)
        {
            if (!MusicService.LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to remove song!")
                    .WithDescription("I couldn't find the music player for this server.\n" +
                    "Please ensure I am connected to a voice channel before using this command.")
                    .SendEmbed(Context.Channel);

                return;
            }

            if (player.Vueue.Count < index)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to remove song!")
                    .WithDescription($"I couldn't find a song at the index of {index}. The length of the queue is {player.Vueue.Count}.")
                    .SendEmbed(Context.Channel);

                return;
            }

            var rtrack = player.Vueue.ToArray()[index];

            player.Vueue.Remove(rtrack);

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"📑 Removed {rtrack.Title}!")
                .WithDescription($"I successfully removed {rtrack.Title} by {rtrack.Author} at position {index}.")
                .SendEmbed(Context.Channel);
        }
    }
}
