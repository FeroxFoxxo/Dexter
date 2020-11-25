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
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BotChannelParameterAttribute : ParameterPreconditionAttribute {

        /// <summary>
        /// The CheckPermissionsAsync is an overriden method from its superclass,
        /// which checks to see if a command can be run in the specified channel.
        /// </summary>
        /// <param name="CommandContext">The Context is used to find the name of the channel that the message has been sent in.</param>
        /// <param name="ParameterInfo">The information regarding the parameter that the command has failed on.</param>
        /// <param name="Parameter">The parameter that the command has failed on.</param>
        /// <param name="ServiceProvider">The Service Provider contains references to initialized classes
        /// such as the BotConfigurations class, used to find if the command has been run in a specified bot channel.</param>
        /// <returns>The result of the checked permission, returning successful if it is able to be run or an error if not.
        /// This error is then thrown to the Command Handler Service to log to the user.</returns>
        public override Task<PreconditionResult> CheckPermissionsAsync (ICommandContext CommandContext, ParameterInfo ParameterInfo, object Parameter, IServiceProvider ServiceProvider) {
            if (ServiceProvider.GetService<BotConfiguration>() == null)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(
                InitializeDependencies.ServiceProvider.GetRequiredService<BotConfiguration>().BotChannels.Contains(CommandContext.Channel.Id) ?
                    PreconditionResult.FromSuccess() :
                    PreconditionResult.FromError($"Heya! You're not permitted to use the command {ParameterInfo.Command.Name} " +
                    $"in the channel {CommandContext.Channel.Name} with the parameter {ParameterInfo.Name}. Please use a designated bot channel instead <3"));
        }

    }

}
