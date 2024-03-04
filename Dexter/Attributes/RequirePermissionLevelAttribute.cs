using System;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Events;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Attributes.Methods
{

	/// <summary>
	/// The Require Permission Level attribute is an abstract class that extends the superclass of the
	/// precondition attribute. This will run a check to see if the user meets the required permission
	/// level specified by the class that extends this, and if so to run the command. It is applied to methods.
	/// </summary>

	[AttributeUsage(AttributeTargets.Method)]

	public abstract class RequirePermissionLevelAttribute : PreconditionAttribute
	{

		/// <summary>
		/// The Permission Level is the level at which a user has to meet or exceed to be able to run the command.
		/// </summary>

		public readonly PermissionLevel PermissionLevel;

		/// <summary>
		/// The RequirePermissionLevel constructor takes in the level at which a user has to be at to run the command.
		/// </summary>
		/// <param name="permissionLevel">The permission level required to run the command.</param>

		public RequirePermissionLevelAttribute(PermissionLevel permissionLevel)
		{
			PermissionLevel = permissionLevel;
		}

		/// <summary>
		/// The CheckPermissionsAsync is an overriden method from its superclass, which checks
		/// to see if a command can be run by a user through their roles that they have applied.
		/// </summary>
		/// <param name="context">The Context is used to find the user who has run the command.</param>
		/// <param name="commandInfo">The Command is used to find the name of the command that has been run.</param>
		/// <param name="services">The Services are used to find the role IDs to get the permission level of the user from the BotConfiguration.</param>
		/// <returns>The result of the checked permission, returning successful if it is able to be run or an error if not.
		/// This error is then thrown to the Command Handler Service to log to the user.</returns>

		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo commandInfo, IServiceProvider services)
		{
			BotConfiguration botConfiguration = services.GetService<BotConfiguration>();

			if (services.GetService<HelpAbstraction>() != null)
				botConfiguration = services.GetService<HelpAbstraction>().BotConfiguration;

			DiscordShardedClient discordShardedClient = services.GetService<DiscordShardedClient>();

			if (services.GetService<HelpAbstraction>() != null)
				discordShardedClient = services.GetService<HelpAbstraction>().DiscordShardedClient;

			if (botConfiguration == null)
				return PreconditionResult.FromSuccess();

			return context.User.GetPermissionLevel(discordShardedClient, botConfiguration) >= PermissionLevel
				? PreconditionResult.FromSuccess()
				: PreconditionResult.FromError($"Haiya! To run the `{commandInfo.Name}` command you need to have the " +
				$"`{PermissionLevel}` role! Are you sure you're {LanguageHelper.GuessIndefiniteArticle(PermissionLevel.ToString())} " +
				$"`{PermissionLevel.ToString().ToLower()}`? <3");
		}

	}

}
