using LuisBot.Service;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BookingBot.Dialogs.SubDialogs
{
    [Serializable]
    public class AskDayDialog : LuisDialog<object>
    {

        private ILuisService _service;
        private string intent = "Register";
        private string day { get; set; }
        private string month { get; set; }

        [NonSerialized]
        LuisResult _result = null;

        public AskDayDialog(LuisResult result, ILuisService service)
        {
            if (result == null || service == null)
            {
                throw new ArgumentException("Action chain cannot be null or empty.");
            }
            _result = result;
            _service = service;

        }

        public async override Task StartAsync(IDialogContext context)
        {
            //await this.MessageReceivedAsync(context, null);
            context.Wait(this.MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            string month;
            var message = await item;
            var paramValue = message.Text;
            var result = await _service.QueryAsync(paramValue.ToString(), context.CancellationToken);
            var entity = result.Entities;
            string sat;

            var queryIntent = result.Intents.FirstOrDefault().Score < 0.6 ? "None" : result.Intents.FirstOrDefault().Intent;  //None으로 잘 안떨어진다. 스코어가 0.4 이하인것들은 None으로 보겠다.
            if (!"None".Equals(queryIntent, StringComparison.InvariantCultureIgnoreCase) &&
                !intent.Equals(queryIntent, StringComparison.InvariantCultureIgnoreCase))
            {
                /*
                 * Intent가 None값이라면.. 질의에 대한 대답으로 판단.
                 * 새로운 Intent라면 시나리오 변경.
                 */
                PromptDialog.Confirm(context, this.ChangeIntentConfirm, "응? 휴가 안가실건가요?");
            }
            else
            {
                if ( !AssignEntities(result, out sat) )
                {
                    await context.PostAsync(sat);
                    context.Wait(this.MessageReceivedAsync);
                }
                else
                {
                    //날짜 검증
                    /*
                     * 1. 유효한 날짜인가 검증.
                     *   - 답변 : 날짜 입력 형식이 맞지 않습니다. ~ (##### 이렇게 입력하시오)
                     */
                    string validateMessage;
                    if (!ValidateDateFormat(day, out validateMessage))
                    {
                        await context.PostAsync(validateMessage);
                        context.Wait(this.MessageReceivedAsync);
                    }
                    else //유효한 날짜가 입력되었다
                    {
                        //날짜를 넘겨주자.
                        context.Done<Object>(null);
                    }
                }
            }
        }

        public bool AssignEntities(LuisResult result, out string errorMessage)
        {
            var entityResult = false;
            errorMessage = " 언제 휴가를 사용하실건가요?";
            EntityRecommendation entity;
            if (result.TryFindEntity("Day", out entity))
            {
                //일단 단일 휴가 등록만 처리
                //TODO: 여러 휴가 등록처리.
                day = entity.Entity;
                entityResult = true;
            }
            if (result.TryFindEntity("Month", out entity))
            {
                //일단 단일 휴가 등록만 처리
                //TODO: 여러 휴가 등록처리.
                month = entity.Entity;
                errorMessage = month + errorMessage;
            }
            return entityResult;
            //foreach (var group in result.Entities.GroupBy(e => e.Entity))
            //{
            //    if (group.Count() > 1)
            //    {
            //        var entityToUpdate = group.FirstOrDefault();
            //        var entityWithValue = group.FirstOrDefault();
            //    }
            //}
        }


        private async Task ChangeIntentConfirm(IDialogContext context, IAwaitable<bool> result)
        {
            if (await result == true)
            {
                /*
                 * TODO: intent변경 하려는 의도가 맞을때, 사용자가 입력한 query를 가지고 이동.
                 * 필요하면 여기서 얻은 정보도 같이 넘긴다.
                 */
                context.Done<Object>(null);
            }
            else
            {
                await context.PostAsync("그럼 언제 휴가를 떠날 예정인가요?");
                context.Wait(this.MessageReceivedAsync);
                //await this.MessageReceivedAsync(context, null);
            }
        }
        public bool ValidateDateFormat(string day, out string errorMessage)
        {
            errorMessage = "";
            // 날짜는 1-31사이 검증
            if (!DateService.DateBetween(day))
            {
                errorMessage = "비정상적인 날짜입니다. 다시 한번 확인해주세요.";
                return false;
            }

            // 오늘 날짜보다 적은 날짜이면 다음달로 본다. 
            new DateTime();
            DateService service = new DateService();
            var today = DateTime.Today;
            var thisYear = today.ToString("yyyy");
            var thisMonth = today.ToString("MM");
            var input = service.GetConvertDate(day); //00~31 << 형태로 변환.

            var dayoff_str = thisYear + thisMonth + input;

            DateTime dayoff;
            if (!DateTime.TryParseExact(dayoff_str, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dayoff))
            {
                //잘못된 날짜 입력시.ex) 4월 31일.
                var printDay = DateService.GetConvertPrintDate(input);
                var printMonth = DateService.GetConvertPrintMonth(thisMonth);
                errorMessage = printMonth + " "+ printDay + "은 뭔가 잘못된 입력된것 같은데요? 날짜 다시 한번 확인해주세요.";
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
                    var month = DateService.GetConvertPrintMonth(dayoff.ToString("MM"));
                    var inputDay = dayoff.ToString("dd");
                    errorMessage = month+" " + day + "이 맞나요? 그런 날짜는 존재하지 않아요. 다시 확인해보세요.";
                    return false;
                }
            }

            // TODO: 요일 검증. (토요일, 일요일은 휴가를 사용할 필요가 없다.)
            if ((int)dayoff.DayOfWeek == 0 || (int)dayoff.DayOfWeek == 6) //0~6 일요일 - 토요일
            {
                var month = DateService.GetConvertPrintMonth(dayoff.ToString("MM"));
                errorMessage = month+" " +day + "은 주말입니다. 그냥 쉬시고 다른날짜를 입력해 주세요~";
                return false;
            }
            // TODO: 공휴일 검증. (설날, 추석, 어린이날 등), DB로 공휴일 관리를 해야할듯 싶습니다. 이건 일단 PASS  

            return true;
        }
    }
}