using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Net.Http;

namespace Dexter.Events
{

	/// <summary>
	/// The Startup Serivce module applies the token for the bot, as well as running the bot once all dependencies have loaded up.
	/// Furthermore, it logs and sends a message to the moderation channel when it does start up, including various information
	/// like its bot and Discord.NET versionings.
	/// </summary>

	public class StartupNotification : Event
	{

		/// <summary>
		/// <see langword="true"/> if the bot has finished its startup process; <see langword="false"/> otherwise.
		/// </summary>

		public bool HasStarted = false;

		/// <summary>
		/// The Initialize method hooks the client ready event to the Display Startup Version Async method.
		/// </summary>

		public override void InitializeEvents()
		{
			DiscordShardedClient.ShardReady += (DiscordSocketClient _) => DisplayStartupVersionAsync();
		}

		/// <summary>
		/// The Display Startup Version Async method runs on ready and is what attempts to log the initialization of the bot
		/// to a specified guild that the bot has sucessfully started and the versionings that it is running.
		/// </summary>
		/// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

		public async Task DisplayStartupVersionAsync()
		{
			await DiscordShardedClient.SetActivityAsync(new Game(BotConfiguration.BotStatus));
			await DiscordShardedClient.SetStatusAsync(UserStatus.Invisible);

			if (HasStarted)
            {
                return;
            }

            SocketChannel LoggingChannel = DiscordShardedClient.GetChannel(BotConfiguration.ModerationLogChannelID);

			if (LoggingChannel == null || LoggingChannel is not ITextChannel)
            {
                return;
            }

            Dictionary<string, List<string>> NulledConfigurations = [];

			Assembly.GetExecutingAssembly().GetTypes()
					.Where(Type => Type.IsSubclassOf(typeof(JSONConfig)) && !Type.IsAbstract)
					.ToList().ForEach(
				Configuration =>
				{
					object Service = ServiceProvider.GetService(Configuration);

					Configuration.GetProperties().ToList().ForEach(
						Property =>
						{
							object Value = Property.GetValue(Service);

							if (Value != null)
                            {
                                if (!string.IsNullOrEmpty(Value.ToString()) && !Value.ToString().Equals("0"))
                                {
                                    return;
                                }
                            }

                            if (!NulledConfigurations.TryGetValue(Configuration.Name, out List<string> value))
                            {
                                value = ([]);
                                NulledConfigurations.Add(Configuration.Name, value);
                            }

                            value.Add(Property.Name);
						}
					);
				}
			);

			using HttpClient HTTPClient = new();

			HTTPClient.DefaultRequestHeaders.Add("User-Agent",
				"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

			using HttpResponseMessage Response = HTTPClient.GetAsync(BotConfiguration.CommitAPICall).Result;
			string JSON = Response.Content.ReadAsStringAsync().Result;

			dynamic Commits = JArray.Parse(JSON);
			string LastCommit = Commits[0].commit.message;

			string UnsetConfigurations = string.Empty;

			foreach (KeyValuePair<string, List<string>> Configuration in NulledConfigurations)
            {
                UnsetConfigurations += $"**{Configuration.Key} -** {string.Join(", ", Configuration.Value.Take(Configuration.Value.Count - 1)) + (Configuration.Value.Count > 1 ? " and " : "") + Configuration.Value.LastOrDefault()}\n";
            }

            if (BotConfiguration.EnableStartupAlert || NulledConfigurations.Count > 0)
            {
                await BuildEmbed(EmojiEnum.Love)
					.WithTitle("Startup complete!")
					.WithDescription($"This is **{DiscordShardedClient.CurrentUser.Username} v{Startup.Version}** running **Discord.Net v{DiscordConfig.Version}**!")
					.AddField("Latest Commit:", LastCommit.Length > 1000 ? $"{LastCommit[..1000]}..." : LastCommit)
					.AddField(NulledConfigurations.Count > 0, "Unapplied Configurations:", UnsetConfigurations.Length > 600 ? $"{UnsetConfigurations[..600]}..." : UnsetConfigurations)
					.SendEmbed(LoggingChannel as ITextChannel);
            }

            HasStarted = true;
		}

	}

}
