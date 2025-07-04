using Linea.Interface;

namespace Linea.Test
{
    /// <summary>
    /// Summary description for CliTests
    /// </summary>
    public class ConsoleMechanicTests
    {


        [Fact]
        public void Clear()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.Write("012345678");



            cm.SetCursorPosition(1, 5);

            cm.Clear();


            Assert.Equal(0, cm.CursorColumn);
            Assert.Equal(0, cm.CursorRow);
            Assert.Equal(1, cm.RowCount);

            Assert.Equal("", cm.GetRowText(0));
        }


        [Fact]
        public void AutoWrap()
        {
            ConsoleMechanics cm = new ConsoleMechanics(100, 10);
            string s = new String('a', 150);
            cm.Write(s);
            Assert.Equal(2, cm.RowCount);
            Assert.Equal(50, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);
            Assert.Equal(s, cm.CurrentRowText);
        }

        [Fact]
        public void DiscardRows()
        {
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("1");
            cm.WriteLine("2");
            cm.WriteLine("3");
            cm.WriteLine("4");
            cm.WriteLine("5");
            cm.Write("6");

            Assert.Equal(5, cm.RowCount);
            Assert.Equal("2", cm.GetRowText(0));
        }


        [Fact]
        public void OverwriteMode_Basic()
        {
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");

            cm.SetCursorPosition(1, 1);
            cm.Write("abc", ConsoleWriteMode.Overwrite);
            Assert.Equal("0abc456789", cm.GetRowText(1));
        }


        [Fact]
        public void OverwriteMode_AcrossPhysicalRows()
        {
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");

            cm.SetCursorPosition(1, 5);
            cm.Write("abcdefghi", ConsoleWriteMode.Overwrite);
            Assert.Equal("01234abcde", cm.GetRowText(0));
            Assert.Equal("fghi", cm.GetRowText(1));
        }


        [Fact]
        public void OverwriteMode_NewLine()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");

            cm.SetCursorPosition(1, 5);
            cm.WriteLine(ConsoleWriteMode.Overwrite);
            cm.Write("ABC", ConsoleWriteMode.Overwrite);
            Assert.Equal("0123456789", cm.GetRowText(1));
            Assert.Equal("ABC3456789", cm.GetRowText(2));
        }



        [Fact]
        public void ShiftMode_Basic()
        {
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            //console buffer height is full of rows

            cm.SetCursorPosition(1, 4);
            cm.Write("abc", ConsoleWriteMode.Shift);

            //cursor moved 3 places to the right
            Assert.Equal(7, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);
            //cursor row shall have remained the same

            Assert.Equal("0123abc456", cm.GetRowText(1));
        }


        [Fact]
        public void ShiftMode_AcrossPhysicalRows()
        {
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            //console buffer height is full of rows
            cm.SetCursorPosition(1, 1);
            cm.Write("abc", ConsoleWriteMode.Shift);
            //this new content will create a new row, that will cause the first to disappear

            //cursor moved 3 places to the right
            Assert.Equal(4, cm.CursorColumn);
            //cursor must have moved to first row, ie drifted up with the console content
            Assert.Equal(0, cm.CursorRow);

            Assert.Equal("0abc123456", cm.GetRowText(0));
            Assert.Equal("789", cm.GetRowText(1));
            Assert.Equal("", cm.GetRowText(4));
        }


        [Fact]
        public void ShiftMode_NewLine()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");



            cm.SetCursorPosition(1, 5);
            cm.WriteLine(ConsoleWriteMode.Shift);
            cm.Write("ABC", ConsoleWriteMode.Shift);


            //we started writing in a new line,
            //so the cursor should have moved to column 3
            Assert.Equal(3, cm.CursorColumn);
            //cursor must have moved from second to third row,
            //but then drifted up with the console content when the buffer overflowed
            //so we should find it in the second row
            Assert.Equal(1, cm.CursorRow);

            Assert.Equal("01234", cm.GetRowText(0));
            Assert.Equal("ABC56789", cm.GetRowText(1));
        }


        [Fact]
        public void Delete_ToTheLeft_Basic()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");



            cm.SetCursorPosition(1, 5);
            cm.Delete(4, ConsoleMechanics.CharacterDeleteDirection.ToTheLeft);


            Assert.Equal(1, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);

            Assert.Equal("056789", cm.GetRowText(1));
        }

        [Fact]
        public void Delete_ToTheLeft_AcrossRows()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 10);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.Write/**/("ABCDEFGHIJ");
            cm.WriteLine("0123456789");
            cm.WriteLine("**********");



            cm.SetCursorPosition(2, 3);
            cm.Delete(5, ConsoleMechanics.CharacterDeleteDirection.ToTheLeft);


            Assert.Equal(9, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);

            Assert.Equal("012345678D", cm.GetRowText(1));
            Assert.Equal("EFGHIJ0123", cm.GetRowText(2));
            Assert.Equal("456789", cm.GetRowText(3));
            Assert.Equal("**********", cm.GetRowText(4));
        }


        [Fact]
        public void Delete_ToTheLeft_LeavingRowsEmpty()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 10);

            cm.WriteLine("0123456789");
            cm.Write/**/("0123456789");
            cm.Write/**/("ABCDEFGHIJ");
            cm.WriteLine("0123456789");
            cm.WriteLine("**********");



            cm.SetCursorPosition(3, 9);
            cm.Delete(26, ConsoleMechanics.CharacterDeleteDirection.ToTheLeft);


            Assert.Equal(4, cm.RowCount);
            Assert.Equal(3, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);

            Assert.Equal("0123456789", cm.GetRowText(0));
            Assert.Equal("0129", cm.GetRowText(1));
            Assert.Equal("**********", cm.GetRowText(2));
        }
        [Fact]
        public void Delete_ToTheRight_Basic()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 5);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");



            cm.SetCursorPosition(1, 5);
            cm.Delete(4, ConsoleMechanics.CharacterDeleteDirection.ToTheRight);


            Assert.Equal(5, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);

            Assert.Equal("012349", cm.GetRowText(1));
        }

        [Fact]
        public void Delete_ToTheRight_AcrossRows()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 10);

            cm.WriteLine("0123456789");
            cm.WriteLine("0123456789");
            cm.Write/**/("ABCDEFGHIJ");
            cm.WriteLine("**********");



            cm.SetCursorPosition(1, 7);
            cm.Delete(5, ConsoleMechanics.CharacterDeleteDirection.ToTheRight);


            Assert.Equal(7, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);

            Assert.Equal("0123456BCD", cm.GetRowText(1));
            Assert.Equal("EFGHIJ****", cm.GetRowText(2));
            Assert.Equal("******", cm.GetRowText(3));
        }


        [Fact]
        public void Delete_ToTheRight_LeavingRowsEmpty()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(10, 10);

            cm.WriteLine("0123456789");
            cm.Write/**/("ABCDEFGHIJ");
            cm.WriteLine("0123456789");
            cm.Write/**/("0123456789");
            cm.Write/**/("*********");

            cm.SetCursorPosition(1, 7);
            cm.Delete(31, ConsoleMechanics.CharacterDeleteDirection.ToTheRight);


            Assert.Equal(2, cm.RowCount);
            Assert.Equal(7, cm.CursorColumn);
            Assert.Equal(1, cm.CursorRow);

            Assert.Equal("0123456789", cm.GetRowText(0));
            Assert.Equal("ABCDEFG**", cm.GetRowText(1));
        }

        [Fact]
        public void Events_BasicChanges_OnlyOnce()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(20, 10);

            cm.WriteLine("********************");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("********************");

            int? firstChangedRow = null;
            int? lastChangedRow = null;

            void Handler(object sender, ConsoleContentChangeArgs e)
            {
                if (firstChangedRow != null)
                {
                    throw new InvalidOperationException("Event Called more than once");
                }

                firstChangedRow = e.FirstRow;
                lastChangedRow = e.LastRow;
            }


            cm.ContentChanged += Handler;

            cm.SetCursorPosition(2, 10);
            cm.Write("###", ConsoleWriteMode.Overwrite);

            Assert.NotNull(firstChangedRow);
            Assert.NotNull(lastChangedRow);
            Assert.Equal(2, firstChangedRow.Value);
            Assert.Equal(2, lastChangedRow.Value);
            firstChangedRow = null;
            lastChangedRow = null;
        }


        [Fact]
        public void Events_AcrossRows()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(20, 10);

            cm.WriteLine("********************");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("********************");

            int? firstChangedRow = null;
            int? lastChangedRow = null;

            void Handler(object sender, ConsoleContentChangeArgs e)
            {
                if (firstChangedRow != null)
                {
                    throw new InvalidOperationException("Event Called more than once");
                }

                firstChangedRow = e.FirstRow;
                lastChangedRow = e.LastRow;
            }


            cm.ContentChanged += Handler;

            cm.SetCursorPosition(2, 19);
            cm.Write("###", ConsoleWriteMode.Overwrite);

            Assert.NotNull(firstChangedRow);
            Assert.NotNull(lastChangedRow);
            Assert.Equal(2, firstChangedRow.Value);
            Assert.Equal(3, lastChangedRow.Value);
            firstChangedRow = null;
            lastChangedRow = null;
        }


        [Fact]
        public void Events_SuspendRestart()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(20, 10);

            cm.WriteLine("********************");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("********************");

            int? firstChangedRow = null;
            int? lastChangedRow = null;

            void Handler(object sender, ConsoleContentChangeArgs e)
            {
                if (firstChangedRow != null)
                {
                    throw new InvalidOperationException("Event Called more than once");
                }

                firstChangedRow = e.FirstRow;
                lastChangedRow = e.LastRow;
            }


            cm.ContentChanged += Handler;

            cm.PauseEvents();
            cm.SetCursorPosition(5, 9);
            cm.Write("###", ConsoleWriteMode.Overwrite);
            cm.SetCursorPosition(7, 9);
            cm.Write("###", ConsoleWriteMode.Overwrite);
            cm.SetCursorPosition(6, 9);
            cm.Write("###", ConsoleWriteMode.Overwrite);
            cm.ResumeEvents();

            Assert.NotNull(firstChangedRow);
            Assert.NotNull(lastChangedRow);
            Assert.Equal(5, firstChangedRow.Value);
            Assert.Equal(7, lastChangedRow.Value);
            firstChangedRow = null;
            lastChangedRow = null;
        }

        [Fact]
        public void Events_AllChanged()
        {
            //A new Line should move the cursor to the row below,
            //removing all remaining characters in the current row
            ConsoleMechanics cm = new ConsoleMechanics(20, 6);

            cm.WriteLine("********************");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.WriteLine("0123456789ABCDEFGHIJ");
            cm.WriteLine("9876543210abcdefghij");
            cm.Write("******************");

            int? firstChangedRow = null;
            int? lastChangedRow = null;

            void Handler(object sender, ConsoleContentChangeArgs e)
            {
                if (firstChangedRow != null)
                {
                    throw new InvalidOperationException("Event Called more than once");
                }

                firstChangedRow = e.FirstRow;
                lastChangedRow = e.LastRow;
            }


            cm.ContentChanged += Handler;

            cm.SetCursorPosition(2, 19);
            cm.Write("###", ConsoleWriteMode.Overwrite);

            Assert.NotNull(firstChangedRow);
            Assert.NotNull(lastChangedRow);
            Assert.Equal(0, firstChangedRow.Value);
            Assert.Equal(cm.Height - 1, lastChangedRow.Value);
            firstChangedRow = null;
            lastChangedRow = null;
        }


    }
}
