using Dexter.Databases.Mail;
using Dexter.Enums;
using Discord.Commands;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Extensions;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using System.Linq;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// Displays the ID attached to a modmail token
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("unlock")]
        [Summary("Unlocks a user's modmail. Notifies the user and attaches it to the modmail message.")]
        [Alias("getmodmail")]
        [RequireModerator]
        [BotChannel]

        public async Task ModmailGetUser(string Token, [Remainder] string Reason) {
            ModMail ModMail = ModMailDB.ModMail.Find(Token);

            if(ModMail == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Invalid Token!")
                    .WithDescription("I wasn't able to find a modmail with that token!")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (Reason.Length > 200) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Your unlock reason is to long!")
                    .WithDescription(
                        "Modmail unlock reasons should only be brief and concise. " +
                        "Unlocks usually follow a given warning or a DM response. " +
                        "You should write your full write up in that. <3")
                    .WithCurrentTimestamp()
                    .SendEmbed(Context.Channel);
                return;
            }

            await SendForAdminApproval(
                UnlockModmail,
                new() { { "Token", Token }, { "Reason", Reason } },
                Context.User.Id,
                $"{Context.User.Mention} has suggested that the modmail `{Token}` should be unlocked and the user revealed due to `{Reason}`"
            );

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"The ModMail {Token} was suggested to be unlocked!")
                .WithDescription($"Once it has passed admin approval, the user of the modmail will be notified and the modmail will be resent into the channel with the user's name.")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// The Unlock Modmail method runs on a modmail being unlocked by a moderator and approved by an administrator. It notifies the user and modifies the modmail message.
        /// </summary>
        /// <param name="Parameters"></param>

        public async void UnlockModmail(Dictionary<string, string> Parameters) {
            string Token = Parameters["Token"];
            string Reason = Parameters["Reason"];

            ModMail ModMail = ModMailDB.ModMail.Find(Token);

            IUser User = DiscordSocketClient.GetUser(ModMail.UserID);

            ITextChannel TextChannel = DiscordSocketClient.GetChannel(ModerationConfiguration.ModMailChannelID) as ITextChannel;

            IMessage ModmailMessage = await TextChannel.GetMessageAsync(ModMail.MessageID);

            await ModmailMessage.DeleteAsync();

            EmbedBuilder Embed = ModmailMessage.Embeds.First().ToEmbedBuilder();

            if (User != null)
                Embed.WithTitle($"{User.Username}#{User.Discriminator}'s Modmail");
            else
                Embed.WithTitle($"{ModMail.UserID}'s Modmail");

            Embed.AddField("Unlock Reason:", Reason);

            IMessage Message = await TextChannel.SendMessageAsync(embed: Embed.Build());

            ModMail.MessageID = Message.Id;

            ModMailDB.SaveChanges();

            if (User != null)
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Modmail Unlocked")
                    .WithDescription(
                    $"Hi, a moderator has unlocked your modmail message for `{ModMail.Tracker}`. " +
                    $"This will be due to it breaking server or Discord TOS rules and us needing to identify you. " +
                    $"We only do this in extreme circumstances. You will likely be notified of this shortly.\n" +
                    $"- {DiscordSocketClient.GetGuild(BotConfiguration.GuildID).Name} Staff Team")
                    .AddField("Reason: ", Reason)
                .SendEmbed(await User.GetOrCreateDMChannelAsync());
        }

    }

}
