using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;

namespace Dexter.Commands
{

    public partial class UtilityCommands
    {

        /// <summary>
        /// Creates a VC for use by DivineFur+ Members.
        /// </summary>
        /// <param name="VCName">The name of the VC that the user wishes to create.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("createvc")]
        [Summary("Creates a personal VC [DIVINE FUR+ ONLY].")]
        [Alias("privatevc")]
        [RequireDivineFur]

        public async Task CreateVCCommand([Remainder] string VCName)
        {
            if (VCName.Length > 100 || VCName.Length == 0)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Invalid Channel Name")
                    .WithDescription(
                        "Looks like we ran into an error! " +
                        "Your private channel name must be between 1-100 characters long. " +
                        $"Your current channel name is {VCName.Length} characters long.")
                    .WithCurrentTimestamp()
                    .WithFooter("USFurries Level Rewards")
                    .SendEmbed(Context.Channel);
                return;
            }

            IGuildUser User = Context.Guild.GetUser(Context.User.Id);

            if (User.VoiceChannel == null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("You're not in a voice channel!")
                    .WithDescription(
                        "Haiya! To be able to create a voice channel, you have to be in one first! This is so Dexter can drag you in. Please join a VC and then run this command!")
                    .WithCurrentTimestamp()
                    .WithFooter("USFurries Level Rewards")
                    .SendEmbed(Context.Channel);
                return;
            }

            IVoiceChannel Channel = await Context.Guild.CreateVoiceChannelAsync(VCName);

            await Channel.ModifyAsync((VoiceChannelProperties properties) => properties.CategoryId = UtilityConfiguration.PrivateCategoryID);

            await Channel.AddPermissionOverwriteAsync(Context.User, OverwritePermissions.AllowAll(Channel).Modify(createInstantInvite: PermValue.Deny, prioritySpeaker: PermValue.Deny));

            IRole role = Context.Guild.GetRole(LevelingConfiguration.Levels.Values.Min());

            await Channel.AddPermissionOverwriteAsync(role, OverwritePermissions.DenyAll(Channel).Modify(viewChannel: PermValue.Allow, speak: PermValue.Allow, useVoiceActivation: PermValue.Allow, stream: PermValue.Allow));

            await User.ModifyAsync((GuildUserProperties Properties) =>
            {
                Properties.Channel = new Optional<IVoiceChannel> ( Channel );
            });

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle($"Created {VCName}.")
                .WithDescription("Haiya! Your private voice channel has sucessfully been created. " +
                    "You should have full permission to edit it, move members and much more! " +
                    "Have fun~!")
                .WithCurrentTimestamp()
                .WithFooter("USFurries Level Rewards")
                .SendEmbed(Context.Channel);
        }

    }

}
