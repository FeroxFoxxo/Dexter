using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{

    public partial class UtilityCommands
    {

        /// <summary>
        /// Creates a VC for use by DivineFur+ Members.
        /// </summary>
        /// <param name="vcName">The name of the VC that the user wishes to create.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("createvc")]
        [Summary("Creates a personal VC [UNIFURSAL+ ONLY].")]
        [ExtendedSummary("Usage: `createvc [Channel Name]`\n" +
            "This command creates a new private voice channels that only those you drag in will be able to interact in. You must be in a voice channel when you use this command so the bot can drag you in.\n" +
            "If this channel is empty at any time, it will automatically be deleted.\n" +
            "To move users in, tell them to join the Waiting Room channel which exists in the private VC category (if it doesn't exist, it will be created when you create a new channel); then drag them in from there.")]
        [Alias("privatevc")]
        [RequireUnifursal]
        [BotChannel]

        public async Task CreateVCCommand([Remainder] string vcName)
        {
            IGuildUser user = Context.Guild.GetUser(Context.User.Id);

            if (vcName.Length > 100 || vcName.Length == 0)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Invalid Channel Name")
                    .WithDescription(
                        "Looks like we ran into an error! " +
                        "Your private channel name must be between 1-100 characters long. " +
                        $"Your current channel name is {vcName.Length} characters long.")

                    .SendEmbed(Context.Channel);
            }
            else if (Context.Guild.Channels.Where(channel => channel.Name == vcName).FirstOrDefault() != null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Channel Name Already Exists!")
                    .WithDescription(
                        "Looks like we ran into an error! " +
                        "Please ensure your channel name does not equal the name of another channel, " +
                        "as this leads to confusion for other members. Please make sure this is different!!")

                    .SendEmbed(Context.Channel);
            }
            else if (vcName == UtilityConfiguration.WaitingVCName)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Please do not set your VC to the waiting lobby name!")
                    .WithDescription(
                        "Looks like we ran into an error! " +
                        "Please ensure your channel name does not equal the name of another channel, " +
                        "as this leads to confusion for other members. Please make sure this is different!!")

                    .SendEmbed(Context.Channel);
            }
            else if (user.VoiceChannel == null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("You're not in a voice channel!")
                    .WithDescription(
                        "Haiya! To be able to create a voice channel, you have to be in one first! This is so Dexter can drag you in. Please join a VC and then run this command!")

                    .SendEmbed(Context.Channel);
            }
            else
            {
                IVoiceChannel? waitingChannel = Context.Guild.VoiceChannels.FirstOrDefault(Channel => Channel.Name == UtilityConfiguration.WaitingVCName);

                IRole awooRole = Context.Guild.GetRole(LevelingConfiguration.Levels[LevelingConfiguration.Levels.Keys.Min()]);
                IRole staffRole = Context.Guild.GetRole(BotConfiguration.ModeratorRoleID);

                if (waitingChannel is null)
                {
                    IRole vcRequiredRole = Context.Guild.GetRole(BotConfiguration.UnifursalRoleID);
                    IRole greetFurRole = Context.Guild.GetRole(BotConfiguration.GreetFurRoleID);

                    waitingChannel = await Context.Guild.CreateVoiceChannelAsync(UtilityConfiguration.WaitingVCName);

                    await waitingChannel.ModifyAsync((VoiceChannelProperties properties) => properties.CategoryId = UtilityConfiguration.PrivateCategoryID);

                    await waitingChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, OverwritePermissions.DenyAll(waitingChannel));

                    await waitingChannel.AddPermissionOverwriteAsync(awooRole, OverwritePermissions.DenyAll(waitingChannel).Modify(viewChannel: PermValue.Allow, connect: PermValue.Allow));

                    await waitingChannel.AddPermissionOverwriteAsync(vcRequiredRole, OverwritePermissions.DenyAll(waitingChannel).Modify(viewChannel: PermValue.Allow, connect: PermValue.Allow, moveMembers: PermValue.Allow));
                    await waitingChannel.AddPermissionOverwriteAsync(greetFurRole, OverwritePermissions.DenyAll(waitingChannel).Modify(viewChannel: PermValue.Allow, connect: PermValue.Allow, moveMembers: PermValue.Allow));

                    await waitingChannel.AddPermissionOverwriteAsync(staffRole, OverwritePermissions.InheritAll.Modify(manageChannel: PermValue.Allow, viewChannel: PermValue.Allow, connect: PermValue.Allow, speak: PermValue.Allow, muteMembers: PermValue.Allow, deafenMembers: PermValue.Allow, moveMembers: PermValue.Allow, useVoiceActivation: PermValue.Allow));
                }

                IVoiceChannel newChannel = await Context.Guild.CreateVoiceChannelAsync(vcName);

                await newChannel.ModifyAsync((VoiceChannelProperties properties) => properties.CategoryId = UtilityConfiguration.PrivateCategoryID);

                await newChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, OverwritePermissions.DenyAll(waitingChannel));

                await newChannel.AddPermissionOverwriteAsync(Context.User, OverwritePermissions.AllowAll(newChannel).Modify(createInstantInvite: PermValue.Deny, prioritySpeaker: PermValue.Deny));

                await newChannel.AddPermissionOverwriteAsync(awooRole, OverwritePermissions.DenyAll(newChannel).Modify(speak: PermValue.Allow, useVoiceActivation: PermValue.Allow, stream: PermValue.Allow));

                await newChannel.AddPermissionOverwriteAsync(staffRole, OverwritePermissions.InheritAll.Modify(manageChannel: PermValue.Allow, viewChannel: PermValue.Allow, connect: PermValue.Allow, speak: PermValue.Allow, muteMembers: PermValue.Allow, deafenMembers: PermValue.Allow, moveMembers: PermValue.Allow, useVoiceActivation: PermValue.Allow));

                await user.ModifyAsync((GuildUserProperties properties) =>
                {
                    properties.Channel = new Optional<IVoiceChannel>(newChannel);
                });

                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle($"Created \"{vcName}\"")
                    .WithDescription("Haiya! Your private voice channel has sucessfully been created. " +
                        "You should have full permission to edit it, move members and much more! " +
                        "Have fun~!")

                    .SendEmbed(Context.Channel);
            }
        }

    }

}
