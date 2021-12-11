using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands
{
	public partial class MusicCommands
	{

		[Command("stop")]
		[Summary("Displays the current music queue.")]
		[MusicBotChannel]
		[RequireDJ]

		public async Task StopCommand()
		{
			if (!LavaNode.TryGetPlayer(Context.Guild, out var player))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to stop songs!")
					.WithDescription("I couldn't find the music player for this server.\n" +
					"Please ensure I am connected to a voice channel before using this command.")
					.SendEmbed(Context.Channel);

				return;
			}

			string vcName = $"**{player.VoiceChannel.Name}**";

			try
			{
				string prevTrack = player.Track.Title;

				await player.StopAsync();

				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Playback halted.")
					.WithDescription($"Stopped {prevTrack} from playing in {vcName}.").SendEmbed(Context.Channel);
			}
			catch (Exception)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to stop songs!")
					.WithDescription($"Failed to disconnect from {vcName}.\nIf the issue persists, please contact the developers for support.")
					.SendEmbed(Context.Channel);

				Logger.LogError($"Failed to disconnect from voice channel '{vcName}' in {Context.Guild.Id}.");

				return;
			}
		}
	}
}
