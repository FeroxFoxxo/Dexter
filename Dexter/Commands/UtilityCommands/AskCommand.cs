using System.Threading.Tasks;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;
using Genbox.WolframAlpha;

namespace Dexter.Commands {
    public partial class UtilityCommands {

		/// <summary>
		/// Evaluates a mathematical expression and gives a result or throws an error.
		/// </summary>
		/// <param name="Question">A properly formatted stringified math expression.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("ask", RunMode = RunMode.Async)]
		[Summary("Evaluates mathematical expressions and answers questions!")]
		[Alias("math", "calc", "calculate")]
                [BotChannel] 
		public async Task WolframCommand([Remainder] string Question) {
			if (string.IsNullOrEmpty(UtilityConfiguration.WolframAppAPI) || UtilityConfiguration.WolframAppAPI == "NOTSET") {
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

			string Response = await WolframAlphaClient.SpokenResultAsync(Question);

			Response = Response.Replace("Wolfram Alpha", Context.Client.CurrentUser.Username);
			Response = Response.Replace("Wolfram|Alpha", Context.Client.CurrentUser.Username);
			Response = Response.Replace("Stephen Wolfram", "the goat overlords");
			Response = Response.Replace("and his team", "and their team");

			if (Response == "Error 1: Invalid appid")
				WolframAlphaClient = null;
			else if (Response == "DexterBot did not understand your input" || Response == "No spoken result available")
				await Context.Message.AddReactionAsync(new Emoji("❓"));
			else 
				await Context.Channel.SendMessageAsync(Response);
		}

	}

}

