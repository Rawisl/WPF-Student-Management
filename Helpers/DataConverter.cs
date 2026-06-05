using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace WPF_Student_Management.Helpers
{
    // Converter dịch SubjectID (Chuyên môn) sang SubjectName
    public class SubjectIdToNameConverter : IValueConverter
    {
        private static Dictionary<string, string> _cache = new Dictionary<string, string>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return "Trống";
            string id = value.ToString();

            if (_cache.ContainsKey(id))
                return _cache[id];

            try
            {
                var dt = DatabaseHelper.ExecuteQuery($"SELECT SubjectName FROM Subject WHERE SubjectID = '{id}'");
                if (dt.Rows.Count > 0)
                {
                    string name = dt.Rows[0][0].ToString();
                    _cache[id] = name;
                    return name;
                }
            }
            catch { }

            return "Không rõ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // Converter dịch RoleID sang RoleName
    public class RoleIdToNameConverter : IValueConverter
    {
        private static Dictionary<string, string> _cache = new Dictionary<string, string>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return "Không rõ";
            string id = value.ToString();

            if (_cache.ContainsKey(id))
                return _cache[id];

            try
            {
                var dt = DatabaseHelper.ExecuteQuery($"SELECT RoleName FROM Role WHERE RoleID = '{id}'");
                if (dt.Rows.Count > 0)
                {
                    string name = dt.Rows[0][0].ToString();
                    _cache[id] = name;
                    return name;
                }
            }
            catch { }

            return "Không rõ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}