using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BookingBot.Dialogs.SubDialogs
{
    [Serializable]
    public class AskMonthDialog : IDialog
    {

        private ILuisService _service;
        private string _reservationNo;

        [NonSerialized]
        LuisResult _result = null;

        public AskMonthDialog() { }
        public AskMonthDialog(LuisResult result, ILuisService service)
        {
            _result = result;
            _service = service;
        }

        public async Task StartAsync(IDialogContext context)
        {
            //await this.MessageReceivedAsync(context, null);
            context.Wait(this.MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {

            var message = await item;
            if (message != null)
            {
                //var message = await item;
                var paramValue = message.Text;
                var result = await _service.QueryAsync(paramValue.ToString(), context.CancellationToken);
                var queryIntent = result.Intents.FirstOrDefault();

                if (!"None".Equals(queryIntent.Intent, StringComparison.InvariantCultureIgnoreCase))
                {
                    /*
                     * Intent가 None값이라면.. 질의에 대한 대답으로 판단.
                     * 새로운 Intent라면 시나리오 변경.
                     */
                    PromptDialog.Confirm(context, this.ChangeIntentConfirm, "응? 휴가 안가실건가요?");
                }
                else
                {
                    //날짜 검증
                    /*
                     * 1. 유효한 날짜인가 (자릿수, 숫자, 문자 등)
                     *   - 답변 : 날짜 입력 형식이 맞지 않습니다. ~ (##### 이렇게 입력하시오)
                     */
                    if (!ValidateDateFormat(paramValue))
                    {
                        await context.PostAsync("날짜 입력 형식이 맞지 않습니다. 다시 확인해주시겠어요?~^^ (예 : 2017년 10월 31일 출발인 경우 171031로 입력해주세요)");
                        context.Wait(this.MessageReceivedAsync);
                    }
                    else //유효한 날짜가 입력되었다.
                    {
                        //날짜를 넘겨주면 되겠다. 예약번호가 180402라고 치고
                        context.Done<Object>(null);
                    }
                }
            }
            else
            {
                await context.PostAsync("탑승일 날짜를 입력해라. 이렇게 180402");
                context.Wait(this.MessageReceivedAsync);
            }
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
                await context.PostAsync("탑승일 날짜를 입력해라. 이렇게 180402");
                context.Wait(this.MessageReceivedAsync);
                //await this.MessageReceivedAsync(context, null);
            }
        }

        public bool ValidateDateFormat(string paramValue)
        {
            //모두 숫자인경우, 그리고 6자리이정도만 체크한다치고
            var isAllDigits = paramValue.All(c => Char.IsDigit(c));
            return isAllDigits && paramValue.Count() == 6;
        }
    }
}