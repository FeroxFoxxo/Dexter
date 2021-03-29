using Dexter.Attributes.Methods;
using Dexter.Databases.Mail;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// Sends a direct message to a target user.
        /// </summary>
        /// <param name="User">The target user</param>
        /// <param name="Message">The full message to send the target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("userdm")]
        [Summary("Sends a direct message to a user specified.")]
        [Alias("dm", "message", "mail")]
        [RequireModerator]

        public async Task UserDMCommand(IUser User, [Remainder] string Message) {
            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("User DM")
                .WithDescription(Message)
                .AddField("Recipient", User.GetUserInformation())
                .AddField("Sent By", Context.User.GetUserInformation())
                .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User,
                    BuildEmbed(EmojiEnum.Unknown)
                    .WithTitle($"Message From {Context.Guild.Name}")
                    .WithDescription(Message)
                    .WithCurrentTimestamp()
                );
        }

        /// <summary>
        /// Sends a direct message to a target user.
        /// </summary>
        /// <param name="Token">The token for the modmail.</param>
        /// <param name="Message">The full message to send the target user</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("userdm")]
        [Summary("Sends a direct message to a modmail token specified.")]
        [Alias("dm", "message", "mail")]
        [RequireModerator]

        public async Task UserDMCommand(string Token, [Remainder] string Message) {
            ModMail ModMail = ModMailDB.ModMail.Find(Token);

            IUser User = null;

            if (ModMail != null)
                User = DiscordSocketClient.GetUser(ModMail.UserID);

            if (ModMail == null || User == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Could Not Find Token!")
                    .WithDescription("Haiya! I couldn't find the modmail for the given token. Are you sure this exists in the database? " +
                        "The token should be given as the footer of the embed. Make sure this is the token and not the modmail number.")
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
            } else {
                SocketChannel SocketChannel = DiscordSocketClient.GetChannel(ModerationConfiguration.ModMailChannelID);

                if (SocketChannel is SocketTextChannel TextChannel) {
                    IMessage MailMessage = await TextChannel.GetMessageAsync(ModMail.MessageID);

                    if (MailMessage is IUserMessage MailMSG) {
                        try {
                            await MailMSG.ModifyAsync(MailMSGs => MailMSGs.Embed = MailMessage.Embeds.FirstOrDefault().ToEmbedBuilder()
                                .WithColor(Color.Green)
                                .AddField($"Replied By: {Context.User.Username}", Message.Length > 300 ? $"{Message.Substring(0, 300)} ..." : Message)
                                .Build()
                            );
                        } catch (InvalidOperationException) {
                            IMessage Messaged = await MailMSG.Channel.SendMessageAsync(embed: MailMSG.Embeds.FirstOrDefault().ToEmbedBuilder().Build());
                            ModMail.MessageID = Messaged.Id;
                            ModMailDB.SaveChanges();
                        }
                    } else
                        throw new Exception($"Woa, this is strange! The message required isn't a socket user message! Are you sure this message exists? ModMail Type: {MailMessage.GetType()}");
                } else
                    throw new Exception($"Eek! The given channel of {SocketChannel} turned out *not* to be an instance of SocketTextChannel, rather {SocketChannel.GetType().Name}!");

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Modmail User DM")
                    .WithDescription(Message)
                    .AddField("Sent By", Context.User.GetUserInformation())
                    .SendDMAttachedEmbed(Context.Channel, BotConfiguration, User,
                        BuildEmbed(EmojiEnum.Unknown)
                        .WithTitle($"Modmail From {Context.Guild.Name}")
                        .WithDescription(Message)
                        .WithCurrentTimestamp()
                        .WithFooter(Token)
                    );
            }
        }

    }

}
