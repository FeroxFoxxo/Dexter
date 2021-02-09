using Dexter.Databases.Mail;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands {

    public partial class ModeratorCommands {

        [Command("modmail")]
        [Summary("Sends an anonymous message to the moderators. This should be used in DMs.")]

        public async Task SendModMail([Remainder] string Message) {
            if (Message.Length > 1500) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Your modmail message is too big!")
                    .WithDescription("Please try to summarise your modmail a touch! If you are unable to, try send it in two different messages! " +
                        $"This count should be under 1500 characters. The current modmail message character count is {Message.Length}.")
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

            await Context.Channel.SendMessageAsync($"Haiya! Your message has been sent to the staff team. Your modmail token is: `{ModMail.Tracker}`. Thank you~! <3");
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