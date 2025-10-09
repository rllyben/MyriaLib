namespace MyriaLib.Entities.Items
{


#if WPF
    // Optional WPF converter (put in your UI project, or keep here behind a compile symbol WPF)
    using System.Windows.Data;
    using System.Windows.Markup;

    /// <summary>
    /// Bind Money to string in XAML: {Binding Wallet, Converter={StaticResource MoneyConverter}, ConverterParameter=S}
    /// Parameters: S (short), L (long), C (compact), B (raw bronze)
    /// </summary>
    [ValueConversion(typeof(Money), typeof(string))]
    public class MoneyToStringConverter : MarkupExtension, IValueConverter
    {
        public string Format { get; set; } = "S";
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Money m)
            {
                var fmt = (parameter as string) ?? Format;
                return m.ToString(fmt, culture);
            }
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
#endif
}
