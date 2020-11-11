using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Dexter.Attributes {

    /// <summary>
    /// The Require Permission Level attribute is an abstract class that extends the superclass of the
    /// precondition attribute. This will run a check to see if the user meets the required permission
    /// level specified by the class that extends this, and if so to run the command. It is applied to methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class RequirePermissionLevelAttribute : PreconditionAttribute {

        /// <summary>
        /// The Permission Level is the level at which a user has to meet or exceed to be able to run the command.
        /// </summary>
        public readonly PermissionLevel Level;

        /// <summary>
        /// The RequirePermissionLevel constructor takes in the level at which a user has to be at to run the command.
        /// </summary>
        /// <param name="Level">The permission level required to run the command.</param>
        public RequirePermissionLevelAttribute(PermissionLevel Level) {
            this.Level = Level;
        }

        /// <summary>
        /// The CheckPermissionsAsync is an overriden method from its superclass, which checks
        /// to see if a command can be run by a user through their roles that they have applied.
        /// </summary>
        /// <param name="Context">The Context is used to find the user who has run the command.</param>
        /// <param name="Command">The Command is used to find the name of the command that has been run.</param>
        /// <param name="Services">The Services are used to find the role IDs to get the permission level of the user from the BotConfiguration.</param>
        /// <returns></returns>
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            return Task.FromResult((Context.User as IGuildUser).GetPermissionLevel(Services.GetRequiredService<BotConfiguration>()) >= Level
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"Haiya! To run the {Command.Name} command you need to have the " +
                $"{Level} role! Are you sure you're a {Level.ToString().ToLower()}? <3"));
        }

    }

}
