using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot_Builder_Echo_Bot_V4.Dialogs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBot"/> class.
        /// We are passing in our states we want to keep track of as well as
        /// our dependency injection for SendGrid.
        /// </summary>
        /// <param name="userState">Checking the state of the user.</param>
        /// <param name="conversationState">Checking the state of the conversation.</param>
        /// <param name="emailSender">our email DI.</param>
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

        // This lets us set up a Dialog between bot and
        // user
        public DialogSet Dialogs { get; set; }

        /// <summary>
        /// This handles all of the turns between the bot and user. Since this
        /// is using Dialogs, we are not setting the Cancellation token to cancel
        /// after every action which allows us to create more meaningful interactions.
        /// </summary>
        /// <param name="turnContext">Information for the current turn.</param>
        /// <param name="cancellationToken">Parameter to setup for the cancellation of turns</param>
        /// <returns>Task that is queued to be executed</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var dialogContext = await Dialogs.CreateContextAsync(turnContext);

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var dialogResult = await dialogContext.ContinueDialogAsync();

                if (!dialogContext.Context.Responded)
                {
                    switch (dialogResult.Status)
                    {
                        case DialogTurnStatus.Empty:
                            var card = CreateAdaptiveCardAttachment();
                            var attach = CreateAttachment(turnContext.Activity, card);

                            // This sends our attachment.
                            await dialogContext.Context.SendActivityAsync(attach).ConfigureAwait(false);
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
                //else
                //{
                //    var card = CreateAdaptiveCardAttachment();
                //    var attach = CreateAttachment(turnContext.Activity, card);

                //    // This sends our attachment.
                //    await dialogContext.Context.SendActivityAsync(attach).ConfigureAwait(false);
                //}
            }
            else
            {
                var greeting = "Hello! I am a simple bot implemented with the Microsoft Bot Framework";
                await turnContext.SendActivityAsync(greeting);
            }

            await _conversationState.SaveChangesAsync(turnContext);
            await _userState.SaveChangesAsync(turnContext);
        }

        /// <summary>
        /// This Helper Method takes in the current activity context
        /// and the Attachment that was created via the CreateAdaptiveCardAttachment
        /// method and connects it to the Activity.
        /// </summary>
        /// <param name="activity">Turn Context.</param>
        /// <param name="attachment">Our Attachment we've created.</param>
        /// <returns>Activity for the turnContext.</returns>
        private Activity CreateAttachment(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment>() { attachment };
            return response;
        }

        /// <summary>
        /// This Helper Method reads our selected JSON file
        /// to create Attachment based off of the JSON format.
        /// </summary>
        /// <returns>Attachment</returns>
        private Attachment CreateAdaptiveCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\Dialogs\Resources\endCard.json");
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
        }
    }
}
