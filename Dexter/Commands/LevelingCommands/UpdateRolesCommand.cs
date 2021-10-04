using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Discord.Rest;
using static Dexter.Services.LevelingService;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {

        /// <summary>
        /// Updates the roles for the user who uses this command based on Dex XP.
        /// </summary>
        /// <returns></returns>

        [Command("updateroles")]
        [Summary("Updates your ranked roles to fit the Dexter System XP")]
        [BotChannel]

        public async Task UpdateRolesCommand()
        {
            RestGuildUser user = await DiscordShardedClient.Rest.GetGuildUserAsync(Context.Guild?.Id ?? BotConfiguration.GuildID, Context.User.Id);

            if (user is null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to find user in the server!")
                    .WithDescription("You must be in the server for this command to work. If you are, this error may be due to caching, try again later.")
                    .SendEmbed(Context.Channel);
                return;
            }

            RoleModificationResponse response = await LevelingService.UpdateRolesWithInfo(user, true);

            if (!response.success)
            {
                await Context.Channel.SendMessageAsync(response.ToString());
            }
            else
            {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Operation Successful!")
                    .WithDescription(response.ToString())
                    .SendEmbed(Context.Channel);
            }

        }
    }
}
