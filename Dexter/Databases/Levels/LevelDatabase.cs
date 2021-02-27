using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Dexter.Databases.Levels {
    
    public class LevelDatabase : Database {

        public LevelingConfiguration LevelingConfiguration { get; set; }

        public BotConfiguration BotConfiguration { get; set; }

        public DbSet<UserLevel> Levels { get; set; }

        public async void IncrementUserXP (int XPIncreased, IGuildUser User, DiscordSocketClient DiscordSocketClient) {
            if (Levels.Find(User.Id) == null) {
                Levels.Add(new UserLevel() { UserID = User.Id, CurrentUserLevel = 0, UserXP = 0 });
                SaveChanges();
            }

            UserLevel Level = Levels.Find(User.Id);

            Level.UserXP += XPIncreased;

            double NextLevel = double.Parse((Level.CurrentUserLevel + 1).ToString());

            double XPRequired = 5 / 3 * Math.Pow(NextLevel, 3) + 22.5 * Math.Pow(NextLevel, 2) + 455 / 6;

            if (Level.UserXP > XPRequired) {
                Level.CurrentUserLevel += 1;

                if (LevelingConfiguration.Levels.ContainsKey(Level.CurrentUserLevel))
                    if (!User.RoleIds.Contains(LevelingConfiguration.Levels[Level.CurrentUserLevel])) {
                        IRole Role = User.Guild.GetRole(LevelingConfiguration.Levels[Level.CurrentUserLevel]);

                        await User.AddRoleAsync(Role);

                        await new EmbedBuilder().BuildEmbed(EmojiEnum.Love, BotConfiguration)
                            .WithTitle("You Just Leveled Up!")
                            .WithDescription($"OwO What's this? You just got the {Role.Name} role from spending time in VC!\nCongrats! <3")
                            .WithCurrentTimestamp()
                            .WithFooter("USFurries Staff Team")
                            .SendEmbed(User, DiscordSocketClient.GetChannel(LevelingConfiguration.VoiceTextChannel) as ITextChannel);
                    }
            }

            SaveChanges();
        }

    }

}
