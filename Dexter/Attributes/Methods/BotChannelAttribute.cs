using Dexter.Configurations;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Attributes.Methods {

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
        /// <param name="CommandContext">The Context is used to find the name of the channel that the message has been sent in.</param>
        /// <param name="CommandInfo">The Command is used to find the name of the command run,
        /// and to specify whether or not it is able to run in a given channel.</param>
        /// <param name="ServiceProvider">The Service Provider contains references to initialized classes
        /// such as the BotConfigurations class, used to find if the command has been run in a specified bot channel.</param>
        /// <returns>The result of the checked permission, returning successful if it is able to be run or an error if not.
        /// This error is then thrown to the Command Handler Service to log to the user.</returns>
        
        public override Task<PreconditionResult> CheckPermissionsAsync (ICommandContext CommandContext,
                CommandInfo CommandInfo, IServiceProvider ServiceProvider) {
            if (ServiceProvider.GetService<BotConfiguration>() == null)
                return Task.FromResult(PreconditionResult.FromSuccess());

            return Task.FromResult(
                ServiceProvider.GetRequiredService<BotConfiguration>().BotChannels.Contains(CommandContext.Channel.Id) || CommandContext.Channel.GetType() == typeof (SocketDMChannel) ?
                    PreconditionResult.FromSuccess() :
                    PreconditionResult.FromError($"Heya! You're not permitted to use the command `{CommandInfo.Name}` " +
                    $"in the channel `#{CommandContext.Channel}`. Please use a designated bot channel instead <3"));
        }

    }

}
