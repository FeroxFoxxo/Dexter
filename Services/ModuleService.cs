using Dexter.Abstractions;
using Dexter.Attributes;
using Dexter.Databases.Configuration;
using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dexter.Extensions;
using Discord.WebSocket;

namespace Dexter.Services {

    /// <summary>
    /// The Module service adds all the modules that have been specified as enabled to the command service, aswell as all essential moodules.
    /// It does this through a database dedicated to keeping track of the status of modules, looping through it and setting them respectively.
    /// </summary>
    public class ModuleService : InitializableModule {

        private readonly DiscordSocketClient Client;

        private readonly LoggingService LoggingService;

        private readonly CommandService CommandService;

        private readonly IServiceProvider Services;

        private readonly ConfigurationDB ConfigurationDB;

        /// <summary>
        /// The constructor for the ModuleService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// </summary>
        /// <param name="Client">An instance of the DiscordSocketClient, which we use to hook into the OnReady event.</param>
        /// <param name="CommandService">An instance of the CommandService, which tracks all the currently enabled commands and their delegates.</param>
        /// <param name="LoggingService">The LoggingService instance, which we use to log information on the currently enabled modules for use when we start up.</param>
        /// <param name="Services">The ServiceProvider, which contains references to all the classes that have been specified through recursion, more specifically the CommandModule classes.</param>
        /// <param name="ConfigurationDB">An instance of the ConfigurationDB, which keeps track of enabled and disabled commands.</param>
        public ModuleService(DiscordSocketClient Client, CommandService CommandService, LoggingService LoggingService,
                IServiceProvider Services, ConfigurationDB ConfigurationDB) {
            this.Client = Client;
            this.CommandService = CommandService;
            this.LoggingService = LoggingService;
            this.Services = Services;
            this.ConfigurationDB = ConfigurationDB;
        }

        /// <summary>
        /// The AddDelegates method hooks the client's Ready event up to the EnableModules method, which will set all the modules as defined in the ConfigurationDB.
        /// </summary>
        public override void AddDelegates() {
            Client.Ready += EnableModules;
        }

        /// <summary>
        /// The Enable Modules method runs on client ready and, through reflextion, adds all modules that have been enabled into the Command Service.
        /// These enabled modules include both those that have been set to be enabled in the database, and those with the EssentialModule attribute.
        /// </summary>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task EnableModules() {
            // Remove all modules.
            foreach (ModuleInfo Module in CommandService.Modules)
                await CommandService.RemoveModuleAsync(Module);

            int Essentials = 0, Others = 0;

            // Find all modules that are deemed essential through the attribute.
            string[] EssentialModules = GetModuleTypes().Where(Module => Module.IsDefined(typeof(EssentialModuleAttribute))).Select(Module => Module.Name.Sanitize()).ToArray();

            // Check for configs not in project but in database.
            foreach (Configuration Configuration in ConfigurationDB.Configurations.ToArray())
                if (GetModuleTypeByName(Configuration.ConfigurationName) == null) {
                    ConfigurationDB.Configurations.Remove(Configuration);
                    await ConfigurationDB.SaveChangesAsync();
                }

            // Check for configs not in database but in project.
            foreach (Type Type in GetModuleTypes()) {
                string TypeName = Type.Name.Sanitize();

                if (ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationName == TypeName).FirstOrDefault() == null) {
                    ConfigurationDB.Configurations.Add(new Configuration() {
                        ConfigurationName = TypeName,
                        ConfigurationType = ConfigurationType.Disabled
                    });
                    await ConfigurationDB.SaveChangesAsync();
                }
            }

            // Check for attribute set in database not matching project.
            foreach (Configuration Configuration in ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationType == ConfigurationType.Essential).ToArray())
                if (!EssentialModules.Contains(Configuration.ConfigurationName)) {
                    Configuration.ConfigurationType = ConfigurationType.Disabled;
                    await ConfigurationDB.SaveChangesAsync();
                }

            // Check for attribute not set in database but in project.
            foreach (Type Type in GetModuleTypes().Where(Module => Module.IsDefined(typeof(EssentialModuleAttribute)))) {
                string TypeName = Type.Name.Sanitize();

                Configuration Config = ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationName == TypeName).FirstOrDefault();

                if (Config.ConfigurationType != ConfigurationType.Essential) {
                    Config.ConfigurationType = ConfigurationType.Essential;
                    await ConfigurationDB.SaveChangesAsync();
                }
            }

            // Loop through all enabled or essential modules.
            foreach (Configuration Configuration in ConfigurationDB.Configurations.ToArray())
                switch (Configuration.ConfigurationType) {
                    case ConfigurationType.Enabled:
                        await CommandService.AddModuleAsync(GetModuleTypeByName(Configuration.ConfigurationName), Services);
                        Others++;
                        break;
                    case ConfigurationType.Essential:
                        await CommandService.AddModuleAsync(GetModuleTypeByName(Configuration.ConfigurationName), Services);
                        Essentials++;
                        break;
                }

            // Logs the number of currently enabled modules to the console.
            await LoggingService.LogMessageAsync(
                new LogMessage(LogSeverity.Info, "Modules", $"Initialized the module service with {Essentials} essential module(s) and {Others} other module(s).")
            );
        }

        /// <summary>
        /// The Get Modules method finds all modules of type ConfigrationType in the database, returning all the names of the modules in one string array.
        /// </summary>
        /// <param name="Type">The type of the modules you would like to return in a string array - either essential, enabled or disabled.</param>
        /// <returns>Returns an array of strings containing the name of the module that has type ConfigurationType.</returns>
        public string[] GetModules(ConfigurationType Type) {
            return ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationType == Type).Select(Configuration => Configuration.ConfigurationName).ToArray();
        }

        /// <summary>
        /// The Get Module State returns a boolean of whether a module is enabled or disable by its name in the database.
        /// </summary>
        /// <param name="ModuleName">The name of the module you'd like to check for in the database.</param>
        /// <returns>Whether or not the module is, in fact, enabled or not.</returns>
        public bool GetModuleState(string ModuleName) {
            return ConfigurationDB.Configurations.AsQueryable().Where(Module => Module.ConfigurationName == ModuleName).FirstOrDefault().ConfigurationType != ConfigurationType.Disabled;
        }

        /// <summary>
        /// The Set Module State method sets a module to be either enabled or disabled in the database, and then modifies it in the Command Service to be serviceable or not. 
        /// </summary>
        /// <param name="ModuleName">The name of the module of which you wish to have enabled.</param>
        /// <param name="IsEnabed">A boolean of whether or not the module should be enabled.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task SetModuleState(string ModuleName, bool IsEnabed) {
            ConfigurationDB.Configurations.AsQueryable().Where(Module => Module.ConfigurationName == ModuleName).FirstOrDefault().ConfigurationType = IsEnabed ? ConfigurationType.Enabled : ConfigurationType.Disabled;
            await ConfigurationDB.SaveChangesAsync();

            if (IsEnabed)
                await CommandService.AddModuleAsync(GetModuleTypeByName(ModuleName), Services);
            else
                await CommandService.RemoveModuleAsync(GetModuleTypeByName(ModuleName));
        }

        /// <summary>
        /// The Verify Module Name method checks to see if there is a module of name ModuleName and, if so, it returns true and sanitizes the name.
        /// </summary>
        /// <param name="ModuleName">The name of the module you wish to check exists, sanitised if true.</param>
        /// <returns>A boolean of whether or not the module exists in the project.</returns>
        public static bool VerifyModuleName(ref string ModuleName) {
            Type HasMatch = GetModuleTypeByName(ModuleName);

            if (HasMatch != null) {
                ModuleName = HasMatch.Name.Sanitize();
                return true;
            }

            return false;
        }

        /// <summary>
        /// The Get Module Types returns an array of all the types that extend the abstract class DiscordModule.
        /// </summary>
        /// <returns>An array of types which extend DiscordModule.</returns>
        public static Type[] GetModuleTypes()
            => Assembly.GetExecutingAssembly().GetTypes()
                .Where(Type => typeof(DiscordModule).IsAssignableFrom(Type) && !Type.IsAbstract)
                .ToArray();

        /// <summary>
        /// The Get Module Type By Name method attempts to find a module reflexively that has the name of the specified module name, with its case ignored.
        /// If a match does exist, it returns the type of the module. If otherwise, it returns null.
        /// </summary>
        /// <param name="ModuleName">The name of the module you'd like to be reflexively searched for.</param>
        /// <returns>The type of the module that has been searched - is null if the module does not exist.</returns>
        public static Type GetModuleTypeByName(string ModuleName)
            => GetModuleTypes().FirstOrDefault(Module => string.Equals(Module.Name.Sanitize(), ModuleName, StringComparison.InvariantCultureIgnoreCase));
    }

}
