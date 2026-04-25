using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WPF_Student_Management.Converters
{
    public class GradeTypeConverter : IValueConverter
    {
        // Hàm này dịch từ Source (DB/ViewModel) -> Target (UI)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string gradeType)
            {
                if (gradeType == "Score") return "Định lượng [1-10]";
                if (gradeType == "PassFail") return "Định tính (Đạt/Không đạt)";
            }

            return value;
        }

        // Hàm này dịch ngược từ UI -> DB (ít dùng cho DataGrid chỉ để xem, nên quăng lỗi NotImplemented)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
