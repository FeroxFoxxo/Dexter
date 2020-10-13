using Dexter.Abstractions;
using Dexter.CustomCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Dexter.Services {
    public class CustomCommandsService : InitializableModule {

        private const string CommandFile = "commands.json";

        private List<CustomCommand> Commands;

        private static readonly JsonSerializerOptions JSONOptions = new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        public override void AddDelegates() {
            LoadCustomCommands();
        }

        public void SaveCustomCommands() =>
            File.WriteAllText(CommandFile, JsonSerializer.Serialize(Commands, JSONOptions));

        public bool TryGetCommand(string Name, out CustomCommand Command) {
            Command = GetCommandByName(Name);

            return Command != null;
        }

        public CustomCommand GetCommandByName(string Name) => Commands.FirstOrDefault(Command => Command.Matches(Name));

        private void LoadCustomCommands() {
            if (!File.Exists(CommandFile)) {
                Commands = new List<CustomCommand>();
                SaveCustomCommands();
                return;
            }

            Commands = JsonSerializer.Deserialize<List<CustomCommand>>(File.ReadAllText(CommandFile), JSONOptions);
        }

    }
}
