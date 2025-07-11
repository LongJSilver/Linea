using System;
using System.Collections.Generic;

namespace Linea.Command
{
    public struct CommandDescriptor : IEquatable<CommandDescriptor>
    {


        private readonly List<string> _names;
        public readonly string Name => _names[0];
        public readonly string DisplayName;
        public readonly string Description;

        public IReadOnlyCollection<string> Names => _names;

        public CommandDescriptor(string name, string displayName, string? longDescription = null)
            : this(name, displayName)
        {
            this.Description = longDescription ?? string.Empty;
        }

        public CommandDescriptor(string name, string displayName)
        {
            _names = new List<string>
           {
               name
           };
            this.DisplayName = displayName ?? name;
            this.Description = string.Empty;
        }
        public void AddAlias(string alias)
        {
            _names.Add(alias);
        }

        public override readonly string ToString() => String.Format("Command '{0}'", this.DisplayName);

        public override bool Equals(object? obj)
        {
            return obj is CommandDescriptor descriptor && Equals(descriptor);
        }

        public bool Equals(CommandDescriptor other)
        {
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
         
    }
}
