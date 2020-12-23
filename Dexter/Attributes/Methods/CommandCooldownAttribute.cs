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

namespace Dexter.Attributes.Methods {

    /// <summary>
    /// The Command Cooldown attribute ensures that the given command does not run
    /// multiple times in the same channel as to reduce on spam. It does this through
    /// adding the cooldown of the command to the cooldown database and will not run this
    /// command again in that same channel unless either the cooldown has expired or the
    /// channel is a bot channel.
    /// </summary>
    
    public class CommandCooldownAttribute : PreconditionAttribute {

        /// <summary>
        /// The Cooldown Time is the time it will take for the cooldown on this command to expire.
        /// It is set through the constructor of the attribute.
        /// </summary>
        
        public readonly int CooldownTimer;

        /// <summary>
        /// The constructor of the attribute sets the cooldown time of the command.
        /// </summary>
        /// <param name="CooldownTimer">The Cooldown Timer is the set time it will take until this command is able to be rerun.</param>
        
        public CommandCooldownAttribute(int CooldownTimer) {
            this.CooldownTimer = CooldownTimer;
        }

        /// <summary>
        /// The CheckPermissionsAsync is an overriden method from its superclass, which checks
        /// to see if a command can be run by a user through the cooldown of the command in the database.
        /// </summary>
        /// <param name="CommandContext">The Context is used to find the channel that has had the command run in.</param>
        /// <param name="CommandInfo">The CommandInfo is used to find the name of the command that has been run.</param>
        /// <param name="ServiceProvider">The ServiceProvider is used to get the database of cooldowns.</param>
        /// <returns>The result of the checked permission, returning successful if it is able to be run or an error if not.
        /// This error is then thrown to the Command Handler Service to log to the user.</returns>

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext CommandContext, CommandInfo CommandInfo, IServiceProvider ServiceProvider) {
            if (ServiceProvider.GetService<BotConfiguration>() == null)
                return PreconditionResult.FromSuccess();

            if (ServiceProvider.GetRequiredService<BotConfiguration>().BotChannels.Contains(CommandContext.Channel.Id))
                return PreconditionResult.FromSuccess();

            CooldownDB CooldownDB = ServiceProvider.GetRequiredService<CooldownDB>();

            Cooldown Cooldown = CooldownDB.Cooldowns.Find($"{CommandInfo.Name}{CommandContext.Channel.Id}");

            if (Cooldown != null) {
                if (Cooldown.TimeOfCooldown + CooldownTimer < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                    CooldownDB.Remove(Cooldown);
                    CooldownDB.SaveChanges();
                } else {
                    DateTime Time = DateTime.UnixEpoch.AddSeconds(Cooldown.TimeOfCooldown + CooldownTimer);

                    await new EmbedBuilder().BuildEmbed(EmojiEnum.Wut, ServiceProvider.GetService<BotConfiguration>())
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

            CooldownDB.SaveChanges();

            return PreconditionResult.FromSuccess();
        }

    }

}
