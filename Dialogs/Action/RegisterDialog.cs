using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace LuisBot.Dialogs
{
    [Serializable]
    public class RegisterDialog : IDialog<object>
    {
        private ILuisService _service { get; set; }
        private string day { get; set; }
        private string month { get; set; }

        [NonSerialized]
        LuisResult _result = null;

        public RegisterDialog() { }
        public RegisterDialog(LuisResult result, ILuisService service)
        {
            _result = result;
            _service = service;
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {

            var message = await item;
            if (HasDayoffDate(context, _result))
            {
                await context.Forward(new RegisterDialog(_result, _service), this.ResumeAfterDialog, new Activity { Text = _result.Query }, CancellationToken.None);
            }


        }

        public bool HasDayoffDate(IDialogContext context, LuisResult result)
        {
            var hasDayoffDate = false;
            if (day == null)
            {
                EntityRecommendation entity;
                if (result.TryFindEntity("Day", out entity))
                {
                    //일단 단일 휴가 등록만 처리
                    //TODO: 여러 휴가 등록처리.
                    day = entity.Entity;
                    hasDayoffDate = true;
                }
            }
            else
            {
                hasDayoffDate = true;
            }
            return hasDayoffDate;
        }

        public bool HasDayoffType(IDialogContext context, LuisResult result)
        {
            
            return false;
        }

        private async Task ResumeAfterDialog(IDialogContext context, IAwaitable<object> result)
        {
            var response = await result;
            if (response == null)
            {
                //Intent가 변경된 경우. 이쪽으로 들어와서 처리하면 된다.
                await context.PostAsync("대화 종료.");
                context.Done<object>(null);
            }
        }
    }
}