using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_Student_Management.Helpers
{
    public class GenderToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return parameter?.ToString() ?? "";
            }
            return Binding.DoNothing;
        }
    }
}