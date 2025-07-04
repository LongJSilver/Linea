using Linea.Interface;
using Linea.WPF;
using Linea.WPF.Converters;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Linea.WPF
{
    /// <summary>
    /// Interaction logic for WPFConsole.xaml
    /// </summary>
    public partial class WPFConsole : UserControl, IConsole
    {

        public static readonly DependencyProperty CursorRowProperty =
            DependencyProperty.Register(
                nameof(CursorRow),
                typeof(int),
                typeof(WPFConsole),
                new PropertyMetadata(0));

        public static readonly DependencyProperty CursorVisibleProperty =
            DependencyProperty.Register(
                nameof(CursorVisible),
                typeof(bool),
                typeof(WPFConsole),
                new PropertyMetadata(true, GlobalCursorVisibleChanged));

        private static void GlobalCursorVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var Console = d as WPFConsole;
            if (Console == null) return;
            Console.UpdateCursorStatus();
        }

        public static readonly DependencyProperty CursorColumnProperty =
            DependencyProperty.Register(
                nameof(CursorColumn),
                typeof(int),
                typeof(WPFConsole),
                new PropertyMetadata(0));

        public static readonly DependencyProperty ConsoleHeightProperty =
            DependencyProperty.Register(
                nameof(ConsoleHeight),
                typeof(int),
                typeof(WPFConsole),
                new PropertyMetadata(9000));

        public static readonly DependencyProperty ConsoleTextProperty =
            DependencyProperty.Register(
                nameof(ConsoleText),
                typeof(IList),
                typeof(WPFConsole),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ConsoleFontFamilyProperty =
            DependencyProperty.Register(
                nameof(ConsoleFontFamily),
                typeof(FontFamily),
                typeof(WPFConsole),
                new PropertyMetadata(new FontFamily("Consolas")));

        public static readonly DependencyProperty ConsoleFontSizeProperty =
            DependencyProperty.Register(
                nameof(ConsoleFontSize),
                typeof(double),
                typeof(WPFConsole),
                new PropertyMetadata(14.0));

        public static readonly DependencyProperty ConsoleForegroundProperty =
            DependencyProperty.Register(
                nameof(ConsoleForeground),
                typeof(Brush),
                typeof(WPFConsole),
                new PropertyMetadata(Brushes.White));

        public static readonly DependencyProperty ConsoleBackgroundProperty =
            DependencyProperty.Register(
                nameof(ConsoleBackground),
                typeof(Brush),
                typeof(WPFConsole),
                new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty CursorVisibilityProperty =
            DependencyProperty.Register(
                nameof(CursorVisibility),
                typeof(Visibility),
                typeof(WPFConsole),
                new PropertyMetadata(Visibility.Visible));

        const int ON_TIME = 700;
        const int OFF_TIME = 300;
        private long _latestTextInput;
        public event ConsoleBufferSizeDelegate? BufferSizeChanged;
        public event ConsoleCursorLocationDelegate? CursorLocationChanged;

        private DispatcherTimer _cursorTimer;
        private readonly Stopwatch _sw;


        public int BufferWidth { get; private set; }
        public int BufferHeight { get; private set; }

        public int CursorRow => (int)GetValue(CursorRowProperty);
        public int CursorColumn => (int)GetValue(CursorColumnProperty);

        private readonly BlockingCollection<CliKeyEvent> _events = [];

        public bool Available => _events.Count != 0;
        CliKeyEvent IConsole.Read() => _events.Take();

        public IList? ConsoleText
        {
            get => (IList)GetValue(ConsoleTextProperty);
            set
            {
                if (value == null)
                    SetValue(ConsoleTextProperty, value);
                else
                {
                    if (value is not DispatchedList) value = new DispatchedList(value, Dispatcher);
                    SetValue(ConsoleTextProperty, value);
                }
            }
        }

        private Visibility CursorVisibility
        {
            get => (Visibility)GetValue(CursorVisibilityProperty);
            set => SetValue(CursorVisibilityProperty, value);
        }

        public bool CursorVisible
        {
            get => (bool)GetValue(CursorVisibleProperty);
            set => SetValue(CursorVisibleProperty, value);
        }

        public int ConsoleHeight
        {
            get => (int)GetValue(ConsoleHeightProperty);
            set => SetValue(ConsoleHeightProperty, value);
        }

        public FontFamily ConsoleFontFamily
        {
            get => (FontFamily)GetValue(ConsoleFontFamilyProperty);
            set => SetValue(ConsoleFontFamilyProperty, value);
        }

        public double ConsoleFontSize
        {
            get => (double)GetValue(ConsoleFontSizeProperty);
            set => SetValue(ConsoleFontSizeProperty, value);
        }

        public Brush ConsoleForeground
        {
            get => (Brush)GetValue(ConsoleForegroundProperty);
            set => SetValue(ConsoleForegroundProperty, value);
        }

        public Brush ConsoleBackground
        {
            get => (Brush)GetValue(ConsoleBackgroundProperty);
            set => SetValue(ConsoleBackgroundProperty, value);
        }

        public WPFConsole()
        {
            InitializeComponent();
            MinWidth = 50; MinHeight = 50;
            ConsoleBackground = Brushes.AliceBlue;
            ConsoleForeground = Brushes.SteelBlue;
            ConsoleFontFamily = new FontFamily("Consolas");
            ConsoleFontSize = 14.0;

            ConsoleBox.Loaded += ConsoleBox_Loaded; ;
            ConsoleBox.SizeChanged += ConsoleBox_SizeChanged;

            foreach (var key in new string[] { "FontToWidth", "FontToHeight",
                                               "FontToRow", "FontToColumn" })
            {
                var converter = (AbstractFontConverter)this.Resources[key];
                converter.Visual = this;
            }
            _sw = new();
            _sw.Start();
            _latestTextInput = _sw.ElapsedMilliseconds;
            _cursorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _cursorTimer.Tick += (s, e) => ToggleCursorVisibility();
            UpdateCursorStatus();
            // Attach a handler that listens for GotFocus from any child
            this.AddHandler(UIElement.GotFocusEvent, new RoutedEventHandler(AnyFocusChange), true);

            this.AddHandler(UIElement.LostFocusEvent, new RoutedEventHandler(AnyFocusChange), true);

        }

        private void AnyFocusChange(object sender, RoutedEventArgs e)
        {
            UpdateCursorStatus();
        }

        private void UpdateCursorStatus()
        {
            if (CursorVisible && IsKeyboardFocusWithin)
            {
                _cursorTimer.Start();
                ToggleCursorVisibility();
            }
            else
            {
                _cursorTimer.Stop();
                CursorVisibility = Visibility.Hidden;
            }
        }

        private void ToggleCursorVisibility()
        {
            long now = _sw.ElapsedMilliseconds;
            long mod = (now - _latestTextInput) % (ON_TIME + OFF_TIME);
            if (mod > ON_TIME)
            {
                CursorVisibility = Visibility.Hidden;
            }
            else
            {
                CursorVisibility = Visibility.Visible;
            }
        }

        private void ConsoleBox_Loaded(object sender, RoutedEventArgs e)
        {
            ConsoleBox.Loaded -= ConsoleBox_Loaded;
            UpdateBufferSize();
        }

        private void ConsoleBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBufferSize();
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void UpdateBufferSize()
        {
            var itemsHost = ConsoleBox;
            if (itemsHost != null)
            {
                double usableWidth = itemsHost.ActualWidth;
                double usableHeight = itemsHost.ActualHeight;
                // Queste sono le dimensioni effettive dell'area scrollabile degli item

                var typeface = new Typeface(ConsoleFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                var formattedText = new FormattedText(
                    "W",  //any character to measure
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    ConsoleFontSize,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                SetBufferSize((int)Math.Floor(usableWidth / formattedText.Width),
                                     ConsoleHeight);

            }
        }

        public IList<string> Text
        {
            set { ConsoleText = (IList)value; }
        }

        public void Clear()
        {
            Dispatcher.Invoke(() =>
            {
                var backup = this.ConsoleText;
                this.ConsoleText = null;
                this.ConsoleText = backup;
            });
        }

        public void SetRowText(int Row, string value)
        {
            //Dispatcher.Invoke(() =>
            //{
            //    // Forza il refresh della riga nella ListBox
            //    DependencyObject item = ConsoleBox.ItemContainerGenerator.ContainerFromIndex(Row);
            //    if (item != null)
            //    {
            //        // Trova il ContentPresenter e forza il rinfresco del binding
            //        var contentPresenter = item as ContentPresenter;
            //        if (contentPresenter != null)
            //        {
            //            contentPresenter.Content = null;
            //            contentPresenter.Content = ConsoleText[Row];
            //        }
            //    }
            //});
        }

        void IConsole.SetCursorPosition(int row, int column)
        {
            Dispatcher.Invoke(() =>
            {
                SetValue(CursorRowProperty, row);
                SetValue(CursorColumnProperty, column);


                double Top = Canvas.GetTop(CursorRectangle);
                double Left = Canvas.GetLeft(CursorRectangle);
                if (!double.IsNaN(Top) && !double.IsNaN(Left))
                {
                    Point p = new Point(Left, Top);
                    p = OverlayCanvas.PointToScreen(p);
                    p = ConsoleScrollViewer.PointFromScreen(p);

                    ConsoleScrollViewer.ScrollToVerticalOffset(p.Y);
                }
            });
        }

        void SetBufferSize(int width, int height)
        {
            if (width < 0 || height < 0)
                throw new ArgumentOutOfRangeException("Width and height must be non-negative.");

            int oldH = BufferHeight;
            int oldW = BufferWidth;
            BufferWidth = width;
            BufferHeight = height;
            if (oldH != BufferHeight || oldW != BufferWidth)
                BufferSizeChanged?.Invoke(this, BufferWidth, BufferHeight);
        }

        public void SetRowCount(int RowCount)
        {

        }


        private void UserControl_PreviewKeyDown(object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (!e.Key.ToCliKey(out CliKey converted)) return;
            if (Cli.IsManagedNonCharacter(converted))
            {
                e.Handled = true;

                bool IsShiftDown = e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift);
                bool IsAltDown = e.KeyboardDevice.IsKeyDown(Key.LeftAlt) || e.KeyboardDevice.IsKeyDown(Key.RightAlt);
                bool IsControlDown = e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl);

                _events.Add(new CliKeyEvent('\0', converted,
                    IsShiftDown,
                    IsAltDown,
                    IsControlDown));
                switch (converted)
                {
                    case CliKey.Backspace:
                    case CliKey.Delete:
                        _latestTextInput = _sw.ElapsedMilliseconds;
                        break;
                }
            }
        }


        private void UserControl_TextInput(object sender, TextCompositionEventArgs e)
        {
            _events.Add(new CliKeyEvent(e.Text));
            _latestTextInput = _sw.ElapsedMilliseconds;
        }

        private void ConsoleBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ConsoleBox.Focus();
        }



    }
}
