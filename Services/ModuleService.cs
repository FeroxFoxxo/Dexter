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

namespace Dexter.Services {
    public class ModuleService : InitializableModule {

        private readonly LoggingService LoggingService;
        private readonly CommandService CommandService;
        private readonly IServiceProvider Services;
        private readonly ConfigurationDB ConfigurationDB;

        public ModuleService(CommandService _CommandService, LoggingService _LoggingService, IServiceProvider _Services, ConfigurationDB _ConfigurationDB) {
            CommandService = _CommandService;
            LoggingService = _LoggingService;
            Services = _Services;
            ConfigurationDB = _ConfigurationDB;
        }

        public async override void AddDelegates() {
            int Essentials = 0, Others = 0;

            string[] EssentialModules = GetEssentialModules();

            // Check for configs not in project but in database
            foreach (Config Configuration in ConfigurationDB.Configurations.ToArray())
                if (GetModuleTypeByName(Configuration.ConfigurationName) == null) {
                    ConfigurationDB.Configurations.Remove(Configuration);
                    await ConfigurationDB.SaveChangesAsync();
                }

            // Check for configs not in database but in project.
            foreach (Type Type in GetModuleTypes()) {
                string TypeName = Type.Name.Sanitize();

                if (ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationName == TypeName).FirstOrDefault() == null) {
                    ConfigurationDB.Configurations.Add(new Config() {
                        ConfigurationName = TypeName,
                        ConfigurationType = ConfigrationType.Disabled
                    });
                    await ConfigurationDB.SaveChangesAsync();
                }
            }

            // Check for attribute set in database not matching project.
            foreach (Config Configuration in ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationType == ConfigrationType.Essential).ToArray())
                if (!EssentialModules.Contains(Configuration.ConfigurationName)) {
                    Configuration.ConfigurationType = ConfigrationType.Disabled;
                    await ConfigurationDB.SaveChangesAsync();
                }

            // Check for attribute not set in database but in project.
            foreach (Type Type in GetModuleTypes().Where(Module => Module.IsDefined(typeof(EssentialModuleAttribute)))) {
                string TypeName = Type.Name.Sanitize();

                Config Config = ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationName == TypeName).FirstOrDefault();

                if(Config.ConfigurationType != ConfigrationType.Essential) {
                    Config.ConfigurationType = ConfigrationType.Essential;
                    await ConfigurationDB.SaveChangesAsync();
                }
            }

            // Loop through all enabled or essential modules.
            foreach (Config Configuration in ConfigurationDB.Configurations.ToArray())
                switch (Configuration.ConfigurationType) {
                    case ConfigrationType.Enabled:
                        await CommandService.AddModuleAsync(GetModuleTypeByName(Configuration.ConfigurationName), Services);
                        Others++;
                        break;
                    case ConfigrationType.Essential:
                        await CommandService.AddModuleAsync(GetModuleTypeByName(Configuration.ConfigurationName), Services);
                        Essentials++;
                        break;
                }

            await LoggingService.LogMessageAsync(
                new LogMessage(LogSeverity.Info, "Modules", $"Initialized the module service with {Essentials} essential module(s) and {Others} other module(s).")
            );
        }

        public string[] GetModules(ConfigrationType Type) {
            return ConfigurationDB.Configurations.AsQueryable().Where(Configuration => Configuration.ConfigurationType == Type).Select(Configuration => Configuration.ConfigurationName).ToArray();
        }

        public bool GetModuleState(string ModuleName) {
            return ConfigurationDB.Configurations.AsQueryable().Where(Module => Module.ConfigurationName == ModuleName).FirstOrDefault().ConfigurationType != ConfigrationType.Disabled;
        }

        public async Task SetModuleState(string ModuleName, bool IsEnabed) {
            ConfigurationDB.Configurations.AsQueryable().Where(Module => Module.ConfigurationName == ModuleName).FirstOrDefault().ConfigurationType = IsEnabed ? ConfigrationType.Enabled : ConfigrationType.Disabled;
            await ConfigurationDB.SaveChangesAsync();

            if(IsEnabed)
                await CommandService.AddModuleAsync(GetModuleTypeByName(ModuleName), Services);
            else
                await CommandService.RemoveModuleAsync(GetModuleTypeByName(ModuleName));
        }

        public static bool VerifyModuleName(ref string ModuleName) {
            Type HasMatch = GetModuleTypeByName(ModuleName);

            if (HasMatch != null) {
                ModuleName = HasMatch.Name.Sanitize();
                return true;
            }

            return false;
        }

        public static string[] GetEssentialModules()
            => GetModuleTypes().Where(Module => Module.IsDefined(typeof(EssentialModuleAttribute))).Select(Module => Module.Name.Sanitize()).ToArray();

        private static Type[] GetModuleTypes()
            => Assembly.GetExecutingAssembly().GetTypes()
                .Where(Type => typeof(ModuleD).IsAssignableFrom(Type) && !Type.IsAbstract)
                .ToArray();

        private static Type GetModuleTypeByName(string ModuleName)
            => GetModuleTypes().FirstOrDefault(Module => string.Equals(Module.Name.Sanitize(), ModuleName, StringComparison.InvariantCultureIgnoreCase));
    }
}
