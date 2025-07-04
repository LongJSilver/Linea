using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Linea.Windows
{
    public class WindowsConsole : IConsole, IDisposable
    {
        #region "Windows API"
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        #endregion
        private readonly ConsoleInputWatcher _watcher;


        static bool ConsoleExists()
        {
            return GetConsoleWindow() != IntPtr.Zero;
        }
        private readonly bool _consoleCreatedByUs;
        public WindowsConsole()
        {
            if (!ConsoleExists())
            {
                _consoleCreatedByUs = true;
                AllocConsole();
            }
            else
            {
                _consoleCreatedByUs = false;
            }
            _watcher = new ConsoleInputWatcher();
            _watcher.Start();
            _watcher.OnResize += _watcher_OnResize;
            _watcher.OnRawKey += _watcher_OnRawKey;
        }

        private readonly BlockingCollection<CliKeyEvent> _events = new BlockingCollection<CliKeyEvent>();

        public bool Available => _events.Count != 0;
        CliKeyEvent IConsole.Read() => _events.Take();

        private void _watcher_OnRawKey(ushort arg1, bool isDown, uint mod, char keyChar)
        {
            if (!isDown) return;
            CliKey key = (CliKey)arg1;
            ConsoleModifiers modifiers = (ConsoleModifiers)mod;
            CliKeyEvent keyEvent = new CliKeyEvent(keyChar, key,
                modifiers.HasFlag(ConsoleModifiers.Shift),
                modifiers.HasFlag(ConsoleModifiers.Alt),
                modifiers.HasFlag(ConsoleModifiers.Control)
                );
            _events.Add(keyEvent);
        }

        private void _watcher_OnResize(short x, short y)
        {
            this.BufferSizeChanged?.Invoke(this, x, y);
        }

        #region IConsole
        public CliKeyEvent Read()
        {
            ConsoleKeyInfo k = Console.ReadKey(true);
            CliKey key = (CliKey)k.Key;

            CliKeyEvent keyEvent = new CliKeyEvent(k.KeyChar, key,
                k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                k.Modifiers.HasFlag(ConsoleModifiers.Alt),
                k.Modifiers.HasFlag(ConsoleModifiers.Control)
                );
            return keyEvent;
        }

        public int CursorLeft => Console.CursorLeft;
        public int CursorTop => Console.CursorTop;
        public int BufferWidth => Console.BufferWidth;
        public int BufferHeight => Console.BufferHeight;

        public IList<string> Text { set { } }

        public event ConsoleBufferSizeDelegate BufferSizeChanged;
        public event ConsoleCursorLocationDelegate CursorLocationChanged;


        public void SetCursorPosition(int row, int column)
        {
            Console.SetCursorPosition(column, row);
        }

        public void SetRowText(int Row, string value)
        {
            int cursorLeft = Console.CursorLeft;
            int cursorTop = Console.CursorTop;
            Console.SetCursorPosition(0, Row);

            Console.Write(value);
            if (value.Length < Console.BufferWidth)
            {
                Console.Write(new string(' ', Console.BufferWidth - value.Length));
            }

            Console.SetCursorPosition(cursorLeft, cursorTop);
        }

        public void Dispose()
        {
            _watcher.Dispose();
            if (_consoleCreatedByUs)
                FreeConsole();
        }

        public void Clear()
        {
            Console.Clear();
        }

        public void SetRowCount(int RowCount)
        {
        }

        #endregion
    }

}
