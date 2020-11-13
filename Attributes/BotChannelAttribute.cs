using Dexter.Configurations;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Attributes {

    /// <summary>
    /// The BotChannel attribute specifies the command can only be used in the bot channels
    /// provided through the BotConfiguration file. It is applied to the command's method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class BotChannelAttribute : PreconditionAttribute {

        /// <summary>
        /// The CheckPermissionsAsync is an overriden method from its superclass,
        /// which checks to see if a command can be run in the specified channel.
        /// </summary>
        /// <param name="Context">The Context is used to find the name of the channel that the message has been sent in.</param>
        /// <param name="Command">The Command is used to find the name of the command run,
        /// and to specify whether or not it is able to run in a given channel.</param>
        /// <param name="Services">The Services is the Service Provider which contains references to initialized classes
        /// such as the BotConfigurations class, used to find if the command has been run in a specified bot channel.</param>
        /// <returns>The result of the checked permission, returning successful if it is able to be run or an error if not.
        /// This error is then thrown to the Command Handler Service to log to the user.</returns>
        public override Task<PreconditionResult> CheckPermissionsAsync (ICommandContext Context,
                CommandInfo Command, IServiceProvider Services) {

            return Task.FromResult(
                InitializeDependencies.BotConfiguration.BotChannels.Contains(Context.Channel.Id) ?
                    PreconditionResult.FromSuccess() :
                    PreconditionResult.FromError($"Heya! You're not permitted to use the command {Command.Name} " +
                    $"in the channel {Context.Channel.Name}. Please use a designated bot channel instead <3"));
        }

    }

}
