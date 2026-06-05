using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_Student_Management.Helpers
{
    //helper này để đồng bộ giữa currentview (cái cửa sổ đang hiện trên màn hình) và giao diện sidebar
    public class ActiveViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value chính là biến CurrentView
            // parameter là tên của ViewModel dạng Text
            if (value == null || parameter == null)
                return false;

            // Nếu tên của View hiện tại trùng với tên Parameter gắn trên nút -> Trả về True
            return value.GetType().Name == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}