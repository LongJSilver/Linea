using Linea.Command;
using Linea.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Linea.Interface
{
    public delegate void CommandHandler(String command, params string[] args);
    public enum CliCommandMode
    {
        TypeOnly = 0,
        IndexAndType,
        IndexOnly
    }

    public partial class Cli : ICliFunctions
    {
        private enum InputMode
        {
            /// <summary>
            /// La normale modalità di inserimento comandi.
            /// </summary>
            UserCommand,
            /// <summary>
            /// Modalità speciale di inserimento input.
            /// Viene utilizzata quando l'utente deve fornire un input ad un comando in esecuzione.
            /// </summary>
            UserCommandInput,
            /// <summary>
            /// Modalità speciale di inserimento input.
            /// Viene utilizzata quando l'utente deve fornire una password in input ad un comando in esecuzione.
            /// </summary>
            UserCommandInputPassword,
            /// <summary>
            /// L'input dell'utente è bloccato, soltanto il sistema può scrivere nella console.
            /// </summary>
            SystemText
        }
        public char CommandSeparator { get; set; } = ' ';
        private const int MAX_WAIT = 100;

        private IHistoryStorageService? _historyStorage;
        private IConsole _console;
        private string[]? _completions;
        private int _completionStart;
        private int _completionsIndex;
        private bool IsInAutoCompleteMode => this._completions != null;
        private Task? _readLoop;

        private ListIndexer<(CliModifier, CliKey), CliKeyEventHandler> _ExternalKeyboardEventHandlers;
        private Dictionary<(CliModifier, CliKey), Action> _InternalKeyboardEventHandlers;

        private InputMode _mode = InputMode.UserCommand;
        private string _prompt = "";

        private ConsoleMechanics _cm;
        private SecureString? _password;
        private bool _acceptingInput = true;
        private List<string> _history;
        private int _historyIndex;
        private readonly Dictionary<int, CommandDescriptor> _indexedCommands = new Dictionary<int, CommandDescriptor>();
        private uint PromptLength => (uint)(_promptInfo.ConsoleW * (_promptInfo.endRow - _promptInfo.startRow)
                                           + (_promptInfo.endColumn - _promptInfo.startColumn));
        private int LogicalCursorPos => (int)(_cm.LogicalCursorPosition - PromptLength);
        private string CurrentText
        {
            get
            {
                string current = _cm.CurrentRowText;
                return current.Substring((int)PromptLength);
            }
        }

        /// <summary>
        /// <see cref="True"/> se siamo in modalità <see cref="InputMode.UserCommand"/> e <see cref="_acceptingInput"/> è <see cref="True"/>
        /// </summary>
        private bool ShouldWritePrompt => this._mode == InputMode.UserCommand && this._acceptingInput;
        private bool UserWroteSomething => LogicalCursorPos > 0;
        private bool CanOverrideCurrentLine => !this.UserWroteSomething && this._mode == InputMode.UserCommand;

        private bool _EK123J4242HB523 = false;
        private CliCommandMode _ASDE4FAEQ3RAQ3QG = default;
        private CliCommandMode CommandMode
        {
            get { return this._ASDE4FAEQ3RAQ3QG; }
            set
            {
                CliCommandMode OldCommandMode = this._ASDE4FAEQ3RAQ3QG;
                this._ASDE4FAEQ3RAQ3QG = value;

                switch (OldCommandMode)
                {
                    case CliCommandMode.TypeOnly:
                        if (this.CommandMode != CliCommandMode.TypeOnly)
                        {
                            if (this.CanOverrideCurrentLine)
                            {
                                this.ClearLine();
                                this.RefreshPrompt();
                            }
                        }
                        break;
                    case CliCommandMode.IndexAndType:
                    case CliCommandMode.IndexOnly:

                        if (this.CommandMode == CliCommandMode.TypeOnly)
                        {
                            if (this.CanOverrideCurrentLine)
                            {
                                this.ClearLine();
                                this.RefreshPrompt();
                            }
                        }
                        break;
                    default:
                        break;
                }


            }
        }
        public string PromptString
        {
            get { return this._prompt; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    value = String.Empty;
                }

                if (!String.Equals(value, this._prompt))
                {
                    this._prompt = value;
                    if (this.ShouldWritePrompt)
                    {
                        this.RefreshPrompt();
                    }

                }
            }
        }


        public bool ShouldPersistHistory
        {
            get
            {
                return this._EK123J4242HB523 && _historyStorage != null;
            }
            set
            {
                if (value && _historyStorage == null) throw new InvalidOperationException("There is no History storage service defined");
                if (value && !this._EK123J4242HB523)
                {
                    this.ReadHistoryFromSecureStorage();
                }

                if (value && this._historyStorage != null)
                {
                    _historyStorage.StoreList(this._history);
                }
                this._EK123J4242HB523 = value;
            }
        }


        public Cli(IConsole console) : this(console, null)
        {

        }
        public Cli(IConsole console, IHistoryStorageService? historyService)
        {
            _cm = new ConsoleMechanics(Math.Max(console.BufferWidth, 1), Math.Max(console.BufferHeight, 2));
            _cm.ContentChanged += this._cm_ContentChanged;
            _cm.ConsoleRowCountChanged += _cm_ConsoleRowCountChanged;
            _cm.CursorLocationChanged += this._cm_CursorLocationChanged;
            console.Text = _cm.GenerateList();
            console.BufferSizeChanged += this.Console_BufferSizeChanged;

            this._historyStorage = historyService;
            this._console = console ?? throw new ArgumentNullException(nameof(console));
            this._history = new List<string>();
            this._historyIndex = 0;
            this._ExternalKeyboardEventHandlers = new ListIndexer<(CliModifier, CliKey), CliKeyEventHandler>();
            this._InternalKeyboardEventHandlers = new Dictionary<(CliModifier, CliKey), Action>();

            //this.RegisterHandler(this);
            this.RegisterEvents();
            this.PromptString = "Cli >";
        }

        private void _cm_ConsoleRowCountChanged(object sender, int rowCount)
        {
            _console.SetRowCount(rowCount);
        }

        private void _cm_CursorLocationChanged(object sender, int row, int column)
        {
            _console.SetCursorPosition(row, column);
        }

        private void Console_BufferSizeChanged(IConsole sender, int width, int height)
        {
            _cm.Width = width;
            _cm.Height = height;
            _cm.Update();
        }

        private void _cm_ContentChanged(object sender, ConsoleContentChangeArgs e)
        {
            if (e.AllChanged)
            {
                _console.Clear();
            }
            for (int i = e.FirstRow; i <= e.LastRow; i++)
            {
                _console.SetRowText(i, _cm.GetRowText(i));
            }

            _console.SetCursorPosition(_cm.CursorRow, _cm.CursorColumn);
        }

        public void Run()
        {
            BeginRun().Wait();
        }
        public async Task BeginRun()
        {
            WritePrompt();
            _PromptNeedsRefresh = false;
            this.ShouldWork = true;
            await (this._readLoop = Task.Run(this.ReadLoop));
        }


        private void ReadLoop()
        {
            int UnavailableTimes = 0;
            int waitFor = MAX_WAIT;
            while (this.ShouldWork)
            {
                if (this._PromptNeedsRefresh)
                {
                    this.InternalRefreshPrompt();
                }

                if (this._console.Available)
                {
                    waitFor = 5;
                    UnavailableTimes = 0;
                    CliKeyEvent c = this._console.Read();
                    if (this._acceptingInput)
                    {
                        this.Handle(c);
                    }
                }
                else
                {
                    if (waitFor < MAX_WAIT)
                    {
                        UnavailableTimes++;
                        if (UnavailableTimes % 30 == 0)
                        {
                            waitFor = Math.Min(MAX_WAIT, waitFor + 1);
                        }
                    }
                    SleepHandle.Reset();
                    SleepHandle.WaitOne(waitFor);
                }
            }
        }

        private bool ShouldWork;
        private void RegisterEvents()
        {
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.Backspace)] = this.Backspace;
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.Delete)] = this.Delete;
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.Escape)] = this.EscapeAction;

            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.End)] = this.MoveCursorEnd;
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.Home)] = this.MoveCursorHome;

            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.LeftArrow)] = this.MoveCursorLeft;
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.RightArrow)] = this.MoveCursorRight;
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.UpArrow)] = this.PrevHistory;
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.DownArrow)] = this.NextHistory;
            this._InternalKeyboardEventHandlers[(CliModifier.None, CliKey.Tab)] = () =>
            {

                if (this.IsInAutoCompleteMode)
                {
                    this.NextAutoComplete();
                }
                else
                {
                    if (!this._cm.IsEndOfLine())
                    {
                        return;
                    }

                    this.GetSuggestions();


                    if (this._completions != null)
                    {
                        this.StartAutoComplete();
                    }
                }


            };
            this._InternalKeyboardEventHandlers[(CliModifier.BothShift, CliKey.Tab)] = () =>
            {
                if (this.IsInAutoCompleteMode)
                {
                    this.PreviousAutoComplete();
                }
            };

        }
        private void GetSuggestions()
        {
            this._completionStart = -1;
            string text = CurrentText;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[text.Length - 1 - i] == this.CommandSeparator)
                {
                    this._completionStart = text.Length - 1 - i;
                    break;
                }
            }

            if (this._completionStart == -1)
            {
                this._completionStart = 0;
                string substr = text.Substring(this._completionStart, text.Length - this._completionStart);
                //siamo all'inizio

                this._completions = (from cd
                                    in this._CommandToHandler
                                     where cd.Key.Name.StartsWith(substr, StringComparison.InvariantCultureIgnoreCase)
                                        && cd.Value.IsActive(cd.Key.Name)
                                     select cd.Key.Name).ToArray<string>();

                this._completions = this._completions?.Length == 0 ? null : this._completions;
            }
            else
            {
                //c'è già qualcosa scritto
            }
        }

        private bool CanRegisterEventsFor(CliKey k)
        {
            switch (k)
            {
                case CliKey.LeftWindows:
                case CliKey.RightWindows:
                case CliKey.Tab:
                case CliKey.Escape:
                case CliKey.Unknown:
                    return false;
                default: return true;
            }
        }

        public void RegisterEvent(CliModifier modifiers, CliKey key, CliKeyEventHandler h)
        {
            if (this.CanRegisterEventsFor(key))
            {
                (CliModifier modifiers, CliKey key) dictKey = (modifiers, key);
                this._ExternalKeyboardEventHandlers.Add(dictKey, h);
            }
        }

        public void RegisterEvent(CliKey key, CliKeyEventHandler h)
        {
            this.RegisterEvent(CliModifier.None, key, h);
        }

        private void Handle(CliKeyEvent keyInfo)
        {
            (CliModifier, CliKey) CollectionKey = (keyInfo.Modifiers, keyInfo.Key);


            // If in auto complete mode and Tab wasn't pressed
            if (this.IsInAutoCompleteMode && keyInfo.Key != CliKey.Tab)
            {
                this.ResetAutoComplete();
            }

            if (this._ExternalKeyboardEventHandlers.TryGetValue(CollectionKey, out IList<CliKeyEventHandler> actions))
            {
                foreach (CliKeyEventHandler item in actions)
                {
                    item(keyInfo);
                }
            }

            if (keyInfo.Handled)
            {
                return;
            }

            if (keyInfo.Key == CliKey.Enter && keyInfo.SimpleModifiers == CliSimpleModifier.None)
            {
                //ENTER!
                if (this._mode == InputMode.UserCommand)
                {
                    String Command = CurrentText;
                    this._cm.WriteLine();
                    if (string.IsNullOrEmpty(Command))
                    {
                        this.PromptUser();
                    }
                    else
                    {
                        this._mode = InputMode.SystemText;
                        this._acceptingInput = false;

                        this._currentCommandLine = this.FindActualCommand(Command, out CliCommandMode InterpretedAs);
                        if (InterpretedAs == CliCommandMode.TypeOnly)
                        {
                            this.AddToHistory(Command);
                        }

                        Task.Run(HandleCurrentCommand);

                    }
                }
                else if (this._mode == InputMode.UserCommandInput)
                {
                    this._currentCommandPromptResult = CurrentText;
                    this.TerminatePromptCommandInput(CommandPromptResult.OK);
                }
                else if (this._mode == InputMode.UserCommandInputPassword)
                {
                    this._password!.MakeReadOnly();
                    this._currentCommandPromptPasswordResult = this._password;
                    this._password = null;
                    this.TerminatePromptCommandInput(CommandPromptResult.OK);
                }
                else if (this._mode == InputMode.SystemText)
                {
                    _cm.WriteLine();
                }
            }
            else
            {
                if (this._InternalKeyboardEventHandlers.TryGetValue(CollectionKey, out Action action))
                {
                    action();
                }
                else
                {
                    if (this._mode == InputMode.UserCommandInputPassword)
                    {
                        if (keyInfo.IsText)
                            foreach (var c in keyInfo.Text!)
                                this._password!.AppendChar(c);
                        else if (keyInfo.KeyChar != '\0')
                            this._password!.AppendChar(keyInfo.KeyChar);
                    }
                    else
                    {
                        if (keyInfo.IsText)
                            this.WriteString(keyInfo.Text!);
                        else if (keyInfo.KeyChar != '\0')
                            this.WriteChar(keyInfo.KeyChar);
                    }

                }



            }
        }

    }
}
