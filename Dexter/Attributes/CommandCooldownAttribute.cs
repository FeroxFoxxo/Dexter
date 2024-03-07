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

namespace Dexter.Attributes
{

    /// <summary>
    /// The Command Cooldown attribute ensures that the given command does not run
    /// multiple times in the same channel as to reduce on spam. It does this through
    /// adding the cooldown of the command to the cooldown database and will not run this
    /// command again in that same channel unless either the cooldown has expired or the
    /// channel is a bot channel.
    /// </summary>
    /// <remarks>
    /// The constructor of the attribute sets the cooldown time of the command.
    /// </remarks>
    /// <param name="cooldownTimer">The Cooldown Timer is the set time it will take until this command is able to be rerun.</param>

    public class CommandCooldownAttribute(int cooldownTimer) : PreconditionAttribute
    {

        /// <summary>
        /// The Cooldown Time is the time it will take for the cooldown on this command to expire.
        /// It is set through the constructor of the attribute.
        /// </summary>

        public readonly int CooldownTimer = cooldownTimer;
        const int RETRIES = 5;

        /// <summary>
        /// The CheckPermissionsAsync is an overriden method from its superclass, which checks
        /// to see if a command can be run by a user through the cooldown of the command in the database.
        /// </summary>
        /// <param name="context">The Context is used to find the channel that has had the command run in.</param>
        /// <param name="commandInfo">The CommandInfo is used to find the name of the command that has been run.</param>
        /// <param name="services">The ServiceProvider is used to get the database of cooldowns.</param>
        /// <returns>The result of the checked permission, returning successful if it is able to be run or an error if not.
        /// This error is then thrown to the Command Handler Service to log to the user.</returns>

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo commandInfo, IServiceProvider services)
        {
            if (services.GetService<BotConfiguration>() == null)
            {
                return PreconditionResult.FromSuccess();
            }

            if (services.GetRequiredService<BotConfiguration>().BotChannels.Contains(context.Channel.Id))
            {
                return PreconditionResult.FromSuccess();
            }

            CooldownDB cooldownDB = services.GetRequiredService<CooldownDB>();

            Cooldown cooldown = cooldownDB.Cooldowns.Find($"{commandInfo.Name}{context.Channel.Id}");

            if (cooldown != null)
            {
                if (cooldown.TimeOfCooldown + CooldownTimer < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    cooldownDB.Remove(cooldown);
                }
                else
                {
                    DateTime time = DateTime.UnixEpoch.AddSeconds(cooldown.TimeOfCooldown + CooldownTimer);

                    await new EmbedBuilder().BuildEmbed(EmojiEnum.Wut, services.GetService<BotConfiguration>(), EmbedCallingType.Command)
                        .WithAuthor($"Hiya, {context.User.Username}!")
                        .WithTitle($"Please wait {time.Humanize()} until you are able to use this command.")
                        .WithDescription("Thanks for your patience, we really do appreciate it. <3")
                        .SendEmbed(context.User, context.Channel as ITextChannel);

                    return PreconditionResult.FromError("");
                }
            }

            cooldownDB.Add(
                new Cooldown()
                {
                    Token = $"{commandInfo.Name}{context.Channel.Id}",
                    TimeOfCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            );

            for (int i = 0; i < RETRIES; i++)
            {
                try
                {
                    cooldownDB.SaveChanges();
                    break;
                }
                catch
                {
                    Console.WriteLine($"Failed to save CooldownDB, attempt {i + 1}/{RETRIES}");
                    await Task.Yield();
                }
            }

            return PreconditionResult.FromSuccess();
        }

    }

}
