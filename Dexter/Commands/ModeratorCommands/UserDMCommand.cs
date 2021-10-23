using System;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Mail;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using Discord.Net;

namespace Dexter.Commands
{

	public partial class ModeratorCommands
	{
		/// <summary>
		/// Sends a direct message to a target user.
		/// </summary>
		/// <param name="id">The target user's unique identifier</param>
		/// <param name="message">The full message to send the target user</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("userdm")]
		[Summary("Sends a direct message to a user given by their ID.")]
		[Alias("dm", "message", "mail")]
		[RequireModerator]
		[Priority(10)]

		public async Task UserDMCommand(ulong id, [Remainder] string message)
		{
			IUser user = await Client.Rest.GetUserAsync(id);

			if (user is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to find given user!")
					.WithDescription("Discord could not find a user with this ID. Are you sure you aren't using a message ID?")
					.SendEmbed(Context.Channel);
				return;
			}

			if (string.IsNullOrEmpty(message))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Empty message!")
					.WithDescription("I received an empty message. It would be rude for me to send that; I believe.")
					.SendEmbed(Context.Channel);
				return;
			}

			try
			{
				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("User DM")
					.WithDescription(message)
					.AddField("Recipient", user.GetUserInformation())
					.AddField("Sent By", Context.User.GetUserInformation())
					.SendDMAttachedEmbed(Context.Channel, BotConfiguration, user,
						BuildEmbed(EmojiEnum.Unknown)
						.WithTitle($"Message From {Context.Guild.Name}")
						.WithDescription(message)
					);
			}
			catch (HttpException e)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("HTTP Exception!")
					.WithDescription($"Discord didn't let me send the message! This is likely because I can't DM {user.GetUserInformation()}.\n" +
					$"They may have closed DMs or not share a server with me. Here's the exception trace:\n{e}")
					.SendEmbed(Context.Channel);
			}
		}

		/// <summary>
		/// Sends a direct message to a target user.
		/// </summary>
		/// <param name="user">The target user</param>
		/// <param name="message">The full message to send the target user</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("userdm")]
		[Summary("Sends a direct message to a user specified.")]
		[Alias("dm", "message", "mail")]
		[RequireModerator]
		[Priority(1)]

		public async Task UserDMCommand(IUser user, [Remainder] string message)
		{
			if (user is null)
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Unable to find given user!")
					.WithDescription("This may be due to caching! Try using their ID if you haven't.")
					.SendEmbed(Context.Channel);
				return;
			}

			if (string.IsNullOrEmpty(message))
			{
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Empty message!")
					.WithDescription("I received an empty message. It would be rude for me to send that; I believe.")
					.SendEmbed(Context.Channel);
				return;
			}

			await BuildEmbed(EmojiEnum.Love)
				.WithTitle("User DM")
				.WithDescription(message)
				.AddField("Recipient", user.GetUserInformation())
				.AddField("Sent By", Context.User.GetUserInformation())
				.SendDMAttachedEmbed(Context.Channel, BotConfiguration, user,
					BuildEmbed(EmojiEnum.Unknown)
					.WithTitle($"Message From {Context.Guild.Name}")
					.WithDescription(message)

				);
		}

		/// <summary>
		/// Sends a direct message to a target user.
		/// </summary>
		/// <param name="token">The token for the modmail.</param>
		/// <param name="message">The full message to send the target user</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("userdm")]
		[Summary("Sends a direct message to a modmail token specified.")]
		[Alias("dm", "message", "mail")]
		[RequireModerator]
		[Priority(5)]

		public async Task UserDMCommand(string token, [Remainder] string message)
		{
			ModMail modMail = ModMailDB.ModMail.Find(token);

			IUser user = null;

			if (modMail != null)
				user = DiscordShardedClient.GetUser(modMail.UserID);

			if (modMail == null || user == null)
			{
				if (Regex.IsMatch(token, @"<@!?[0-9]{18}>"))
				{
					token = token[^18..^1];
				}
				if (ulong.TryParse(token, out ulong userID) && userID != 0)
				{
					await UserDMCommand(userID, message);
					return;
				}
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Could Not Find Token!")
					.WithDescription("Haiya! I couldn't find the modmail for the given token. Are you sure this exists in the database? " +
						"The token should be given as the footer of the embed. Make sure this is the token and not the modmail number.")

					.SendEmbed(Context.Channel);
			}
			else
			{
				SocketChannel channel = DiscordShardedClient.GetChannel(ModerationConfiguration.ModMailChannelID);

				if (channel is SocketTextChannel txtChannel)
				{
					IMessage mailMessage = await txtChannel.GetMessageAsync(modMail.MessageID);

					if (mailMessage is IUserMessage mailMsg)
					{
						try
						{
							await mailMsg.ModifyAsync(msg => msg.Embed = mailMessage.Embeds.FirstOrDefault().ToEmbedBuilder()
								.WithColor(Color.Green)
								.AddField($"Replied By: {Context.User.Username}", message.Length > 300 ? $"{message.Substring(0, 300)} ..." : message)
													.Build()
							);
						}
						catch (InvalidOperationException)
						{
							IMessage messaged = await mailMsg.Channel.SendMessageAsync(embed: mailMsg.Embeds.FirstOrDefault().ToEmbedBuilder().Build());
							modMail.MessageID = messaged.Id;
						}
					}
					else
						throw new Exception($"Woa, this is strange! The message required isn't a socket user message! Are you sure this message exists? ModMail Type: {mailMessage.GetType()}");
				}
				else
					throw new Exception($"Eek! The given channel of {channel} turned out *not* to be an instance of SocketTextChannel, rather {channel.GetType().Name}!");

				await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Modmail User DM")
					.WithDescription(message)
					.AddField("Sent By", Context.User.GetUserInformation())
					.SendDMAttachedEmbed(Context.Channel, BotConfiguration, user,
						BuildEmbed(EmojiEnum.Unknown)
						.WithTitle($"Modmail From {Context.Guild.Name}")
						.WithDescription(message)
	
						.WithFooter(token)
					);
			}
		}

	}

}
