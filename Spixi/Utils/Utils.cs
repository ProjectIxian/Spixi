using IXICore;
using IXICore.Meta;
using System.Text;

namespace SPIXI
{
    public class Utils
    {
        public static DateTime unixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string unixTimeStampToString(double unixTimeStamp)
        {
            DateTime datetime = unixTimeStampToDateTime(unixTimeStamp);
            return datetime.ToString("MM/dd/yyyy HH:mm:ss");
        }

        public static string unixTimeStampToHumanFormatString(double unixTimeStamp)
        {
            DateTime datetime = unixTimeStampToDateTime(unixTimeStamp);
            return datetime.ToString("dd MMM, yyyy, h:mm tt");
        }

        public static string escapeHtmlParameter(string str)
        {
            return str.Replace("\"", "&#34;").Replace("'", "&#39;").Replace("\\", "&#92;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r\n", "\\n").Replace("\n", "\\n");
        }

        public static string amountToHumanFormatString(IxiNumber amount)
        {
            string amount_string = amount.ToString();
            if (amount > 1)
                return amount_string[..^6];
            return amount_string;
        }

        public static void sendUiCommand(SpixiContentPage contentPage, string command, params string[] arguments)
        {
            try
            {
                string cmd_str = command + "(";
                StringBuilder sb = new StringBuilder(cmd_str);
                bool first = true;

                foreach (string arg in arguments)
                {
                    if (!first)
                    {
                        sb.Append(",");
                    }
                    sb.Append("'");
                    sb.Append(escapeHtmlParameter(arg));
                    sb.Append("'");
                    first = false;
                }

                sb.Append(");");
                cmd_str = sb.ToString();

                contentPage.sendMessage(cmd_str);

            }
            catch(Exception e)
            {
                Logging.error("Exception occured in sendUiCommand " + e);
            }
        }
    }
}
