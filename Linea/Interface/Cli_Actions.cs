using Linea.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Linea.Interface
{
    public partial class Cli
    {
        #region Fundamentals

        //-----------------------------------------------------------------------------------------
        #region Writing to _text

        private void WriteString(string StringToWrite, params object[] args)
        {
            if (String.IsNullOrEmpty(StringToWrite))
            {
                return;
            }

            if (args != null)
            {
                StringToWrite = String.Format(StringToWrite, args);
            }

            this._cm.Write(StringToWrite, ConsoleWriteMode.Shift);
        }

        TextWriter ICliFunctions.Writer => _cm.Writer;

        private void WriteChar(char c)
        {
            this._cm.Write(c, ConsoleWriteMode.Shift);
        }

        private void Backspace()
        {
            if (LogicalCursorPos == 0) return;
            this._cm.Delete(1, ConsoleMechanics.CharacterDeleteDirection.ToTheLeft);
        }
        private void Delete()
        {
            if (_cm.IsEndOfLine()) return;
            this._cm.Delete(1, ConsoleMechanics.CharacterDeleteDirection.ToTheRight);
        }

        #endregion
        //-----------------------------------------------------------------------------------------

        private void MoveCursorRight() => MoveCursorRight(1);

        /// <summary>
        /// Avanza il cursore del numero passato come parametro, e incrementa <see cref="_cursorPos"/>.
        /// Questa funzione assume che il contenuto di _text e il contenuto della console siano allineati.
        /// </summary>
        /// <param name="thisMuch"></param>
        private void MoveCursorRight(int thisMuch = 1)
        {
            if (thisMuch < 0) return;

            int cursorLimit = this.CurrentText.Length;
            thisMuch = Math.Min(thisMuch, cursorLimit - (int)LogicalCursorPos);

            if (thisMuch < 0) return;

            _cm.MoveCursor(thisMuch);
        }

        private void MoveCursorLeft() => MoveCursorLeft(1);

        /// <summary>
        /// Indietreggia il cursore del numero passato come parametro, e decrementa <see cref="_cursorPos"/>.
        /// Questa funzione assume che il contenuto di _text e il contenuto della console siano allineati.
        /// </summary>
        /// <param name="thisMuch"></param>
        private void MoveCursorLeft(int thisMuch = 1)
        {
            if (thisMuch <= 0) return;

            thisMuch = Math.Min(thisMuch, LogicalCursorPos);

            if (thisMuch < 0) return;

            _cm.MoveCursor(-thisMuch);
        }

        #endregion

        #region CompositeActions


        /// <summary>
        /// Riporta il cursore all'inizio della linea.<para/>
        /// Questa funzione assume che <see cref="_text"/> sia allineato alla console.
        /// </summary>
        private void MoveCursorHome()
        {
            this.MoveCursorLeft(this.LogicalCursorPos);
        }

        /// <summary>
        /// Porta il cursore alla fine della linea.<para/>
        /// Questa funzione assume che <see cref="_text"/> sia allineato alla console.
        /// </summary>
        private void MoveCursorEnd()
        {
            this.MoveCursorRight(CurrentText.Length - LogicalCursorPos);
        }

        private void NewLineIfNeeded()
        {
            if (_cm.CursorColumn > 0)
            {
                _cm.WriteLine();
            }
        }

        private void WriteNewString(string str)
        {
            this.ClearLine();
            this.WriteString(str);
        }

        private void ClearLine()
        {
            MoveCursorHome();
            _cm.Delete(CurrentText.Length, ConsoleMechanics.CharacterDeleteDirection.ToTheRight);
        }

        #endregion

        #region Writing Actions

        private void PromptUser()
        {
            this._mode = InputMode.UserCommand;
            this.WritePrompt();
            this._acceptingInput = true;
        }
        private (int startColumn, int startRow, int endColumn, int endRow, int ConsoleW) _promptInfo;

        private void ClearPrompt()
        {
            int cRow = _cm.CursorRow;
            int cCol = _cm.CursorColumn;

            for (int row = this._promptInfo.startRow; row < this._promptInfo.endRow; row++)
            {
                this._cm.SetCursorPosition(row, 0);

                this._cm.Write(new string(' ', this._promptInfo.ConsoleW), ConsoleWriteMode.Overwrite);
            }
            this._cm.SetCursorPosition(0, this._promptInfo.endRow);
            this._cm.Write(new string(' ', this._promptInfo.endColumn), ConsoleWriteMode.Overwrite);

            this._cm.SetCursorPosition(cRow, cCol);
        }

        private void WritePrompt(string? prompt = null)
        {
            if (prompt == null) prompt = this.PromptString;

            this._promptInfo.startColumn = this._cm.CursorColumn;
            this._promptInfo.startRow = this._cm.CursorRow;
            this._promptInfo.ConsoleW = this._cm.Width;

            if (this._mode == InputMode.UserCommand && this.CommandMode != CliCommandMode.TypeOnly)
            {
                this.PrintNumberedCommandList();
            }
            this._cm.Write(prompt);

            this._promptInfo.endColumn = this._cm.CursorColumn;
            this._promptInfo.endRow = this._cm.CursorRow;
        }

        private bool _PromptNeedsRefresh = false;
        private void RefreshPrompt()
        {
            this._PromptNeedsRefresh = true;
        }
        private void InternalRefreshPrompt()
        {
            if (this._mode != InputMode.UserCommand || this._cm.Width != this._promptInfo.ConsoleW)
            {
                return;
            }

            this._PromptNeedsRefresh = false;
            _cm.PauseEvents();
            string backup = CurrentText;
            int backupPos = LogicalCursorPos;
            this.ClearLine();
            this.ClearPrompt();
            this._cm.SetCursorPosition(this._promptInfo.startRow, this._promptInfo.startColumn);
            this.WritePrompt();
            this.WriteString(backup);
            this._cm.SetCursorPosition(this._promptInfo.endRow, this._promptInfo.endColumn);
            this.MoveCursorRight(backupPos);
            _cm.ResumeEvents();
        }
        #endregion

        #region History

        private void PrevHistory()
        {
            if (this._historyIndex > 0)
            {
                this._historyIndex--;
                this.WriteNewString(this._history[this._historyIndex]);
            }
        }

        private void NextHistory()
        {
            if (this._historyIndex < this._history.Count)
            {
                this._historyIndex++;
                if (this._historyIndex == this._history.Count)
                {
                    this.ClearLine();
                }
                else
                {
                    this.WriteNewString(this._history[this._historyIndex]);
                }
            }
        }

        private void AddToHistory(String command)
        {
            if (_history.Count > 0 && _history[_history.Count - 1].Equals(command))
                return;
            this._history.Add(command);
            this._historyIndex = this._history.Count;
            if (this.ShouldPersistHistory)
            {
                this._historyStorage!.StoreList(this._history);
            }
        }

        private void ReadHistoryFromSecureStorage()
        {
            if (_historyStorage == null) throw new InvalidOperationException("There is no History storage service defined");
            this._history.Clear();
            IEnumerable<string> h = _historyStorage.ReadList();
            foreach (string item in h)
            {
                this._history.Add(item);
            }
            this._historyIndex = this._history.Count;
        }

        #endregion

        #region Autocomplete

        private void StartAutoComplete()
        {
            _cm.PauseEvents();
            try
            {
                _cm.Delete(LogicalCursorPos - _completionStart, ConsoleMechanics.CharacterDeleteDirection.ToTheLeft);
                this._completionsIndex = 0;
                this.WriteString(this._completions![this._completionsIndex]);
            }
            finally
            {
                _cm.ResumeEvents();
            }
        }

        private void NextAutoComplete() => ChangeAutoComplete(+1);
        private void PreviousAutoComplete() => ChangeAutoComplete(-1);

        private void ChangeAutoComplete(int i)
        {
            _cm.PauseEvents();
            try
            {
                _cm.Delete(LogicalCursorPos - _completionStart, ConsoleMechanics.CharacterDeleteDirection.ToTheLeft);

                this._completionsIndex = Ext.ToValidIndex(this._completionsIndex + i, this._completions!.Length);

                this.WriteString(this._completions![this._completionsIndex]);
            }
            finally
            {
                _cm.ResumeEvents();
            }
        }

        private void ResetAutoComplete()
        {
            this._completions = null;
            this._completionsIndex = 0;
        }


        #endregion

    }
}
