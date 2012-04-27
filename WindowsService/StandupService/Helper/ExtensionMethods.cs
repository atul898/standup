using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yahara.Standup.Helper
{

    public static class ExtensionMethods
    {
        public static DateTime StringToDateTime(this string strDate)
        {
            if (strDate.Length != 8)
            {
                //return today's date
                DateTime d = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                return d;
            }
            string mm = strDate.Substring(0, 2);
            string dd = strDate.Substring(2, 2);
            string yyyy = strDate.Substring(4, 4);
            DateTime date = new DateTime(Convert.ToInt32(yyyy), Convert.ToInt32(mm), Convert.ToInt32(dd));
            return date;
        }

        public static string DateTimeToString(this DateTime date)
        {
            return date.ToString("MMddyyyy");
        }

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }

            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime GetEndOfDay(this DateTime date)
        {

            return date.Date.AddSeconds(86399);

        }

    }
}
