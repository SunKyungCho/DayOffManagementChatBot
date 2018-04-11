using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BookingBot.Dialogs
{
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        private string intentName;
        public ILuisService service { get; set; }

        protected RootLuisDialog(LuisService service) : base(service) { }
        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            try
            {
                var message = await item;
                var messageText = await GetLuisQueryTextAsync(context, message);

                Trace.TraceInformation(":::::: User Message :::::::>" + messageText);

                if (!string.IsNullOrEmpty(messageText))
                {
                    var tasks = this.services.Select(s => s.QueryAsync(messageText, context.CancellationToken)).ToArray();
                    var results = await Task.WhenAll(tasks);

                    var winners = from result in results.Select((value, index) => new { value, index })
                                  let resultWinner = BestIntentFrom(result.value)
                                  where resultWinner != null && (resultWinner.Score > 0.6 || (results[0].Intents.Count == 1 && resultWinner.Score > 0.4)) //<== 가끔 0.6 이하로 걸리는 경우가 있으므로 점수가 0.4 이상인 결과가 하나만 나오는 경우 맞다고 간주.
                                  select new LuisServiceResult(result.value, resultWinner, this.services[result.index]);

                    var winner = this.BestResultFrom(winners) ?? new LuisServiceResult(results[0], new IntentRecommendation { Intent = "None", Score = 1 }, this.services[0]);
                    service = winner.LuisService;
                    await DispatchToIntentHandler(context, item, winner.BestIntent, winner.Result);
                }
                else
                {
                    var intent = new IntentRecommendation() { Intent = string.Empty, Score = 1.0 };
                    var result = new LuisResult() { TopScoringIntent = intent };
                    await DispatchToIntentHandler(context, item, intent, result);
                }


            }
            catch (Exception ex)
            {
                Trace.TraceInformation("It is possible that the LUIS appid has changed.");
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
            }

        }

        protected virtual async Task ResumeAfterCallback(IDialogContext context, IAwaitable<object> result)
        {
            var message = await result;
            if (message == null)
            {
                await context.PostAsync("대화 종료.");
                context.Done<object>(null);
            }
            else
            {
                IAwaitable<IMessageActivity> re = Awaitable.FromItem<IMessageActivity>((IMessageActivity)message);
                await this.MessageReceived(context, re);
            }
        }
    }
}