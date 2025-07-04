using Linea.Command;
using Linea.Utils;
using System.Reflection;
using System.Windows;
using Linea.Interface;


namespace Linea.Sample.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Cli cli = new Cli(ConsoleBox);
        cli.PromptString = ">>> ";

        CliCommandHandler h = new CliCommandHandler();

        Assembly a = typeof(MainWindow).Assembly;
        System.IO.Stream? xml = a.GetManifestResourceStream($"Linea.Sample.WPF.App.SampleCommands.xml");

        h.LoadFromXML(xml!, StandardCommands.Linker);
        cli.RegisterHandler(h);
        cli.BeginRun();
    }

}