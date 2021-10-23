using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Dexter.Commands
{
	public partial class MusicCommands
	{

		[Command("join")]
		[Summary("Tells me to join the voice channel you are currently in.")]
		[MusicBotChannel]

		public async Task JoinCommand()
		{
			if (MusicService.LavaNode.HasPlayer(Context.Guild))
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"Unable to join channel!")
					.WithDescription("I'm already connected to a voice channel somewhere in this server.")
					.SendEmbed(Context.Channel);
			else
			{
				var voiceState = Context.User as IVoiceState;

				if (voiceState?.VoiceChannel == null)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle($"Unable to join channel!")
						.WithDescription("You must be connected to a voice channel to use this command.")
						.SendEmbed(Context.Channel);

					return;
				}

				try
				{
					await MusicService.LavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);

					await BuildEmbed(EmojiEnum.Love)
						.WithTitle($"Joined {voiceState.VoiceChannel.Name}!")
						.WithDescription("Hope you have a blast!")
						.SendEmbed(Context.Channel);
				}
				catch (Exception exception)
				{
					await BuildEmbed(EmojiEnum.Annoyed)
						.WithTitle($"Failed to join {voiceState.VoiceChannel.Name}.")
						.WithDescription("Error: " + exception.Message)
						.SendEmbed(Context.Channel);
				}
			}
		}
	}

}
