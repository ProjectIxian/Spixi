using System.Linq;
using Xamarin.Forms;

namespace SPIXI
{
    public static class UIHelpers
    {
        public static void setContactStatus(byte[] address, bool online, int unread, string excerpt, long timestamp)
        {
            var stack = App.Current.MainPage.Navigation.NavigationStack;
            foreach (Page p in stack)
            {
                if (p.GetType() == typeof(HomePage))
                {
                    ((HomePage)p).setContactStatus(address, online, unread, excerpt, timestamp);
                }
            }
        }

        // Reload the webview contents on all pages in the navigation stack
        // On iOS it will also pop the current page in the navigation stack
        public static void reloadAllPages()
        {
            var stack = App.Current.MainPage.Navigation.NavigationStack;
            foreach (Page p in stack)
            {
                ((SpixiContentPage)p).reload();
            }
        }
    }
}
