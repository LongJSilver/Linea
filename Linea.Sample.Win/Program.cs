using Linea.Command;
using Linea.Interface;
using Linea.Utils;
using Linea.Windows;
using System.Reflection;

namespace Linea.Sample.Win;

public class Program
{

    public static void Main()
    {
        Cli cli = new Cli(new WindowsConsole());
        cli.PromptString = ">>> ";

        CliCommandHandler h = new CliCommandHandler();

        Assembly a = typeof(Program).Assembly;
        var xml = a.GetManifestResourceStream($"ConsoleAppTest.SampleCommands.xml");

        h.LoadFromXML(xml, StandardCommands.Linker);
        cli.RegisterHandler(h);
        cli.Run();
    }

}
