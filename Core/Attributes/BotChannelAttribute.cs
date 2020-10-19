using Dexter.Core.Abstractions;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Core.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public class BotChannelAttribute : PreconditionAttribute {

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            return Task.FromResult(
                (Context as CommandModule).BotConfiguration.BotChannels.Contains(Context.Channel.Id) ?
                    PreconditionResult.FromSuccess() :
                    PreconditionResult.FromError($"Heya! You're not permitted to use this command in {Context.Channel.Name}. Please use a designated bot channel instead <3"));
        }

    }
}
