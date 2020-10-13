using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.CustomCommands.Features {
    public class AliasFeature : CustomCommandFeature {
        public string[] Aliases { get; set; }

        public override Task Execute(ICommandContext Context) => Task.CompletedTask;

        public bool Matches(string Name) => Aliases.Any(Attributes => Name.Equals(Attributes, StringComparison.InvariantCultureIgnoreCase));
    }
}
