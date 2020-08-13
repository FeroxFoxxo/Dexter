using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Dexter.Core.Configuration {
    public static class JSONConfigurator {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions {
            WriteIndented = true
        };

        private static readonly Dictionary<Type, object> Configurations = new Dictionary<Type, object>();

        public static void LoadConfigurations() {
            foreach (Type ConfigurationFile in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(AbstractConfiguration).IsAssignableFrom(x)))
                if (ConfigurationFile != typeof(AbstractConfiguration))
                    LoadConfiguration(ConfigurationFile);
        }

        private static void LoadConfiguration(Type ConfigurationFile) {
            if (!File.Exists(ConfigurationFile.Name + ".json")) {
                object Abstract = Activator.CreateInstance(ConfigurationFile);
                File.WriteAllText(ConfigurationFile.Name + ".json", JsonSerializer.Serialize(Abstract, _jsonOptions));
                Configurations.Add(ConfigurationFile, Abstract);
            } else {
                string json = File.ReadAllText(ConfigurationFile.Name + ".json");
                Configurations.Add(ConfigurationFile, JsonSerializer.Deserialize(json, ConfigurationFile, _jsonOptions));
            }
        }

        public static object GetConfiguration(Type ConfigurationClass, string Field) {
            Field = "<" + Field + ">k__BackingField";

            if (!Configurations.ContainsKey(ConfigurationClass)) {
                ConsoleLogger.LogError("Configuration does not include " + ConfigurationClass.Name + "! Are you sure it extends AbstractConfig? RETURNING NULL...");
                return null;
            } else if (Configurations[ConfigurationClass].GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault(item => item.Name == Field) == null) {
                ConsoleLogger.LogError(ConfigurationClass.Name + " does not include field \"" + Field + "\"! RETURNING NULL...");
                return null;
            } else
                return Configurations[ConfigurationClass].GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault(item => item.Name == Field).GetValue(Configurations[ConfigurationClass]);
        }
    }

    public abstract class AbstractConfiguration { }
}
