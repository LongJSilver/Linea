using Linea.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Linea.Utils.Ext;

namespace Linea.Interface
{
    public enum ConsoleWriteMode : byte
    {
        Overwrite = 0,
        Shift
    }

    public class ConsoleContentChangeArgs
    {
        public readonly bool AllChanged;
        public readonly int FirstRow;
        public readonly int LastRow;

        internal ConsoleContentChangeArgs(int firstRow, int lastRow, bool allChanged)
        {
            FirstRow = firstRow;
            LastRow = lastRow;
            AllChanged = allChanged;
        }

        internal ConsoleContentChangeArgs(uint firstRow, uint lastRow, bool allChanged)
        {
            FirstRow = (int)firstRow;
            LastRow = (int)lastRow;
            AllChanged = allChanged;

        }
    }

    public delegate void ConsoleContentChangeEventHandler(object sender, ConsoleContentChangeArgs e);
    public delegate void CursorLocationChangeEventHandler(object sender, int row, int column);
    public delegate void ConsoleRowCountChangeEventHandler(object sender, int rowCount);

    public partial class ConsoleMechanics
    {
        private volatile bool _eventsSuspended = false;
        private volatile bool _allRowsChanged = false;
        private readonly object _EventsLock = new object();
        private HashSet<uint> _changedRows = new HashSet<uint>();
        public event ConsoleContentChangeEventHandler? ContentChanged;
        public event CursorLocationChangeEventHandler? CursorLocationChanged;
        public event ConsoleRowCountChangeEventHandler? ConsoleRowCountChanged;


        public bool IsStartOfLine() => CursorColumn == 0 && CurrentPhysicalRow.Ordinal == 0;
        public bool IsEndOfLine() => CursorColumn == CurrentPhysicalRow.Length
                                 && (
                                            IsCursorInLastRow
                                        || _rows[CursorRow + 1].LogicalRowID != CurrentPhysicalRow.LogicalRowID
                                    );

        public bool IsStartOfBuffer() => CursorRow == 0 && CursorColumn == 0;
        public bool IsEndOfBuffer() => CursorRow == _rows.Count - 1 && CurrentPhysicalRow.Length < Width - 1;

        public IList<string> GenerateList() => new ConsoleMechanicsStringList(this);

        private void FireEvents()
        {
            if (_eventsSuspended)
                return;

            if (_changedRows.Count == 0 && !_allRowsChanged)
                return;
            List<ConsoleContentChangeArgs> ToCall = new List<ConsoleContentChangeArgs>();
            lock (_EventsLock)
            {
                if (ContentChanged == null)
                {
                    _allRowsChanged = false;
                    _changedRows.Clear();
                    return;
                }
                if (_allRowsChanged)
                {
                    ToCall.Add(new ConsoleContentChangeArgs(0, (uint)(_rows.Count - 1), _allRowsChanged));
                }
                else if (_changedRows.Count > 0)
                {
                    // Get all changed rows, sort them
                    var changed = new List<uint>(_changedRows);
                    changed.Sort();

                    int clusterStartIndex = 0;
                    uint clusterEndValue = changed[0];

                    for (int i = 1; i < changed.Count; i++)
                    {
                        if (changed[i] == clusterEndValue + 1)
                        {
                            // Extend the current cluster
                            clusterEndValue = changed[i];
                        }
                        else
                        {
                            // Fire event for the previous cluster
                            ToCall.Add(new ConsoleContentChangeArgs(changed[clusterStartIndex], clusterEndValue, _allRowsChanged));
                            // Start a new cluster
                            clusterStartIndex = i;
                            clusterEndValue = changed[i];
                        }
                    }
                    // Fire event for the last cluster
                    ToCall.Add(new ConsoleContentChangeArgs(changed[clusterStartIndex], clusterEndValue, _allRowsChanged));
                }

                _allRowsChanged = false;
                _changedRows.Clear();
            }
            if (_latestRowCount != _rows.Count)
            {
                ConsoleRowCountChanged?.Invoke(this, _rows.Count);
                _latestRowCount = _rows.Count;
            }
            foreach (var item in ToCall)
            {
                ContentChanged(this, item);
            }

            InternalSetCursor(Cursor);
        }
        private int _latestRowCount = -1;
        private bool _widthChanged => _requestedWidth != _width;

        private FixedSizeList<PhysicalRow> _rows;

        private readonly StringEventTextWriter _writer;
        public TextWriter Writer => _writer;

        private uint _width;
        private uint _requestedWidth;
        private uint _height;

        public int Height
        {
            get
            {
                ResizeIfNeeded();
                return (int)_height;
            }
            set
            {
                uint old = _height;
                _height = Math.Max(NN(value), 2);
                if (old != _height)
                {
                    _rows.Capacity = _height;
                }
            }
        }

        public int Width
        {
            get { ResizeIfNeeded(); return (int)_width; }
            set
            {
                _requestedWidth = (uint)Math.Max(2, value);
            }
        }

        public void SetCursorPosition(int row, int column)
        {
            if (row < 0 || row >= _rows.Count)
                throw new ArgumentOutOfRangeException(nameof(row), "Row index is out of range.");
            if (column < 0 || column >= Width)
                throw new ArgumentOutOfRangeException(nameof(column), "Column index is out of range.");
            InternalSetCursor(row, column);
        }

        private void InternalSetCursor(int row, int column)
        {
            Cursor = ((uint)row, (uint)column);
            CursorLocationChanged?.Invoke(this, (int)Cursor.Row, (int)Cursor.Column);
        }
        private void InternalSetCursor(uint row, uint column)
        {
            Cursor = ((uint)row, (uint)column);
            CursorLocationChanged?.Invoke(this, (int)Cursor.Row, (int)Cursor.Column);
        }
        private void InternalSetCursor(CursorPhysicalLocation loc)
        {
            Cursor = loc;
            CursorLocationChanged?.Invoke(this, (int)Cursor.Row, (int)Cursor.Column);
        }

        private CursorPhysicalLocation _cursor = (0, 0);
        private CursorPhysicalLocation Cursor
        {
            get => _cursor; set
            {
                _cursor = value;
            }
        }

        private PhysicalRow CurrentPhysicalRow => _rows[Cursor.Row];

        public int RowCount => _rows.Count;

        public ConsoleMechanics(int width, int height)
        {
            this._height = NN(height);
            this._width = this._requestedWidth = Math.Max(2, NN(width));
            _rows = new FixedSizeList<PhysicalRow>(_height);
            _rows.ItemDiscarding += this._rows_ItemDiscarding;
            NewLine(ConsoleWriteMode.Shift);
            this._writer = new StringEventTextWriter();
            this._writer.CharWritten += this._writer_CharWritten;
            this._writer.CharsWritten += this._writer_CharsWritten;
        }

        private CursorLogicalLocation GetCursorLogicalPosition()
        {
            ResizeIfNeeded();
            if (Cursor.Row < 0 || Cursor.Row > _rows.Count)
                throw new ArgumentOutOfRangeException(nameof(Cursor.Row), "Cursor row is out of range.");
            if (Cursor.Column < 0 || Cursor.Column >= Width + 1)
                throw new ArgumentOutOfRangeException(nameof(Cursor.Column), "Cursor column is out of range.");
            //*******************************************//
            var PRow = CurrentPhysicalRow;
            //saving the current "row" index is meaningless, especially
            //considering that our row buffer is circular and could easily 
            //clear rows at the beginning and shift everything up to make space for 
            //new text content; hence we backup the logical row GUID instead

            Guid CursorLocation_LogicalID = PRow.LogicalRowID;
            uint CursorLogicalColumn = Cursor.Column;

            //we now count the characters from the beginning of the logical row
            uint ordinal = CurrentPhysicalRow.Ordinal;
            for (uint i = 1; i <= ordinal; i++)
            {
                CursorLogicalColumn += (uint)_rows[Cursor.Row - i].Builder.Length;
            }

            return new CursorLogicalLocation(CursorLocation_LogicalID, CursorLogicalColumn);
        }

        private CursorPhysicalLocation ConvertToPhysical(CursorLogicalLocation logicalBackup)
        {
            /*
                        * We could be unsuccessful in finding the original logical row
                        * the cursor was sitting on before the resize process, 
                        * but this could only mean that the cursor was high enough
                        * that its physical row was removed from the top of the buffer.
                        * It is only logical therefore that the cursor fall back
                        * at the very top of the buffer.
                        */
            CursorPhysicalLocation result = new CursorPhysicalLocation();
            result.Column = 0;
            result.Row = 0;

            int LogicalRowStartsAt = -1;
            int physicalRowCount = 0;

            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].LogicalRowID == logicalBackup.LogicalID)
                {
                    if (physicalRowCount == 0)
                    {
                        LogicalRowStartsAt = i;
                    }
                    physicalRowCount++;
                }
            }

            if (physicalRowCount > 0)
            {
                int MaxRow = LogicalRowStartsAt + physicalRowCount - 1;
                result.Column = logicalBackup.Column % _requestedWidth;
                result.Row = ((uint)LogicalRowStartsAt) + (logicalBackup.Column / _requestedWidth);
                if (MaxRow < result.Row)
                {
                    result = new(MaxRow, _rows[MaxRow].Builder.Length);
                }
            }

            return result;
            //*******************************************//
        }

        private volatile bool _resizing = false;
        private void ResizeIfNeeded()
        {
            if (!_resizing && _widthChanged)
            {
                lock (_EventsLock)
                {
                    try
                    {
                        _resizing = true;
                        _allRowsChanged = true;
                        if (_rows.Count > 0)
                        {
                            /*
                            * more than one physical row can share the same logicalRowID
                            * when the physical rows were allocated to accomodate text longer than the
                            * buffer width. This implies that all physical rows with the same id are actually
                            * a single logical row, that would have been allocated in a single physical row
                            * had the buffer been large enough.
                            * This also implies that the cursor has a logical address, of which the column 
                            * coordinate is the character count from the beginning of the LOGICAL row

                           */
                            CursorLogicalLocation logicalBackup = GetCursorLogicalPosition();

                            List<PhysicalRow> tempList = new((int)_rows.Capacity);
                            Guid? CurrentID = default;
                            StringBuilder temp = StringBuilderPool.Rent();

                            void FinalizePendingRow()
                            {
                                if (!CurrentID.HasValue) return;
                                uint ordinal = 0;
                                do
                                {
                                    PhysicalRow pr = new PhysicalRow(CurrentID!.Value, ordinal, StringBuilderPool.Rent());

                                    int ToMove = Math.Min((int)_requestedWidth, temp.Length);
                                    pr.Builder.Append(temp.ToString(0, ToMove));
                                    temp.Remove(0, ToMove);

                                    tempList.Add(pr);
                                    ordinal++;
                                }
                                while (temp.Length > 0);

                                CurrentID = null;
                                temp.Clear();
                            }

                            while (_rows.Count > 0)
                            {
                                PhysicalRow existing = _rows.RemoveFromHead();
                                Guid LogicalID = existing.LogicalRowID;

                                if (LogicalID != CurrentID)
                                {
                                    FinalizePendingRow();
                                    CurrentID = LogicalID;
                                }

                                temp.Append(existing.Builder.ToString());
                                StringBuilderPool.Return(existing.Builder);
                            }

                            FinalizePendingRow();

                            StringBuilderPool.Return(temp);

                            _rows.Capacity = this._height;
                            foreach (var item in tempList)
                            {
                                _rows.Add(item);
                            }

                            Cursor = (ConvertToPhysical(logicalBackup));//not calling the event here
                                                                        //*******************************************//
                        }
                    }
                    finally
                    {
                        _resizing = false;

                    }

                }

                _width = _requestedWidth;
            }
        }

        private uint _min_callerRequired_PhysicalHistory = 300;
        private uint _min_callerRequired_MimumLogicalHistory = 300;

        /// <summary>
        /// Minimum number of on-screen rows to keep in history. 
        /// <para />If this number is smaller than <see cref="Height"/>, the console will still keep as many rows in memory as needed to fill the visible console space.
        /// 
        /// </summary>
        public uint MinimumPhysicalHistory
        {
            get => _min_callerRequired_PhysicalHistory;
            set
            {
                _min_callerRequired_PhysicalHistory = value;
            }
        }

        /// <summary>
        /// Minimum number of logical rows to keep in history.
        /// <para />If this number is significantly small, the console will still keep as many rows in memory as needed to fill the visible console space.
        /// </summary>
        public uint MinimumLogicalHistory
        {
            get => _min_callerRequired_MimumLogicalHistory;
            set
            {
                _min_callerRequired_MimumLogicalHistory = value;
            }
        }

        public int CursorColumn => (int)Cursor.Column;
        public int CursorRow => (int)Cursor.Row;

        /// <summary>
        /// The full text of the current row; if the text is wrapped into multiple rows, it will be spliced together before returning.
        /// </summary>
        public string CurrentRowText
        {
            get
            {
                ResizeIfNeeded();
                StringBuilder sb = StringBuilderPool.Rent();
                try
                {
                    var current = CurrentPhysicalRow;
                    uint startIndex = Cursor.Row - current.Ordinal;

                    PhysicalRow row;
                    for (uint i = startIndex; (i < _rows.Count && (row = _rows[i]).LogicalRowID == current.LogicalRowID); i++)
                    {
                        sb.Append(row.Builder.ToString());
                    }
                    return sb.ToString();
                }
                finally
                {
                    StringBuilderPool.Return(sb);
                }

            }
        }

        private void _rows_ItemDiscarding(int index, PhysicalRow obj)
        {
            lock (_EventsLock)
            {
                _allRowsChanged = true;
                Guid CurrentID = obj.LogicalRowID;
                int i = index + 1;
                while (i < _rows.Count)
                {
                    PhysicalRow row = _rows[i];
                    if (row.LogicalRowID != CurrentID) break;
                    row.Ordinal--;
                    i++;
                }

                StringBuilderPool.Return(obj.Builder);

                if (i <= Cursor.Row)
                {
                    Cursor = (Cursor.Row - 1, Cursor.Column); //not calling the event here
                }
            }
        }

        private void _writer_CharsWritten(char[] c, uint start, uint len)
        {
            ResizeIfNeeded();
            UnpackCharacters(ConsoleWriteMode.Shift, c, start, len);
        }

        private void UnpackCharacters(ConsoleWriteMode mode, char[] charray, uint start, int totalLen)
            => UnpackCharacters(mode, charray, start, NN(totalLen));
        private void UnpackCharacters(ConsoleWriteMode mode, char[] charray, uint start, uint totalLen)
        {
            uint limit = start + totalLen;
            uint startAt = start, len = 0;

            void finalizeRun()
            {
                if (len > 0)
                {
                    WriteCharacterRun(mode, charray, startAt, len);
                    startAt += len;
                    len = 0;
                }
            }

            while (startAt + len < limit)
            {
                char c = charray[startAt + len];
                if (c == '\n' || c == '\r')
                {
                    finalizeRun();
                    startAt++;
                    WriteNewLineCharacter(mode, c);
                }
                else
                {
                    len++;
                }
            }
            finalizeRun();
        }

        private void _writer_CharWritten(char c)
        {
            ResizeIfNeeded();

            if (c == '\n' || c == '\r')
            {
                WriteNewLineCharacter(ConsoleWriteMode.Shift, c);
            }
            else
            {
                this.WriteCharacterRun(ConsoleWriteMode.Shift, new char[] { c });
            }
        }

        public void WriteLine(string? value, ConsoleWriteMode mode = ConsoleWriteMode.Shift)
        {
            ResizeIfNeeded();
            if (value != null)
            {
                Write(value, mode);
            }
            WriteNewLineCharacter(mode, '\n');
            FireEvents();
        }

        public void WriteLine(ConsoleWriteMode mode = ConsoleWriteMode.Shift) => WriteLine(null, mode);

        private readonly char[] _single_char_buffer = new char[1];
        public void Write(char value, ConsoleWriteMode mode = ConsoleWriteMode.Shift)
        {
            ResizeIfNeeded();

            _single_char_buffer[0] = value;
            UnpackCharacters(mode, _single_char_buffer, 0, 1);

            FireEvents();
        }

        public void Write(string value, ConsoleWriteMode mode = ConsoleWriteMode.Shift)
        {
            ResizeIfNeeded();
            var charray = value.ToCharArray();
            UnpackCharacters(mode, charray, 0, value.Length);
            FireEvents();
        }

        private bool _justSeenCarriageReturn = false;

        private void WriteNewLineCharacter(ConsoleWriteMode mode, char obj)
        {
            //PhysicalRow currentPhysical = CurrentPhysicalRow;
            //int targetLocation = Cursor.Column;

            if (obj == '\r')
            {
                NewLine(mode);
            }
            else if (obj == '\n')
            {
                //LogicalRow previous;
                if (Cursor.Column == 0 && _justSeenCarriageReturn)
                {
                }
                else
                {
                    NewLine(mode);
                }
            }
            _justSeenCarriageReturn = obj == '\r';
        }


        private void WriteCharacterRun(ConsoleWriteMode mode, char[] chars)
            => WriteCharacterRun(mode, chars, 0, (uint)chars.Length);

        private void WriteCharacterRun(ConsoleWriteMode mode, char[] chars, uint startAt, uint len)
        {
            lock (_EventsLock)
            {
                _justSeenCarriageReturn = false;

                var LogicalBackup = GetCursorLogicalPosition();

                PhysicalRow currentPhysical = CurrentPhysicalRow;

                uint targetLocation = Cursor.Column;


                _changedRows.Add(Cursor.Row);
                if (mode == ConsoleWriteMode.Overwrite)
                {
                    uint ToRemove = Math.Min((uint)currentPhysical.Builder.Length - Cursor.Column, len);
                    currentPhysical.Remove(targetLocation, ToRemove);
                }

                currentPhysical.Insert(targetLocation, chars, startAt, len);

                OverflowCurrentRow(mode);
                uint nextOrdinal = FixOrdinals(LogicalBackup.LogicalID);

                LogicalBackup.Column += len;
                Cursor = ConvertToPhysical(LogicalBackup); //not calling the event here
                if (Cursor.Row == _rows.Count)
                {
                    PhysicalRow newPhysicalRow = new PhysicalRow(LogicalBackup.LogicalID, nextOrdinal, StringBuilderPool.Rent());
                    _rows.Add(newPhysicalRow);
                    _changedRows.Add(Cursor.Row);
                }
            }
        }
        /// <summary>
        /// Must be called within lock
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="rowOffset"></param>
        private void OverflowCurrentRow(ConsoleWriteMode mode, uint rowOffset = 0)
        {
            //**************************//
            PhysicalRow currentPhysical = CurrentPhysicalRow;
            uint checkingRow = rowOffset;
            while (_rows[(int)(Cursor.Row + checkingRow)].Builder.Length > Width)
            {
                PhysicalRow fromRow = _rows[(int)(Cursor.Row + checkingRow)];
                _changedRows.Add(Cursor.Row + checkingRow);
                string remainingText = fromRow.TrimEnd(_width);
                bool needNewRow = (Cursor.Row + checkingRow >= _rows.Count - 1) //we are at last Row
                                 || _rows[Cursor.Row + checkingRow + 1].LogicalRowID != fromRow.LogicalRowID; // OR next row is not a continuation of this
                if (needNewRow)
                {
                    PhysicalRow newPhysicalRow = new PhysicalRow(currentPhysical.LogicalRowID, currentPhysical.Ordinal + 1, StringBuilderPool.Rent());
                    newPhysicalRow.Builder.Append(remainingText);
                    bool Overflowing = IsBufferFull;
                    _rows.Insert(Cursor.Row + checkingRow + 1, newPhysicalRow);
                    if (Overflowing) checkingRow--;
                }
                else
                {
                    PhysicalRow toRow = _rows[Cursor.Row + checkingRow + 1];
                    if (mode == ConsoleWriteMode.Overwrite)
                    {
                        int ToRemove = Math.Min(toRow.Builder.Length, remainingText.Length);
                        toRow.Builder.Remove(0, ToRemove);
                    }

                    toRow.Builder.Insert(0, remainingText);
                }
                _changedRows.Add(Cursor.Row + checkingRow + 1);
                checkingRow++;
            }
            //**************************//
        }


        /// <summary>
        /// returns the next free ordinal
        /// </summary>
        /// <param name="logicalRowID"></param>
        /// <returns></returns>
        private uint FixOrdinals(Guid logicalRowID)
        {
            int firstRowIndex = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].LogicalRowID == logicalRowID)
                {
                    firstRowIndex = i;
                    break;
                }
            }
            if (firstRowIndex == -1) return 0;

            uint ordinal = 0;
            while (firstRowIndex < _rows.Count && _rows[firstRowIndex].LogicalRowID == logicalRowID)
            {
                _rows[firstRowIndex].Ordinal = ordinal++;
                firstRowIndex++;
            }
            return ordinal;
        }

        public enum CharacterDeleteDirection
        {
            ToTheLeft,
            ToTheRight
        }
        public void Delete(int count = 1, CharacterDeleteDirection direction = CharacterDeleteDirection.ToTheLeft)
        {
            if (count <= 0) return;
            uint uintCount = (uint)count;
            lock (_EventsLock)
            {
                if (direction == CharacterDeleteDirection.ToTheLeft)
                {
                    var stillToRemove = uintCount;
                    while (stillToRemove > 0)
                    {
                        PhysicalRow startingRow = CurrentPhysicalRow;
                        uint startColumn = Cursor.Column;
                        uint removableInThisRow = Math.Min(stillToRemove, startColumn);

                        if (removableInThisRow > 0)
                        {
                            startingRow.Remove(startColumn - removableInThisRow, removableInThisRow);
                            _changedRows.Add(Cursor.Row);
                            BackShift(Cursor.Row, removableInThisRow);
                            Cursor = (Cursor.Row, Cursor.Column - removableInThisRow); //not calling the event here

                            stillToRemove -= removableInThisRow;
                        }

                        if (stillToRemove > 0 && Cursor.Row > 0)
                        {
                            //we are crossing to the previous row
                            //if that row has a different logical index this means we are removing a new line character
                            //and merging all the rows below (with the same ID as the current one) into the previous row
                            Guid logicalIDToDelete = startingRow.LogicalRowID;
                            Cursor = (Cursor.Row - 1, Cursor.Column); //not calling the event here

                            var previousRow = CurrentPhysicalRow;
                            Guid newlogicalID = previousRow.LogicalRowID;
                            if (newlogicalID != logicalIDToDelete)
                            {
                                ChangeLogicalID(logicalIDToDelete, 0, newlogicalID, previousRow.Ordinal + 1);
                                stillToRemove -= 1;//crossing back one logical line requires one backspace
                            }

                            Cursor = (Cursor.Row, previousRow.Length); //not calling the event here

                            //not calling the event here
                        }
                    }
                }
                else
                {
                    PhysicalRow startingRow = CurrentPhysicalRow;
                    uint startColumn = Cursor.Column;
                    uint stillToRemove = uintCount;
                    while (stillToRemove > 0)
                    {
                        //look ahead
                        uint removableInThisRow = Math.Min(stillToRemove, startingRow.Length - startColumn);
                        if (removableInThisRow > 0)
                        {
                            _changedRows.Add(Cursor.Row);
                            startingRow.Remove(startColumn, removableInThisRow);
                            stillToRemove -= removableInThisRow;
                        }

                        while (stillToRemove > 0)
                        {
                            if (IsCursorInLastRow)
                            {
                                return; //no more rows to check
                            }
                            PhysicalRow nextRow = _rows[(Cursor.Row + 1)];
                            if (nextRow.LogicalRowID != startingRow.LogicalRowID)
                            {
                                Guid newlogicalID = startingRow.LogicalRowID;
                                Guid logicalIDToDelete = nextRow.LogicalRowID;
                                ChangeLogicalID(logicalIDToDelete, 0, newlogicalID, startingRow.Ordinal + 1);
                                //now the next row is considered part of the current logical row
                                stillToRemove--;
                            }

                            if (stillToRemove >= nextRow.Length)
                            {
                                stillToRemove -= nextRow.Length;
                                ChangedAllAfter(Cursor.Row + 1);
                                _rows.RemoveAt(Cursor.Row + 1);
                                continue;
                            }
                            else if (stillToRemove > 0)
                            {
                                //we can delete whatever is left from the next row and we are done after shifting back
                                _changedRows.Add(Cursor.Row + 1);

                                nextRow.TrimBeginning(stillToRemove);
                                BackShift(Cursor.Row + 1, stillToRemove);

                                stillToRemove = 0;
                                BackShift(Cursor.Row, removableInThisRow);
                            }
                        }
                    }
                }

            }
            FireEvents();
        }

        private void ChangedAllAfter(uint j)
        {
            for (uint i = j; i < _rows.Count; i++)
            {
                _changedRows.Add((uint)i);
            }
        }
        private void ChangeLogicalID(Guid from, uint startFromOldOrdinal, Guid To, uint firstNewOrdinal)
        {
            foreach (PhysicalRow row in _rows)
            {
                if (row.LogicalRowID == from && row.Ordinal >= startFromOldOrdinal)
                {
                    row.LogicalRowID = To;
                    row.Ordinal = firstNewOrdinal++;
                }
            }
        }

        private void BackShift(uint originalTargetRow, uint originalCount)
        {
            uint currentTargetRow = originalTargetRow;
            while (currentTargetRow < _rows.Count - 1)
            {
                PhysicalRow targetRow = _rows[(int)currentTargetRow];
                PhysicalRow originRow = _rows[(int)currentTargetRow + 1];
                if (originRow.LogicalRowID != targetRow.LogicalRowID)
                {
                    //we are at the end of a logical row, we cannot shift anymore
                    break;
                }
                string s = originRow.TrimBeginning(Math.Min(originalCount, (uint)originRow.Builder.Length));
                targetRow.Builder.Append(s);
                _changedRows.Add(originalTargetRow);
                _changedRows.Add(originalTargetRow + 1);

                if (originRow.Builder.Length == 0)
                {
                    ChangedAllAfter(currentTargetRow + 1);
                    _rows.RemoveAt((int)currentTargetRow + 1);
                    break;
                }
                currentTargetRow++;
            }
        }

        private void NewLine(ConsoleWriteMode mode)
        {
            lock (_EventsLock)
            {
                if (_rows.Count == 0)
                {
                    _changedRows.Add(0);
                    _rows.Add(new PhysicalRow(Guid.NewGuid(), 0, StringBuilderPool.Rent()));
                    Cursor = (0, 0); //not calling the event here
                    return;
                }

                PhysicalRow previousPhysical = CurrentPhysicalRow;

                uint previousColumn = Cursor.Column;

                bool IsEndOfLogicalRow = (IsCursorInLastRow) || _rows[Cursor.Row + 1].LogicalRowID != previousPhysical.LogicalRowID;

                bool shouldCreateNewEmptyRow = mode == ConsoleWriteMode.Shift && IsEndOfLogicalRow;
                bool shouldMoveContentToNextRow = mode == ConsoleWriteMode.Shift;
                bool shouldAdvanceCursorRow = true;

                if (shouldCreateNewEmptyRow)
                {
                    if (previousColumn == 0
                        && CurrentPhysicalRow.IsEmpty
                        && CurrentPhysicalRow.Ordinal > 0
                    )
                    {
                        //if we are in shift mode, and the cursor is in the first column of an empty row which is a continuation of previous rows,
                        //then the cursor was just pushed over by the latest typed characted but no new line was explicitly requested.
                        //If we get a new-line in this situation there is no need to create a new row, we just associate a new ID to the existing one
                        CurrentPhysicalRow.LogicalRowID = Guid.NewGuid();
                        CurrentPhysicalRow.Ordinal = 0;
                        shouldAdvanceCursorRow = false;
                    }
                    else
                    {
                        var newPhysicalRow = new PhysicalRow(Guid.NewGuid(), 0, StringBuilderPool.Rent());
                        _changedRows.Add(Cursor.Row + 1);
                        _rows.Insert(Cursor.Row + 1, newPhysicalRow);
                    }
                }

                if (shouldAdvanceCursorRow)
                    Cursor = (Cursor.Row + 1, Cursor.Column); //not calling the event here

                Cursor = (Cursor.Row, 0); //not calling the event here

                if (shouldMoveContentToNextRow)
                {
                    if (previousColumn < previousPhysical.Builder.Length)
                    {
                        var RowBelow = _rows[Cursor.Row];
                        //Move the remaining characters to the next row
                        string remainingText = previousPhysical.TrimEnd(previousColumn);
                        _changedRows.Add(Cursor.Row);
                        if (remainingText.Length > 0)
                        {
                            RowBelow.Builder.Append(remainingText);
                            OverflowCurrentRow(mode, 0);
                        }
                    }
                }
            }
        }


        public bool IsCursorInLastRow => Cursor.Row == _rows.Count - 1;
        /// <summary>
        /// Whether the buffer has any free space at the bottom. When true, any new line will not actually move the cursor.
        /// </summary>
        public bool IsBufferFull => _rows.Count >= Height;

        public uint LogicalCursorPosition
        {
            get
            {
                var current = CurrentPhysicalRow;
                return (uint)(current.Ordinal * Width + Cursor.Column);
            }
        }

        public uint LogicalRowStartsAt => Cursor.Row - CurrentPhysicalRow.Ordinal;


        public string GetRowText(int i)
        {
            if (i < 0 || i >= _rows.Count)
                throw new IndexOutOfRangeException();
            return _rows[i].Builder.ToString();
        }

        public void Clear()
        {
            lock (_EventsLock)
            {
                _allRowsChanged = true;
                _changedRows.Clear();
                Cursor = (0, 0);
                _rows.Capacity = 1;
                _rows[0].Builder.Clear();
                _rows.Capacity = _height;
            }
            FireEvents();
        }
        public void PauseEvents()
        {
            _eventsSuspended = true;
        }
        public void Update()
        {
            ResizeIfNeeded();
            ResumeEvents();
        }
        public void ResumeEvents()
        {
            _eventsSuspended = false;
            FireEvents();
        }

        public void MoveCursor(int thisMuch)
        {
            int current = (int)(Cursor.Row * Width + Cursor.Column);
            int max = (_rows.Count) * Width + _rows.GetLast().Builder.Length;

            int destination = Math.Max(Math.Min(max, current + thisMuch), 0);
            SetCursorPosition(destination / Width, destination % Width);
        }

        private class StringEventTextWriter : TextWriter
        {
            private readonly StringBuilder _builder = new StringBuilder();
            // Event raised for each character written
            public event Action<char[], uint, uint>? CharsWritten;
            public event Action<char>? CharWritten;

            public override Encoding Encoding => Encoding.Unicode;

            public override void Write(char value)
            {
                //_builder.Append(value);
                CharWritten?.Invoke(value);
            }
            public override void Write(string? value)
            {
                if (value == null) return;
                CharsWritten?.Invoke(value.ToCharArray(), 0, (uint)value.Length);
            }

            public override void Write(char[] buffer, int index, int count)
            {
                if (buffer == null) return;
                var str = new string(buffer, index, count);
                CharsWritten?.Invoke(buffer, (uint)index, (uint)count);
            }

            // Expose the accumulated string
            public override string ToString() => string.Empty; // _builder.ToString();
        }

        [DebuggerDisplay("Text=\"{Builder}\", LogicalRowID={LogicalRowID}, Ordinal={Ordinal}")]
        private class PhysicalRow
        {
            public Guid LogicalRowID { get; set; }
            public StringBuilder Builder { get; }
            public uint Ordinal { get; set; }
            public bool IsEmpty => Builder.Length == 0;
            public PhysicalRow(Guid logicalRowID, uint ordinal, StringBuilder builder)
            {
                LogicalRowID = logicalRowID;
                Ordinal = ordinal;
                Builder = builder;
            }
            public void Remove(uint startIndex, uint length)
            {
                Builder.Remove((int)startIndex, (int)length);
            }
            public void Insert(uint index, string s)
            {
                Builder.Insert((int)index, s);
            }
            public void Insert(uint index, char[] chars, uint startAt, uint len)
            {
                Builder.Insert((int)index, chars, (int)startAt, (int)len);
            }
            public uint Length => (uint)Builder.Length;
            public string TrimBeginning(uint count)
            {
                if (count < 0 || count > Builder.Length)
                    throw new ArgumentOutOfRangeException(nameof(count), "Count is out of range of the string builder.");

                if (count == 0) return string.Empty;

                string removed = Builder.ToString(0, (int)count);
                Builder.Remove(0, (int)count);

                return removed;
            }

            public string TrimEnd(uint from)
            {
                if (from < 0 || from > Builder.Length)
                    throw new ArgumentOutOfRangeException(nameof(from), "Index is out of range of the string builder.");

                if (from == Builder.Length)
                    return string.Empty;

                string removed = Builder.ToString((int)from, Builder.Length - (int)from);
                Builder.Remove((int)from, Builder.Length - (int)from);
                return removed;
            }
        }

        private struct CursorLogicalLocation
        {
            public Guid LogicalID { get; set; }
            public uint Column { get; set; }

            public CursorLogicalLocation(Guid logicalID, uint column)
            {
                this.LogicalID = logicalID;
                this.Column = column;
            }
        }
        private struct CursorPhysicalLocation
        {
            public uint Row { get; set; }
            public uint Column { get; set; }
            public CursorPhysicalLocation(uint row, uint column)
            {
                Row = row;
                Column = column;
            }
            public CursorPhysicalLocation(int row, int column)
                : this((uint)row, (uint)column)
            {

            }

            public static implicit operator CursorPhysicalLocation((uint row, uint column) tuple)
                => new CursorPhysicalLocation(tuple.row, tuple.column);

        }
    }


}
