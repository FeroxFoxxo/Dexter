using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Databases.Levels {
    
    public class LevelDatabase : Database {

        public LevelingConfiguration LevelingConfiguration { get; set; }

        public BotConfiguration BotConfiguration { get; set; }

        public DbSet<UserLevel> Levels { get; set; }

        public async Task IncrementUserXP (int XPIncreased, IGuildUser User, ITextChannel Fallback, bool SendLevelUp) {
            if (Levels.Find(User.Id) == null) {
                Levels.Add(new UserLevel() { UserID = User.Id, CurrentUserLevel = 0, UserXP = 0 });
                await SaveChangesAsync();
            }

            UserLevel Level = Levels.Find(User.Id);

            Level.UserXP += XPIncreased;

            double NextLevel = double.Parse((Level.CurrentUserLevel + 1).ToString());

            double XPRequired = 5.0 / 3.0 * Math.Pow(NextLevel, 3.0) + 22.5 * Math.Pow(NextLevel, 2.0) + 455.0 / 6.0;

            if (Level.UserXP > XPRequired) {
                Level.CurrentUserLevel += 1;

                if (SendLevelUp)
                    await Fallback.SendMessageAsync($"OwO What's this? {User.Mention} just advanced to **level {Level.CurrentUserLevel}!!! YAY!!!");

                if (LevelingConfiguration.Levels.ContainsKey(Level.CurrentUserLevel))
                    if (!User.RoleIds.Contains(LevelingConfiguration.Levels[Level.CurrentUserLevel])) {
                        IRole Role = User.Guild.GetRole(LevelingConfiguration.Levels[Level.CurrentUserLevel]);

                        await User.AddRoleAsync(Role);

                        await new EmbedBuilder().BuildEmbed(EmojiEnum.Love, BotConfiguration)
                            .WithTitle("You Just Ranked Up!")
                            .WithDescription($"OwO, what's this? You just got the {Role.Name} role!\nCongrats~! <3")
                            .WithCurrentTimestamp()
                            .WithFooter($"{User.Guild.Name} Staff Team")
                            .SendEmbed(User, Fallback);
                    }
            }

            await SaveChangesAsync();
        }

    }

}
