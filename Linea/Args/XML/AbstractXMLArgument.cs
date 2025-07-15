using MapXML.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Linea.Args.XML
{
    internal class ArgumentsNode
    {
        public List<ArgumentNode> Items { get; private set; }
            = new List<ArgumentNode>();

        [XMLChild("Constraint")]
        public List<ConstraintNode> Constraints { get; private set; }
            = new List<ConstraintNode>();

        [XMLChild("Simple")] public void Add(SimpleArgumentNode item) => Items.Add(item);
        [XMLChild("Flag")] public void Add(FlagArgumentNode item) => Items.Add(item);
        [XMLChild("Named")] public void Add(NamedArgumentNode item) => Items.Add(item);

        public void AddAll(ArgumentDescriptorCollection coll)
        {
            Items.ForEach(x =>
            {
                x.Create(coll);
            });

            Constraints.ForEach(x =>
            {
                x.Create(coll);
            });
        }
    }

    internal class PossibleValues
    {
        [XMLChild("Value")]
        internal List<string> Values { get; set; } = new List<string>();
    }

    internal abstract class ArgumentNode
    {
        [XMLAttribute("Name")]
        internal string? Name { get; set; }

        [XMLChild("Description")]
        [XMLTextContent()]
        internal string? Description { get; set; }

        [XMLChild()]
        [XMLAttribute()]
        internal string? EnquireString { get; set; }

        [XMLChild("PossibleValues")]
        protected PossibleValues? _possibleValues;

        protected IEnumerable<string> PossibleValues => _possibleValues?.Values ?? Enumerable.Empty<string>();

        [XMLChild("Alias")]
        internal List<string> _aliases { get; set; } = new List<string>();

        public ArgumentAliasCollection Aliases => new ArgumentAliasCollection(
            Name ?? throw new ArgumentNullException("Name", ""),
            Description,
            _aliases);
        public void Create(ArgumentDescriptorCollection coll)
        {
            var res = InternalCreate(coll);
            if (EnquireString != null)
                res.EnquireString = this.EnquireString;
        }
        public abstract ArgumentDescriptor InternalCreate(ArgumentDescriptorCollection coll);
    }

    internal abstract class ValueArgument : ArgumentNode
    {
        [XMLAttribute("ValueType")]
        public ArgumentValueType ValueType { get; set; } = ArgumentValueType.String;
        [XMLAttribute("Mandatory")]
        public bool IsMandatory { get; set; } = false;
        [XMLAttribute("Repeatable")]
        public bool IsRepeatable { get; set; } = false;


        protected ArgumentOptions Options => ArgumentOptions.None
            | (IsMandatory ? ArgumentOptions.Mandatory : ArgumentOptions.None)
            | (IsRepeatable ? ArgumentOptions.Repeatable : ArgumentOptions.None)
            ;
    }
    internal class SimpleArgumentNode : ValueArgument
    {
        public override ArgumentDescriptor InternalCreate(ArgumentDescriptorCollection coll)
        {
            return coll.AddSimpleValue(this.Aliases, this.ValueType, this.Options, this.PossibleValues.ToArray());
        }
    }

    internal class FlagArgumentNode : ArgumentNode
    {
        public FlagArgumentNode()
        {
        }

        public override ArgumentDescriptor InternalCreate(ArgumentDescriptorCollection coll)
        {
            return coll.AddFlag(this.Aliases);
        }
    }

    internal class NamedArgumentNode : ValueArgument
    {

        public override ArgumentDescriptor InternalCreate(ArgumentDescriptorCollection coll)
        {
            return coll.AddNamedValue(this.Aliases, this.ValueType, this.Options, this.PossibleValues.ToArray());
        }
    }
    internal class ConstraintNode
    {
        [XMLAttribute("Type")]
        internal ConstraintType? Type { get; set; } = null;

        [XMLChild("Argument")]
        internal List<string> _arguments { get; set; } = new List<string>();


        public void Create(ArgumentDescriptorCollection coll)
        {
            if (Type == null)
                throw new ArgumentNullException(nameof(Type), "Constraint type must be specified.");
            if (_arguments.Count == 0)
                throw new ArgumentException("At least one argument must be specified for the constraint.", nameof(_arguments));
            coll.AddConstraint(Type.Value, _arguments.ToArray());
        }
    }
}
