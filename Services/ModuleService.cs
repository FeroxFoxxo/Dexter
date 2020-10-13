using Dexter.Abstractions;
using Dexter.Attributes;
using Dexter.Configuration;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dexter.Services {
    public class ModuleService : InitializableModule {

        private readonly LoggingService LoggingService;
        private readonly CommandService CommandService;
        private readonly IServiceProvider Services;
        private readonly BotConfiguration BotConfiguration;

        private Type[] ModuleTypes;

        public ModuleService(CommandService _CommandService, LoggingService _LoggingService, IServiceProvider _Services, BotConfiguration _BotConfiguration) {
            CommandService = _CommandService;
            LoggingService = _LoggingService;
            Services = _Services;
            BotConfiguration = _BotConfiguration;
        }

        public async override void AddDelegates() {
            ModuleTypes = GetModuleTypes();

            int Essentials = 0, Others = 0;

            foreach (Type EssentialModule in ModuleTypes.Where(Module => Module.IsDefined(typeof(EssentialModuleAttribute)))) {
                await CommandService.AddModuleAsync(EssentialModule, Services);
                Essentials++;
            }

            foreach (KeyValuePair<string, bool> Module in BotConfiguration.ModuleConfigurations) {
                if (Module.Value) {
                    await CommandService.AddModuleAsync(GetModuleTypeByName(Module.Key), Services);
                    Others++;
                }
            }

            await LoggingService.LogMessageAsync(
                new LogMessage(LogSeverity.Info, "Modules", $"Initialized the module service with {Essentials} essential module(s) and {Others} other module(s)."));
        }

        public bool GetModuleState(string ModuleName) {
            if (BotConfiguration.ModuleConfigurations.TryGetValue(ModuleName, out bool IsActive))
                return IsActive;

            return false;
        }

        public bool VerifyModuleName(ref string ModuleName) {
            Type HasMatch = GetModuleTypeByName(ModuleName);

            if (HasMatch != null) {
                ModuleName = HasMatch.Name.Sanitize();
                return true;
            }

            return false;
        }

        public async Task SetModuleState(string ModuleName, bool IsActive) {
            if (!BotConfiguration.ModuleConfigurations.TryAdd(ModuleName, IsActive))
                BotConfiguration.ModuleConfigurations[ModuleName] = IsActive;

            Type TargetModule = GetModuleTypeByName(ModuleName);

            if (IsActive)
                await CommandService.AddModuleAsync(TargetModule, Services);
            else
                await CommandService.RemoveModuleAsync(TargetModule);
        }

        public string[] GetEnabledModules()
            => BotConfiguration.ModuleConfigurations.Where(IsActive => IsActive.Value).Select(Module => Module.Key).ToArray();

        public string[] GetEssentialModules()
            => ModuleTypes.Where(Module => Module.IsDefined(typeof(EssentialModuleAttribute))).Select(Module => Module.Name.Sanitize()).ToArray();

        public string[] GetDisabledModules()
            => GetAllModuleNames().Except(GetEnabledModules().Concat(GetEssentialModules())).ToArray();

        public string[] GetAllModuleNames()
            => ModuleTypes.Select(Module => Module.Name.Sanitize()).ToArray();

        private static Type[] GetModuleTypes()
            => Assembly.GetExecutingAssembly().GetTypes()
                .Where(Type => typeof(Abstractions.Module).IsAssignableFrom(Type) && !Type.IsAbstract)
                .ToArray();

        private Type GetModuleTypeByName(string ModuleName)
            => ModuleTypes.FirstOrDefault(Module => string.Equals(Module.Name.Sanitize(), ModuleName, StringComparison.InvariantCultureIgnoreCase));
    }
}
