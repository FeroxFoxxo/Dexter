using Dexter.Configurations;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Dexter.Attributes.Methods
{

	/// <summary>
	/// The MusicBotChannelAttribute attribute specifies the command can only be used in the music channels
	/// provided through the BotConfiguration file. It is applied to the command's method.
	/// </summary>

	[AttributeUsage(AttributeTargets.Method)]
	public class MusicBotChannelAttribute : PreconditionAttribute
	{

		/// <summary>
		/// The CheckPermissionsAsync is an overriden method from its superclass,
		/// which checks to see if a command can be run in the specified channel.
		/// </summary>
		/// <param name="context">The Context is used to find the name of the channel that the message has been sent in.</param>
		/// <param name="commandInfo">The Command is used to find the name of the command run,
		/// and to specify whether or not it is able to run in a given channel.</param>
		/// <param name="services">The Service Provider contains references to initialized classes
		/// such as the BotConfigurations class, used to find if the command has been run in a specified bot channel.</param>
		/// <returns>The result of the checked permission, returning successful if it is able to be run or an error if not.
		/// This error is then thrown to the Command Handler Service to log to the user.</returns>

		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
				CommandInfo commandInfo, IServiceProvider services)
		{
			if (services.GetService<BotConfiguration>() == null)
				return Task.FromResult(PreconditionResult.FromSuccess());

			return Task.FromResult(
				services.GetRequiredService<BotConfiguration>().MusicChannelID == context.Channel.Id || context.Channel.GetType() == typeof(SocketDMChannel) ?
					PreconditionResult.FromSuccess() :
					PreconditionResult.FromError($"Heya! You're not permitted to use the command `{commandInfo.Name}` " +
					$"in the channel `#{context.Channel}`. Please use a designated music channel instead <3"));
		}

	}

}