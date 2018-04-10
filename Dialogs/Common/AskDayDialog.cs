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
            if (message != null)
            {
                //var message = await item;
                var paramValue = message.Text;
                var result = await _service.QueryAsync(paramValue.ToString(), context.CancellationToken);
                var entity = result.Entities;
                var queryIntent = result.Intents.FirstOrDefault().Score < 0.6 ? "None":result.Intents.FirstOrDefault().Intent;  //None으로 잘 안떨어진다. 스코어가 0.4 이하인것들은 None으로 보겠다.
                if (!"None".Equals(queryIntent, StringComparison.InvariantCultureIgnoreCase))
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
                     * 1. 유효한 날짜인가 검증.
                     *   - 답변 : 날짜 입력 형식이 맞지 않습니다. ~ (##### 이렇게 입력하시오)
                     */
                    string validateMessage;
                    if (!DateService.ValidateDateFormat(paramValue, out validateMessage))
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
            else if(context.ConversationData.TryGetValue("month", out month))
            {
                await context.PostAsync(month+" 언제 휴가 가실건가요?");
                context.Wait(this.MessageReceivedAsync);
            }
            else
            {
                await context.PostAsync("언제 휴가 가실건가요?");
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
                await context.PostAsync("그럼 언제 휴가를 떠날 예정인가요?");
                context.Wait(this.MessageReceivedAsync);
                //await this.MessageReceivedAsync(context, null);
            }
        }
    }
}