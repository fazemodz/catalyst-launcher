using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Catalyst_Launcher.UI;

/// <summary>
/// Letter-spacing for TextBlock. WPF has no tracking property, so the text is
/// rebuilt as alternating character / spacer runs; the spacer is a plain space
/// scaled down until its advance matches the requested tracking. Used only on
/// the short uppercase captions that label a title-block cell.
/// </summary>
public static class Tracking
{
    /// <summary>Extra space between characters, in device-independent pixels.</summary>
    public static readonly DependencyProperty AmountProperty =
        DependencyProperty.RegisterAttached(
            "Amount", typeof(double), typeof(Tracking),
            new PropertyMetadata(0d, OnAmountChanged));

    public static void SetAmount(DependencyObject o, double v) => o.SetValue(AmountProperty, v);
    public static double GetAmount(DependencyObject o) => (double)o.GetValue(AmountProperty);

    // A space glyph advances roughly a quarter of its em box in the faces used here.
    private const double SpaceAdvanceRatio = 0.26;

    private static void OnAmountChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not TextBlock tb) return;

        // A style setter lands before the element's own Text does, so wait for
        // the tree to be built before rewriting anything.
        if (tb.IsLoaded)
        {
            Apply(tb);
            return;
        }

        void OnLoaded(object sender, RoutedEventArgs args)
        {
            tb.Loaded -= OnLoaded;
            Apply(tb);
        }
        tb.Loaded += OnLoaded;
    }

    private static void Apply(TextBlock tb)
    {
        double amount = GetAmount(tb);
        string source = tb.Text;

        if (amount <= 0 || string.IsNullOrEmpty(source)) return;

        double spacerSize = Math.Max(0.5, amount / SpaceAdvanceRatio);

        tb.Inlines.Clear();
        for (int i = 0; i < source.Length; i++)
        {
            tb.Inlines.Add(new Run(source[i].ToString()));
            if (i < source.Length - 1)
                tb.Inlines.Add(new Run(" ") { FontSize = spacerSize });
        }
    }
}
