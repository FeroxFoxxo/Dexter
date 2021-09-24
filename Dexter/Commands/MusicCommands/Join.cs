using Dexter.Attributes.Methods;
using Discord.Commands;
using System.Threading.Tasks;

namespace Dexter.Commands
{
    public partial class MusicCommands
    {

        [Command("join")]
        [Summary("Joins the voice channel you're in, assuming the bot isn't in one already.")]
        [BotChannel]

        public async Task JoinCommand()
        {

        }

    }

}
