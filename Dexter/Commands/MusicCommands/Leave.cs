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

        [Command("leave")]
        [Summary("Disconnects me from the current voice channel.")]

        public async Task LeaveCommand()
        {
            if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to leave VC!")
                    .WithDescription("I couldn't find the music player for this server. " +
                    "Please ensure I am connected to a voice channel before using this command.")
                    .SendEmbed(Context.Channel);

                return;
            }

            string vcName = $"**{player.VoiceChannel.Name}**";

            try
            {
                await LavaNode.LeaveAsync(player.VoiceChannel);

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Unable to leave VC!")
                    .WithDescription($"Disconnected from `{vcName}`.")
                    .SendEmbed(Context.Channel);
            }
            catch (Exception)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to leave VC!")
                    .WithDescription($"Failed to disconnect from {vcName}. If the issue persists, please contact the developers for support.")
                    .SendEmbed(Context.Channel);

                await Debug.LogMessageAsync($"Failed to disconnect from voice channel '{vcName}' in {Context.Guild.Id} via $leave.", LogSeverity.Error);

                return;
            }
        }

    }
}
