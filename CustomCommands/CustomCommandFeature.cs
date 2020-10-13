using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.CustomCommands {
    public abstract class CustomCommandFeature {
        public abstract Task Execute(ICommandContext Context);
    }
}
