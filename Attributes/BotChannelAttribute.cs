using Dexter.Configurations;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public class BotChannelAttribute : PreconditionAttribute {

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext Context, CommandInfo Command, IServiceProvider Services) {
            return Task.FromResult(
                Services.GetRequiredService<BotConfiguration>().BotChannels.Contains(Context.Channel.Id) ?
                    PreconditionResult.FromSuccess() :
                    PreconditionResult.FromError($"Heya! You're not permitted to use this command in {Context.Channel.Name}. Please use a designated bot channel instead <3"));
        }

    }
}
