using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Enums;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Webhook;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dexter.Extensions {

    /// <summary>
    /// The EmbedBuilder Extensions class offers a variety of different extensions that can be applied to an embed to modify or send it.
    /// </summary>
    
    public static class EmbedExtensions {

        /// <summary>
        /// Builds an embed with the attributes specified by the emoji enum.
        /// </summary>
        /// <param name="EmbedBuilder">The EmbedBuilder which you wish to be built upon.</param>
        /// <param name="Thumbnails">The type of EmbedBuilder you wish it to be, specified by an enum of possibilities.</param>
        /// <param name="BotConfiguration">The BotConfiguration which is used to find the thumbnail of the embed.</param>
        /// <returns>The built embed, with the thumbnail and color applied.</returns>
        
        public static EmbedBuilder BuildEmbed(this EmbedBuilder EmbedBuilder, EmojiEnum Thumbnails, BotConfiguration BotConfiguration) {
            Color Color = Thumbnails switch {
                EmojiEnum.Annoyed => Color.Red,
                EmojiEnum.Love => Color.Green,
                EmojiEnum.Sign => Color.Blue,
                EmojiEnum.Wut => Color.Teal,
                EmojiEnum.Unknown => Color.Orange,
                _ => Color.Magenta
            };

            return EmbedBuilder.WithThumbnailUrl(BotConfiguration.ThumbnailURLs[(int)Thumbnails]).WithColor(Color);
        }

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IMessageChannel.
        /// </summary>
        /// <param name="EmbedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="MessageChannel">The IMessageChannel you wish to send the embed to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public static async Task SendEmbed(this EmbedBuilder EmbedBuilder, IMessageChannel MessageChannel) =>
            await MessageChannel.SendMessageAsync(embed: EmbedBuilder.Build());

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified DiscordWebhookClient channel.
        /// </summary>
        /// <param name="EmbedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="DiscordWebhookClient">The DiscordWebhookClient you wish to send the embed to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>
        
        public static async Task SendEmbed(this EmbedBuilder EmbedBuilder, DiscordWebhookClient DiscordWebhookClient) =>
            await DiscordWebhookClient.SendMessageAsync(embeds: new Embed[1] { EmbedBuilder.Build() });

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IMessageChannel and sends an embed to the user specified.
        /// </summary>
        /// <param name="EmbedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="MessageChannel">The IMessageChannel you wish to send the embed to.</param>
        /// <param name="BotConfiguration">The BotConfiguration which is used to find the thumbnail of the embed.</param>
        /// <param name="User">The IUser you wish to send the DM embed to.</param>
        /// <param name="DMEmbedBuilder">The Embed you wish to send to the user.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task SendDMAttachedEmbed(this EmbedBuilder EmbedBuilder, IMessageChannel MessageChannel,
                BotConfiguration BotConfiguration, IUser User, EmbedBuilder DMEmbedBuilder) {

            if (User == null)
                EmbedBuilder.AddField("Failed", "I cannot notify this fluff as they have left the server!");
            else {
                try {
                    IMessageChannel DMChannel = await User.GetOrCreateDMChannelAsync();
                    await DMChannel.SendMessageAsync(embed: DMEmbedBuilder.Build());
                } catch {
                    EmbedBuilder.BuildEmbed(EmojiEnum.Annoyed, BotConfiguration);
                    EmbedBuilder.AddField("Failed", "This fluff may have either blocked DMs from the server or me!");
                }
            }

            await EmbedBuilder.SendEmbed(MessageChannel);
        }

        /// <summary>
        /// Builds an EmbedBuilder and sends it to the specified IUser.
        /// </summary>
        /// <param name="EmbedBuilder">The EmbedBuilder you wish to send.</param>
        /// <param name="User">The IUser you wish to send the embed to.</param>
        /// <param name="Fallback">The Fallback is the channel it will send the embed to if the user has blocked DMs.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public static async Task SendEmbed(this EmbedBuilder EmbedBuilder, IUser User, ITextChannel Fallback) {
            try {
                await User.SendMessageAsync(embed: EmbedBuilder.Build());
            } catch (HttpException) {
                IUserMessage Message = await Fallback.SendMessageAsync(User.Mention, embed: EmbedBuilder
                    .WithAuthor($"Psst, {User.Username}! Please unblock me or allow direct messages from {Fallback.Guild.Name}. <3")
                    .Build());
                _ = Task.Run(async () => {
                    await Task.Delay(5000);
                    await Message.DeleteAsync();
                });
            }
        }

        /// <summary>
        /// The AddField method adds a field to an EmbedBuilder if a given condition is true.
        /// </summary>
        /// <param name="EmbedBuilder">The EmbedBuilder you wish to add the field to.</param>
        /// <param name="Condition">The condition which must be true to add the field.</param>
        /// <param name="Name">The name of the field you wish to add.</param>
        /// <param name="Value">The description of the field you wish to add.</param>
        /// <param name="InLine">Sets the inline parameter of the Embed Field.</param>
        /// <returns>The embed with the field added to it if true.</returns>
        
        public static EmbedBuilder AddField(this EmbedBuilder EmbedBuilder, bool Condition, string Name, object Value, bool InLine = false) {
            if (Condition)
                EmbedBuilder.AddField(Name, Value, InLine);

            return EmbedBuilder;
        }

        /// <summary>
        /// The GetParametersForCommand adds fields to an embed containing the parameters and summary of the command.
        /// </summary>
        /// <param name="EmbedBuilder">The EmbedBuilder you wish to add the fields to.</param>
        /// <param name="CommandInfo">The command of which you wish to search for.</param>
        /// <param name="BotConfiguration">The BotConfiguration, which is used to find the prefix for the parameter.</param>
        /// <returns>The embed with the parameter fields for the command added.</returns>
        
        public static EmbedBuilder GetParametersForCommand(this EmbedBuilder EmbedBuilder, CommandInfo CommandInfo, BotConfiguration BotConfiguration) {
            string CommandDescription = string.Empty;

            if (CommandInfo.Parameters.Count > 0)
                CommandDescription = $"Parameters: {string.Join(", ", CommandInfo.Parameters.Select(p => p.Name))}";

            Attribute ExtendedSummary = CommandInfo.Attributes.Where(Attribute => Attribute is ExtendedSummaryAttribute).FirstOrDefault();

            if (ExtendedSummary is not null)
                CommandDescription += $"\nSummary: {(ExtendedSummary as ExtendedSummaryAttribute).ExtendedSummary}";
            else if (!string.IsNullOrEmpty(CommandInfo.Summary))
                CommandDescription += $"\nSummary: {CommandInfo.Summary}";

            EmbedBuilder.AddField(string.Join(", ", CommandInfo.Aliases.Select(Name => $"{BotConfiguration.Prefix}{Name}")), CommandDescription);

            return EmbedBuilder;
        }

    }

}
