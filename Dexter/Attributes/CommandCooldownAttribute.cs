using Dexter.Configurations;
using Dexter.Databases.Cooldowns;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Attributes {

    public class CommandCooldownAttribute : PreconditionAttribute {

        private readonly int CooldownTimer;

        public CommandCooldownAttribute(int CooldownTimer) {
            this.CooldownTimer = CooldownTimer;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext CommandContext, CommandInfo CommandInfo, IServiceProvider ServiceProvider) {
            if (ServiceProvider.GetService<BotConfiguration>() == null ||
                    InitializeDependencies.ServiceProvider.GetRequiredService<BotConfiguration>().BotChannels.Contains(CommandContext.Channel.Id))
                return PreconditionResult.FromSuccess();

            CooldownDB CooldownDB = InitializeDependencies.ServiceProvider.GetRequiredService<CooldownDB>();

            Cooldown Cooldown = CooldownDB.Cooldowns.AsQueryable()
                .Where(Cooldown => Cooldown.Token.Equals($"{CommandInfo.Name}{CommandContext.Channel.Id}")).FirstOrDefault();

            if (Cooldown != null) {
                if (Cooldown.TimeOfCooldown + CooldownTimer < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                    CooldownDB.Remove(Cooldown);
                    await CooldownDB.SaveChangesAsync();
                } else {
                    DateTime Time = DateTime.UnixEpoch.AddSeconds(Cooldown.TimeOfCooldown + CooldownTimer);

                    await new EmbedBuilder().BuildEmbed(EmojiEnum.Wut)
                        .WithAuthor($"Hiya, {CommandContext.User.Username}!")
                        .WithTitle($"Please wait {Time.Humanize()} until you are able to use this command.")
                        .WithDescription("Thanks for your patience, we really do appreciate it. <3")
                        .SendEmbed(CommandContext.User, CommandContext.Channel as ITextChannel);

                    return PreconditionResult.FromError("");
                }
            }

            CooldownDB.Add(
                new Cooldown() {
                    Token = $"{CommandInfo.Name}{CommandContext.Channel.Id}",
                    TimeOfCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            );

            await CooldownDB.SaveChangesAsync();

            return PreconditionResult.FromSuccess();
        }

    }

}
