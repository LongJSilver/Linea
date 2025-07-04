using Linea.Args;
using Linea.Command;
using System;
using System.Threading;

namespace Linea.Interface
{
    public static class StandardCommands
    {
        public static void Wait(string command, ParsedArguments args, ICliFunctions clif)
        {
            int ms = 0;
            bool shouldWait = false;
            if (args != null && args.Count == 1)
            {
                shouldWait = true;
                ms = args[0].AsInt;
            }
            else
            {
                while (true)
                {
                    clif.WriteLine("Type the number of milliseconds to wait for:");
                    if (clif.PromptForInput("", out string? result))
                    {
                        if (Int32.TryParse(result, out ms))
                        {
                            shouldWait = true;
                            break;
                        }
                        else
                        {
                            clif.Write("Not a valid number. ");
                        }
                    }
                    else
                    {
                        clif.WriteLine("Aborted by user.");
                        shouldWait = false;
                        break;
                    }
                }
            }
            if (shouldWait)
            {
                clif.WriteLine(string.Format("Waiting {0} milliseconds...", ms));

                Thread.Sleep(ms);
            }
        }

        public static void Echo(string command, ParsedArguments args,
             ICliFunctions clif)
        {
            if (args != null && args.Count > 0)
            {
                for (int i = 0; i < args.Count; i++)
                {
                    if (i > 0)
                    {
                        clif.Write(" ");
                    }
                    clif.Write(args[i].Value ?? string.Empty);
                }
                clif.WriteLine("");
            }
            else
            {
                clif.WriteLine("Enter a string to echo:");
                if (clif.PromptForInput("", out string? result))
                {
                    clif.WriteLine(result);
                }
                else
                {
                    clif.WriteLine("Aborted by user.");
                }
            }
        }

        public static void Exit(string command, ParsedArguments args, ICliFunctions clif)
        {
            clif.Exit();
        }

        public static void TestException(string command, ParsedArguments args, ICliFunctions clif)
        {
            throw new Exception("Test Exception");
        }

        public static CommandDelegate Linker(string arg)
        {
            switch (arg.ToLower())
            {
                case "wait": return Wait;
                case "echo": return Echo;
                case "exit": return Exit;
                case "testexception": return TestException;
                default:
                    throw new ArgumentException($"Unknown command '{arg}'", nameof(arg));
            }
        }
    }
}
