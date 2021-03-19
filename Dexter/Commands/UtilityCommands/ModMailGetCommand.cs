using Dexter.Databases.Mail;
using Dexter.Enums;
using Discord.Commands;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Helpers;
using Dexter.Extensions;

namespace Dexter.Commands {

    public partial class UtilityCommands {

        /// <summary>
        /// Displays the ID attached to a modmail token
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("modmailget")]
        [Summary("Displays the ID attached to a modmail token")]
        [Alias("modmailuser")]
        [RequireAdministrator]
        [BotChannel]

        public async Task ModmailGetUser(string Token) {
            ModMail ModMail = ModMailDB.ModMail.Find(Token);

            if(ModMail == null) {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Invalid Token!")
                    .WithDescription("I wasn't able to find a modmail with that token!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Modmail Found!")
                .WithDescription(LanguageHelper.Truncate(ModMail.Message, 256))
                .AddField("Author ID: ", $"{ModMail.UserID} ||<@{ModMail.UserID}>||")
                .SendEmbed(Context.Channel);
        }

    }

}
