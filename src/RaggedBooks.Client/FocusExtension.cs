using System.Windows;
using System.Windows.Input;

namespace RaggedBooks.Client;

public static class FocusExtension
{
    public static bool GetIsFocused(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFocusedProperty, value);
    }

    // Register attached property
    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused",
            typeof(bool),
            typeof(FocusExtension),
            new UIPropertyMetadata(false, OnIsFocusedChanged)
        );

    private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement uie && (bool)e.NewValue)
        {
            // When IsFocused becomes true, set actual focus.
            uie.Focus();
            Keyboard.Focus(uie);
        }
    }
}
