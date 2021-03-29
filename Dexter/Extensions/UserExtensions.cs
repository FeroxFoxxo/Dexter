using Dexter.Configurations;
using Dexter.Enums;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;

namespace Dexter.Extensions {

    /// <summary>
    /// The User Extensions class offers a variety of different extensions that can be applied to user to return specific attributes.
    /// </summary>
    
    public static class UserExtensions {

        /// <summary>
        /// The GetPermissionLevel returns the highest permission the user has access to for commands.
        /// </summary>
        /// <param name="User">The User of which you want to get the permission level of.</param>
        /// <param name="DiscordSocketClient">The instance of the DiscordSocketUser which is used to get the main guild.</param>
        /// <param name="BotConfiguration">The instance of the bot configuration which is used to get the role ID for roles.</param>
        /// <returns>What permission level the user has, in the form from the PermissionLevel enum.</returns>

        public static PermissionLevel GetPermissionLevel(this IUser User, DiscordSocketClient DiscordSocketClient, BotConfiguration BotConfiguration) {
            IGuildUser GuildUser = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(User.Id);

            if (GuildUser == null)
                return PermissionLevel.Default;
            else if (GuildUser.RoleIds.Contains(BotConfiguration.AdministratorRoleID))
                return PermissionLevel.Administrator;
            else if (GuildUser.RoleIds.Contains(BotConfiguration.DeveloperRoleID))
                return PermissionLevel.Developer;
            else if (GuildUser.RoleIds.Contains(BotConfiguration.ModeratorRoleID))
                return PermissionLevel.Moderator;
            else if (GuildUser.RoleIds.Contains(BotConfiguration.GreetFurRoleID))
                return PermissionLevel.GreetFur;
            else
                return PermissionLevel.Default;
        }

        /// <summary>
        /// The GetUserInformation method returns a string of the users username, followed by the discriminator, the mention and the ID.
        /// It is used as a standardized way throughout the bot to display information on a user.
        /// </summary>
        /// <param name="User">The user of which you want to create the standardized string of the user's information of.</param>
        /// <returns>A string which contains the user's username, discriminator, mention and ID.</returns>
        
        public static string GetUserInformation(this IUser User) {
            return $"{User.Username}#{User.Discriminator} ({User.Mention}) ({User.Id})";
        }

        /// <summary>
        /// Returns the URL for a User's avatar, or the URL of the user's Default Discord avatar (Discord logo with a set background color) if they're using a default avatar.
        /// </summary>
        /// <param name="User">Target user whose avatar is being obtained.</param>
        /// <param name="DefaultSize">The size of the image to return in. This can be any power of 2 in the range [16, 2048].</param>
        /// <returns>A string holding the URL of the target user's avatar.</returns>

        public static string GetTrueAvatarUrl (this IUser User, ushort DefaultSize = 128) {
            return string.IsNullOrEmpty(User.GetAvatarUrl(size: DefaultSize)) ? User.GetDefaultAvatarUrl() : User.GetAvatarUrl(size: DefaultSize);
        }

    }

}
