﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookingBot.Dialogs.SubDialogs;
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

        public async Task StartAsync(IDialogContext context)
        {
            //await this.MessageReceivedAsync(context, null);
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {

            //var message = await item;
            //if (!HasDayoffDate(context, _result))
            //{
                //await context.Forward(new RegisterDialog(result), base.ResumeAfterCallback, new Activity { Text = result.Query }, CancellationToken.None);
            await context.Forward(new AskDayDialog(_result, _service), ResumeAfterDialog, new Activity { Text = _result.Query }, CancellationToken.None);
                //await context.Forward(new AskDayDialog(_result, _service), ResumeAfterDialog, null, CancellationToken.None);
            if (day != null)
            {
                await context.PostAsync(month + " " + day + " 휴가 등록해줄게.");
                context.Done<Object>(null);
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
                    context.ConversationData.SetValue("day", day);
                    hasDayoffDate = true;

                }
                if (result.TryFindEntity("Month", out entity))
                {
                    //일단 단일 휴가 등록만 처리
                    //TODO: 여러 휴가 등록처리.
                    month = entity.Entity;
                    context.ConversationData.SetValue("month", month);
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

                await context.PostAsync("휴가 등록완료!");
                context.Done<object>(null);
            }
        }
    }
}