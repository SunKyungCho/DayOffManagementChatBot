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

        public static bool ValidateDateFormat(string day, out string errorMessage)
        {
            errorMessage = "";
            // 날짜는 1-31사이 검증



            // 오늘 날짜보다 적은 날짜이면 다음달로 본다. 
            new DateTime();
            DateService service = new DateService();
            var today = DateTime.Today;
            var thisMonth = today.ToString("yyyyMMdd").Substring(0, 6);
            var input = service.GetConvertDate(day); //00~31 << 형태로 변환.

            var dayoff_str = thisMonth + input;

            DateTime dayoff;
            if (!DateTime.TryParseExact(dayoff_str, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dayoff))
            {
                //잘못된 날짜 입력시.ex) 4월 31일.
                errorMessage = "뭔가 잘못된 입력된것 같은데? 날짜 다시 한번 확인해주세요.";
                return false;
            }

            var result = DateTime.Compare(today, dayoff);
            if (result > 0)
            {
                //다음달로 리턴.
                var nextMonth = DateTime.Today.AddMonths(1).ToString("yyyyMM") + input;
                // 다음달이 유효하지않다면 ex)2월 31일 같은 것
                if (!DateTime.TryParseExact(nextMonth, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dayoff))
                {
                    var month = dayoff.ToString("MM");
                    var inputDay = dayoff.ToString("dd");
                    errorMessage = "다음달 " + day + "가 맞나요? 그런 날짜는 존재하지 않아요. 다시 확인해보세요.";
                    return false;
                }
            }

            // TODO: 요일 검증. (토요일, 일요일은 휴가를 사용할 필요가 없다.)
            if ((int)dayoff.DayOfWeek == 0 || (int)dayoff.DayOfWeek == 6) //0~6 일요일 - 토요일
            {
                errorMessage = day + "은 주말입니다. 그냥 쉬시고 다른날짜를 입력해 주세요~";
                return false;
            }

            // TODO: 공휴일 검증. (설날, 추석, 어린이날 등), DB로 공휴일 관리를 해야할듯 싶습니다. 이건 일단 PASS  
            return true;
        }
    }

}