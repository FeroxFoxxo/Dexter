using Dexter.Attributes.Methods;
using Dexter.Databases.Levels;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {

        /// <summary>
        /// Removes the associated preferences entry related to a given user.
        /// </summary>
        /// <param name="target">The user to remove preferences for.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("clearrankcard")]
        [Summary("Clears the rank card custom settings for a given user.")]
        [RequireModerator]

        public async Task ClearRankCardCommand(IUser target)
        {
            _ = LevelingDB.GetOrCreateLevelData(target.Id, out LevelPreferences settings);

            LevelingDB.Prefs.Remove(settings);
            LevelingDB.SaveChanges();

            await BuildEmbed(Enums.EmojiEnum.Love)
                .WithTitle("Success!")
                .WithDescription($"Removed user preferences for user {target.Mention}, if none existed, nothing was performed.")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Removes the associated preferences entry related to a given user.
        /// </summary>
        /// <param name="targetID">The ID of the user to remove preferences for.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("clearrankcard")]
        [Summary("Clears the rank card custom settings for a given user by their ID.")]
        [RequireModerator]

        public async Task ClearRankCardCommand(ulong targetID)
        {
            _ = LevelingDB.GetOrCreateLevelData(targetID, out LevelPreferences settings);

            LevelingDB.Prefs.Remove(settings);
            LevelingDB.SaveChanges();

            await BuildEmbed(Enums.EmojiEnum.Love)
                .WithTitle("Success!")
                .WithDescription($"Removed user preferences for user <@{targetID}>, if none existed, nothing was performed.")
                .SendEmbed(Context.Channel);
        }

    }
}
