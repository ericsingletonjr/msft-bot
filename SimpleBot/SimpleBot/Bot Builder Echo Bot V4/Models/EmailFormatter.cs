using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot_Builder_Echo_Bot_V4.Models
{
    /// <summary>
    /// Class to store formatted emails for the ease of
    /// reading the strings.
    /// </summary>
    public class EmailFormatter
    {
        public static string SimpleBotEmail()
        {
            StringBuilder message = new StringBuilder();

            message.Append($"<p>Hiya!</p>");
            message.Append("<br />");
            message.Append("<p>This is a cool email that you sent from the Simple Bot!</p>");
            message.Append("<p>No need to reply to anything, and if you didn't send it</p>");
            message.Append("<p><strong>DEFINITELY</strong> ignore it. Bye bye!</p>");
            message.Append("<p>- A Simple Bot</p>");

            return message.ToString();
        }
    }
}
