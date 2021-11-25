using Dexter.Attributes.Methods;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [Summary("Grants the Artist role to a target user. Use the \"silent\" option after the user to avoid mentioning them.")]
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
        [Summary("Grants the External Emoji Enabled role to a target user. Use the \"silent\" option after the user to avoid mentioning them.")]
        [Alias("externalemoji")]
        [RequireModerator]

        public async Task GrantExternalEmotesCommand(IGuildUser target, [Remainder] string options = "")
        {
            IRole r = Context.Guild.GetRole(UtilityConfiguration.ExternalEmotesRole);
            await GrantRoleCommand(r, target, options);
        }

        private async Task GrantRoleCommand(IRole role, IGuildUser target, string options = "")
        {
            bool silent = options.Contains("silent");

            if (target is null)
            {
                await Context.Channel.SendMessageAsync("Unable to find the given user! This may be due to caching. The target user must be in the server.");
                return;
            }

            if (target.RoleIds.Contains(role.Id))
            {
                await Context.Channel.SendMessageAsync($"The target user ({(silent ? target.Nickname ?? target.Username : target.Mention)}) already has the {role.Name} role!");
                return;
            }

            try
            {
                await target.AddRoleAsync(role);
            }
            catch
            {
                await BuildEmbed(Enums.EmojiEnum.Annoyed)
                    .WithTitle("Unable to add role!")
                    .WithDescription($"It seems I am missing permissions to add this role ({role.Name}) to the target user!")
                    .SendEmbed(Context.Channel);
                return;
            }

            await Context.Channel.SendMessageAsync($"Successfully added the {role.Name} role to {(silent ? target.Nickname ?? target.Username : target.Mention)}!");
        }
    }
}
