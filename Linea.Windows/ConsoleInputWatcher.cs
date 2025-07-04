using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace Linea.Windows
{
    internal class ConsoleInputWatcher : IDisposable
    {
        public event Action<char> OnKeyChar;
        public event Action<ushort, bool, uint, char> OnRawKey; // (VK code, isDown, modifiers)
        public event Action<short, short> OnResize;
        public event Action<short, short, uint, uint> OnMouse; // (X, Y, buttonState, controlKeyState)
        public event Action<bool> OnFocusChanged;
        public event Action<uint> OnMenu; // rarely used

        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_WINDOW_INPUT = 0x0008;
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        private const uint ENABLE_PROCESSED_INPUT = 0x0001;
        private const uint ENABLE_MOUSE_INPUT = 0x0010;

        private IntPtr _stdin;
        private Thread _thread;
        private volatile bool _running;

        public void Start()
        {
            _stdin = GetStdHandle(STD_INPUT_HANDLE);

            if (!GetConsoleMode(_stdin, out uint mode))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get console mode");
            }

            mode |= ENABLE_WINDOW_INPUT | ENABLE_MOUSE_INPUT | ENABLE_EXTENDED_FLAGS | ENABLE_PROCESSED_INPUT;

            if (!SetConsoleMode(_stdin, mode))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not set console mode");
            }
            _running = true;
            _thread = new Thread(ReadLoop) { IsBackground = true };
            _thread.Start();
        }

        public void Stop() => _running = false;

        private void ReadLoop()
        {
            var records = new INPUT_RECORD[1];

            while (_running)
            {
                if (!ReadConsoleInput(_stdin, records, 1, out _))
                    continue;

                var record = records[0];
                switch ((EventType)record.EventType)
                {
                    case EventType.KEY_EVENT:
                        var key = record.KeyEvent;
                        OnRawKey?.Invoke(key.wVirtualKeyCode, key.bKeyDown, key.dwControlKeyState, key.UnicodeChar);
                        if (key.bKeyDown && key.UnicodeChar != 0)
                            OnKeyChar?.Invoke(key.UnicodeChar);
                        break;

                    case EventType.WINDOW_BUFFER_SIZE_EVENT:
                        var size = record.WindowBufferSizeEvent.dwSize;
                        OnResize?.Invoke(size.X, size.Y);
                        break;

                    case EventType.MOUSE_EVENT:
                        var mouse = record.MouseEvent;
                        var pos = mouse.dwMousePosition;
                        OnMouse?.Invoke(pos.X, pos.Y, mouse.dwButtonState, mouse.dwControlKeyState);
                        break;

                    case EventType.FOCUS_EVENT:
                        OnFocusChanged?.Invoke(record.FocusEvent.bSetFocus > 0);
                        break;

                    case EventType.MENU_EVENT:
                        OnMenu?.Invoke(record.MenuEvent.dwCommandId);
                        break;
                }
            }
        }

        #region Native Declarations

        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleInput, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleInput, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadConsoleInput(
            IntPtr hConsoleInput,
            [Out] INPUT_RECORD[] lpBuffer,
            uint nLength,
            out uint lpNumberOfEventsRead);

        public void Dispose()
        {
            Stop();
        }

        enum EventType : ushort
        {
            KEY_EVENT = 0x0001,
            MOUSE_EVENT = 0x0002,
            WINDOW_BUFFER_SIZE_EVENT = 0x0004,
            MENU_EVENT = 0x0008,
            FOCUS_EVENT = 0x0010
        }

        [StructLayout(LayoutKind.Sequential)]
        struct COORD
        {
            public short X;
            public short Y;

            public COORD(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };


        [StructLayout(LayoutKind.Explicit)]
        struct INPUT_RECORD
        {
            [FieldOffset(0)]
            public ushort EventType;
            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;
            [FieldOffset(4)]
            public MOUSE_EVENT_RECORD MouseEvent;
            [FieldOffset(4)]
            public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
            [FieldOffset(4)]
            public MENU_EVENT_RECORD MenuEvent;
            [FieldOffset(4)]
            public FOCUS_EVENT_RECORD FocusEvent;
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        struct KEY_EVENT_RECORD
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.Bool)]
            public bool bKeyDown;
            [FieldOffset(4), MarshalAs(UnmanagedType.U2)]
            public ushort wRepeatCount;
            [FieldOffset(6), MarshalAs(UnmanagedType.U2)]
            public ushort wVirtualKeyCode;
            [FieldOffset(8), MarshalAs(UnmanagedType.U2)]
            public ushort wVirtualScanCode;
            [FieldOffset(10)]
            public char UnicodeChar;
            [FieldOffset(12), MarshalAs(UnmanagedType.U4)]
            public uint dwControlKeyState;
        }


        [StructLayout(LayoutKind.Explicit)]
        struct MOUSE_EVENT_RECORD
        {
            [FieldOffset(0)]
            public COORD dwMousePosition;
            [FieldOffset(4)]
            public uint dwButtonState;
            [FieldOffset(8)]
            public uint dwControlKeyState;
            [FieldOffset(12)]
            public uint dwEventFlags;
        }

        struct WINDOW_BUFFER_SIZE_RECORD
        {
            public COORD dwSize;

            public WINDOW_BUFFER_SIZE_RECORD(short x, short y)
            {
                this.dwSize = new COORD(x, y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MENU_EVENT_RECORD
        {
            public uint dwCommandId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FOCUS_EVENT_RECORD
        {
            public uint bSetFocus;
        }
        #endregion
    }
}
