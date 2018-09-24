using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot_Builder_Echo_Bot_V4.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Bot_Builder_Echo_Bot_V4.Dialogs
{
    public class BasicDialogs : ComponentDialog
    {
        // Prompt names
        private const string EmailPrompt = "emailPrompt";

        // Dialog IDs
        private const string _simpleId = "simpleId";

        // Dependency Injection
        private readonly IEmailSender _emailSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicDialogs"/> class.
        /// We pass in our accessors and email DI.
        /// </summary>
        /// <param name="userProfileStateAccessor">Used to access our bot accessors.</param>
        /// <param name="emailSender">Used for our email dependency injection.</param>
        public BasicDialogs(IStatePropertyAccessor<BasicState> userProfileStateAccessor, IEmailSender emailSender)
            : base(nameof(BasicDialogs))
        {
            _emailSender = emailSender;
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            var waterfallSteps = new WaterfallStep[]
            {
                InitializeStateStepAsync,
                PromptForEmailAsync,
                DisplayThanksAsync,
            };
            AddDialog(new WaterfallDialog(_simpleId, waterfallSteps));
            AddDialog(new TextPrompt(EmailPrompt, ValidateEmailAsync));
        }

        // This allows us to get access to our model that holds our bot accessors
        public IStatePropertyAccessor<BasicState> UserProfileAccessor { get; }

        /// <summary>
        /// This method initializes the beginning of our dialog steps. First it checks if the state is null
        /// and then proceeds to check if there are options at which point it will use those options or initialize
        /// a new BasicState. If the state is not null, it will proceed to the next step in the waterfall.
        /// </summary>
        /// <param name="stepContext">stepContext that keeps track of where we are in the waterfall.</param>
        /// <param name="cancellationToken">Used to cancel work.</param>
        /// <returns>Task that is queued to be executed.</returns>
        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var basicState = await UserProfileAccessor.GetAsync(stepContext.Context, () => null);
            if (basicState == null)
            {
                var basicStateOpt = stepContext.Options as BasicState;
                if (basicStateOpt != null)
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, basicStateOpt);
                }
                else
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, new BasicState());
                }
            }

            return await stepContext.NextAsync();
        }

        /// <summary>
        /// Method used to ask for the user to give their email. If the BasicState is null,
        /// it will create a prompt to ask the user for their email. If we have an email stored
        /// from the previous conversation, it will remove it.
        /// </summary>
        /// <param name="stepContext">stepContext that keeps track of where we are in the waterfall.</param>
        /// <param name="cancellationToken">Used to cancel work.</param>
        /// <returns>Task that is queued to be executed.</returns>
        private async Task<DialogTurnResult> PromptForEmailAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            var basicState = await UserProfileAccessor.GetAsync(stepContext.Context);

            // if we have an email already, clear the stored information
            // and save the context.
            if (basicState != null && !string.IsNullOrWhiteSpace(basicState.Email))
            {
                basicState.Email = null;
                await UserProfileAccessor.SetAsync(stepContext.Context, basicState);
            }

            if (string.IsNullOrWhiteSpace(basicState.Email))
            {
                // prompt for email if it is not stored
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "Would you please give me your email?",
                    },
                };
                return await stepContext.PromptAsync(EmailPrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        /// <summary>
        /// Method used to do a very poor email validation.
        /// </summary>
        /// <param name="promptContext">This is used to grab our users response from the previous prompt.</param>
        /// <param name="cancellationToken">Used to cancel work.</param>
        /// <returns>Task that is queued to be executed.</returns>
        private async Task<bool> ValidateEmailAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user entered at least a @ character
            // for their email
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (value.Contains("@"))
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Your email needs to be in the format of text@domain.com").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// This method is our setup for ending this dialog. We save our email into
        /// our state and then proceed to our final step.
        /// </summary>
        /// <param name="stepContext">stepContext that keeps track of where we are in the waterfall.</param>
        /// <param name="cancellationToken">Used to cancel work.</param>
        /// <returns>Task that is queued to be executed.</returns>
        private async Task<DialogTurnResult> DisplayThanksAsync(
                                                    WaterfallStepContext stepContext,
                                                    CancellationToken cancellationToken)
        {
            // Save email to state
            var basicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var email = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(basicState.Email) && email != null)
            {
                basicState.Email = email;
                await UserProfileAccessor.SetAsync(stepContext.Context, basicState);
            }

            return await GreetUserAsync(stepContext);
        }

        /// <summary>
        /// Method that wraps up our dialog waterfall. An email is sent to the user-given email
        /// and them proceeds to end the step context.
        /// </summary>
        /// <param name="stepContext">stepContext that keeps track of where we are in the waterfall</param>
        /// <returns>Task that is queued to be executed.</returns>
        private async Task<DialogTurnResult> GreetUserAsync(WaterfallStepContext stepContext)
        {
            var context = stepContext.Context;
            var basicState = await UserProfileAccessor.GetAsync(context);

            // Send our email via SendGrid
            await _emailSender.SendEmailAsync(basicState.Email, "Hello from a simple bot!", EmailFormatter.SimpleBotEmail());

            // Nice informative message informing the user they should expect an email
            await context.SendActivityAsync(
                $"Thanks! I've sent a nifty email at {basicState.Email}! If you don't get it within a few minutes I hit my limit for the day!");

            return await stepContext.EndDialogAsync();
        }
    }
}
