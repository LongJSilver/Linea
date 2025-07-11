using Linea.Args.XML;
using Linea.Command;
using MapXML.Attributes;
using System;
using System.Collections.Generic;

namespace Linea.Commmand.XML
{
    internal class CommandsNode
    {
        [XMLChild("Command")]
        internal List<CommandNode> Commands { get; private set; }
            = new List<CommandNode>();

        internal void Create(CliCommandHandler handler, Func<string, CommandDelegate> func)
        {
            foreach (var commandNode in Commands)
            {
                CliCommand command = commandNode.Create(func(commandNode.Name!));
                handler.AddCommand(command);
            }
        }
    }

    internal class CommandNode
    {

        [XMLChild("Alias")]
        internal List<string> Aliases { get; } = new List<string>();

        [XMLAttribute()]
        [XMLChild()]
        internal string? Name { get; set; }

        [XMLAttribute()]
        [XMLChild()]
        internal string? DisplayName { get; set; }
         
        [XMLAttribute()]
        [XMLChild()]
        internal string? Description { get; set; }

        [XMLChild("Arguments")]
        internal ArgumentsNode? Arguments { get; set; }

        [XMLAttribute()]
        [XMLChild()]
        internal bool? Indexable { get; set; } = null;

        [XMLAttribute()]
        [XMLChild()]
        internal bool? EnforceConstraints { get; set; } = null;

        [XMLAttribute()]
        [XMLChild()]
        internal bool? AskForMissingArguments { get; set; } = null;

        public CliCommand Create(CommandDelegate deleg)
        {
            CommandDescriptor commandDescriptor = new CommandDescriptor(
                Name ?? throw new ArgumentNullException(nameof(Name), ""),
                DisplayName ?? Name,
                Description);

            CliCommand command = CliCommand.FromDelegate(commandDescriptor, deleg);

            foreach (var alias in Aliases)
            {
                command.Descriptor.AddAlias(alias);
            }

            if (this.Arguments != null)
                this.Arguments.AddAll(command.Arguments);

            if (Indexable.HasValue) command.IsIndexable = Indexable.Value;
            if (EnforceConstraints.HasValue) command.ShouldEnforceConstraints = EnforceConstraints.Value;
            if (AskForMissingArguments.HasValue) command.ShouldAskForMissingArguments = AskForMissingArguments.Value;

            return command;
        }
    }



}
