using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using Victoria.Node;

namespace Dexter.Commands
{
    public partial class MusicCommands
    {

        [Command("clearqueue")]
        [Alias("clearq")]
        [Summary("Clears the current music player queue.")]
        [MusicBotChannel]
        [RequireDJ]

        public async Task ClearQueueCommand()
        {
            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to clear queue!")
                    .WithDescription("I couldn't find the music player for this server.\n" +
                    "Please ensure I am connected to a voice channel before using this command.")
                    .SendEmbed(Context.Channel);

                return;
            }

            try
            {
                int songCount = player.Vueue.Count;

                player.Vueue.Clear();

                await player.StopAsync();

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Playback halted.")
                    .WithDescription($"Cleared {songCount} from playing in {player.VoiceChannel.Name}.").SendEmbed(Context.Channel);
            }
            catch (Exception)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to clear queue!")
                    .WithDescription($"Failed to clear queue.\nIf the issue persists, please contact the developers for support.")
                    .SendEmbed(Context.Channel);

                await Debug.LogMessageAsync($"Failed to clear queue from voice channel '{player.VoiceChannel.Name}' in {Context.Guild.Id}.", LogSeverity.Error);

                return;
            }
        }
    }
}
