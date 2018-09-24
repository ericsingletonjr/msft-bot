using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot_Builder_Echo_Bot_V4.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Bot_Builder_Echo_Bot_V4.Dialogs
{
    public class BasicDialogs : ComponentDialog
    {
        // Prompt names
        private const string EmailPrompt = "emailPrompt";

        // Dialog IDs
        private const string _simpleId = "simpleId";

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

        public IStatePropertyAccessor<BasicState> UserProfileAccessor { get; }

        private IEmailSender _emailSender;

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

        private async Task<bool> ValidateEmailAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user entered at least a @ character
            // for their email
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (value.Length > 5)
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

        private async Task<DialogTurnResult> DisplayThanksAsync(
                                                    WaterfallStepContext stepContext,
                                                    CancellationToken cancellationToken)
        {
            // Save email
            var basicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var email = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(basicState.Email) && email != null)
            {
                basicState.Email = email;
                await UserProfileAccessor.SetAsync(stepContext.Context, basicState);
            }

            return await GreetUserAsync(stepContext);
        }

        private async Task<DialogTurnResult> GreetUserAsync(WaterfallStepContext stepContext)
        {
            var context = stepContext.Context;
            var basicState = await UserProfileAccessor.GetAsync(context);
            await _emailSender.SendEmailAsync(basicState.Email, "Hello from a simple bot!", EmailFormatter.SimpleBotEmail());
            // Display their profile information and end dialog.
            await context.SendActivityAsync(
                $"Thanks! I've sent a nifty email at {basicState.Email}! If you don't get it within a few minutes I hit my limit for the day!");
            return await stepContext.EndDialogAsync();
        }
    }
}
