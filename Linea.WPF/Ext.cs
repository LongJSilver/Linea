using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linea.WPF
{
    internal static class Ext
    {
        internal static bool ToCliKey(this System.Windows.Input.Key key, out Linea.CliKey resultKey)
        {
            resultKey = Linea.CliKey.Unknown;

            switch (key)
            {
                case System.Windows.Input.Key.Back: resultKey = Linea.CliKey.Backspace; break;
                case System.Windows.Input.Key.Tab: resultKey = Linea.CliKey.Tab; break;
                case System.Windows.Input.Key.Clear: resultKey = Linea.CliKey.Clear; break;
                case System.Windows.Input.Key.Enter: resultKey = Linea.CliKey.Enter; break;
                case System.Windows.Input.Key.Pause: resultKey = Linea.CliKey.Pause; break;
                case System.Windows.Input.Key.Escape: resultKey = Linea.CliKey.Escape; break;
                case System.Windows.Input.Key.Space: resultKey = Linea.CliKey.Spacebar; break;
                case System.Windows.Input.Key.PageUp: resultKey = Linea.CliKey.PageUp; break;
                case System.Windows.Input.Key.PageDown: resultKey = Linea.CliKey.PageDown; break;
                case System.Windows.Input.Key.End: resultKey = Linea.CliKey.End; break;
                case System.Windows.Input.Key.Home: resultKey = Linea.CliKey.Home; break;
                case System.Windows.Input.Key.Left: resultKey = Linea.CliKey.LeftArrow; break;
                case System.Windows.Input.Key.Up: resultKey = Linea.CliKey.UpArrow; break;
                case System.Windows.Input.Key.Right: resultKey = Linea.CliKey.RightArrow; break;
                case System.Windows.Input.Key.Down: resultKey = Linea.CliKey.DownArrow; break;
                case System.Windows.Input.Key.Select: resultKey = Linea.CliKey.Select; break;
                case System.Windows.Input.Key.Print: resultKey = Linea.CliKey.Print; break;
                case System.Windows.Input.Key.Execute: resultKey = Linea.CliKey.Execute; break;
                case System.Windows.Input.Key.PrintScreen: resultKey = Linea.CliKey.PrintScreen; break;
                case System.Windows.Input.Key.Insert: resultKey = Linea.CliKey.Insert; break;
                case System.Windows.Input.Key.Delete: resultKey = Linea.CliKey.Delete; break;
                case System.Windows.Input.Key.Help: resultKey = Linea.CliKey.Help; break;

                // Numeri
                case System.Windows.Input.Key.D0: resultKey = Linea.CliKey.D0; break;
                case System.Windows.Input.Key.D1: resultKey = Linea.CliKey.D1; break;
                case System.Windows.Input.Key.D2: resultKey = Linea.CliKey.D2; break;
                case System.Windows.Input.Key.D3: resultKey = Linea.CliKey.D3; break;
                case System.Windows.Input.Key.D4: resultKey = Linea.CliKey.D4; break;
                case System.Windows.Input.Key.D5: resultKey = Linea.CliKey.D5; break;
                case System.Windows.Input.Key.D6: resultKey = Linea.CliKey.D6; break;
                case System.Windows.Input.Key.D7: resultKey = Linea.CliKey.D7; break;
                case System.Windows.Input.Key.D8: resultKey = Linea.CliKey.D8; break;
                case System.Windows.Input.Key.D9: resultKey = Linea.CliKey.D9; break;

                // Lettere
                case System.Windows.Input.Key.A: resultKey = Linea.CliKey.A; break;
                case System.Windows.Input.Key.B: resultKey = Linea.CliKey.B; break;
                case System.Windows.Input.Key.C: resultKey = Linea.CliKey.C; break;
                case System.Windows.Input.Key.D: resultKey = Linea.CliKey.D; break;
                case System.Windows.Input.Key.E: resultKey = Linea.CliKey.E; break;
                case System.Windows.Input.Key.F: resultKey = Linea.CliKey.F; break;
                case System.Windows.Input.Key.G: resultKey = Linea.CliKey.G; break;
                case System.Windows.Input.Key.H: resultKey = Linea.CliKey.H; break;
                case System.Windows.Input.Key.I: resultKey = Linea.CliKey.I; break;
                case System.Windows.Input.Key.J: resultKey = Linea.CliKey.J; break;
                case System.Windows.Input.Key.K: resultKey = Linea.CliKey.K; break;
                case System.Windows.Input.Key.L: resultKey = Linea.CliKey.L; break;
                case System.Windows.Input.Key.M: resultKey = Linea.CliKey.M; break;
                case System.Windows.Input.Key.N: resultKey = Linea.CliKey.N; break;
                case System.Windows.Input.Key.O: resultKey = Linea.CliKey.O; break;
                case System.Windows.Input.Key.P: resultKey = Linea.CliKey.P; break;
                case System.Windows.Input.Key.Q: resultKey = Linea.CliKey.Q; break;
                case System.Windows.Input.Key.R: resultKey = Linea.CliKey.R; break;
                case System.Windows.Input.Key.S: resultKey = Linea.CliKey.S; break;
                case System.Windows.Input.Key.T: resultKey = Linea.CliKey.T; break;
                case System.Windows.Input.Key.U: resultKey = Linea.CliKey.U; break;
                case System.Windows.Input.Key.V: resultKey = Linea.CliKey.V; break;
                case System.Windows.Input.Key.W: resultKey = Linea.CliKey.W; break;
                case System.Windows.Input.Key.X: resultKey = Linea.CliKey.X; break;
                case System.Windows.Input.Key.Y: resultKey = Linea.CliKey.Y; break;
                case System.Windows.Input.Key.Z: resultKey = Linea.CliKey.Z; break;

                // Tasti Windows e Applicazione
                case System.Windows.Input.Key.LWin: resultKey = Linea.CliKey.LeftWindows; break;
                case System.Windows.Input.Key.RWin: resultKey = Linea.CliKey.RightWindows; break;
                case System.Windows.Input.Key.Apps: resultKey = Linea.CliKey.Applications; break;

                // Sleep
                case System.Windows.Input.Key.Sleep: resultKey = Linea.CliKey.Sleep; break;

                // Tastierino numerico
                case System.Windows.Input.Key.NumPad0: resultKey = Linea.CliKey.NumPad0; break;
                case System.Windows.Input.Key.NumPad1: resultKey = Linea.CliKey.NumPad1; break;
                case System.Windows.Input.Key.NumPad2: resultKey = Linea.CliKey.NumPad2; break;
                case System.Windows.Input.Key.NumPad3: resultKey = Linea.CliKey.NumPad3; break;
                case System.Windows.Input.Key.NumPad4: resultKey = Linea.CliKey.NumPad4; break;
                case System.Windows.Input.Key.NumPad5: resultKey = Linea.CliKey.NumPad5; break;
                case System.Windows.Input.Key.NumPad6: resultKey = Linea.CliKey.NumPad6; break;
                case System.Windows.Input.Key.NumPad7: resultKey = Linea.CliKey.NumPad7; break;
                case System.Windows.Input.Key.NumPad8: resultKey = Linea.CliKey.NumPad8; break;
                case System.Windows.Input.Key.NumPad9: resultKey = Linea.CliKey.NumPad9; break;
                case System.Windows.Input.Key.Multiply: resultKey =  Linea.CliKey.Multiply; break;
                case System.Windows.Input.Key.Add: resultKey =       Linea.CliKey.Add;      break;
                case System.Windows.Input.Key.Separator: resultKey = Linea.CliKey.Separator; break;
                case System.Windows.Input.Key.Subtract: resultKey =  Linea.CliKey.Subtract;     break;
                case System.Windows.Input.Key.Decimal: resultKey =   Linea.CliKey.Decimal;      break;
                case System.Windows.Input.Key.Divide: resultKey =    Linea.CliKey.Divide;      break;

                // Tasti funzione
                case System.Windows.Input.Key.F1: resultKey = Linea.CliKey.F1; break;
                case System.Windows.Input.Key.F2: resultKey = Linea.CliKey.F2; break;
                case System.Windows.Input.Key.F3: resultKey = Linea.CliKey.F3; break;
                case System.Windows.Input.Key.F4: resultKey = Linea.CliKey.F4; break;
                case System.Windows.Input.Key.F5: resultKey = Linea.CliKey.F5; break;
                case System.Windows.Input.Key.F6: resultKey = Linea.CliKey.F6; break;
                case System.Windows.Input.Key.F7: resultKey = Linea.CliKey.F7; break;
                case System.Windows.Input.Key.F8: resultKey = Linea.CliKey.F8; break;
                case System.Windows.Input.Key.F9: resultKey = Linea.CliKey.F9; break;
                case System.Windows.Input.Key.F10: resultKey = Linea.CliKey.F10; break;
                case System.Windows.Input.Key.F11: resultKey = Linea.CliKey.F11; break;
                case System.Windows.Input.Key.F12: resultKey = Linea.CliKey.F12; break;
                case System.Windows.Input.Key.F13: resultKey = Linea.CliKey.F13; break;
                case System.Windows.Input.Key.F14: resultKey = Linea.CliKey.F14; break;
                case System.Windows.Input.Key.F15: resultKey = Linea.CliKey.F15; break;
                case System.Windows.Input.Key.F16: resultKey = Linea.CliKey.F16; break;
                case System.Windows.Input.Key.F17: resultKey = Linea.CliKey.F17; break;
                case System.Windows.Input.Key.F18: resultKey = Linea.CliKey.F18; break;
                case System.Windows.Input.Key.F19: resultKey = Linea.CliKey.F19; break;
                case System.Windows.Input.Key.F20: resultKey = Linea.CliKey.F20; break;
                case System.Windows.Input.Key.F21: resultKey = Linea.CliKey.F21; break;
                case System.Windows.Input.Key.F22: resultKey = Linea.CliKey.F22; break;
                case System.Windows.Input.Key.F23: resultKey = Linea.CliKey.F23; break;
                case System.Windows.Input.Key.F24: resultKey = Linea.CliKey.F24; break;
                // Blocchi numerici
                case System.Windows.Input.Key.NumLock: resultKey = Linea.CliKey.NumLock; break;
            }

            return resultKey != Linea.CliKey.Unknown;
        }

    }
}
