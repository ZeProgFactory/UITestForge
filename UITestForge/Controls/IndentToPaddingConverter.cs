using System.Globalization;

namespace UITestForge.Controls;

public class IndentToPaddingConverter : IValueConverter
{
   public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
      => new Thickness(value is double left ? left : 0, 4, 6, 4);

   public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotSupportedException();
}
