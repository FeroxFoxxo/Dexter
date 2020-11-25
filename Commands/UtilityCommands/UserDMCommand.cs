using Dexter.Attributes;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using System.Threading.Tasks;

namespace Dexter.Commands {
    public partial class UtilityCommands {

        [Command("userdm")]
        [Summary("Sends a direct message to a user specified.")]
        [Alias("dm", "message")]
        [RequireModerator]

        public async Task UserDMCommand(IUser User, [Remainder] string Message) {
            EmbedBuilder Embed = BuildEmbed(EmojiEnum.Unknown)
                .WithTitle("User DM")
                .WithDescription(Message)
                .AddField("Recipient", User.GetUserInformation())
                .AddField("Sent By", Context.User.GetUserInformation());

            try {
                await User.SendMessageAsync($"**__Message From {Context.Guild.Name}__**\n{Message}");

                Embed.BuildEmbed(EmojiEnum.Love)
                    .AddField("Success", "The DM was successfully sent!");
            } catch (HttpException) {
                Embed.BuildEmbed(EmojiEnum.Annoyed)
                    .AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
            }

            await Embed.SendEmbed(Context.Channel);
        }

    }
}
