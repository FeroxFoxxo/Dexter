using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;
using Genbox.WolframAlpha;
using Genbox.WolframAlpha.Objects;
using Genbox.WolframAlpha.Responses;

namespace Dexter.Commands {
	public partial class UtilityCommands {

		/// <summary>
		/// Evaluates a mathematical expression and gives a result or throws an error.
		/// </summary>
		/// <param name="Question">A properly formatted stringified math expression.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("ask", RunMode = RunMode.Async)]
		[Summary("Evaluates a mathematical expression")]
		[Alias("math", "calc", "calculate")]

		public async Task WolframCommand([Remainder] string Question) {
			if (string.IsNullOrEmpty(UtilityConfiguration.WolframAppAPI)) {
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle("Whoops! This is on us. <3")
					.WithDescription("It looks like one of our developers forgot to set an API key to use this service. " +
					"We appologise for the inconvenience~!")
					.WithCurrentTimestamp()
					.WithFooter("USFurries Developer Team")
					.SendEmbed(Context.Channel);

				return;
			}

			if (WolframAlphaClient == null)
				WolframAlphaClient = new WolframAlphaClient(UtilityConfiguration.WolframAppAPI);

			string Response = await WolframAlphaClient.ShortAnswerAsync(Question);

			if (Response.Length > 500)
				Response = $"{Response.Substring(0, 500)}...";

			await Context.Channel.SendMessageAsync(Response);
		}

	}

}

