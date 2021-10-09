using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Enums;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Webhook;
using Victoria.Player;

namespace Dexter.Extensions
{

    /// <summary>
    /// The EmbedBuilder Extensions class offers a variety of different extensions that can be applied to an embed to modify or send it.
    /// </summary>

    public static class EmbedExtensions
    {

        /// <summary>
        /// Builds an embed with the attributes specified by the emoji enum.
        /// </summary>
        /// <param name="embedBuilder">The EmbedBuilder which you wish to be built upon.</param>
        /// <param name="thumbnails">The type of EmbedBuilder you wish it to be, specified by an enum of possibilities.</param>
        /// <param name="botConfiguration">The BotConfiguration which is used to find the thumbnail of the embed.</param>
        /// <param name="calledType">The EmbedCallingType that the embed was called to be made from.</param>
        /// <returns>The built embed, with the thumbnail and color applied.</returns>

        public static EmbedBuilder BuildEmbed(this EmbedBuilder embedBuilder, EmojiEnum thumbnails,
            BotConfiguration botConfiguration, EmbedCallingType calledType)
        {
            Color Color = thumbnails switch
            {
                EmojiEnum.Annoyed => Color.Red,
                EmojiEnum.Love => Color.Green,
                EmojiEnum.Sign => Color.Blue,
                EmojiEnum.Wut => Color.Teal,
                EmojiEnum.Unknown => Color.Magenta,
                _ => Color.Magenta
            };

            string name = StringExtensions.GetLastMethodCalled(2).Key;

            string delete = calledType switch
            {
                EmbedCallingType.Command => "Commands",
                EmbedCallingType.Service => "Service",
                EmbedCallingType.Game => "Game",
                _ => ""
            };

            name = name.Replace(delete, "");

            name = string.Concat(name.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');

            return embedBuilder
                .WithThumbnailUrl(botConfiguration.ThumbnailURLs[(int)thumbnails])
                .WithColor(Color)
                .WithCurrentTimestamp()
                .WithFooter($"USFurries {name} Module");
        }

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IMessageChannel.
        /// </summary>
        /// <param name="embedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="messageChannel">The IMessageChannel you wish to send the embed to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task SendEmbed(this EmbedBuilder embedBuilder, IMessageChannel messageChannel) =>
            await messageChannel.SendMessageAsync(embed: embedBuilder.Build());

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified DiscordWebhookClient channel.
        /// </summary>
        /// <param name="embedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="discordWebhookClient">The DiscordWebhookClient you wish to send the embed to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task SendEmbed(this EmbedBuilder embedBuilder, DiscordWebhookClient discordWebhookClient) =>
            await discordWebhookClient.SendMessageAsync(embeds: new Embed[1] { embedBuilder.Build() });

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IMessageChannel and sends an embed to the user specified.
        /// </summary>
        /// <param name="embedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="messageChannel">The IMessageChannel you wish to send the embed to.</param>
        /// <param name="botConfiguration">The BotConfiguration which is used to find the thumbnail of the embed.</param>
        /// <param name="user">The IUser you wish to send the DM embed to.</param>
        /// <param name="dmEmbedBuilder">The Embed you wish to send to the user.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task SendDMAttachedEmbed(this EmbedBuilder embedBuilder, IMessageChannel messageChannel,
                BotConfiguration botConfiguration, IUser user, EmbedBuilder dmEmbedBuilder)
        {

            if (user == null)
                embedBuilder.AddField("Failed", "I cannot notify this fluff as they have left the server!");
            else
            {
                try
                {
                    IMessageChannel dmChannel = await user.CreateDMChannelAsync();
                    await dmChannel.SendMessageAsync(embed: dmEmbedBuilder.Build());
                }
                catch
                {
                    embedBuilder.BuildEmbed(EmojiEnum.Annoyed, botConfiguration, EmbedCallingType.Command);
                    embedBuilder.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
                }
            }

            await embedBuilder.SendEmbed(messageChannel);
        }

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IUser.
        /// </summary>
        /// <param name="embedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="user">The IUser you wish to send the embed to.</param>
        /// <param name="fallback">The Fallback is the channel it will send the embed to if the user has blocked DMs.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task SendEmbed(this EmbedBuilder embedBuilder, IUser user, ITextChannel fallback)
        {
            try
            {
                await user.SendMessageAsync(embed: embedBuilder.Build());
            }
            catch (HttpException)
            {
                IUserMessage message = await fallback.SendMessageAsync(user.Mention, embed: embedBuilder
                    .WithAuthor($"Psst, {user.Username}! Please unblock me or allow direct messages from {fallback.Guild.Name}. <3")
                                        .Build());

                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await message.DeleteAsync();
                });
            }
        }

        /// <summary>
        /// The AddField method adds a field to an EmbedBuilder if a given condition is true.
        /// </summary>
        /// <param name="embedBuilder">The EmbedBuilder you wish to add the field to.</param>
        /// <param name="condition">The condition which must be true to add the field.</param>
        /// <param name="name">The name of the field you wish to add.</param>
        /// <param name="value">The description of the field you wish to add.</param>
        /// <param name="inLine">Sets the inline parameter of the Embed Field.</param>
        /// <returns>The embed with the field added to it if true.</returns>

        public static EmbedBuilder AddField(this EmbedBuilder embedBuilder, bool condition, string name, object value, bool inLine = false)
        {
            if (condition)
                embedBuilder.AddField(name, value, inLine);

            return embedBuilder;
        }

        public static EmbedBuilder GetNowPlaying(this EmbedBuilder builder, LavaTrack track)
        {
            return builder.WithTitle("🎵 Now playing").WithDescription(
                                $"Title: **{track.Title}**\n" +
                                $"Duration: **{track.Duration.HumanizeTimeSpan()}**");
        }

        public static EmbedBuilder GetQueuedTrack(this EmbedBuilder builder, LavaTrack track, int queueSize)
        {
            return builder.WithTitle("⏳ Queued").WithDescription(
                                $"Title: **{track.Title}**\n" +
                                $"Duration: **{track.Duration.HumanizeTimeSpan()}**\n" +
                                $"Queue Position: **{queueSize}**.");
        }

        public static EmbedBuilder[] GetQueue(this LavaPlayer player, string title, BotConfiguration botConfiguration)
        {
            var embeds = player.Vueue.ToArray().GetQueueFromTrackArray(title, botConfiguration);

            if (player.Track != null)
            {
                string trackDurCur, trackDurTotal;

                if (player.Track.Duration.TotalMinutes > 60)
                {
                    trackDurCur = player.Track.Position.ToString("hh\\:mm\\:ss");
                    trackDurTotal = player.Track.Duration.ToString("hh\\:mm\\:ss");
                }
                else
                {
                    trackDurCur = player.Track.Position.ToString("mm\\:ss");
                    trackDurTotal = player.Track.Duration.ToString("mm\\:ss");
                }

                var timeRem = player.Track.Duration - player.Track.Position +
                    TimeSpan.FromSeconds(player.Vueue.Select(x => x.Duration.TotalSeconds).Sum());

                embeds.First().WithDescription("**Now Playing:**\n" +
                                  $"Title: **{player.Track.Title}** " +
                                  $"[{trackDurCur} / {trackDurTotal}]\n\n" +
                                  $"**Duration Left:** \n" +
                                  $"{player.Vueue.Count + 1} Tracks [{timeRem.HumanizeTimeSpan()}]\n\n" +
                                  "Up Next ⬇️");
            }

            return embeds;
        }

        public static EmbedBuilder[] GetQueueFromTrackArray(this LavaTrack[] tracks, string title, BotConfiguration botConfiguration)
        {
            EmbedBuilder CurrentBuilder = new EmbedBuilder()
                .BuildEmbed(EmojiEnum.Unknown, botConfiguration, EmbedCallingType.Command).WithTitle(title);

            List<EmbedBuilder> Embeds = new();

            if (tracks.Length == 0)
            {
                CurrentBuilder.WithDescription(CurrentBuilder.Description += "\n\n*No tracks enqueued.*");
            }

            for (int Index = 0; Index < tracks.Length; Index++)
            {
                EmbedFieldBuilder Field = new EmbedFieldBuilder()
                    .WithName($"#{Index + 1}. **{tracks[Index].Title}**")
                    .WithValue($"{tracks[Index].Author} ({tracks[Index].Duration:mm\\:ss})");

                if (Index % 5 == 0 && Index != 0)
                {
                    Embeds.Add(CurrentBuilder);
                    CurrentBuilder = new EmbedBuilder()
                        .BuildEmbed(EmojiEnum.Unknown, botConfiguration, EmbedCallingType.Command).AddField(Field);
                }
                else
                {
                    try
                    {
                        CurrentBuilder.AddField(Field);
                    }
                    catch (Exception)
                    {
                        Embeds.Add(CurrentBuilder);
                        CurrentBuilder = new EmbedBuilder()
                            .BuildEmbed(EmojiEnum.Unknown, botConfiguration, EmbedCallingType.Command).AddField(Field);
                    }
                }
            }

            Embeds.Add(CurrentBuilder);

            return Embeds.ToArray();
        }

        /// <summary>
        /// The GetParametersForCommand adds fields to an embed containing the parameters and summary of the command.
        /// </summary>
        /// <param name="embedBuilder">The EmbedBuilder you wish to add the fields to.</param>
        /// <param name="commandInfo">The command of which you wish to search for.</param>
        /// <param name="botConfiguration">The BotConfiguration, which is used to find the prefix for the parameter.</param>
        /// <returns>The embed with the parameter fields for the command added.</returns>

        public static EmbedBuilder GetParametersForCommand(this EmbedBuilder embedBuilder, CommandInfo commandInfo, BotConfiguration botConfiguration)
        {
            string commandDescription = string.Empty;

            if (commandInfo.Parameters.Count > 0)
                commandDescription = $"Parameters: {string.Join(", ", commandInfo.Parameters.Select(p => p.Name))}";

            Attribute extendedSummary = commandInfo.Attributes.Where(Attribute => Attribute is ExtendedSummaryAttribute).FirstOrDefault();

            if (extendedSummary is not null)
                commandDescription += $"\nSummary: {(extendedSummary as ExtendedSummaryAttribute).ExtendedSummary}";
            else if (!string.IsNullOrEmpty(commandInfo.Summary))
                commandDescription += $"\nSummary: {commandInfo.Summary}";

            embedBuilder.AddField(string.Join(", ", commandInfo.Aliases.Select(Name => $"{botConfiguration.Prefix}{Name}")), commandDescription);

            return embedBuilder;
        }

    }

}
