using Linea.Command;
using Linea.Utils;
using MapXML.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Threading;

namespace Linea.Interface
{
    public partial class Cli
    {
        private ICommandHandler? _currentCommandHandler;

        /// <summary>
        /// Tutti gli handler vengono associati a ciascuno dei comandi che gestiscono.
        /// </summary>
        private CIDictionary<ICommandHandler> _CommandToHandler
            = new CIDictionary<ICommandHandler>();
        /// <summary>
        /// insieme degli handler di comandi registrati.
        /// </summary>
        private ISet<ICommandHandler> _AllHandlers = new HashSet<ICommandHandler>();

        /// <summary>
        /// Aggiunge un <see cref="ICommandHandler"/> alla lista.<para/> L'aggiunta prevede che l'handler venga associato
        /// a ciascuno dei comandi che può gestire, a meno che quel comando non sia già associato ad un altro handler, nel qual caso viene sollevata un eccezione
        /// ed annullata l'aggiunta.
        /// </summary>
        /// <param name="commandHandler">l'Handler da aggiungere</param>
        public void RegisterHandler(ICommandHandler commandHandler)
        {
            this.InternalAddCommands(commandHandler);//se questo va in errore, non aggiungiamo nulla
            this.RefreshPrompt();
            //non invertire l'ordine!
            commandHandler.HandlerChanged += this.OnHandlerChanged;
            this._AllHandlers.Add(commandHandler);
        }

        private void InternalAddCommands(ICommandHandler commandHandler)
        {
            //è previsto che un handler cambi il suo set di comandi a runtime.
            //per gestire questa evenienza, rimuoviamo l'handler (se presente) e lo aggiungiamo ex-novo.
            this.InternalRemoveCommands(commandHandler);

            if (!this.CanRegister(commandHandler))
            {
                throw new ArgumentException(String.Format("Cannot Register this handler!"));
            }

            //aggiungiamo tutti i comandi
            var names = from c in commandHandler.Commands from n in c.Names select n;

            foreach (string n in names)
            {
                this._CommandToHandler.Add(n, commandHandler);
            }
        }

        /// <summary>
        ///questa funzione verifica che nessuno dei comandi gestiti da questo handler sia già gestito da altri handler.
        /// 
        /// </summary>
        /// <param name="commandHandler"></param>
        /// <returns></returns>
        private bool CanRegister(ICommandHandler commandHandler)
        {
            foreach (CommandDescriptor item in commandHandler.Commands)
            {
                if (item.Names.Any(this._CommandToHandler.ContainsKey))
                    return false;
            }
            return true;
        }

        public void UnRegisterHandler(ICommandHandler c)
        {
            if (this._AllHandlers.Contains(c))
            {
                this.InternalRemoveCommands(c);

                c.HandlerChanged -= this.OnHandlerChanged;
                this._AllHandlers.Remove(c);
            }
        }

        private void InternalRemoveCommands(ICommandHandler c)
        {
            List<string> commandsRegisteredForThisHandler = new();
            foreach (string item in this._CommandToHandler.Keys)
            {
                if (this._CommandToHandler.ContainsKey(item))
                {
                    commandsRegisteredForThisHandler.Add(item);
                }
            }
            foreach (string item in commandsRegisteredForThisHandler)
            {
                this._CommandToHandler.Remove(item);
            }
        }

        private void OnHandlerChanged(ICommandHandler c)
        {
            this.InternalAddCommands(c);
            this.RefreshPrompt();
        }

        /// <summary>
        /// Questa funzione viene passata come argomento ai <see cref="ICommandHandler"/> per consentire loro di richiedere un input all'utente.
        /// <para/>Quando viene chiamata, viene riattivato l'inserimento di input da parte dell'utente e la funzione si blocca acquisendo 
        /// il monitor sull'oggetto <see cref="_currentCommandMonitor"/>.
        /// <para/>
        /// Quando viene risvegliata, se trova il flag <see cref="_currentCommandPromptFlag"/> impostato, restituisce il contenuto di <see cref="_currentCommandPromptResult"/>, che si presume sia stato riempito con l'input dell'utente.
        /// <para/>
        /// La funzione continua a bloccarsi finchè il flag <see cref="_currentCommandPromptFlag"/> non viene impostato, per evitare falsi positivi.
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        private bool GeneralPrompt<V>(string? prompt, [MaybeNullWhen(false), NotNullWhen(true)] out V? result) where V : class
        {
            InputMode toSet;
            if (prompt == null) prompt = ": ";
            else if (!prompt.TrimEnd().EndsWith(":"))
                prompt = prompt + ": ";
            WritePrompt(prompt);

            this._currentCommandPromptFlag = CommandPromptResult.NoneYet;
            if (typeof(V).Equals(typeof(string)) || typeof(V).Equals(typeof(String)))
            {
                this._mode = toSet = InputMode.UserCommandInput;
            }
            else if (typeof(V).Equals(typeof(SecureString)))
            {
                this._mode = toSet = InputMode.UserCommandInputPassword;
                this._password = new SecureString();

            }
            else
            {
                throw new ArgumentException("This method can only be invoked with 'String' or 'SecureString' argument.");
            }

            this._acceptingInput = true;
            lock (this._currentCommandMonitor)
            {
                while (this._currentCommandPromptFlag == CommandPromptResult.NoneYet)
                {
                    Monitor.Wait(this._currentCommandMonitor);
                }
            }

            bool resultFlag;

            if (this._currentCommandPromptFlag == CommandPromptResult.OK)
            {
                if (toSet == InputMode.UserCommandInputPassword)
                {
                    result = (V?)(object?)this._currentCommandPromptPasswordResult;
                }
                else
                {
                    result = this._currentCommandPromptResult as V;
                }
                this._currentCommandPromptFlag = CommandPromptResult.NoneYet;
                resultFlag = true;
            }
            else
            {
                result = default;
                resultFlag = false;
            }


            this._currentCommandPromptFlag = CommandPromptResult.NoneYet;
            return resultFlag;
        }

        private void EscapeAction()
        {
            switch (this._mode)
            {
                case InputMode.UserCommand:
                    ClearLine();
                    break;
                case InputMode.UserCommandInput:
                case InputMode.UserCommandInputPassword:
                    TerminatePromptCommandInput(CommandPromptResult.Canceled);
                    break;
                case InputMode.SystemText:
                    break;
            }
        }

        private void TerminatePromptCommandInput(CommandPromptResult result)
        {
            this._currentCommandPromptFlag = result;
            this._acceptingInput = false;
            this._cm.WriteLine();
            this._mode = InputMode.SystemText;
            lock (this._currentCommandMonitor)
            {
                Monitor.PulseAll(this._currentCommandMonitor);
            }
        }

        bool ICliFunctions.PromptForPasswordUnsafely(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out string? pass)
        {
            if (this.GeneralPrompt(prompt, out SecureString? UserString))
            {
                pass = UserString.ToClearString();
                return pass != null;
            }
            else
            {
                pass = null;
                return false;
            }
        }

        bool ICliFunctions.PromptForPasswordSafely(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out SecureString? pass) => this.GeneralPrompt(prompt, out pass);
        bool ICliFunctions.PromptForInput(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out string? UserString) => this.GeneralPrompt(prompt, out UserString);

        void ICliFunctions.Write(string str, params object[] args) => this.WriteString(str, args);
        void ICliFunctions.WriteLine(string str, params object[] args) => this._cm.WriteLine(String.Format(str, args));
        void ICliFunctions.PrintList(string caption, IEnumerable<string> elements)
        {
            if (!(elements is IList<string> input))
            {
                input = new List<string>(elements);
            }

            this.PrintList(input, caption, (_) => _, false);
        }
        void ICliFunctions.PrintList(IEnumerable<string> elements)
        {
            if (elements is not IList<string> input)
            {
                input = new List<string>(elements);
            }

            this.PrintList(input, null, (_) => _, false);
        }

        bool ICliFunctions.PromptForInput<V>(string prompt, IEnumerable<V> choices,
            Func<V, string> ToString, [MaybeNullWhen(false), NotNullWhen(true)] out V result)
        {
            Dictionary<int, V> _results = new Dictionary<int, V>();
            if (choices is not IList<V> input)
            {
                input = new List<V>(choices);
            }

            this.PrintList(input, prompt, ToString, numbered: true, OnAssociated: (i, j) => _results[i] = j);
            if (!this.GeneralPrompt(null, out string? userTyped))
            {
                result = default;
                return false;
            }
            else
            {
                int index;
                if (int.TryParse(userTyped, out index) && _results.ContainsKey(index))
                {
                    result = _results[index]!;
                    return true;
                }
                else
                {

                    this._cm.WriteLine("Error: the input string was invalid!");

                    result = default;
                    return false;
                }
            }
        }

        bool ICliFunctions.PromptForConfirmation(string prompt, bool defaultToYes = true)
        {
            this.PrintList(new String[] { "Yes", "No" }, prompt, (s) => s, numbered: true, OnAssociated: (i, j) => { });
            if (!this.GeneralPrompt($"[{(defaultToYes ? 'y' : 'n')}] >", out string? userTyped))
            {
                return false;
            }
            else
            {
                userTyped = (userTyped ?? "").ToLower().Trim();
                return userTyped switch
                {
                    "yes" or "y" => true,
                    "no" or "n" => false,
                    _ => defaultToYes,
                };
            }
        }

        bool ICliFunctions.PromptForInput(string prompt, IEnumerable<string> choices, [MaybeNullWhen(false), NotNullWhen(true)] out string? result)
        {
            if (choices is not IList<string> input)
            {
                input = new List<string>(choices);
            }

            this.PrintList(input, prompt, (s) => s, numbered: true, OnAssociated: (i, j) => { });
            if (!this.GeneralPrompt(">", out string? userTyped))
            {
                result = default;
                return false;
            }
            else
            {
                if (input.Contains(userTyped))
                {
                    result = userTyped;
                    return true;
                }

                int index;
                if (int.TryParse(userTyped, out index) && index >= 0 && index < input.Count)
                {
                    result = input[index];
                    return true;
                }
                else
                {
                    this._cm.WriteLine("Error: the input string was invalid!");
                    result = default;
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RawCommand"></param>
        /// <param name="InterpretedAs">
        /// Returns <see cref="CliCommandMode.IndexOnly"/> if the string was interpreted as number in a fixed list. <para/>
        /// Returns <see cref="CliCommandMode.TypeOnly"/> if the string was interpreted literally. <para/>
        /// </param>
        /// <returns></returns>
        private string? FindActualCommand(string RawCommand, out CliCommandMode InterpretedAs)
        {
            switch (this.CommandMode)
            {
                case CliCommandMode.TypeOnly:
                    InterpretedAs = CliCommandMode.TypeOnly;
                    return RawCommand;
                case CliCommandMode.IndexAndType:
                    {
                        int num = -1;
                        if (Int32.TryParse(RawCommand.Trim(), out num) && this._indexedCommands.ContainsKey(num))
                        {
                            this._cm.WriteLine(string.Format("Running <{0}> ...", this._indexedCommands[num].DisplayName));
                            this._cm.WriteLine();
                            InterpretedAs = CliCommandMode.IndexOnly;
                            return this._indexedCommands[num].Name;
                        }
                        else
                        {
                            InterpretedAs = CliCommandMode.TypeOnly;
                            return RawCommand;
                        }
                    }
                case CliCommandMode.IndexOnly:
                    {
                        int num = -1;
                        if (Int32.TryParse(RawCommand.Trim(), out num) && this._indexedCommands.ContainsKey(num))
                        {
                            this._cm.WriteLine(string.Format("Running <{0}> ...", this._indexedCommands[num].DisplayName));
                            this._cm.WriteLine();
                            InterpretedAs = CliCommandMode.IndexOnly;
                            return this._indexedCommands[num].Name;
                        }
                        else
                        {
                            InterpretedAs = CliCommandMode.TypeOnly;
                            return null;
                        }
                    }
                default:
                    InterpretedAs = CliCommandMode.TypeOnly;
                    return null;
            }
        }

        private void ResetCommandStatus()
        {
            this._currentCommandLine = default;
            this._currentCommandPromptResult = default;
            this._currentCommandPromptFlag = default;
        }

        private string? _currentCommandLine = null;
        private string? _currentCommandPromptResult = null;
        private SecureString? _currentCommandPromptPasswordResult = null;
        private readonly object _currentCommandMonitor = new object();
        private CommandPromptResult _currentCommandPromptFlag = CommandPromptResult.NoneYet;
        private readonly AutoResetEvent SleepHandle = new(false);

        private enum CommandPromptResult : byte
        {
            NoneYet = 0,
            OK = 1,
            Canceled = 2
        }
        private void HandleCurrentCommand()
        {
            if (!String.IsNullOrWhiteSpace(this._currentCommandLine))
            {
                this._currentCommandLine = this._currentCommandLine!.Trim();

                IList<string> splitList = CliCommand.SpecialSplit(this._currentCommandLine);

                string[] args; String command;
                if (splitList.Count > 1)
                {
                    command = splitList[0];
                    splitList.RemoveAt(0);
                    args = new string[splitList.Count];
                    splitList.CopyTo(args, 0);
                }
                else
                {
                    command = this._currentCommandLine;
                    args = new string[0];
                }
                splitList.Clear();
                if (this._CommandToHandler.ContainsKey(command))
                {
                    this._currentCommandHandler = this._CommandToHandler[command];
                    //executing!
                    try
                    {
                        this._currentCommandHandler.OnCommand(command, args, this);
                    }
                    catch (Exception e)
                    {
                        this._cm.WriteLine(string.Format("Command terminated with error!\r\n {0}", e));
                    }
                    //executed

                }
                else
                {
                    this.WriteString("Unknown command: {0}", command);
                }
            }
            else
            {
                this.WriteString("Unknown command.");
            }

            this.ResetCommandStatus();
            this.NewLineIfNeeded();
            this.PromptUser();
        }
        private void PrintNumberedCommandList()
        {
            IList<CommandDescriptor> comms =
                (from handl in _AllHandlers
                 from comm in handl.ActiveCommands
                 where handl.ShouldIncludeInIndexedList(comm.Name)
                 select comm).ToList();
                         

            this._indexedCommands.Clear();

            this.PrintList(comms, "Please type one of the following numbers, and hit Enter.",
                (c) => c.DisplayName,
                true,
                (i, v) => this._indexedCommands[i] = v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="elements"></param>
        /// <param name="caption"></param>
        /// <param name="ToString"></param>
        /// <param name="OnAssociated"></param>
        private void PrintList<V>(IList<V> elements, string? caption, Func<V, string> ToString, bool numbered = false, Action<int, V>? OnAssociated = null)
        {
            if (elements.Count == 0)
            {
                return;
            }

            if (OnAssociated == null)
            {
                OnAssociated = (_, __) => { };
            }

            const int SPACE_BETWEEN_COLUMNS = 5;
            const int COLUMN_COUNT = 3;
            const int ROW_PADDING = 10;
            const string NAME_FORMAT = "[{0:D#}] {1}"; //the # symbol needs to be replaced with a single digit number representing the number of digits to print

            if (caption != null)
                this._cm.WriteLine(caption);

            string OurFormat; int IndexSize;
            if (numbered)
            {
                byte digits = (byte)Math.Ceiling(Math.Log10(elements.Count + 1)); //this gives us the digits needed to represent our indices
                OurFormat = NAME_FORMAT.Replace('#', (char)('0' + digits)); // the index format updated with the actual number of digits to use in this occurrence
                IndexSize = OurFormat.Length - 9 /*chars occupied by the format placeholders*/ + digits;
            }
            else
            {
                OurFormat = "{1}";
                IndexSize = 0;
            }

            int AvailableSpace = this._cm.Width - ROW_PADDING - (COLUMN_COUNT - 1) * SPACE_BETWEEN_COLUMNS; //available space in a row
            int ColumnW = AvailableSpace / COLUMN_COUNT;

            int currentConsoleLoc = 0;
            int currentColumn = 0;

            for (int i = 0; i < elements.Count; i++)
            {

                do
                {
                    int expectedLocation = (ColumnW + SPACE_BETWEEN_COLUMNS) * currentColumn;
                    int difference = expectedLocation - currentConsoleLoc;
                    if (difference > 0)
                    {
                        this.WriteString(new string(' ', difference));
                        currentConsoleLoc += difference;
                    }
                    else if (difference < 0)
                    {
                        currentColumn++;
                    }
                    else
                    {
                        break;
                    }
                } while (true);

                string s = String.Format(OurFormat, i, ToString(elements[i]));
                OnAssociated(i, elements[i]);

                if (currentColumn == COLUMN_COUNT ||   //we are beyond the last column
                                                       //    OR
                         (currentColumn != 0 && currentColumn == COLUMN_COUNT - 1 && currentConsoleLoc + SPACE_BETWEEN_COLUMNS + s.Length > AvailableSpace) //this is not the first column and there is no space for this cmd
                   )
                {
                    _cm.WriteLine();
                    currentColumn = 0;
                    currentConsoleLoc = 0;
                }

                this.WriteString(s);
                currentConsoleLoc += s.Length;
                currentColumn++;
            }
            this.NewLineIfNeeded();
        }

        #region Standard Commands


        public bool IsActive(string command)
        {
            var handl = _CommandToHandler[command];
            if (handl == this) return true;
            return handl.IsActive(command);
        }


        public void Exit()
        {
            this.ShouldWork = false;
            this.WriteString("Closing....");
            SleepHandle.Set();
        }

        public static bool IsManagedNonCharacter(CliKey converted)
        {
            switch (converted)
            {
                case Linea.CliKey.Spacebar:
                case Linea.CliKey.D0:
                case Linea.CliKey.D1:
                case Linea.CliKey.D2:
                case Linea.CliKey.D3:
                case Linea.CliKey.D4:
                case Linea.CliKey.D5:
                case Linea.CliKey.D6:
                case Linea.CliKey.D7:
                case Linea.CliKey.D8:
                case Linea.CliKey.D9:
                case Linea.CliKey.A:
                case Linea.CliKey.B:
                case Linea.CliKey.C:
                case Linea.CliKey.D:
                case Linea.CliKey.E:
                case Linea.CliKey.F:
                case Linea.CliKey.G:
                case Linea.CliKey.H:
                case Linea.CliKey.I:
                case Linea.CliKey.J:
                case Linea.CliKey.K:
                case Linea.CliKey.L:
                case Linea.CliKey.M:
                case Linea.CliKey.N:
                case Linea.CliKey.O:
                case Linea.CliKey.P:
                case Linea.CliKey.Q:
                case Linea.CliKey.R:
                case Linea.CliKey.S:
                case Linea.CliKey.T:
                case Linea.CliKey.U:
                case Linea.CliKey.V:
                case Linea.CliKey.W:
                case Linea.CliKey.X:
                case Linea.CliKey.Y:
                case Linea.CliKey.Z:
                    return false;
                case Linea.CliKey.NumPad0:
                case Linea.CliKey.NumPad1:
                case Linea.CliKey.NumPad2:
                case Linea.CliKey.NumPad3:
                case Linea.CliKey.NumPad4:
                case Linea.CliKey.NumPad5:
                case Linea.CliKey.NumPad6:
                case Linea.CliKey.NumPad7:
                case Linea.CliKey.NumPad8:
                case Linea.CliKey.NumPad9:
                case Linea.CliKey.Multiply:
                case Linea.CliKey.Add:
                case Linea.CliKey.Separator:
                case Linea.CliKey.Subtract:
                case Linea.CliKey.Decimal:
                case Linea.CliKey.Divide:
                    return false;
                default:
                    return true;
            }
        }

        #endregion
    }
}
