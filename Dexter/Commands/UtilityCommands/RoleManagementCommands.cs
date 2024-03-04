using Dexter.Attributes.Methods;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Commands
{
    partial class UtilityCommands
    {
        /// <summary>
        /// Grants the Artist role to a given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The user to add the role to.</param>
        /// <param name="options">Options when executing the command, such as "silent" to avoid mentioning the user.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the command completes successfully.</returns>

        [Command("artist")]
        [Summary("Grants the Artist role to a target user. Available options are: \"silent\", \"remove\".")]
        [ExtendedSummary("Grants the Artist role to a target user.\n" +
            "Use the \"silent\" option after the user to avoid mentioning them.\n" +
            "Use the \"remove\" option to remove the role instead of adding it.")]
        [RequireModerator]

        public async Task GrantArtistRoleCommand(IGuildUser target, [Remainder] string options = "")
        {
            IRole r = Context.Guild.GetRole(UtilityConfiguration.ArtistRole);
            await GrantRoleCommand(r, target, options);
        }

        /// <summary>
        /// Grants the External Emoji Enabled role to a given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The user to add the role to.</param>
        /// <param name="options">Options when executing the command, such as "silent" to avoid mentioning the user.</param>
        /// <returns>A <see cref="Task"/> object, which can be awaited until the command completes successfully.</returns>

        [Command("externalemotes")]
        [Summary("Grants the External Emoji Enabled role to a target user.  Available options are: \"silent\", \"remove\".")]
        [ExtendedSummary("Grants the External Emoji Enabled role to a target user.\n" +
            "Use the \"silent\" option after the user to avoid mentioning them.\n" +
            "Use the \"remove\" option to remove the role instead of adding it.")]
        [Alias("externalemoji")]
        [RequireModerator]

        public async Task GrantExternalEmotesCommand(IGuildUser target, [Remainder] string options = "")
        {
            IRole r = Context.Guild.GetRole(UtilityConfiguration.ExternalEmotesRole);
            await GrantRoleCommand(r, target, options);
        }

        private async Task GrantRoleCommand(IRole role, IGuildUser target, string options = "")
        {
            options = options.ToLower();
            bool silent = options.Contains("silent");
            bool remove = options.Contains("remove");

            if (target is null)
            {
                await Context.Channel.SendMessageAsync("Unable to find the given user! This may be due to caching. The target user must be in the server.");
                return;
            }

            string targetname = silent ? target.Nickname ?? target.Username : target.Mention;

            if (target.RoleIds.Contains(role.Id) != remove)
            {
                if (remove)
                    await Context.Channel.SendMessageAsync($"The target user ({targetname}) doesn't have the {role.Name} role!");
                else
                    await Context.Channel.SendMessageAsync($"The target user ({targetname}) already has the {role.Name} role!");
                return;
            }

            try
            {
                if (remove)
                    await target.RemoveRoleAsync(role);
                else
                    await target.AddRoleAsync(role);
            }
            catch
            {
                await BuildEmbed(Enums.EmojiEnum.Annoyed)
                    .WithTitle("Unable to manage roles!")
                    .WithDescription($"It seems I am missing permissions to add/remove this role ({role.Name}) to the target user!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await Context.Channel.SendMessageAsync($"Successfully {(remove ? "removed" : "added")} the {role.Name} role {(remove ? "from" : "to")} {targetname}!");
        }
    }
}
