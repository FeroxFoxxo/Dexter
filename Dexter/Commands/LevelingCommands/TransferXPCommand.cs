using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Databases.Levels;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {
        /// <summary>
        /// Transfers XP from a user to another user.
        /// </summary>
        /// <param name="idfrom">The user to take XP from</param>
        /// <param name="idto">The user to give XP to</param>
        /// <param name="textxpcap">The maximum amount of text XP to transfer</param>
        /// <param name="voicexpcap">The maximum amount of voice XP to tranfer</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("transferxp")]
        [BotChannel]
        [Summary("Transfers all text and voice XP from a level record attached to one User ID to that attached to another.")]
        [ExtendedSummary("Usage: `transferxp [from] [to] (max textxp) (max voicexp)` \n" +
            "Transfers all text and voice XP from a level record attached to one User ID to that attached to another. If no maxima are specified, no cap will be considered.")]
        [RequireAdministrator]
        [Priority(1)]

        public async Task TransferXPCommand(ulong idfrom, ulong idto, long textxpcap = -1, long voicexpcap = -1)
        {
            UserLevel? datafrom = LevelingDB.Levels.Find(idfrom);

            if (datafrom is null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to find user!")
                    .WithDescription($"Whoops, it seems we don't have any user whose ID is {idfrom} in our database!")
                    .SendEmbed(Context.Channel);
                return;
            }
            
            if (datafrom.TextXP + datafrom.VoiceXP == 0)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("\"from\" user has no XP")
                    .WithDescription($"Our record of user {idfrom} has no text or voice XP; no XP can be transferred; perhaps the transferrence already occurred?")
                    .SendEmbed(Context.Channel);
                return;
            }

            UserLevel datato = LevelingDB.GetOrCreateLevelData(idto);

            long textxp = datafrom.TextXP;
            long voicexp = datafrom.VoiceXP;

            if (textxpcap > 0 && textxp > textxpcap) textxp = textxpcap;
            if (voicexpcap > 0 && voicexp > voicexpcap) voicexp = voicexpcap;

            datafrom.TextXP -= textxp;
            datafrom.VoiceXP -= voicexp;
            datato.TextXP += textxp;
            datato.VoiceXP += voicexp;

            await LevelingDB.SaveChangesAsync();

            await BuildEmbed(EmojiEnum.Love)
                .WithTitle("Transferrence successful!")
                .WithDescription($"Successfully transferred {textxp} text XP and {voicexp} voice XP from user {idfrom} to user {idto}.\n" +
                $"If you wish to undo this; use the command `{BotConfiguration.Prefix}transferxp {idto} {idfrom} {textxp} {voicexp}`.")
                .SendEmbed(Context.Channel);

            IGuildUser? userfrom = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetUser(idfrom);
            IGuildUser? userto = DiscordShardedClient.GetGuild(BotConfiguration.GuildID).GetUser(idto);

            if (userfrom is not null) await LevelingService.UpdateRoles(userfrom, true);
            if (userto is not null) await LevelingService.UpdateRoles(userto, true);
        }

        /// <summary>
        /// Transfers XP from a user to another user.
        /// </summary>
        /// <param name="from">The user to take XP from</param>
        /// <param name="to">The user to give XP to</param>
        /// <param name="textxpcap">The maximum amount of text XP to transfer</param>
        /// <param name="voicexpcap">The maximum amount of voice XP to tranfer</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("transferxp")]
        [BotChannel]
        [RequireAdministrator]

        public async Task TransferXPCommand(IUser from, IUser to, long textxpcap = -1, long voicexpcap = -1)
        {
            await TransferXPCommand(from.Id, to.Id, textxpcap, voicexpcap);
        }

    }
}
