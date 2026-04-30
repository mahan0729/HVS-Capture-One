using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HVSCaptureOne.App.Converters;

/// <summary>
/// Converts a bool to Visibility: true → Visible, false → Collapsed.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>Converts bool to Visibility.</summary>
    /// <returns>Visible when value is true; Collapsed otherwise.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Converts Visibility back to bool.</summary>
    /// <returns>True when Visible.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>
/// Converts a bool to Visibility: true → Collapsed, false → Visible.
/// Used to hide elements when a condition is true (e.g. hide Back button on first step).
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <summary>Converts bool to inverse Visibility.</summary>
    /// <returns>Collapsed when value is true; Visible otherwise.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Converts Visibility back to inverse bool.</summary>
    /// <returns>True when Collapsed.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

/// <summary>
/// Converts a string to Visibility: non-empty → Visible, null/empty → Collapsed.
/// Used to show validation messages only when text is present.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    /// <summary>Converts a string to Visibility.</summary>
    /// <returns>Visible when the string is non-null and non-empty; Collapsed otherwise.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Not supported.</summary>
    /// <returns>Throws NotSupportedException.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Inverts a boolean value. Used to bind complementary RadioButtons to a single bool property.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    /// <summary>Returns the logical negation of the bool value.</summary>
    /// <returns>!value</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    /// <summary>Returns the logical negation of the bool value (symmetric).</summary>
    /// <returns>!value</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}
