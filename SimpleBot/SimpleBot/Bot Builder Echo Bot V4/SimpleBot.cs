using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot_Builder_Echo_Bot_V4.Dialogs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Bot_Builder_Echo_Bot_V4
{
    /// <summary>
    /// This is going to be an implementation of the bot that will be
    /// interacting with the user.
    /// </summary>
    public class SimpleBot : IBot
    {
        private readonly IStatePropertyAccessor<BasicState> _basicStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IEmailSender _emailSender;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;

        public SimpleBot(UserState userState, ConversationState conversationState, IEmailSender emailSender)
        {
            _emailSender = emailSender;

            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _basicStateAccessor = _userState.CreateProperty<BasicState>(nameof(BasicState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            Dialogs = new DialogSet(_dialogStateAccessor);
            Dialogs.Add(new BasicDialogs(_basicStateAccessor, _emailSender));
        }

        public DialogSet Dialogs { get; set; }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var dialogContext = await Dialogs.CreateContextAsync(turnContext);

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.Value;
                var dialogResult = await dialogContext.ContinueDialogAsync();

                if (!dialogContext.Context.Responded)
                {
                    switch (dialogResult.Status)
                    {
                        case DialogTurnStatus.Empty:
                            await dialogContext.BeginDialogAsync(nameof(BasicDialogs));
                            break;

                        case DialogTurnStatus.Waiting:
                            // The active dialog is waiting for a response from the user, so do nothing.
                            break;

                        case DialogTurnStatus.Complete:
                            await dialogContext.EndDialogAsync();
                            break;

                        default:
                            await dialogContext.CancelAllDialogsAsync();
                            break;
                    }
                }
            }
            else
            {
                var greeting = "Hello! I am a simple bot implemented with the Microsoft Bot Framework";
                await turnContext.SendActivityAsync(greeting);
            }

            await _conversationState.SaveChangesAsync(turnContext);
            await _userState.SaveChangesAsync(turnContext);
        }
    }
}
