using Dexter.Configuration;
using Dexter.Core.Enums;
using Discord;
using System;
using System.Linq;

namespace Dexter.Core.Extensions {
    public static class UserExtensions {

        public static PermissionLevel GetPermissionLevel(this IGuildUser User, BotConfiguration Configuration) {
            if (User.GuildPermissions.Has(GuildPermission.Administrator))
                return PermissionLevel.Administrator;

            if (User.RoleIds.Contains(Configuration.ModeratorRoleID))
                return PermissionLevel.Moderator;

            return PermissionLevel.Default;
        }

        public static string GetUserInformation(this IUser User) {
            return $"{User.Username}#{User.Discriminator} ({User.Mention}) ({User.Id})";
        }

    }
}
