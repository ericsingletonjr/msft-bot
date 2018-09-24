using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Bot_Builder_Echo_Bot_V4
{
    public class SimpleBotAccessors
    {
        private readonly SimpleBotAccessors _accessors;

        public SimpleBotAccessors(SimpleBotAccessors accessors)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }

        public SimpleBotAccessors(ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public ConversationState ConversationState { get; }

    }
}
