using Dexter.Abstractions;
using Dexter.Attributes;
using Discord;
using Discord.Commands;
using Discord.Net;
using System.Threading.Tasks;

namespace Dexter.Commands.UtilityCommands {
    public partial class UtilityCommands {

        [Command("userdm")]
        [Summary("Sends a direct message to a user!")]
        [Alias("dm", "message")]
        [RequireModerator]
        public async Task UserDMCommand(IGuildUser User, [Remainder] string Message) {
            EmbedBuilder Embed = Context.BuildEmbed(EmojiEnum.Unknown)
                .WithTitle("**User DM**")
                .WithDescription(Message)
                .AddField("Recipient", $"{User.Mention} {User.Username + User.Discriminator} ({User.Id})")
                .AddField("Sent By", $"{Context.User.Mention} {Context.User.Username + Context.User.Discriminator} ({Context.User.Id})");

            try {
                await User.SendMessageAsync($"**__Message From {Context.Guild.Name}__**\n{Message}");

                Embed.WithColor(Color.Green)
                    .AddField("Success", "The DM was successfully sent!")
                    .WithThumbnailUrl(BotConfiguration.ThumbnailURLs[(int)EmojiEnum.Love]);
            } catch (HttpException) {
                Embed.WithColor(Color.Red)
                    .AddField("Failed", "This fluff may have either DMs from the server or me! :C")
                    .WithThumbnailUrl(BotConfiguration.ThumbnailURLs[(int)EmojiEnum.Annoyed]);
            }

            await Embed.SendEmbed(Context.Channel);
        }

    }
}
