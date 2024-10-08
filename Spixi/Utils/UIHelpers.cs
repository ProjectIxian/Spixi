﻿using IXICore;
using SPIXI.Meta;
using System.Linq;

namespace SPIXI
{
    public static class UIHelpers
    {
        public static void setContactStatus(Address address, bool online, int unread, string excerpt, long timestamp)
        {
            Page page = Application.Current.MainPage.Navigation.NavigationStack.Last();
            if (page != null && page is HomePage)
            {
                ((HomePage)page).setContactStatus(address, online, unread, excerpt, timestamp);
            }else
            {
                Node.shouldRefreshContacts = true;
            }
        }

        // Reload the webview contents on all pages in the navigation stack
        // On iOS it will also pop the current page in the navigation stack
        public static void reloadAllPages()
        {
            var stack = Application.Current.MainPage.Navigation.NavigationStack;
            foreach (Page p in stack)
            {
                ((SpixiContentPage)p).reload();
            }
        }
    }
}
