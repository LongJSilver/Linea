﻿using Linea.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;

namespace Linea
{
    public delegate void CliKeyEventHandler(CliKeyEvent ke);
    public delegate bool CliCommandInputDelegate(string prompt, out string UserString);
    public delegate void CliCommandWriteDelegate(string str, params string[] args);
    public delegate void ConsoleBufferSizeDelegate(IConsole sender, int width, int height);
    public delegate void ConsoleCursorLocationDelegate(IConsole sender, int row, int column);

    public interface IConsole
    {
        /// <summary>
        /// 
        /// </summary>
        event ConsoleBufferSizeDelegate BufferSizeChanged;

        /// <summary>
        /// Event that is raised when the cursor location changes due to external sources.<para/>
        /// must not be called when the cursor location is changed by the <see cref="SetCursorPosition(int, int)"/> method .<para/>
        /// </summary>
        event ConsoleCursorLocationDelegate CursorLocationChanged;

        int BufferWidth { get; }
        int BufferHeight { get; }
        void SetCursorPosition(int row, int column);
        void SetRowText(int Row, string value);
        void SetRowCount(int RowCount);

        IList<string> Text { set; }

        void Clear();
        CliKeyEvent Read();
        bool Available { get; }
    }

    public interface ICliFunctions
    {
        bool PromptForInput(string prompt, IEnumerable<string> choices, [MaybeNullWhen(false), NotNullWhen(true)] out string? result);
        bool PromptForInput<V>(string prompt, IEnumerable<V> choices, Func<V, string> ToString, [MaybeNullWhen(false), NotNullWhen(true)] out V result);
        bool PromptForInput(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out string? UserString);
        bool PromptForPasswordUnsafely(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out string? UserString);
        bool PromptForPasswordSafely(string prompt, [MaybeNullWhen(false), NotNullWhen(true)] out SecureString? UserString);
        void Write(string str, params object[] args);
        void WriteLine(string str, params object[] args);
        void PrintList(IEnumerable<string> enumerable);
        void PrintList(string caption, IEnumerable<string> enumerable);
        bool PromptForConfirmation(string v, bool defaultToYes = true);
        void Exit();
        TextWriter Writer { get; }
    }

    public interface ICommandHandler
    {
        event Action<ICommandHandler> HandlerChanged;
        void OnCommand(string command, string[] args, ICliFunctions cli);
        IEnumerable<CommandDescriptor> Commands { get; }
        IEnumerable<CommandDescriptor> ActiveCommands { get; }
        bool IsActive(string command);
        /// <summary>
        /// The subset of this handler's commands that can be presented to the user as an indexed list.<para/>
        /// When the console is in Indexed Mode, the IndexableCommands are presented 
        /// in a list associated to a number or letter that the user can use to select 
        /// a command instead of typing the full name or alias.<para/>
        /// Example:<para/>
        /// [1] Command A<para/>
        /// [2] Command B<para/>
        /// [3] Command C<para/>
        /// 
        /// </summary>        
        bool ShouldIncludeInIndexedList(string command);
    }

    #region "ENUMS" 
    //
    // Summary:
    //     Specifies the standard keys on a console.
    public enum CliKey
    {
        Unknown = 0,
        //
        // Summary:
        //     The BACKSPACE key.
        Backspace = 8,
        //
        // Summary:
        //     The TAB key.
        Tab = 9,
        //
        // Summary:
        //     The CLEAR key.
        Clear = 12,
        //
        // Summary:
        //     The ENTER key.
        Enter = 13,
        //
        // Summary:
        //     The PAUSE key.
        Pause = 19,
        //
        // Summary:
        //     The ESC (ESCAPE) key.
        Escape = 27,
        //
        // Summary:
        //     The SPACEBAR key.
        Spacebar = 32,
        //
        // Summary:
        //     The PAGE UP key.
        PageUp = 33,
        //
        // Summary:
        //     The PAGE DOWN key.
        PageDown = 34,
        //
        // Summary:
        //     The END key.
        End = 35,
        //
        // Summary:
        //     The HOME key.
        Home = 36,
        //
        // Summary:
        //     The LEFT ARROW key.
        LeftArrow = 37,
        //
        // Summary:
        //     The UP ARROW key.
        UpArrow = 38,
        //
        // Summary:
        //     The RIGHT ARROW key.
        RightArrow = 39,
        //
        // Summary:
        //     The DOWN ARROW key.
        DownArrow = 40,
        //
        // Summary:
        //     The SELECT key.
        Select = 41,
        //
        // Summary:
        //     The PRINT key.
        Print = 42,
        //
        // Summary:
        //     The EXECUTE key.
        Execute = 43,
        //
        // Summary:
        //     The PRINT SCREEN key.
        PrintScreen = 44,
        //
        // Summary:
        //     The INS (INSERT) key.
        Insert = 45,
        //
        // Summary:
        //     The DEL (DELETE) key.
        Delete = 46,
        //
        // Summary:
        //     The HELP key.
        Help = 47,
        //
        // Summary:
        //     The 0 key.
        D0 = 48,
        //
        // Summary:
        //     The 1 key.
        D1 = 49,
        //
        // Summary:
        //     The 2 key.
        D2 = 50,
        //
        // Summary:
        //     The 3 key.
        D3 = 51,
        //
        // Summary:
        //     The 4 key.
        D4 = 52,
        //
        // Summary:
        //     The 5 key.
        D5 = 53,
        //
        // Summary:
        //     The 6 key.
        D6 = 54,
        //
        // Summary:
        //     The 7 key.
        D7 = 55,
        //
        // Summary:
        //     The 8 key.
        D8 = 56,
        //
        // Summary:
        //     The 9 key.
        D9 = 57,
        //
        // Summary:
        //     The A key.
        A = 65,
        //
        // Summary:
        //     The B key.
        B = 66,
        //
        // Summary:
        //     The C key.
        C = 67,
        //
        // Summary:
        //     The D key.
        D = 68,
        //
        // Summary:
        //     The E key.
        E = 69,
        //
        // Summary:
        //     The F key.
        F = 70,
        //
        // Summary:
        //     The G key.
        G = 71,
        //
        // Summary:
        //     The H key.
        H = 72,
        //
        // Summary:
        //     The I key.
        I = 73,
        //
        // Summary:
        //     The J key.
        J = 74,
        //
        // Summary:
        //     The K key.
        K = 75,
        //
        // Summary:
        //     The L key.
        L = 76,
        //
        // Summary:
        //     The M key.
        M = 77,
        //
        // Summary:
        //     The N key.
        N = 78,
        //
        // Summary:
        //     The O key.
        O = 79,
        //
        // Summary:
        //     The P key.
        P = 80,
        //
        // Summary:
        //     The Q key.
        Q = 81,
        //
        // Summary:
        //     The R key.
        R = 82,
        //
        // Summary:
        //     The S key.
        S = 83,
        //
        // Summary:
        //     The T key.
        T = 84,
        //
        // Summary:
        //     The U key.
        U = 85,
        //
        // Summary:
        //     The V key.
        V = 86,
        //
        // Summary:
        //     The W key.
        W = 87,
        //
        // Summary:
        //     The X key.
        X = 88,
        //
        // Summary:
        //     The Y key.
        Y = 89,
        //
        // Summary:
        //     The Z key.
        Z = 90,
        //
        // Summary:
        //     The left Windows logo key (Microsoft Natural Keyboard).
        LeftWindows = 91,
        //
        // Summary:
        //     The right Windows logo key (Microsoft Natural Keyboard).
        RightWindows = 92,
        //
        // Summary:
        //     The Application key (Microsoft Natural Keyboard).
        Applications = 93,
        //
        // Summary:
        //     The Computer Sleep key.
        Sleep = 95,
        //
        // Summary:
        //     The 0 key on the numeric keypad.
        NumPad0 = 96,
        //
        // Summary:
        //     The 1 key on the numeric keypad.
        NumPad1 = 97,
        //
        // Summary:
        //     The 2 key on the numeric keypad.
        NumPad2 = 98,
        //
        // Summary:
        //     The 3 key on the numeric keypad.
        NumPad3 = 99,
        //
        // Summary:
        //     The 4 key on the numeric keypad.
        NumPad4 = 100,
        //
        // Summary:
        //     The 5 key on the numeric keypad.
        NumPad5 = 101,
        //
        // Summary:
        //     The 6 key on the numeric keypad.
        NumPad6 = 102,
        //
        // Summary:
        //     The 7 key on the numeric keypad.
        NumPad7 = 103,
        //
        // Summary:
        //     The 8 key on the numeric keypad.
        NumPad8 = 104,
        //
        // Summary:
        //     The 9 key on the numeric keypad.
        NumPad9 = 105,
        //
        // Summary:
        //     The Multiply key (the multiplication key on the numeric keypad).
        Multiply = 106,
        //
        // Summary:
        //     The Add key (the addition key on the numeric keypad).
        Add = 107,
        //
        // Summary:
        //     The Separator key.
        Separator = 108,
        //
        // Summary:
        //     The Subtract key (the subtraction key on the numeric keypad).
        Subtract = 109,
        //
        // Summary:
        //     The Decimal key (the decimal key on the numeric keypad).
        Decimal = 110,
        //
        // Summary:
        //     The Divide key (the division key on the numeric keypad).
        Divide = 111,
        //
        // Summary:
        //     The F1 key.
        F1 = 112,
        //
        // Summary:
        //     The F2 key.
        F2 = 113,
        //
        // Summary:
        //     The F3 key.
        F3 = 114,
        //
        // Summary:
        //     The F4 key.
        F4 = 115,
        //
        // Summary:
        //     The F5 key.
        F5 = 116,
        //
        // Summary:
        //     The F6 key.
        F6 = 117,
        //
        // Summary:
        //     The F7 key.
        F7 = 118,
        //
        // Summary:
        //     The F8 key.
        F8 = 119,
        //
        // Summary:
        //     The F9 key.
        F9 = 120,
        //
        // Summary:
        //     The F10 key.
        F10 = 121,
        //
        // Summary:
        //     The F11 key.
        F11 = 122,
        //
        // Summary:
        //     The F12 key.
        F12 = 123,
        //
        // Summary:
        //     The F13 key.
        F13 = 124,
        //
        // Summary:
        //     The F14 key.
        F14 = 125,
        //
        // Summary:
        //     The F15 key.
        F15 = 126,
        //
        // Summary:
        //     The F16 key.
        F16 = 127,
        //
        // Summary:
        //     The F17 key.
        F17 = 128,
        //
        // Summary:
        //     The F18 key.
        F18 = 129,
        //
        // Summary:
        //     The F19 key.
        F19 = 130,
        //
        // Summary:
        //     The F20 key.
        F20 = 131,
        //
        // Summary:
        //     The F21 key.
        F21 = 132,
        //
        // Summary:
        //     The F22 key.
        F22 = 133,
        //
        // Summary:
        //     The F23 key.
        F23 = 134,
        //
        // Summary:
        //     The F24 key.
        F24 = 135,

        NumLock = 144,
        //
        // Summary:
        //     The Browser Back key (Windows 2000 or later).
        BrowserBack = 166,
        //
        // Summary:
        //     The Browser Forward key (Windows 2000 or later).
        BrowserForward = 167,
        //
        // Summary:
        //     The Browser Refresh key (Windows 2000 or later).
        BrowserRefresh = 168,
        //
        // Summary:
        //     The Browser Stop key (Windows 2000 or later).
        BrowserStop = 169,
        //
        // Summary:
        //     The Browser Search key (Windows 2000 or later).
        BrowserSearch = 170,
        //
        // Summary:
        //     The Browser Favorites key (Windows 2000 or later).
        BrowserFavorites = 171,
        //
        // Summary:
        //     The Browser Home key (Windows 2000 or later).
        BrowserHome = 172,
        //
        // Summary:
        //     The Volume Mute key (Microsoft Natural Keyboard, Windows 2000 or later).
        VolumeMute = 173,
        //
        // Summary:
        //     The Volume Down key (Microsoft Natural Keyboard, Windows 2000 or later).
        VolumeDown = 174,
        //
        // Summary:
        //     The Volume Up key (Microsoft Natural Keyboard, Windows 2000 or later).
        VolumeUp = 175,
        //
        // Summary:
        //     The Media Next Track key (Windows 2000 or later).
        MediaNext = 176,
        //
        // Summary:
        //     The Media Previous Track key (Windows 2000 or later).
        MediaPrevious = 177,
        //
        // Summary:
        //     The Media Stop key (Windows 2000 or later).
        MediaStop = 178,
        //
        // Summary:
        //     The Media Play/Pause key (Windows 2000 or later).
        MediaPlay = 179,
        //
        // Summary:
        //     The Start Mail key (Microsoft Natural Keyboard, Windows 2000 or later).
        LaunchMail = 180,
        //
        // Summary:
        //     The Select Media key (Microsoft Natural Keyboard, Windows 2000 or later).
        LaunchMediaSelect = 181,
        //
        // Summary:
        //     The Start Application 1 key (Microsoft Natural Keyboard, Windows 2000 or later).
        LaunchApp1 = 182,
        //
        // Summary:
        //     The Start Application 2 key (Microsoft Natural Keyboard, Windows 2000 or later).
        LaunchApp2 = 183,
        //
        // Summary:
        //     The OEM 1 key (OEM specific).
        Oem1 = 186,
        //
        // Summary:
        //     The OEM Plus key on any country/region keyboard (Windows 2000 or later).
        OemPlus = 187,
        //
        // Summary:
        //     The OEM Comma key on any country/region keyboard (Windows 2000 or later).
        OemComma = 188,
        //
        // Summary:
        //     The OEM Minus key on any country/region keyboard (Windows 2000 or later).
        OemMinus = 189,
        //
        // Summary:
        //     The OEM Period key on any country/region keyboard (Windows 2000 or later).
        OemPeriod = 190,
        //
        // Summary:
        //     The OEM 2 key (OEM specific).
        Oem2 = 191,
        //
        // Summary:
        //     The OEM 3 key (OEM specific).
        Oem3 = 192,
        //
        // Summary:
        //     The OEM 4 key (OEM specific).
        Oem4 = 219,
        //
        // Summary:
        //     The OEM 5 (OEM specific).
        Oem5 = 220,
        //
        // Summary:
        //     The OEM 6 key (OEM specific).
        Oem6 = 221,
        //
        // Summary:
        //     The OEM 7 key (OEM specific).
        Oem7 = 222,
        //
        // Summary:
        //     The OEM 8 key (OEM specific).
        Oem8 = 223,
        //
        // Summary:
        //     The OEM 102 key (OEM specific).
        Oem102 = 226,
        //
        // Summary:
        //     The IME PROCESS key.
        Process = 229,
        //
        // Summary:
        //     The PACKET key (used to pass Unicode characters with keystrokes).
        Packet = 231,

        //
        // Summary:
        //     The ATTN key.
        Attention = 246,
        //
        // Summary:
        //     The CRSEL (CURSOR SELECT) key.
        CrSel = 247,
        //
        // Summary:
        //     The EXSEL (EXTEND SELECTION) key.
        ExSel = 248,
        //
        // Summary:
        //     The ERASE EOF key.
        EraseEndOfFile = 249,
        //
        // Summary:
        //     The PLAY key.
        Play = 250,
        //
        // Summary:
        //     The ZOOM key.
        Zoom = 251,
        //
        // Summary:
        //     A constant reserved for future use.
        NoName = 252,
        //
        // Summary:
        //     The PA1 key.
        Pa1 = 253,
        //
        // Summary:
        //     The CLEAR key (OEM specific).
        OemClear = 254

    }

    [Flags]
    public enum CliModifier
    {
        None = 0,
        LCtrl = 1,
        RCtrl = 2,
        LAlt = 4,
        RAlt = 8,
        LShift = 16,
        RShift = 32,
        BothCtrl = LCtrl | RCtrl,
        BothAlt = LAlt | RAlt,
        BothShift = LShift | RShift
    }

    [Flags]
    public enum CliSimpleModifier
    {
        None = 0,
        Ctrl = 1,
        Alt = 2,
        Shift = 4
    }
    public static class CliEvents
    {
        public static bool ContainsAll(this CliModifier who, CliModifier what)
        {
            return (who & what) == what;
        }

        public static bool ContainsSomeOf(this CliModifier who, CliModifier what)
        {
            return (who & what) != 0;
        }

        public static bool ContainsNoneOf(this CliModifier who, CliModifier what)
        {
            return (who & what) == 0;
        }

        public static bool ContainsAll(this CliSimpleModifier who, CliSimpleModifier what)
        {
            return (who & what) == what;
        }

        public static bool ContainsSomeOf(this CliSimpleModifier who, CliSimpleModifier what)
        {
            return (who & what) != 0;
        }

        public static bool ContainsNoneOf(this CliSimpleModifier who, CliSimpleModifier what)
        {
            return (who & what) == 0;
        }
    }
    public class CliKeyEvent
    {
        //
        // Summary:
        //     Initializes a new instance of the CliKeyEvent Class using  the specified
        //     character, console key, and modifier keys.
        //
        // Parameters:
        //   keyChar:
        //     The Unicode character that corresponds to the key parameter.
        //
        //   key:
        //     The console key that corresponds to the keyChar parameter.
        //
        //   shift:
        //     true to indicate that a SHIFT key was pressed; otherwise, false.
        //
        //   alt:
        //     true to indicate that an ALT key was pressed; otherwise, false.
        //
        //   control:
        //     true to indicate that a CTRL key was pressed; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     The numeric value of the key parameter is less than 0 or greater than 255.
        public CliKeyEvent(char keyChar, CliKey key, bool shift, bool alt, bool control)
        {
            this.KeyChar = keyChar;
            this.Key = key;
            if (shift)
            {
                this.SimpleModifiers |= CliSimpleModifier.Shift;
                this.Modifiers |= CliModifier.BothShift;
            }
            if (alt)
            {

                this.SimpleModifiers |= CliSimpleModifier.Alt;
                this.Modifiers |= CliModifier.BothAlt;
            }

            if (control)
            {
                this.SimpleModifiers |= CliSimpleModifier.Ctrl;
                this.Modifiers |= CliModifier.BothCtrl;
            }
        }

        public CliKeyEvent(char keyChar, CliKey key, CliModifier modifiers)
        {
            this.KeyChar = keyChar;
            this.Key = key;
            this.Modifiers = modifiers;

            if (modifiers.ContainsSomeOf(CliModifier.BothShift))
            {
                this.SimpleModifiers |= CliSimpleModifier.Shift;
            }

            if (modifiers.ContainsSomeOf(CliModifier.BothAlt))
            {

                this.SimpleModifiers |= CliSimpleModifier.Alt;
            }

            if (modifiers.ContainsSomeOf(CliModifier.BothCtrl))
            {
                this.SimpleModifiers |= CliSimpleModifier.Ctrl;
            }
        }
        public CliKeyEvent(string str)
        {
            this.KeyChar = '\0';

            this.Key = CliKey.Unknown;
            this.Modifiers = CliModifier.None;
            this.Text = str;
        }
        //
        // Summary:
        //     Gets the Unicode character represented by the current System.ConsoleKeyInfo object.
        //
        // Returns:
        //     An object that corresponds to the console key represented by the current System.ConsoleKeyInfo
        //     object.
        public char KeyChar { get; private set; }

        public string? Text { get; private set; }

        public bool IsText => Text != null && Text.Length > 0;

        //
        // Summary:
        //     Gets the console key represented by the current System.ConsoleKeyInfo object.
        //
        // Returns:
        //     A value that identifies the console key that was pressed.
        public CliKey Key { get; private set; }
        //
        // Summary:
        //     Gets a bitwise combination of System.ConsoleModifiers values that specifies one
        //     or more modifier keys pressed simultaneously with the console key.
        //
        // Returns:
        //     A bitwise combination of the enumeration values. There is no default value.
        public CliSimpleModifier SimpleModifiers { get; private set; }
        public CliModifier Modifiers { get; private set; }

        public bool Handled { get; set; } = false;

    }
    #endregion
}
