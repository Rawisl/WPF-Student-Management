using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Student_Management.Helpers
{
    public class DateFormatter
    {
        public static string GetUIDate(string dbDate)

        {

            if (string.IsNullOrEmpty(dbDate)) return "";

            DateTime result;

            if (DateTime.TryParseExact(dbDate, AppConstants.DbDateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))

            {

                return result.ToString(AppConstants.UIDateFormat);

            }

            return "";

        }

        public static string GetDbDate(string uiDate)

        {

            if (string.IsNullOrEmpty(uiDate)) return "";

            DateTime result;

            if (DateTime.TryParseExact(uiDate, AppConstants.UIDateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))

            {

                return result.ToString(AppConstants.DbDateFormat);

            }

            return "";

        }
    }
}
