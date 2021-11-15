using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Databases.EventTimers;
using Dexter.Databases.UserProfiles;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Runtime.InteropServices;

namespace Dexter.Commands
{

    public partial class ModeratorCommands
    {
        /// <summary>
        /// Sets a user's age as "verified" in their profile.
        /// </summary>
        /// <remarks>This command is admin-only.</remarks>
        /// <param name="userId">The unique ID of the target user to apply the status to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("ageverify")]
        [Alias("verifyage")]
        [RequireAdministrator]
        [Priority(1)]

        public async Task AgeVerify(ulong userId)
        {
            IUser u = await Client.Rest.GetUserAsync(userId);

            if (u is null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to find user!")
                    .WithDescription("You might have copied a message ID instead of a user ID, confirm that the ID is correct and try again.")
                    .SendEmbed(Context.Channel);
                return;
            }

            await AgeVerify(u);
        }

        /// <summary>
        /// Sets a user's age as "verified" in their profile.
        /// </summary>
        /// <remarks>This command is admin-only.</remarks>
        /// <param name="user">The target user to apply the status to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("ageverify")]
        [Summary("Grants a user the verified age status in their Dexter Profile; thereby preventing them from modifying it further and adding a (verified) tag to it.")]
        [Alias("verifyage")]
        [RequireAdministrator]
        [Priority(2)]

        public async Task AgeVerify(IUser user)
        {
            if (user is null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to find user!")
                    .WithDescription("This may be due to caching, try to use the user's ID instead of a mention if you didn't.")
                    .SendEmbed(Context.Channel);
                return;
            }

            UserProfile profile = ProfilesDB.Profiles.Find(user.Id);

            if (profile is null || (profile.BirthYear == default && !profile.Settings.AgeVerified))
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("This user hasn't configured their age!")
                    .WithDescription("The user's birth year has been reset or has never been set to a proper value.")
                    .SendEmbed(Context.Channel);
                return;
            }

            ProfilePreferences temp = profile.Settings;
            if (profile.Settings.AgeVerified)
            {
                temp.AgeVerified = false;
                profile.Settings = temp;
                ProfilesDB.SaveChanges();

                await BuildEmbed(EmojiEnum.Sign)
                    .WithTitle("Age verification revoked!")
                    .WithDescription($"{user.Mention} is not longer flagged as verified")
                    .SendEmbed(Context.Channel);
                return;
            }

            DayInYear day = profile.Borkday;
            int year = profile.BirthYear ?? 0;

            string resultBirthday = day != default
                ? $"{day}, {year}"
                : $"year: {year}";

            temp.AgeVerified = true;
            profile.Settings = temp;
            ProfilesDB.SaveChanges();

            await BuildEmbed(EmojiEnum.Sign)
                .WithTitle("Age verification applied!")
                .WithDescription($"{user.Mention} has had their birth date verified as {resultBirthday}")
                .SendEmbed(Context.Channel);
        }

        /// <summary>
        /// Grants information about how to verify
        /// </summary>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("ageverify")]
        [Summary("Provides information about how to get your age verified by administrators")]
        [Alias("verifyage")]
        [BotChannel]

        public async Task VerifyAgeInfo()
        {
            await BuildEmbed(EmojiEnum.Sign)
                .WithTitle("Age Verification Process")
                .WithDescription("To verify your age, contact an admin inquiring about age verification.\n" +
                    "You will need to provide proof of age in the form of a government-issued ID (properly __**censoring all personal information**__ ASIDE from the birth date).\n" +
                    "The picture must also include the following information written on a piece of paper:\n" +
                    "-Your Discord tag\n" +
                    "-The current date")
                .SendEmbed(Context.Channel);
        }

    }

}
