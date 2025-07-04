using Linea.Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Linea.Utils;

namespace Linea.Test
{
    public class ArgumentsFromXML
    {
        public Stream GetTestXML(String name)
        {
            Assembly a = typeof(ArgumentsFromXML).Assembly;
            return a.GetManifestResourceStream($"Linea.Test.DataFiles.{name}.xml") ?? throw new ArgumentException(nameof(name));
        }

        [Fact]
        public void LoadArgumentsFromXML()
        {
            using Stream xml = GetTestXML("SampleArguments");
            ArgumentDescriptorCollection coll = new ArgumentDescriptorCollection();
            coll.LoadFromXML(xml);

            // Check count
            Assert.Equal(6, coll.Count);

            // Check Simple arguments
            var origin = coll["OriginPath"];
            Assert.NotNull(origin);
            Assert.True(origin.IsMandatory);
            Assert.Equal(ArgumentValueType.String, origin.ValueType);
            Assert.Contains("src", origin.Aliases);

            var dest = coll["DestinationPath"];
            Assert.NotNull(dest);
            Assert.True(dest.IsMandatory);
            Assert.Equal(ArgumentValueType.String, dest.ValueType);
            Assert.Contains("dst", dest.Aliases);

            var ext = coll["Extension"];
            Assert.NotNull(ext);
            Assert.False(ext.IsMandatory);
            Assert.Equal(ArgumentValueType.Enum, ext.ValueType);
            Assert.Contains("ext", ext.Aliases);
            Assert.True(ext.HasPossibleValuesDefined);
            Assert.Contains("txt", ext.PossibleValues.Values);
            Assert.Contains("csv", ext.PossibleValues.Values);
            Assert.Contains("xml", ext.PossibleValues.Values);

            // Check Flag arguments
            var flagO = coll["o"];
            Assert.NotNull(flagO);
            Assert.True(flagO.IsFlag);
            Assert.Contains("Overwrite", flagO.Aliases);

            var flagT = coll["t"];
            Assert.NotNull(flagT);
            Assert.True(flagT.IsFlag);

            // Check Named argument
            var retry = coll["RetryCount"];
            Assert.NotNull(retry);
            Assert.False(retry.IsMandatory);
            Assert.False(retry.IsRepeatable);
            Assert.Equal(ArgumentValueType.Integer, retry.ValueType);
            Assert.Contains("retries", retry.Aliases);
        }
    }
}
