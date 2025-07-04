using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Linea.WPF.Converters
{
    internal abstract class AbstractFontConverter
    {
        public Visual? Visual { get; set; } = null;
        private static readonly Dictionary<(string, double), (double Width, double Height)> _formattedTextCache = new();
        protected (double Width, double Height) CreateFormattedText(FontFamily family, double fontSize)
        {
            if (!_formattedTextCache.TryGetValue((family.Source, fontSize), out var result))
            {
                var typeface = new Typeface(family!, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                var ftext = new FormattedText(
                      "W",  //any character to measure
                      System.Globalization.CultureInfo.CurrentCulture,
                      FlowDirection.LeftToRight,
                      typeface,
                      fontSize,
                      Brushes.Black,
                      VisualTreeHelper.GetDpi(Visual).PixelsPerDip
                );

                result = (ftext.Width, ftext.Height);
                _formattedTextCache[(family.Source, fontSize)] = result;
            }

            return result;
        }
    }

    internal class FontToSizeConverter : AbstractFontConverter, IMultiValueConverter
    {
        public enum DimensionType
        {
            Width, Height
        }

        public DimensionType Dimension { get; set; } = FontToSizeConverter.DimensionType.Width;

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return null;
            double? fontSize = null;
            FontFamily? family;
            if (values[0] is FontFamily)
            {
                family = values[0] as FontFamily;
                fontSize = TryConvertToDouble(values[1]);
            }
            else if (values[1] is FontFamily)
            {
                family = values[1] as FontFamily;
                fontSize = TryConvertToDouble(values[0]);
            }
            else
            {
                return null;
            }
            if (family == null || fontSize == null) return null;

            var formattedText = CreateFormattedText(family, fontSize.Value);
            return Dimension == DimensionType.Width ? formattedText.Width :
                formattedText.Height;
        }

        private static double? TryConvertToDouble(object value)
        {
            try
            {
                return System.Convert.ToDouble(value);
            }
            catch (Exception)
            {
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class FontToCursorLocation : AbstractFontConverter, IMultiValueConverter
    {
        public enum CoordinateType
        {
            Row, Column
        }

        public CoordinateType Coordinate { get; set; } = CoordinateType.Row;

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return null;

            FontFamily? family = null;
            double? fontSize = null;
            int? baseSize = null;

            foreach (var val in values)
            {
                if (family == null && val is FontFamily ff)
                {
                    family = ff;
                    continue;
                }


                if (baseSize == null && val is int i)
                {
                    baseSize = i;
                    continue;
                }


                if (fontSize == null)
                {
                    fontSize = TryConvertToDouble(val);
                }
            }

            if (baseSize == null) return null;
            if (family == null || fontSize == null)
                return null;

            var formattedText = CreateFormattedText(family, fontSize.Value);

            return baseSize *
                    (Coordinate == CoordinateType.Row
                        ? formattedText.Height
                        : formattedText.Width);
        }

        private static double? TryConvertToDouble(object value)
        {
            try
            {
                return System.Convert.ToDouble(value);
            }
            catch (Exception)
            {
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
