using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace LuisBot.Service
{
    [Serializable]
    public class DateService
    {

        public static bool DateBetween(string day)
        {
            day = day.Replace("일", "");
            int dayNum = Int32.Parse(day);
            return 1 < dayNum && dayNum <= 31;
        }

        public static string GetConvertPrintDate(string day)
        {
            int day_num = Int32.Parse(day);
            return day_num + "일";
        }

        public static string GetConvertPrintMonth(string month)
        {
            int day_num = Int32.Parse(month);
            return day_num + "월";
        }

        public string GetConvertDate(string day)
        {
            var replaced = day.Replace("일", "");
            if (replaced.Count() > 1)
            {
                return replaced;
            }
            else
            {
                return "0" + replaced;
            }
        }

        public string GetConvertMonth(string month)
        {
            var replaced = month.Replace("월", "");
            if (replaced.Count() > 1)
            {
                return replaced;
            }
            else
            {
                return "0" + replaced;
            }
        }
    }

}