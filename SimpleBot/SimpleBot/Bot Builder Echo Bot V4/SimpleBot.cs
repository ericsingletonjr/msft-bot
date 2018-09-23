using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Schema;

namespace Bot_Builder_Echo_Bot_V4
{
    /// <summary>
    /// This is going to be an implementation of the bot that will be
    /// interacting with the user.
    /// </summary>
    public class SimpleBot : IBot
    {
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var response = "I see you want to talk!";
                await turnContext.SendActivityAsync(response);
            }
            else
            {
                var greeting = "Hello! I am a simple bot implemented with the Microsoft Bot Framework";
                await turnContext.SendActivityAsync(greeting);
            }
        }
    }
}
