using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public BasicDialogs(IStatePropertyAccessor<BasicState> userProfileStateAccessor)
            : base(nameof(BasicDialogs))
        {
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

            // if we have everything we need, greet user and return.
            // if (basicState != null && !string.IsNullOrWhiteSpace(basicState.Email))
            // {
            //    return await GreetUser(stepContext);
            // }
            if (string.IsNullOrWhiteSpace(basicState.Email))
            {
                // prompt for name, if missing
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
            // Save city, if prompted
            var basicState = await UserProfileAccessor.GetAsync(stepContext.Context);
            return await GreetUserAsync(stepContext);
        }

        private async Task<DialogTurnResult> GreetUserAsync(WaterfallStepContext stepContext)
        {
            var context = stepContext.Context;
            var basicState = await UserProfileAccessor.GetAsync(context);

            // Display their profile information and end dialog.
            await context.SendActivityAsync($"Thanks! I've sent a nifty email!");
            return await stepContext.EndDialogAsync();
        }
    }
}
