using Dexter.Abstractions;
using Dexter.CustomCommands.Features;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dexter.CustomCommands {
    public class CustomCommand {
        public string Name { get; set; }

        public List<CustomCommandFeature> Features { get; set; }

        public bool Matches(string _Name) {
            if (Features.Find(Feature => Feature is AliasFeature) is AliasFeature Alias && Alias.Matches(_Name))
                return true;

            return Name.Equals(_Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public virtual async Task ExecuteCommand(ICommandContext Context, CommandModule Module) {
            if (Features.Count > 0)
                foreach (CustomCommandFeature Feature in Features)
                    await Feature.Execute(Context);
            else
                await Module.BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Command not configured!")
                    .WithDescription("No features have been set for this command. :C")
                    .SendEmbed(Context.Channel);
        }
    }
}
