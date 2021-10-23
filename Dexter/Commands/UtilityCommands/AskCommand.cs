using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Discord;
using Discord.Commands;
using Genbox.WolframAlpha;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Commands
{
	public partial class UtilityCommands
	{

		/// <summary>
		/// Evaluates a mathematical expression and gives a result or throws an error.
		/// </summary>
		/// <param name="Question">A properly formatted stringified math expression.</param>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		[Command("ask")]
		[Summary("Evaluates mathematical expressions and answers questions!")]
		[Alias("math", "calc", "calculate")]
		[CommandCooldown(15)]

		public async Task WolframCommand([Remainder] string Question)
		{
			string Response = await ServiceProvider.GetRequiredService<WolframAlphaClient>().SpokenResultAsync(Question);

			Response = Response.Replace("Wolfram Alpha", Context.Client.CurrentUser.Username);
			Response = Response.Replace("Wolfram|Alpha", Context.Client.CurrentUser.Username);
			Response = Response.Replace("Stephen Wolfram", "the goat overlords");
			Response = Response.Replace("and his team", "and their team");

			if (Response == "DexterBot did not understand your input" || Response == "No spoken result available")
				await Context.Message.AddReactionAsync(new Emoji("â“"));
			else
				await Context.Channel.SendMessageAsync(Response);
		}

	}

}

