using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Abstractions;
using Dexter.Commands;
using Dexter.Configurations;
using Dexter.Enums;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Dexter.Events
{
    /// <summary>
    /// The RoleManagementService adds the given roles to the user if they join the server.
    /// </summary>

    public class RoleManagement : Event
    {
        /// <summary>
        /// Holds all relevant information about color and patreon roles.
        /// </summary>
        public UtilityConfiguration UtilityConfiguration { get; set; }

        public CommandService CommandService { get; set; }

        /// <summary>
        /// Runs after dependency injection and wires up all relevant events for this service to run properly.
        /// </summary>
        public override void InitializeEvents()
        {
            DiscordShardedClient.GuildMemberUpdated += CheckUserColorRoles;
        }

        private async Task CheckUserColorRoles(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
        {
            if (before.Value.Roles.Count == after.Roles.Count) return;

            int tierAfter = UtilityCommands.GetRoleChangePerms(after, UtilityConfiguration);

            List<SocketRole> toRemove = new();

            foreach (SocketRole role in after.Roles)
            {
                int roleTier = UtilityCommands.GetColorRoleTier(role.Id, UtilityConfiguration);
                if (role.Name.StartsWith(UtilityConfiguration.ColorRolePrefix)
                    && roleTier > tierAfter)
                {
                    toRemove.Add(role);
                }
            }

            if (toRemove.Count > 0)
                await after.RemoveRolesAsync(toRemove);

            ServiceCollection serviceCollection = new();

            HelpAbstraction helpAbstraction = new()
            {
                BotConfiguration = BotConfiguration,
                DiscordShardedClient = DiscordShardedClient
            };

            serviceCollection.AddSingleton(helpAbstraction);

            List<string> removed = new ();
            List<string> added = new ();

            var beforeRole = await before.GetOrDownloadAsync();

            ICommandContext ccBefore = new FCC(DiscordShardedClient, beforeRole.Guild, beforeRole.Guild.DefaultChannel, beforeRole, null);

            ICommandContext ccAfter = new FCC(DiscordShardedClient, after.Guild, after.Guild.DefaultChannel, after, null);

            foreach (ModuleInfo module in CommandService.Modules)
            {
                foreach (CommandInfo commandInfo in module.Commands)
                {
                    if (string.IsNullOrWhiteSpace(commandInfo.Summary))
                        continue;

                    PreconditionResult oldRole = await commandInfo.CheckPreconditionsAsync(ccBefore, serviceCollection.BuildServiceProvider());

                    PreconditionResult newRole = await commandInfo.CheckPreconditionsAsync(ccAfter, serviceCollection.BuildServiceProvider());

                    string command = $"**{BotConfiguration.Prefix}{string.Join("/", commandInfo.Aliases.ToArray())}:** {commandInfo.Summary}\n\n";

                    if (oldRole.IsSuccess && !newRole.IsSuccess)
                        removed.Add(command);

                    else if (!oldRole.IsSuccess && newRole.IsSuccess)
                        added.Add(command);
                }
            }

            List<EmbedBuilder> menu = new ();

            EmbedBuilder currentBuilder = null;

            if (added.Count > 0)
                currentBuilder = BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Dexter Commands Added!")
                    .WithDescription("Here's the commands you're now able to use!");
            else if (removed.Count > 0)
                currentBuilder = BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Dexter Commands Removed.")
                    .WithDescription("Here's the commands you've had your access revoked from.");

            if (currentBuilder != null)
            {
                if (added.Count > 0)
                {
                    string info = string.Empty;

                    for (int i = 0; i < added.Count; i++)
                    {
                        info += added[i];

                        if ((i % 5 == 0 && i != 0) || info.Length + added[i].Length > 1024)
                        {
                            currentBuilder.AddField("Added:", info);

                            info = string.Empty;
                            menu.Add(currentBuilder);

                            currentBuilder = BuildEmbed(EmojiEnum.Love)
                                .WithTitle("Dexter Commands Added!")
                                .WithDescription("Here's the commands you're now able to use!");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(info))
                    {
                        currentBuilder.AddField("Added:", info);

                        menu.Add(currentBuilder);
                    }
                }

                if (removed.Count > 0)
                {
                    string info = string.Empty;

                    for (int i = 0; i < removed.Count; i++)
                    {
                        info += removed[i];

                        if ((i % 5 == 0 && i != 0) || info.Length + removed[i].Length > 1024)
                        {
                            currentBuilder.AddField("Removed:", info);

                            info = string.Empty;
                            menu.Add(currentBuilder);

                            currentBuilder = BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle("Dexter Commands Removed.")
                                .WithDescription("Here's the commands you've had your access revoked from.");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(info))
                    {
                        currentBuilder.AddField("Removed:", info);

                        menu.Add(currentBuilder);
                    }
                }

                try
                {
                    var dm = await after.CreateDMChannelAsync();

                    _ = CreateReactionMenu(menu.ToArray(), dm);
                } 
                catch (Exception) { }
            }
        }
    }

    public class FCC : ICommandContext
    {
        /// <inheritdoc/>
        public IDiscordClient Client { get; }

        /// <inheritdoc/>
        public IGuild Guild { get; }

        /// <inheritdoc/>
        public IMessageChannel Channel { get; }

        /// <inheritdoc/>
        public IUser User { get; }

        /// <inheritdoc/>
        public IUserMessage Message { get; }

        /// <summary>
        ///     Initializes a new <see cref="CommandContext" /> class with the provided client and message.
        /// </summary>
        public FCC(IDiscordClient client, IGuild guild, IMessageChannel channel, IUser user, IUserMessage message)
        {
            Client = client;
            Guild = guild;
            Channel = channel;
            User = user;
            Message = message;
        }
    }

}
