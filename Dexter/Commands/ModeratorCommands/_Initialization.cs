using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.Cooldowns;
using Dexter.Databases.Relays;
using Dexter.Databases.Infractions;
using Discord.WebSocket;
using Dexter.Databases.Mail;
using Dexter.Databases.FinalWarns;
using System;
using Dexter.Databases.UserProfiles;
using Dexter.Databases.UserRestrictions;

namespace Dexter.Commands {

    /// <summary>
    /// The ModeratorCommands module relates to recording of users in breach of rules and other, miscellaneous commands relating to the moderation of the server.
    /// </summary>

    public partial class ModeratorCommands : DiscordModule {

        /// <summary>
        /// The CooldownDB stores the cooldowns that the cooldown command interfaces with.
        /// </summary>

        public CooldownDB CooldownDB { get; set; }

        /// <summary>
        /// The InfractionsDB stores the warnings that these commands interface.
        /// </summary>

        public InfractionsDB InfractionsDB { get; set; }

        /// <summary>
        /// The RelayDB stores the relays that are used to send messages to a channel in set intervals.
        /// </summary>

        public RelayDB RelayDB { get; set; }

        /// <summary>
        /// The Client is an instance of the DiscordSocketClient, used to get a user on callback.
        /// </summary>

        public DiscordSocketClient Client { get; set; }

        /// <summary>
        /// The ChannelCooldownConfiguration stores the length of time a commission cooldown lasts for.
        /// </summary>

        public ChannelCooldownConfiguration CommissionCooldownConfiguration { get; set; }

        /// <summary>
        /// Works as an interface between the configuration files attached to the Moderation module and its commands.
        /// </summary>

        public ModerationConfiguration ModerationConfiguration { get; set; }

        /// <summary>
        /// The ModMailDB stores the mail to be sent to the moderators.
        /// </summary>
         
        public ModMailDB ModMailDB { get; set; }

        /// <summary>
        /// The BorkdayDB stores information regarding a user's birthday.
        /// </summary>

        public ProfilesDB BorkdayDB { get; set; }

        /// <summary>
        /// Stores information regarding final warns issued in the server.
        /// </summary>

        public FinalWarnsDB FinalWarnsDB { get; set; }

        /// <summary>
        /// Stores information regarding user restrictions in terms of using certain commands.
        /// </summary>

        public RestrictionsDB RestrictionsDB { get; set; }

        /// <summary>
        /// The Random instance is used to pick a set number of random characters from the configuration to create a token.
        /// </summary>

        public Random Random { get; set; }

    }

}
