using Dexter.Databases.Mail;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        /// <summary>
        /// Sends an anonymous message to the moderators in a specific channel prepared for this.
        /// </summary>
        /// <param name="Message">The string message to send to the channel as a modmail.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("modmail")]
        [Summary("Sends an anonymous message to the moderators. This should be used in DMs.")]

        public async Task SendModMail([Remainder] string Message) {
            if (RestrictionsDB.IsUserRestricted(Context.User, Databases.UserRestrictions.Restriction.Modmail)) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("You aren't permitted to send modmails!")
                    .WithDescription("You have been blacklisted from using this service. If you think this is a mistake, feel free to personally contact an administrator")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (Message.Length > 1250) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Your modmail message is too big!")
                    .WithDescription("Please try to summarise your modmail a touch! If you are unable to, try send it in two different messages! " +
                        $"This character count should be under 1250 characters. This is due to how Discord handles embeds and the added information we need to apply to the embed. " +
                        $"The current modmail message character count is {Message.Length}.")
                    .SendEmbed(Context.Channel);
                return;
            }

            string Tracker = CreateToken();

            Attachment Attachment = Context.Message.Attachments.FirstOrDefault();

            string ProxyURL = string.Empty;

            if (Attachment != null)
                ProxyURL = await Attachment.ProxyUrl.GetProxiedImage(Tracker, DiscordSocketClient, ProposalService.ProposalConfiguration);

            IUserMessage UsrMessage = await (DiscordSocketClient.GetChannel(ModerationConfiguration.ModMailChannelID) as ITextChannel).SendMessageAsync(
                embed: BuildEmbed(EmojiEnum.Unknown)
                    .WithTitle($"Anonymous Modmail #{ModMailDB.ModMail.Count() + 1}")
                    .WithDescription(Message)
                    .WithImageUrl(ProxyURL)
                    .WithCurrentTimestamp()
                    .WithFooter(Tracker)
                    .Build()
            );

            ModMail ModMail = new() {
                Message = Message,
                UserID = Context.User.Id,
                MessageID = UsrMessage.Id,
                Tracker = Tracker
            };

            ModMailDB.ModMail.Add(ModMail);

            ModMailDB.SaveChanges();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Successfully Sent Modmail")
                .WithDescription($"Haiya! Your message has been sent to the staff team.\n\n" +
                    $"Your modmail token is: `{ModMail.Tracker}`, which is what the moderators use to reply to you. " +
                    $"Only give this out to a moderator if you wish to be identified.\n\n" +
                    $"Thank you~! - {DiscordSocketClient.GetGuild(BotConfiguration.GuildID).Name} Staff Team <3")
                .WithFooter(ModMail.Tracker)
                .WithCurrentTimestamp()
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// The Create Token method creates a random token for the modmail message.
        /// </summary>
        /// <returns>A randomly generated token in the form of a string that is not in the database already.</returns>

        public string CreateToken() {
            char[] TokenArray = new char[BotConfiguration.TrackerLength];

            for (int i = 0; i < TokenArray.Length; i++)
                TokenArray[i] = BotConfiguration.RandomCharacters[Random.Next(BotConfiguration.RandomCharacters.Length)];

            string Token = new(TokenArray);

            if (ModMailDB.ModMail.Find(Token) == null)
                return Token;
            else
                return CreateToken();
        }

    }

}