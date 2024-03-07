using System;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Runtime.InteropServices;
using Dexter.Abstractions;
using Dexter.Configurations;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Dexter.Commands
{

	public partial class FunCommands
	{

		public IServiceProvider ServiceProvider { get; set; }

		/// <summary>
		/// Returns a random percentage measurement that changes every so often depending on the user's ID along with time parameters.
		/// </summary>
		/// <param name="User">The user to make the measurement about.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("gay", ignoreExtraArgs: true)]
		[Summary("How gay are you? Use this command to find out~!")]
		[Alias("straight", "bisexual")]
		[BotChannel]
		[GameChannelRestricted]

		public async Task GayCommand([Optional] IUser User)
		{
			if (User == null)
            {
                User = Context.User;
            }
            else if (User is IGuildUser gUser)
			{
				var funCommand = ServiceProvider.GetRequiredService<FunConfiguration>();

				if (gUser.RoleIds.Contains(funCommand.RpDeniedRole))
				{
					await Context.Channel.SendMessageAsync(
						embed: new EmbedBuilder()
							.WithColor(Color.Red)
							.WithTitle("Halt! Who goes there-")
							.WithDescription("One of the users you mentioned doesn't want to have commands run on them!")
							.WithFooter($"To apply this yourself, please add the '{gUser.Guild.GetRole(funCommand.RpDeniedRole).Name}' role!")
							.WithCurrentTimestamp()
							.Build()
					);

					return;
				}
            }

			int Percentage = new Random((User.Id / Math.Round(DateTime.UnixEpoch.Subtract(DateTime.UtcNow).TotalDays)).ToString().GetHash()).Next(102);

			await Context.Channel.SendMessageAsync($"**{User.Username}'s** level of gay is {(Percentage > 100 ? "***over 9000!***" : $"**{Percentage}%**")}. "
				+ $"{(User.Id == Context.User.Id ? "You're" : User.Id == Context.Client.CurrentUser.Id ? "I'm" : "They're")} **{(Percentage < 33 ? "heterosexual" : Percentage < 66 ? "bisexual" : "homosexual")}**! "
				+ await DiscordShardedClient.GetGuild(FunConfiguration.EmojiGuildID).GetEmoteAsync(FunConfiguration.EmojiIDs[Percentage < 33 ? "annoyed" : Percentage < 66 ? "wut" : "love"]));
		}

	}

}
