using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_Student_Management.Helpers
{
    public class ScoreDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return "";

            string subjectName = values[0].ToString() ?? "";

            if (double.TryParse(values[1].ToString(), out double score))
            {
                return ScoreHelper.GetDisplayScore(subjectName, score);
            }

            return values[1].ToString() ?? "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Không hỗ trợ chuyển ngược từ Đạt/Không đạt về số");
        }
    }
}