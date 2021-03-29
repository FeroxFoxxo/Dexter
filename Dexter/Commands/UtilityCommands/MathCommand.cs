using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;

namespace Dexter.Commands {
	public partial class UtilityCommands {

		[Command("math")]
		[Summary("Evaluates a mathematical expression")]
		[Alias("calc", "calculate")]
		[BotChannel]

		public async Task MathCommand([Remainder] string Expression) {
			BuildEmbed(EmojiEnum.Love)
				.WithTitle("We'll be right back soon folks!")
				.SendEmbed(Context.Channel);
		}

	}

}

