using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaggedBooks.Client;

public class StringToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            return Invert ? Visibility.Collapsed : Visibility.Visible;
        }

        return Invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException(
            "StringToVisibilityConverter only supports one-way conversion."
        );
    }
}
